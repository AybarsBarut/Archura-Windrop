using Windrop.Domain;

namespace Windrop.Infrastructure.Documents;

public sealed class DocumentPipeline(
    BridgeSettings settings,
    IHistoryRepository history,
    IClipboardService clipboard,
    IUserNotificationService notifications)
{
    public event EventHandler<ReceivedItem>? ItemReceived;

    public async Task<ReceivedItem> ProcessAsync(IncomingDocument document, CancellationToken cancellationToken)
    {
        if (settings.RequireApproval && !await notifications.ApproveAsync(
                document.SourceDevice ?? "A network device", TimeSpan.FromSeconds(10), cancellationToken))
            throw new OperationCanceledException("Incoming document was declined.", cancellationToken);

        Directory.CreateDirectory(settings.SaveFolder);
        var tempPath = Path.Combine(settings.SaveFolder, $".{Guid.NewGuid():N}.receiving");
        ReceivedItem? item = null;
        try
        {
            var (_, header) = await CopyLimitedAsync(document.Content, tempPath, settings.MaxDocumentBytes, cancellationToken);
            var kind = DocumentFormat.Detect(header, document.DeclaredFormat);
            var source = Sanitize(document.SourceDevice ?? "Apple-device");
            var baseName = $"{source}_{DateTime.Now:yyyyMMdd_HHmmss_fff}";
            var finalPath = UniquePath(settings.SaveFolder, baseName, kind.Extension());
            File.Move(tempPath, finalPath);

            string? text = null;
            string[]? convertedPaths = null;
            var primaryPath = finalPath;
            var primaryKind = kind;
            if (kind == ReceivedItemKind.Pdf)
            {
                var analysis = PdfContentAnalyzer.Analyze(finalPath);
                var choice = settings.PdfHandlingMode switch
                {
                    PdfHandlingMode.Automatic => PdfHandlingChoice.Automatic,
                    PdfHandlingMode.Image => PdfHandlingChoice.Image,
                    PdfHandlingMode.TextOnly => PdfHandlingChoice.TextOnly,
                    _ => await notifications.ChoosePdfHandlingAsync(
                        new PdfHandlingContext(analysis.PageCount, analysis.Text is not null,
                            !analysis.IsTextOnly, Preview(analysis.Text)),
                        TimeSpan.FromSeconds(10), cancellationToken)
                };
                if (choice == PdfHandlingChoice.Automatic)
                    choice = analysis.IsTextOnly ? PdfHandlingChoice.TextOnly : PdfHandlingChoice.Image;
                if (choice == PdfHandlingChoice.TextOnly && analysis.Text is not null)
                {
                    text = analysis.Text;
                    primaryPath = UniquePath(settings.SaveFolder, baseName, ReceivedItemKind.Text.Extension());
                    await File.WriteAllTextAsync(primaryPath, text,
                        new System.Text.UTF8Encoding(encoderShouldEmitUTF8Identifier: false), cancellationToken);
                    convertedPaths = [primaryPath];
                    primaryKind = ReceivedItemKind.Text;
                }
                else
                {
                    var conversion = await WindowsPdfConverter.ConvertAllPagesAsync(
                        finalPath, settings.PdfImageFormat, settings.PdfRenderDpi, cancellationToken);
                    convertedPaths = conversion.ImagePaths.ToArray();
                    primaryPath = convertedPaths[0];
                    primaryKind = settings.PdfImageFormat == PdfImageFormat.Jpeg
                        ? ReceivedItemKind.Jpeg : ReceivedItemKind.Png;
                }
            }
            var previewPath = primaryKind is ReceivedItemKind.Png or ReceivedItemKind.Jpeg
                ? primaryPath : null;

            string? postProcessingError = null;
            if (settings.AutoCopyToClipboard)
            {
                try { await clipboard.CopyAsync(new ClipboardPayload(primaryPath, text, primaryKind, previewPath, convertedPaths), cancellationToken); }
                catch (Exception ex) { postProcessingError = $"Clipboard: {ex.Message}"; }
            }
            var primarySize = new FileInfo(primaryPath).Length;
            item = new ReceivedItem(Guid.NewGuid(), DateTimeOffset.Now, primaryPath,
                Path.GetFileName(primaryPath), primaryKind, primarySize, text, previewPath,
                convertedPaths, postProcessingError, primaryPath == finalPath ? null : finalPath);
            try { await history.AddAsync(item, cancellationToken); }
            catch (Exception ex) { postProcessingError = string.Join("; ", new[] { postProcessingError, $"History: {ex.Message}" }.Where(x => x is not null)); }
            if (postProcessingError is null) notifications.Received(item);
            else notifications.Error($"{item.DisplayName} was saved. {postProcessingError}");
            ItemReceived?.Invoke(this, item);
            return item;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            notifications.Error($"Could not receive document: {ex.Message}");
            throw;
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    private static async Task<(long Size, byte[] Header)> CopyLimitedAsync(
        Stream source, string destination, long maximumBytes, CancellationToken cancellationToken)
    {
        var header = new byte[16];
        var headerCount = 0;
        var total = 0L;
        var buffer = new byte[64 * 1024];
        await using var target = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None,
            buffer.Length, FileOptions.Asynchronous | FileOptions.SequentialScan);
        while (true)
        {
            var count = await source.ReadAsync(buffer, cancellationToken);
            if (count == 0) break;
            total += count;
            if (total > maximumBytes) throw new InvalidDataException($"Document exceeds the {maximumBytes / 1024 / 1024} MB limit.");
            if (headerCount < header.Length)
            {
                var copy = Math.Min(count, header.Length - headerCount);
                buffer.AsSpan(0, copy).CopyTo(header.AsSpan(headerCount));
                headerCount += copy;
            }
            await target.WriteAsync(buffer.AsMemory(0, count), cancellationToken);
        }
        return (total, header[..headerCount]);
    }

    private static string UniquePath(string folder, string name, string extension)
    {
        var path = Path.Combine(folder, name + extension);
        for (var index = 2; File.Exists(path); index++) path = Path.Combine(folder, $"{name}_{index}{extension}");
        return path;
    }

    private static string Sanitize(string value)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(value.Select(c => invalid.Contains(c) ? '_' : c).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(cleaned) ? "Apple-device" : cleaned[..Math.Min(cleaned.Length, 60)];
    }

    private static string? Preview(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        var singleLine = string.Join(' ', text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        return singleLine[..Math.Min(singleLine.Length, 180)];
    }
}
