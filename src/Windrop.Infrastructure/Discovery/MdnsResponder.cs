using System.Buffers.Binary;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using Windrop.Domain;

namespace Windrop.Infrastructure.Discovery;

public sealed class MdnsResponder(BridgeSettings settings) : IAsyncDisposable
{
    private static readonly IPEndPoint MulticastEndpoint = new(IPAddress.Parse("224.0.0.251"), 5353);
    private UdpClient? _udp;
    private CancellationTokenSource? _lifetime;
    private Task? _loop;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_udp is not null) return;
        _lifetime = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _udp = new UdpClient(AddressFamily.InterNetwork);
        _udp.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
        _udp.Client.Bind(new IPEndPoint(IPAddress.Any, 5353));
        _udp.JoinMulticastGroup(MulticastEndpoint.Address);
        _loop = RunAsync(_lifetime.Token);
        await AnnounceAsync(_lifetime.Token);
    }

    public async Task StopAsync()
    {
        if (_udp is null) return;
        await _lifetime!.CancelAsync();
        _udp.Dispose();
        try { if (_loop is not null) await _loop; }
        catch (OperationCanceledException) { }
        catch (ObjectDisposedException) { }
        catch (SocketException) when (_lifetime.IsCancellationRequested) { }
        _udp = null;
        _lifetime.Dispose();
        _lifetime = null;
    }

    private async Task RunAsync(CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromSeconds(60));
        var receive = ReceiveLoopAsync(cancellationToken);
        var announce = AnnounceLoopAsync(timer, cancellationToken);
        await Task.WhenAll(receive, announce);
    }

    private async Task ReceiveLoopAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var packet = await _udp!.ReceiveAsync(cancellationToken);
            if (IsServiceDiscoveryQuery(packet.Buffer)) await AnnounceAsync(cancellationToken);
        }
    }

    private async Task AnnounceLoopAsync(PeriodicTimer timer, CancellationToken cancellationToken)
    {
        while (await timer.WaitForNextTickAsync(cancellationToken)) await AnnounceAsync(cancellationToken);
    }

    private async Task AnnounceAsync(CancellationToken cancellationToken)
    {
        var addresses = NetworkInterface.GetAllNetworkInterfaces()
            .Where(n => n.OperationalStatus == OperationalStatus.Up && n.NetworkInterfaceType != NetworkInterfaceType.Loopback)
            .SelectMany(n => n.GetIPProperties().UnicastAddresses)
            .Select(x => x.Address)
            .Where(x => x.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(x))
            .Distinct()
            .ToArray();
        foreach (var address in addresses)
        {
            var packet = BuildResponse(address);
            await _udp!.SendAsync(packet, MulticastEndpoint, cancellationToken);
        }
    }

    private byte[] BuildResponse(IPAddress address)
    {
        var service = "_ipp._tcp.local";
        var universalSubtype = "_universal._sub._ipp._tcp.local";
        var instanceLabel = TruncateLabel(settings.DeviceName.Replace('.', ' '));
        var instance = $"{instanceLabel}.{service}";
        var host = $"{TruncateLabel(Environment.MachineName)}.local";
        using var stream = new MemoryStream();
        WriteU16(stream, 0); WriteU16(stream, 0x8400); WriteU16(stream, 0); WriteU16(stream, 5); WriteU16(stream, 0); WriteU16(stream, 0);
        WriteRecord(stream, service, 12, 0x0001, 120, data => WriteName(data, instance));
        WriteRecord(stream, universalSubtype, 12, 0x0001, 120, data => WriteName(data, instance));
        WriteRecord(stream, instance, 33, 0x8001, 120, data =>
        {
            WriteU16(data, 0); WriteU16(data, 0); WriteU16(data, (ushort)settings.Port); WriteName(data, host);
        });
        WriteRecord(stream, instance, 16, 0x8001, 120, data =>
        {
            WriteTxt(data, "txtvers=1"); WriteTxt(data, "qtotal=1"); WriteTxt(data, "rp=ipp/print");
            WriteTxt(data, $"ty={settings.DeviceName}"); WriteTxt(data, "product=(Archura Bridge)");
            WriteTxt(data, "pdl=application/pdf,image/jpeg,image/png,image/urf"); WriteTxt(data, "air=none");
            WriteTxt(data, "URF=none"); WriteTxt(data, "Color=T"); WriteTxt(data, "Duplex=F");
            WriteTxt(data, $"note={Environment.MachineName}");
        });
        WriteRecord(stream, host, 1, 0x8001, 120, data => data.Write(address.GetAddressBytes()));
        return stream.ToArray();
    }

    public static bool IsServiceDiscoveryQuery(ReadOnlySpan<byte> packet)
    {
        if (packet.Length < 12) return false;
        var flags = BinaryPrimitives.ReadUInt16BigEndian(packet[2..4]);
        if ((flags & 0x8000) != 0) return false;
        var text = Encoding.ASCII.GetString(packet);
        return text.Contains("_ipp", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("_services", StringComparison.OrdinalIgnoreCase);
    }

    private static void WriteRecord(Stream stream, string name, ushort type, ushort @class, uint ttl, Action<MemoryStream> writeData)
    {
        WriteName(stream, name); WriteU16(stream, type); WriteU16(stream, @class); WriteU32(stream, ttl);
        using var data = new MemoryStream(); writeData(data);
        WriteU16(stream, checked((ushort)data.Length)); data.Position = 0; data.CopyTo(stream);
    }

    private static void WriteName(Stream stream, string name)
    {
        foreach (var label in name.Split('.'))
        {
            var data = Encoding.UTF8.GetBytes(label);
            if (data.Length is 0 or > 63) throw new InvalidDataException("Invalid DNS label.");
            stream.WriteByte((byte)data.Length); stream.Write(data);
        }
        stream.WriteByte(0);
    }

    private static void WriteTxt(Stream stream, string text)
    {
        var bytes = Encoding.UTF8.GetBytes(text);
        if (bytes.Length > 255) bytes = bytes[..255];
        stream.WriteByte((byte)bytes.Length); stream.Write(bytes);
    }

    private static string TruncateLabel(string text)
    {
        text = string.IsNullOrWhiteSpace(text) ? "Archura Bridge" : text.Trim();
        while (Encoding.UTF8.GetByteCount(text) > 63) text = text[..^1];
        return text;
    }

    private static void WriteU16(Stream stream, ushort value)
    {
        Span<byte> bytes = stackalloc byte[2]; BinaryPrimitives.WriteUInt16BigEndian(bytes, value); stream.Write(bytes);
    }
    private static void WriteU32(Stream stream, uint value)
    {
        Span<byte> bytes = stackalloc byte[4]; BinaryPrimitives.WriteUInt32BigEndian(bytes, value); stream.Write(bytes);
    }

    public async ValueTask DisposeAsync() => await StopAsync();
}
