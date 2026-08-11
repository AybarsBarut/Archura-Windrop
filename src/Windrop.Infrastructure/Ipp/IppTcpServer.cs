using System.Net;
using System.Net.Sockets;
using System.Text;
using Windrop.Domain;
using Windrop.Infrastructure.Documents;

namespace Windrop.Infrastructure.Ipp;

public sealed class IppTcpServer(BridgeSettings settings, DocumentPipeline pipeline) : IAsyncDisposable
{
    private TcpListener? _listener;
    private CancellationTokenSource? _lifetime;
    private Task? _acceptLoop;
    private int _jobId;

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_listener is not null) return Task.CompletedTask;
        _lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _listener = new TcpListener(IPAddress.Any, settings.Port);
        _listener.Start(20);
        _acceptLoop = AcceptLoopAsync(_lifetime.Token);
        return Task.CompletedTask;
    }

    public async Task StopAsync()
    {
        if (_listener is null) return;
        await _lifetime!.CancelAsync();
        _listener.Stop();
        try { if (_acceptLoop is not null) await _acceptLoop; }
        catch (OperationCanceledException) { }
        catch (SocketException) when (_lifetime.IsCancellationRequested) { }
        _listener = null;
        _lifetime.Dispose();
        _lifetime = null;
    }

    private async Task AcceptLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var client = await _listener!.AcceptTcpClientAsync(cancellationToken);
            _ = HandleClientSafelyAsync(client, cancellationToken);
        }
    }

    private async Task HandleClientSafelyAsync(TcpClient client, CancellationToken cancellationToken)
    {
        using (client)
        {
            try { await HandleClientAsync(client, cancellationToken); }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                try { await WriteHttpErrorAsync(client.GetStream(), 400, ex.Message, cancellationToken); }
                catch { }
            }
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken cancellationToken)
    {
        client.NoDelay = true;
        var stream = client.GetStream();
        var (headerText, bodyPrefix) = await ReadHeadersAsync(stream, cancellationToken);
        var lines = headerText.Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
        if (lines.Length == 0 || !lines[0].StartsWith("POST /ipp/print ", StringComparison.Ordinal))
        {
            await WriteHttpErrorAsync(stream, 404, "IPP endpoint is /ipp/print", cancellationToken);
            return;
        }
        var headers = lines.Skip(1).Select(x => x.Split(':', 2))
            .Where(x => x.Length == 2)
            .ToDictionary(x => x[0].Trim(), x => x[1].Trim(), StringComparer.OrdinalIgnoreCase);
        if (!headers.TryGetValue("Content-Type", out var contentType) ||
            !contentType.StartsWith("application/ipp", StringComparison.OrdinalIgnoreCase))
        {
            await WriteHttpErrorAsync(stream, 415, "Content-Type must be application/ipp", cancellationToken);
            return;
        }
        if (headers.TryGetValue("Expect", out var expect) &&
            expect.Contains("100-continue", StringComparison.OrdinalIgnoreCase))
            await stream.WriteAsync("HTTP/1.1 100 Continue\r\n\r\n"u8.ToArray(), cancellationToken);

        byte[] body;
        if (headers.TryGetValue("Transfer-Encoding", out var transfer) && transfer.Contains("chunked", StringComparison.OrdinalIgnoreCase))
            body = await ReadChunkedAsync(stream, bodyPrefix, settings.MaxDocumentBytes + 64 * 1024, cancellationToken);
        else if (headers.TryGetValue("Content-Length", out var lengthText) && long.TryParse(lengthText, out var length))
            body = await ReadFixedAsync(stream, bodyPrefix, length, settings.MaxDocumentBytes + 64 * 1024, cancellationToken);
        else throw new InvalidDataException("Missing Content-Length or chunked transfer encoding.");

        var request = IppParser.Parse(body);
        byte[] response;
        switch (request.OperationId)
        {
            case IppOperations.GetPrinterAttributes:
                response = PrinterAttributes(request);
                break;
            case IppOperations.ValidateJob:
                response = Success(request).Build();
                break;
            case IppOperations.PrintJob:
            {
                var jobId = Interlocked.Increment(ref _jobId);
                await using var document = new MemoryStream(body, request.DocumentOffset, body.Length - request.DocumentOffset, false);
                await pipeline.ProcessAsync(new IncomingDocument(
                    request.StringAttribute("job-name") ?? $"job-{jobId}",
                    request.StringAttribute("requesting-user-name"),
                    request.StringAttribute("document-format"), document), cancellationToken);
                response = JobCompleted(request, jobId);
                break;
            }
            case IppOperations.GetJobAttributes:
                response = JobCompleted(request, Math.Max(1, _jobId));
                break;
            case IppOperations.GetJobs:
                response = Success(request).Build();
                break;
            default:
                response = new IppResponseWriter(request.Major, request.Minor, 0x0501, request.RequestId)
                    .Group(0x01).Charset("attributes-charset", "utf-8")
                    .Language("attributes-natural-language", "en").Build();
                break;
        }
        await WriteHttpResponseAsync(stream, response, cancellationToken);
    }

    private byte[] PrinterAttributes(IppRequest request) => Success(request)
        .Group(0x04)
        .Uri("printer-uri-supported", $"ipp://{Environment.MachineName}.local:{settings.Port}/ipp/print")
        .Name("printer-name", settings.DeviceName)
        .Enum("printer-state", 3)
        .Keyword("printer-state-reasons", "none")
        .Boolean("printer-is-accepting-jobs", true)
        .Mime("document-format-supported", "application/pdf", "image/jpeg", "image/png", "image/urf")
        .Mime("document-format-default", "application/pdf")
        .Keyword("media-supported", "iso_a4_210x297mm", "na_letter_8.5x11in")
        .Keyword("media-default", "iso_a4_210x297mm")
        .Keyword("print-color-mode-supported", "color", "monochrome")
        .Keyword("print-color-mode-default", "color")
        .Keyword("sides-supported", "one-sided")
        .Keyword("sides-default", "one-sided")
        .Keyword("ipp-versions-supported", "1.1", "2.0")
        .Enums("operations-supported", IppOperations.PrintJob, IppOperations.ValidateJob,
            IppOperations.GetJobAttributes, IppOperations.GetJobs, IppOperations.GetPrinterAttributes)
        .Charset("charset-configured", "utf-8")
        .Charset("charset-supported", "utf-8")
        .Language("natural-language-configured", "en")
        .Build();

    private static IppResponseWriter Success(IppRequest request) =>
        new IppResponseWriter(request.Major, request.Minor, 0x0000, request.RequestId)
            .Group(0x01).Charset("attributes-charset", "utf-8")
            .Language("attributes-natural-language", "en");

    private static byte[] JobCompleted(IppRequest request, int jobId) => Success(request)
        .Group(0x02).Integer("job-id", jobId)
        .Uri("job-uri", $"ipp://localhost/jobs/{jobId}")
        .Enum("job-state", 9)
        .Keyword("job-state-reasons", "job-completed-successfully")
        .Build();

    private static async Task<(string Headers, byte[] Prefix)> ReadHeadersAsync(NetworkStream stream, CancellationToken token)
    {
        using var buffer = new MemoryStream();
        var chunk = new byte[4096];
        while (buffer.Length <= 64 * 1024)
        {
            var count = await stream.ReadAsync(chunk, token);
            if (count == 0) throw new EndOfStreamException();
            buffer.Write(chunk, 0, count);
            var bytes = buffer.GetBuffer().AsSpan(0, (int)buffer.Length);
            var marker = bytes.IndexOf("\r\n\r\n"u8);
            if (marker >= 0)
                return (Encoding.ASCII.GetString(bytes[..marker]), bytes[(marker + 4)..].ToArray());
        }
        throw new InvalidDataException("HTTP headers exceed 64 KB.");
    }

    private static async Task<byte[]> ReadFixedAsync(NetworkStream stream, byte[] prefix, long length, long maximum, CancellationToken token)
    {
        if (length < 0 || length > maximum || length > int.MaxValue) throw new InvalidDataException("Request body is too large.");
        if (prefix.Length > length) throw new InvalidDataException("Received more bytes than Content-Length.");
        var result = new byte[(int)length];
        prefix.CopyTo(result, 0);
        var offset = prefix.Length;
        while (offset < result.Length)
        {
            var count = await stream.ReadAsync(result.AsMemory(offset), token);
            if (count == 0) throw new EndOfStreamException();
            offset += count;
        }
        return result;
    }

    private static async Task<byte[]> ReadChunkedAsync(NetworkStream network, byte[] prefix, long maximum, CancellationToken token)
    {
        await using var source = new PrefixedStream(prefix, network);
        using var result = new MemoryStream();
        while (true)
        {
            var line = await ReadAsciiLineAsync(source, token);
            var separator = line.IndexOf(';');
            var sizeText = separator >= 0 ? line[..separator] : line;
            if (!int.TryParse(sizeText.Trim(), System.Globalization.NumberStyles.HexNumber, null, out var size) || size < 0)
                throw new InvalidDataException("Invalid HTTP chunk size.");
            if (size == 0)
            {
                while ((await ReadAsciiLineAsync(source, token)).Length != 0) { }
                break;
            }
            if (result.Length + size > maximum) throw new InvalidDataException("Request body is too large.");
            var data = new byte[size];
            await source.ReadExactlyAsync(data, token);
            result.Write(data);
            if (await ReadAsciiLineAsync(source, token) is not "") throw new InvalidDataException("Invalid chunk terminator.");
        }
        return result.ToArray();
    }

    private static async Task<string> ReadAsciiLineAsync(Stream stream, CancellationToken token)
    {
        using var line = new MemoryStream();
        var previous = -1;
        while (line.Length < 8192)
        {
            var one = new byte[1];
            if (await stream.ReadAsync(one, token) == 0) throw new EndOfStreamException();
            if (previous == '\r' && one[0] == '\n')
            {
                var data = line.ToArray();
                return Encoding.ASCII.GetString(data, 0, Math.Max(0, data.Length - 1));
            }
            line.WriteByte(one[0]);
            previous = one[0];
        }
        throw new InvalidDataException("HTTP line is too long.");
    }

    private static async Task WriteHttpResponseAsync(Stream stream, byte[] response, CancellationToken token)
    {
        var headers = Encoding.ASCII.GetBytes($"HTTP/1.1 200 OK\r\nContent-Type: application/ipp\r\nContent-Length: {response.Length}\r\nConnection: close\r\n\r\n");
        await stream.WriteAsync(headers, token);
        await stream.WriteAsync(response, token);
    }

    private static async Task WriteHttpErrorAsync(Stream stream, int status, string message, CancellationToken token)
    {
        var body = Encoding.UTF8.GetBytes(message);
        var header = Encoding.ASCII.GetBytes($"HTTP/1.1 {status} Error\r\nContent-Type: text/plain; charset=utf-8\r\nContent-Length: {body.Length}\r\nConnection: close\r\n\r\n");
        await stream.WriteAsync(header, token);
        await stream.WriteAsync(body, token);
    }

    public async ValueTask DisposeAsync() => await StopAsync();

    private sealed class PrefixedStream(byte[] prefix, Stream inner) : Stream
    {
        private int _offset;
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => throw new NotSupportedException();
        public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
        public override int Read(byte[] buffer, int offset, int count) => ReadAsync(buffer.AsMemory(offset, count)).AsTask().GetAwaiter().GetResult();
        public override async ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_offset < prefix.Length)
            {
                var count = Math.Min(buffer.Length, prefix.Length - _offset);
                prefix.AsMemory(_offset, count).CopyTo(buffer);
                _offset += count;
                return count;
            }
            return await inner.ReadAsync(buffer, cancellationToken);
        }
        public override void Flush() { }
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }
}
