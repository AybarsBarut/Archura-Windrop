using System.Text.Json;
using Windrop.Domain;

namespace Windrop.Data;

public sealed class JsonSettingsRepository(string appDataDirectory) : ISettingsRepository
{
    private readonly string _filePath = Path.Combine(appDataDirectory, "settings.json");
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };

    public async Task<BridgeSettings> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath)) return new BridgeSettings { RenderQualityVersion = 1 };
        BridgeSettings settings;
        await using (var stream = File.OpenRead(_filePath))
            settings = await JsonSerializer.DeserializeAsync<BridgeSettings>(stream, Options, cancellationToken)
                ?? new BridgeSettings();
        var previousDefault = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Windrop");
        var changed = false;
        if (string.Equals(Path.GetFullPath(settings.SaveFolder), Path.GetFullPath(previousDefault),
                StringComparison.OrdinalIgnoreCase))
        {
            settings.SaveFolder = BridgeSettings.DefaultSaveFolder;
            changed = true;
        }
        if (settings.RenderQualityVersion < 1)
        {
            settings.PdfRenderDpi = 600;
            settings.RenderQualityVersion = 1;
            changed = true;
        }
        if (changed) await SaveAsync(settings, cancellationToken);
        return settings;
    }

    public async Task SaveAsync(BridgeSettings settings, CancellationToken cancellationToken = default)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
        var temp = _filePath + ".tmp";
        await using (var stream = File.Create(temp))
            await JsonSerializer.SerializeAsync(stream, settings, Options, cancellationToken);
        File.Move(temp, _filePath, true);
    }
}

public sealed class JsonHistoryRepository(string appDataDirectory, int maximumItems = 100) : IHistoryRepository
{
    private readonly string _filePath = Path.Combine(appDataDirectory, "history.json");
    private static readonly JsonSerializerOptions Options = new() { WriteIndented = true };
    private readonly SemaphoreSlim _gate = new(1, 1);

    public async Task<IReadOnlyList<ReceivedItem>> LoadAsync(CancellationToken cancellationToken = default)
    {
        if (!File.Exists(_filePath)) return [];
        await using var stream = File.OpenRead(_filePath);
        return await JsonSerializer.DeserializeAsync<List<ReceivedItem>>(stream, Options, cancellationToken) ?? [];
    }

    public async Task AddAsync(ReceivedItem item, CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var items = (await LoadAsync(cancellationToken)).ToList();
            items.Insert(0, item);
            if (items.Count > maximumItems) items.RemoveRange(maximumItems, items.Count - maximumItems);
            Directory.CreateDirectory(Path.GetDirectoryName(_filePath)!);
            var temp = _filePath + ".tmp";
            await using (var stream = File.Create(temp))
                await JsonSerializer.SerializeAsync(stream, items, Options, cancellationToken);
            File.Move(temp, _filePath, true);
        }
        finally { _gate.Release(); }
    }
}
