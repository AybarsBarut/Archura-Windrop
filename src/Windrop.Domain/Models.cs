namespace Windrop.Domain;

public enum ReceivedItemKind { Pdf, Jpeg, Png, Text, Urf, Unknown }
public enum PdfImageFormat { Png, Jpeg }
public enum PdfHandlingMode { AskEveryTime, Automatic, Image, TextOnly }
public enum PdfHandlingChoice { Automatic, Image, TextOnly }
public enum UiLanguage { English, Turkish, German, Spanish, Russian, SimplifiedChinese }

public sealed record ReceivedItem(
    Guid Id,
    DateTimeOffset ReceivedAt,
    string FilePath,
    string DisplayName,
    ReceivedItemKind Kind,
    long Size,
    string? ExtractedText = null,
    string? PreviewImagePath = null,
    string[]? ConvertedFilePaths = null,
    string? Error = null,
    string? SourceFilePath = null);

public sealed class BridgeSettings
{
    public static string DefaultSaveFolder => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Windrop", "Received");

    public string DeviceName { get; set; } = $"Archura Bridge ({Environment.MachineName})";
    public string SaveFolder { get; set; } = DefaultSaveFolder;
    public int Port { get; set; } = 8631;
    public long MaxDocumentBytes { get; set; } = 200L * 1024 * 1024;
    public bool AutoCopyToClipboard { get; set; } = true;
    public bool OcrFallback { get; set; }
    public PdfImageFormat PdfImageFormat { get; set; } = PdfImageFormat.Png;
    public int PdfRenderDpi { get; set; } = 600;
    public int RenderQualityVersion { get; set; }
    public PdfHandlingMode PdfHandlingMode { get; set; } = PdfHandlingMode.AskEveryTime;
    public UiLanguage Language { get; set; } = UiLanguage.English;
    public bool RequireApproval { get; set; }
    public bool StartWithWindows { get; set; }
}

public sealed record IncomingDocument(
    string SuggestedName,
    string? SourceDevice,
    string? DeclaredFormat,
    Stream Content);

public sealed record ClipboardPayload(
    string FilePath,
    string? Text,
    ReceivedItemKind Kind,
    string? PreviewImagePath = null,
    IReadOnlyList<string>? ConvertedFilePaths = null);

public sealed record PdfHandlingContext(int PageCount, bool HasText, bool HasVisualContent, string? TextPreview);

public interface ISettingsRepository
{
    Task<BridgeSettings> LoadAsync(CancellationToken cancellationToken = default);
    Task SaveAsync(BridgeSettings settings, CancellationToken cancellationToken = default);
}

public interface IHistoryRepository
{
    Task<IReadOnlyList<ReceivedItem>> LoadAsync(CancellationToken cancellationToken = default);
    Task AddAsync(ReceivedItem item, CancellationToken cancellationToken = default);
}

public interface IClipboardService
{
    Task CopyAsync(ClipboardPayload payload, CancellationToken cancellationToken = default);
}

public interface IUserNotificationService
{
    void Received(ReceivedItem item);
    void Error(string message);
    Task<bool> ApproveAsync(string source, TimeSpan timeout, CancellationToken cancellationToken = default);
    Task<PdfHandlingChoice> ChoosePdfHandlingAsync(
        PdfHandlingContext context, TimeSpan timeout, CancellationToken cancellationToken = default);
}

public interface IDocumentReceiver
{
    event EventHandler<ReceivedItem>? ItemReceived;
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}
