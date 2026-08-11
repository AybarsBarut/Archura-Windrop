using Windrop.Domain;
using Windrop.Infrastructure.Discovery;
using Windrop.Infrastructure.Documents;
using Windrop.Infrastructure.Ipp;

namespace Windrop.Infrastructure;

public sealed class BridgeReceiver : IDocumentReceiver, IAsyncDisposable
{
    private readonly DocumentPipeline _pipeline;
    private readonly IppTcpServer _server;
    private readonly MdnsResponder _mdns;

    public BridgeReceiver(BridgeSettings settings, IHistoryRepository history,
        IClipboardService clipboard, IUserNotificationService notifications)
    {
        _pipeline = new DocumentPipeline(settings, history, clipboard, notifications);
        _pipeline.ItemReceived += (_, item) => ItemReceived?.Invoke(this, item);
        _server = new IppTcpServer(settings, _pipeline);
        _mdns = new MdnsResponder(settings);
    }

    public event EventHandler<ReceivedItem>? ItemReceived;

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _server.StartAsync(cancellationToken);
        try { await _mdns.StartAsync(cancellationToken); }
        catch { await _server.StopAsync(); throw; }
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _mdns.StopAsync();
        await _server.StopAsync();
    }

    public async ValueTask DisposeAsync() => await StopAsync();
}
