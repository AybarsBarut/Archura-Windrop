using System.Buffers.Binary;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Windrop.Domain;
using Windrop.Data;
using Windrop.Infrastructure.Documents;
using Windrop.Infrastructure.Discovery;
using Windrop.Infrastructure.Ipp;

var tests = new (string Name, Func<Task> Run)[]
{
    ("Detects PDF magic bytes", Sync(() => Equal(ReceivedItemKind.Pdf, DocumentFormat.Detect("%PDF-1.7"u8)))),
    ("Detects JPEG magic bytes", Sync(() => Equal(ReceivedItemKind.Jpeg, DocumentFormat.Detect(new byte[] { 0xff, 0xd8, 0xff })))),
    ("Detects PNG magic bytes", Sync(() => Equal(ReceivedItemKind.Png, DocumentFormat.Detect(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a })))),
    ("Uses declared URF type", Sync(() => Equal(ReceivedItemKind.Urf, DocumentFormat.Detect([], "image/urf")))),
    ("Parses Print-Job and document offset", Sync(ParsePrintJob)),
    ("Rejects truncated attributes", Sync(RejectTruncated)),
    ("Encodes big-endian response header", Sync(EncodeResponse)),
    ("Ignores mDNS response packets", Sync(TestMdnsQueryFiltering)),
    ("Classifies a text-only PDF", TestTextOnlyPdfAsync),
    ("Classifies and converts a visual PDF", TestVisualPdfConversionAsync),
    ("Migrates the previous default Received folder", TestDefaultFolderMigrationAsync),
    ("Provides strings for every supported language", Sync(TestSupportedLanguages)),
    ("Saves selected text-only PDF output", TestTextOutputPipelineAsync),
    ("Saves selected image PDF output", TestImageOutputPipelineAsync),
    ("Handles Expect 100-continue", TestContinueAsync),
    ("Receives a chunked Print-Job", TestChunkedPrintJobAsync)
};

var failures = 0;
foreach (var (name, run) in tests)
{
    try { await run(); Console.WriteLine($"PASS {name}"); }
    catch (Exception ex) { failures++; Console.Error.WriteLine($"FAIL {name}: {ex.Message}"); }
}
Console.WriteLine($"{tests.Length - failures}/{tests.Length} tests passed");
return failures == 0 ? 0 : 1;

static void ParsePrintJob()
{
    using var stream = new MemoryStream();
    stream.Write(new byte[] { 2, 0, 0, 2, 0, 0, 0, 42, 1 });
    Attribute(stream, 0x47, "attributes-charset", "utf-8");
    Attribute(stream, 0x49, "document-format", "application/pdf");
    stream.WriteByte(3); stream.Write("%PDF-document"u8);
    var bytes = stream.ToArray();
    var parsed = IppParser.Parse(bytes);
    Equal((ushort)2, parsed.OperationId); Equal(42, parsed.RequestId);
    Equal("application/pdf", parsed.StringAttribute("document-format"));
    Equal("%PDF-document", Encoding.ASCII.GetString(bytes[parsed.DocumentOffset..]));
}

static void RejectTruncated()
{
    var threw = false;
    try { IppParser.Parse(new byte[] { 2, 0, 0, 2, 0, 0, 0, 1, 1, 0x47, 0, 20 }); }
    catch (InvalidDataException) { threw = true; }
    Equal(true, threw);
}

static void EncodeResponse()
{
    var bytes = new IppResponseWriter(2, 0, 0, 99).Group(1)
        .Charset("attributes-charset", "utf-8").Language("attributes-natural-language", "en").Build();
    Equal((byte)2, bytes[0]); Equal((byte)0, bytes[1]);
    Equal(99, BinaryPrimitives.ReadInt32BigEndian(bytes.AsSpan(4, 4)));
    Equal((byte)3, bytes[^1]);
}

