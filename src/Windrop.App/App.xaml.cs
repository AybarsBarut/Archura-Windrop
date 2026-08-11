using System.IO;
using System.Windows;
using Windrop.Data;
using Windrop.Domain;
using Windrop.Infrastructure;

namespace Windrop.App;

public partial class App : Application
{
    private BridgeReceiver? _receiver;
    private TrayShell? _tray;
    private MainWindow? _window;
    private JsonSettingsRepository? _settingsRepository;
    private JsonHistoryRepository? _historyRepository;
    private BridgeSettings? _settings;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        AppDomain.CurrentDomain.UnhandledException += (_, args) => Log(args.ExceptionObject.ToString() ?? "Unknown failure");
        DispatcherUnhandledException += (_, args) => { Log(args.Exception.ToString()); args.Handled = true; };

        var appData = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Archura", "Windrop");
        _settingsRepository = new JsonSettingsRepository(appData);
        _historyRepository = new JsonHistoryRepository(appData);
        _settings = await _settingsRepository.LoadAsync();
        Directory.CreateDirectory(_settings.SaveFolder);
        _tray = new TrayShell(Dispatcher, ShowMainWindow, ExitApplication, _settings.Language);
        var clipboard = new WpfClipboardService(Dispatcher);
        _receiver = new BridgeReceiver(_settings, _historyRepository, clipboard, _tray);
        _receiver.ItemReceived += (_, _) => Dispatcher.InvokeAsync(() => _window?.RefreshHistoryAsync());
        try
        {
            await _receiver.StartAsync();
            _tray.SetStatus(TrayStatus.Idle, Localizer.Format(_settings.Language, UiText.Listening, _settings.DeviceName));
        }
        catch (Exception ex)
        {
            Log(ex.ToString());
            _tray.SetStatus(TrayStatus.Error, ex.Message);
            _tray.Error(Localizer.Format(_settings.Language, UiText.StartError, ex.Message));
        }
    }

    private void ShowMainWindow()
    {
        if (_settings is null || _settingsRepository is null || _historyRepository is null) return;
        _window ??= new MainWindow(_settings, _settingsRepository, _historyRepository,
            new WpfClipboardService(Dispatcher), RestartReceiverAsync);
        _window.Show();
        if (_window.WindowState == WindowState.Minimized) _window.WindowState = WindowState.Normal;
        _window.Activate();
        _ = _window.RefreshHistoryAsync();
    }

    private async Task RestartReceiverAsync()
    {
        if (_receiver is not null) await _receiver.DisposeAsync();
        if (_settings is null || _historyRepository is null || _tray is null) return;
        _tray.SetLanguage(_settings.Language);
        _receiver = new BridgeReceiver(_settings, _historyRepository, new WpfClipboardService(Dispatcher), _tray);
        _receiver.ItemReceived += (_, _) => Dispatcher.InvokeAsync(() => _window?.RefreshHistoryAsync());
        await _receiver.StartAsync();
        _tray.SetStatus(TrayStatus.Idle, Localizer.Format(_settings.Language, UiText.Listening, _settings.DeviceName));
    }

    private async void ExitApplication()
    {
        _tray?.Dispose();
        if (_receiver is not null) await _receiver.DisposeAsync();
        Shutdown();
    }

    private static void Log(string message)
    {
        try
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Archura", "Windrop");
            Directory.CreateDirectory(folder);
            File.AppendAllText(Path.Combine(folder, "windrop.log"), $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}");
        }
        catch { }
    }
}
