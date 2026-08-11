using Windows.Data.Pdf;
using Windows.Foundation;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;
using Windrop.Domain;

namespace Windrop.Infrastructure.Documents;

public sealed record PdfConversionResult(IReadOnlyList<string> ImagePaths);

public static class WindowsPdfConverter
{
    private const uint MaximumDimension = 10_000;

    public static async Task<PdfConversionResult> ConvertAllPagesAsync(
        string pdfPath,
        PdfImageFormat format,
        int renderDpi,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var source = await StorageFile.GetFileFromPathAsync(pdfPath);
        var document = await PdfDocument.LoadFromFileAsync(source);
        if (document.PageCount == 0) throw new InvalidDataException("PDF has no pages.");

        var paths = new List<string>(checked((int)document.PageCount));
        var basePath = Path.Combine(Path.GetDirectoryName(pdfPath)!, Path.GetFileNameWithoutExtension(pdfPath));
        var extension = format == PdfImageFormat.Jpeg ? ".jpg" : ".png";
        for (uint index = 0; index < document.PageCount; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            using var page = document.GetPage(index);
            var suffix = document.PageCount == 1 ? string.Empty : $"_page-{index + 1:000}";
            var outputPath = UniquePath(basePath + suffix + extension);
            var scale = Math.Clamp(renderDpi, 72, 600) / 96d;
            var width = Math.Min(MaximumDimension, Math.Max(1u, checked((uint)Math.Round(page.Size.Width * scale))));
            var height = Math.Min(MaximumDimension, Math.Max(1u, checked((uint)Math.Round(page.Size.Height * scale))));
            var options = new PdfPageRenderOptions { DestinationWidth = width, DestinationHeight = height };

            if (format == PdfImageFormat.Png)
                await RenderPngAsync(page, outputPath, options);
            else
                await RenderJpegAsync(page, outputPath, options);
            paths.Add(outputPath);
        }
        return new PdfConversionResult(paths);
    }

    private static async Task RenderPngAsync(PdfPage page, string outputPath, PdfPageRenderOptions options)
    {
        await using (File.Create(outputPath)) { }
        var outputFile = await StorageFile.GetFileFromPathAsync(outputPath);
        using var output = await outputFile.OpenAsync(FileAccessMode.ReadWrite);
        output.Size = 0;
        await page.RenderToStreamAsync(output, options);
        await output.FlushAsync();
    }

    private static async Task RenderJpegAsync(PdfPage page, string outputPath, PdfPageRenderOptions options)
    {
        using var renderedPng = new InMemoryRandomAccessStream();
        await page.RenderToStreamAsync(renderedPng, options);
        renderedPng.Seek(0);
        var decoder = await BitmapDecoder.CreateAsync(renderedPng);
        using var bitmap = await decoder.GetSoftwareBitmapAsync(BitmapPixelFormat.Bgra8, BitmapAlphaMode.Ignore);

        await using (File.Create(outputPath)) { }
        var outputFile = await StorageFile.GetFileFromPathAsync(outputPath);
        using var output = await outputFile.OpenAsync(FileAccessMode.ReadWrite);
        output.Size = 0;
        var properties = new BitmapPropertySet
        {
            ["ImageQuality"] = new BitmapTypedValue(1.0f, PropertyType.Single)
        };
        var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.JpegEncoderId, output, properties);
        encoder.SetSoftwareBitmap(bitmap);
        await encoder.FlushAsync();
        await output.FlushAsync();
    }

    private static string UniquePath(string desiredPath)
    {
        if (!File.Exists(desiredPath)) return desiredPath;
        var directory = Path.GetDirectoryName(desiredPath)!;
        var name = Path.GetFileNameWithoutExtension(desiredPath);
        var extension = Path.GetExtension(desiredPath);
        for (var index = 2; ; index++)
        {
            var candidate = Path.Combine(directory, $"{name}_{index}{extension}");
            if (!File.Exists(candidate)) return candidate;
        }
    }
}