static void TestMdnsQueryFiltering()
{
    var query = new byte[32];
    Encoding.ASCII.GetBytes("_ipp._tcp.local").CopyTo(query, 12);
    Equal(true, MdnsResponder.IsServiceDiscoveryQuery(query));
    var response = query.ToArray(); response[2] = 0x84;
    Equal(false, MdnsResponder.IsServiceDiscoveryQuery(response));
    Equal(false, MdnsResponder.IsServiceDiscoveryQuery(new byte[12]));
}

static async Task TestTextOnlyPdfAsync()
{
    var folder = Path.Combine(Path.GetTempPath(), $"windrop-pdf-text-{Guid.NewGuid():N}");
    Directory.CreateDirectory(folder);
    try
    {
        var path = Path.Combine(folder, "text.pdf");
        await File.WriteAllBytesAsync(path, BuildPdf(withImage: false));
        var analysis = PdfContentAnalyzer.Analyze(path);
        Equal(true, analysis.IsTextOnly); Equal(0, analysis.ImageCount);
        Contains("Hello Windrop", analysis.Text ?? string.Empty);
    }
    finally { Directory.Delete(folder, true); }
}

static async Task TestVisualPdfConversionAsync()
{
    var folder = Path.Combine(Path.GetTempPath(), $"windrop-pdf-image-{Guid.NewGuid():N}");
    Directory.CreateDirectory(folder);
    try
    {
        var pngSource = Path.Combine(folder, "visual-png.pdf");
        var jpegSource = Path.Combine(folder, "visual-jpeg.pdf");
        var pdf = BuildPdf(withImage: true);
        await File.WriteAllBytesAsync(pngSource, pdf); await File.WriteAllBytesAsync(jpegSource, pdf);
        var analysis = PdfContentAnalyzer.Analyze(pngSource);
        Equal(false, analysis.IsTextOnly); Equal(true, analysis.ImageCount > 0);

        var png = await WindowsPdfConverter.ConvertAllPagesAsync(pngSource, PdfImageFormat.Png, 96);
        Equal(1, png.ImagePaths.Count);
        var pngBytes = await File.ReadAllBytesAsync(png.ImagePaths[0]);
        Equal("89504E470D0A1A0A", Convert.ToHexString(pngBytes[..8]));

        var jpeg = await WindowsPdfConverter.ConvertAllPagesAsync(jpegSource, PdfImageFormat.Jpeg, 96);
        Equal(1, jpeg.ImagePaths.Count);
        var jpegBytes = await File.ReadAllBytesAsync(jpeg.ImagePaths[0]);
        Equal("FFD8", Convert.ToHexString(jpegBytes[..2]));
    }
    finally { Directory.Delete(folder, true); }
}

static async Task TestDefaultFolderMigrationAsync()
{
    var folder = Path.Combine(Path.GetTempPath(), $"windrop-settings-{Guid.NewGuid():N}");
    try
    {
        var repository = new JsonSettingsRepository(folder);
        var oldDefault = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Windrop");
        await repository.SaveAsync(new BridgeSettings
        {
            SaveFolder = oldDefault,
            PdfRenderDpi = 200,
            RenderQualityVersion = 0
        });
        var loaded = await repository.LoadAsync();
        Equal(Path.GetFullPath(BridgeSettings.DefaultSaveFolder), Path.GetFullPath(loaded.SaveFolder));
        Equal(600, loaded.PdfRenderDpi);
        Equal(1, loaded.RenderQualityVersion);
    }
    finally { if (Directory.Exists(folder)) Directory.Delete(folder, true); }
}

static void TestSupportedLanguages()
{
    var keys = new[]
    {
        UiText.History, UiText.Settings, UiText.DeviceName, UiText.SaveFolder, UiText.Language,
        UiText.PdfHandling, UiText.AutoCopy, UiText.SaveRestart, UiText.NoItems,
        UiText.ContentReceived, UiText.ApprovalTitle, UiText.PdfChoiceTitle,
        UiText.Automatic, UiText.SaveImage, UiText.TextOnly
    };
    foreach (var language in Enum.GetValues<UiLanguage>())
    foreach (var key in keys)
    {
        var value = Localizer.Get(language, key);
        Equal(true, !string.IsNullOrWhiteSpace(value));
    }
    Equal("Geçmiş", Localizer.Get(UiLanguage.Turkish, UiText.History));
    Equal("Verlauf", Localizer.Get(UiLanguage.German, UiText.History));
    Equal("Historial", Localizer.Get(UiLanguage.Spanish, UiText.History));
    Equal("История", Localizer.Get(UiLanguage.Russian, UiText.History));
    Equal("历史记录", Localizer.Get(UiLanguage.SimplifiedChinese, UiText.History));
}

static async Task TestTextOutputPipelineAsync()
{
    var folder = Path.Combine(Path.GetTempPath(), $"windrop-text-output-{Guid.NewGuid():N}");
    Directory.CreateDirectory(folder);
    try
    {
        var services = new TestServices { PdfChoice = PdfHandlingChoice.TextOnly };
        var settings = new BridgeSettings
        {
            SaveFolder = folder, AutoCopyToClipboard = true, PdfHandlingMode = PdfHandlingMode.AskEveryTime
        };
        var pipeline = new DocumentPipeline(settings, services, services, services);
        await using var pdf = new MemoryStream(BuildPdf(withImage: false));
        var item = await pipeline.ProcessAsync(new IncomingDocument("text", "iPhone", "application/pdf", pdf), CancellationToken.None);
        Equal(ReceivedItemKind.Text, item.Kind);
        Equal(".txt", Path.GetExtension(item.FilePath));
        Equal(true, File.Exists(item.FilePath)); Equal(true, File.Exists(item.SourceFilePath!));
        Contains("Hello Windrop", await File.ReadAllTextAsync(item.FilePath));
        Equal(item.FilePath, services.LastClipboard?.FilePath ?? string.Empty);
    }
    finally { Directory.Delete(folder, true); }
}

static async Task TestImageOutputPipelineAsync()
{
    var folder = Path.Combine(Path.GetTempPath(), $"windrop-image-output-{Guid.NewGuid():N}");
    Directory.CreateDirectory(folder);
    try
    {
        var services = new TestServices { PdfChoice = PdfHandlingChoice.Image };
        var settings = new BridgeSettings
        {
            SaveFolder = folder, AutoCopyToClipboard = true, PdfHandlingMode = PdfHandlingMode.AskEveryTime,
            PdfImageFormat = PdfImageFormat.Jpeg, PdfRenderDpi = 96
        };
        var pipeline = new DocumentPipeline(settings, services, services, services);
        await using var pdf = new MemoryStream(BuildPdf(withImage: false));
        var item = await pipeline.ProcessAsync(new IncomingDocument("image", "iPhone", "application/pdf", pdf), CancellationToken.None);
        Equal(ReceivedItemKind.Jpeg, item.Kind);
        Equal("FFD8", Convert.ToHexString((await File.ReadAllBytesAsync(item.FilePath))[..2]));
        Equal(true, File.Exists(item.SourceFilePath!));
    }
    finally { Directory.Delete(folder, true); }
}

static byte[] BuildPdf(bool withImage)
{
    var content = withImage
        ? "q 200 0 0 200 100 500 cm /Im0 Do Q\n"
        : "BT /F1 24 Tf 72 720 Td (Hello Windrop) Tj ET\n";
    var resources = withImage
        ? "<< /XObject << /Im0 4 0 R >> >>"
        : "<< /Font << /F1 4 0 R >> >>";
    var fourth = withImage
        ? "<< /Type /XObject /Subtype /Image /Width 1 /Height 1 /ColorSpace /DeviceRGB /BitsPerComponent 8 /Filter /ASCIIHexDecode /Length 8 >>\nstream\nFF0000>\nendstream"
        : "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>";
    var objects = new[]
    {
        "<< /Type /Catalog /Pages 2 0 R >>",
        "<< /Type /Pages /Kids [3 0 R] /Count 1 >>",
        $"<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources {resources} /Contents 5 0 R >>",
        fourth,
        $"<< /Length {Encoding.ASCII.GetByteCount(content)} >>\nstream\n{content}endstream"
    };
    using var stream = new MemoryStream();
    void Write(string value) => stream.Write(Encoding.ASCII.GetBytes(value));
    Write("%PDF-1.4\n");
    var offsets = new List<long>();
    for (var index = 0; index < objects.Length; index++)
    {
        offsets.Add(stream.Position); Write($"{index + 1} 0 obj\n{objects[index]}\nendobj\n");
    }
    var xref = stream.Position;
    Write($"xref\n0 {objects.Length + 1}\n0000000000 65535 f \n");
    foreach (var offset in offsets) Write($"{offset:0000000000} 00000 n \n");
    Write($"trailer\n<< /Size {objects.Length + 1} /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
    return stream.ToArray();
}

static async Task TestContinueAsync()
{
    await WithServerAsync(async (server, port, _) =>
    {
        using var client = new TcpClient(); await client.ConnectAsync(IPAddress.Loopback, port);
        var stream = client.GetStream();
        var body = IppRequest(IppOperations.ValidateJob, []);
        var header = Encoding.ASCII.GetBytes($"POST /ipp/print HTTP/1.1\r\nHost: localhost\r\nContent-Type: application/ipp\r\nContent-Length: {body.Length}\r\nExpect: 100-continue\r\n\r\n");
        await stream.WriteAsync(header);
        var interim = await ReadUntilAsync(stream, "\r\n\r\n"u8.ToArray());
        Contains("100 Continue", Encoding.ASCII.GetString(interim));
        await stream.WriteAsync(body);
        var response = await ReadToEndAsync(stream);
        Contains("200 OK", Encoding.ASCII.GetString(response));
    });
}

static async Task TestChunkedPrintJobAsync()
{
    await WithServerAsync(async (server, port, folder) =>
    {
        using var client = new TcpClient(); await client.ConnectAsync(IPAddress.Loopback, port);
        var stream = client.GetStream();
        var document = new byte[] { 0xff, 0xd8, 0xff, 0xe0, 1, 2, 3, 4 };
        var body = IppRequest(IppOperations.PrintJob, document, (0x49, "document-format", "image/jpeg"));
        var header = Encoding.ASCII.GetBytes("POST /ipp/print HTTP/1.1\r\nHost: localhost\r\nContent-Type: application/ipp\r\nTransfer-Encoding: chunked\r\n\r\n");
        await stream.WriteAsync(header);
        var first = body[..Math.Min(17, body.Length)]; var second = body[first.Length..];
        await WriteChunkAsync(stream, first); await WriteChunkAsync(stream, second);
        await stream.WriteAsync("0\r\n\r\n"u8.ToArray());
        var response = await ReadToEndAsync(stream);
        Contains("200 OK", Encoding.ASCII.GetString(response));
        var saved = Directory.GetFiles(folder, "*.jpg"); Equal(1, saved.Length);
        Equal(Convert.ToHexString(document), Convert.ToHexString(await File.ReadAllBytesAsync(saved[0])));
    });
}

static async Task WithServerAsync(Func<IppTcpServer, int, string, Task> test)
{
    var folder = Path.Combine(Path.GetTempPath(), $"windrop-test-{Guid.NewGuid():N}");
    Directory.CreateDirectory(folder);
    var probe = new TcpListener(IPAddress.Loopback, 0); probe.Start();
    var port = ((IPEndPoint)probe.LocalEndpoint).Port; probe.Stop();
    var settings = new BridgeSettings { Port = port, SaveFolder = folder, AutoCopyToClipboard = true };
    var services = new TestServices();
    var pipeline = new DocumentPipeline(settings, services, services, services);
    await using var server = new IppTcpServer(settings, pipeline);
    await server.StartAsync();
    try { await test(server, port, folder); }
    finally { await server.StopAsync(); Directory.Delete(folder, true); }
}

static byte[] IppRequest(ushort operation, byte[] document, params (byte Tag, string Name, string Value)[] attributes)
{
    using var stream = new MemoryStream();
    stream.Write(new byte[] { 2, 0, (byte)(operation >> 8), (byte)operation, 0, 0, 0, 7, 1 });
    Attribute(stream, 0x47, "attributes-charset", "utf-8");
    foreach (var (tag, name, value) in attributes) Attribute(stream, tag, name, value);
    stream.WriteByte(3); stream.Write(document); return stream.ToArray();
}

static async Task WriteChunkAsync(Stream stream, byte[] bytes)
{
    await stream.WriteAsync(Encoding.ASCII.GetBytes($"{bytes.Length:X}\r\n"));
    await stream.WriteAsync(bytes); await stream.WriteAsync("\r\n"u8.ToArray());
}

static async Task<byte[]> ReadUntilAsync(Stream stream, byte[] marker)
{
    using var result = new MemoryStream();
    while (result.Length < 64 * 1024)
    {
        var one = new byte[1]; if (await stream.ReadAsync(one) == 0) break; result.WriteByte(one[0]);
        if (result.Length >= marker.Length && result.GetBuffer().AsSpan((int)result.Length - marker.Length, marker.Length).SequenceEqual(marker)) break;
    }
    return result.ToArray();
}

static async Task<byte[]> ReadToEndAsync(Stream stream)
{
    using var result = new MemoryStream(); await stream.CopyToAsync(result); return result.ToArray();
}

static Func<Task> Sync(Action action) => () => { action(); return Task.CompletedTask; };
static void Contains(string expected, string actual)
{
    if (!actual.Contains(expected, StringComparison.Ordinal)) throw new Exception($"expected to find '{expected}' in '{actual}'");
}

static void Attribute(Stream stream, byte tag, string name, string value)
{
    stream.WriteByte(tag); WriteU16(stream, (ushort)Encoding.UTF8.GetByteCount(name)); stream.Write(Encoding.UTF8.GetBytes(name));
    WriteU16(stream, (ushort)Encoding.UTF8.GetByteCount(value)); stream.Write(Encoding.UTF8.GetBytes(value));
}
static void WriteU16(Stream stream, ushort value)
{
    Span<byte> bytes = stackalloc byte[2]; BinaryPrimitives.WriteUInt16BigEndian(bytes, value); stream.Write(bytes);
}
static void Equal<T>(T expected, T actual)
{
    if (!EqualityComparer<T>.Default.Equals(expected, actual)) throw new Exception($"expected {expected}, got {actual}");
}

sealed class TestServices : IHistoryRepository, IClipboardService, IUserNotificationService
{
    public PdfHandlingChoice PdfChoice { get; init; } = PdfHandlingChoice.Automatic;
    public ClipboardPayload? LastClipboard { get; private set; }
    public Task<IReadOnlyList<ReceivedItem>> LoadAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ReceivedItem>>([]);
    public Task AddAsync(ReceivedItem item, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task CopyAsync(ClipboardPayload payload, CancellationToken cancellationToken = default)
    {
        LastClipboard = payload;
        return Task.CompletedTask;
    }
    public void Received(ReceivedItem item) { }
    public void Error(string message) => throw new Exception(message);
    public Task<bool> ApproveAsync(string source, TimeSpan timeout, CancellationToken cancellationToken = default) => Task.FromResult(true);
    public Task<PdfHandlingChoice> ChoosePdfHandlingAsync(PdfHandlingContext context, TimeSpan timeout,
        CancellationToken cancellationToken = default) => Task.FromResult(PdfChoice);
}
