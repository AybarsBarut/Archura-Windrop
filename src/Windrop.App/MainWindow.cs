using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media.Imaging;
using Microsoft.Win32;
using Windrop.Domain;
using Forms = System.Windows.Forms;

namespace Windrop.App;

public sealed class MainWindow : Window
{
    private readonly BridgeSettings _settings;
    private readonly ISettingsRepository _settingsRepository;
    private readonly IHistoryRepository _historyRepository;
    private readonly IClipboardService _clipboard;
    private readonly Func<Task> _restart;
    private readonly TextBox _deviceName = new();
    private readonly TextBox _saveFolder = new();
    private readonly TextBox _port = new();
    private readonly ComboBox _language = new() { DisplayMemberPath = nameof(DisplayOption<UiLanguage>.Label), SelectedValuePath = nameof(DisplayOption<UiLanguage>.Value) };
    private readonly ComboBox _pdfHandling = new() { DisplayMemberPath = nameof(DisplayOption<PdfHandlingMode>.Label), SelectedValuePath = nameof(DisplayOption<PdfHandlingMode>.Value) };
    private readonly ComboBox _pdfFormat = new() { ItemsSource = Enum.GetValues<PdfImageFormat>() };
    private readonly TextBox _pdfDpi = new();
    private readonly CheckBox _autoCopy = new();
    private readonly CheckBox _approval = new();
    private readonly CheckBox _startup = new();
    private readonly ListView _history = new();
    private readonly TextBlock _status = new();
    private readonly TabItem _historyTab = new();
    private readonly TabItem _settingsTab = new();
    private readonly Button _browseButton = new() { Margin = new Thickness(8, 0, 0, 0), Padding = new Thickness(12, 4, 12, 4) };
    private readonly Button _saveButton = new() { HorizontalAlignment = HorizontalAlignment.Right, Padding = new Thickness(16, 7, 16, 7), Margin = new Thickness(0, 18, 0, 0) };
    private readonly Button _copyButton = new() { Padding = new Thickness(12, 5, 12, 5), Margin = new Thickness(6, 0, 0, 0) };
    private readonly Button _folderButton = new() { Padding = new Thickness(12, 5, 12, 5), Margin = new Thickness(6, 0, 0, 0) };
    private readonly Button _openButton = new() { Padding = new Thickness(12, 5, 12, 5), Margin = new Thickness(6, 0, 0, 0) };
    private readonly Dictionary<string, TextBlock> _labels = [];
    private readonly Dictionary<string, GridViewColumn> _columns = [];

    public MainWindow(BridgeSettings settings, ISettingsRepository settingsRepository,
        IHistoryRepository historyRepository, IClipboardService clipboard, Func<Task> restart)
    {
        _settings = settings;
        _settingsRepository = settingsRepository;
        _historyRepository = historyRepository;
        _clipboard = clipboard;
        _restart = restart;
        Icon = BitmapFrame.Create(new Uri("pack://application:,,,/Assets/app-icon.ico"));
        Width = 800; Height = 590; MinWidth = 680; MinHeight = 480;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Content = BuildContent();
        Closing += (_, args) => { args.Cancel = true; Hide(); };
        ApplyLocalization();
        Populate();
    }

    private UIElement BuildContent()
    {
        var tabs = new TabControl { Margin = new Thickness(12) };
        _historyTab.Content = BuildHistory();
        _settingsTab.Content = BuildSettings();
        tabs.Items.Add(_historyTab);
        tabs.Items.Add(_settingsTab);
        return tabs;
    }

    private UIElement BuildSettings()
    {
        var grid = new Grid { Margin = new Thickness(18) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        grid.ColumnDefinitions.Add(new ColumnDefinition());
        for (var i = 0; i < 11; i++) grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        _labels[UiText.DeviceName] = AddRow(grid, 0, _deviceName);
        var folderPanel = new DockPanel();
        _browseButton.Click += BrowseFolder;
        DockPanel.SetDock(_browseButton, Dock.Right); folderPanel.Children.Add(_browseButton); folderPanel.Children.Add(_saveFolder);
        _labels[UiText.SaveFolder] = AddRow(grid, 1, folderPanel);
        _labels[UiText.TcpPort] = AddRow(grid, 2, _port);
        _labels[UiText.Language] = AddRow(grid, 3, _language);
        _labels[UiText.PdfHandling] = AddRow(grid, 4, _pdfHandling);
        _labels[UiText.VisualOutput] = AddRow(grid, 5, _pdfFormat);
        _labels[UiText.PdfDpi] = AddRow(grid, 6, _pdfDpi);
        Place(grid, _autoCopy, 7); Place(grid, _approval, 8); Place(grid, _startup, 9);
        _saveButton.Click += SaveSettingsAsync; Place(grid, _saveButton, 10);
        return grid;
    }

    private UIElement BuildHistory()
    {
        var grid = new Grid { Margin = new Thickness(8) };
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        var view = new GridView();
        _columns[UiText.Received] = new GridViewColumn { Width = 150, DisplayMemberBinding = new Binding(nameof(ReceivedItem.ReceivedAt)) { StringFormat = "g" } };
        _columns[UiText.File] = new GridViewColumn { Width = 350, DisplayMemberBinding = new Binding(nameof(ReceivedItem.DisplayName)) };
        _columns[UiText.Type] = new GridViewColumn { Width = 90, DisplayMemberBinding = new Binding(nameof(ReceivedItem.Kind)) };
        _columns[UiText.Size] = new GridViewColumn { Width = 100, DisplayMemberBinding = new Binding(nameof(ReceivedItem.Size)) };
        foreach (var column in _columns.Values) view.Columns.Add(column);
        _history.View = view; Grid.SetRow(_history, 0); grid.Children.Add(_history);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 10, 0, 0) };
        _copyButton.Click += CopySelectedAsync; _folderButton.Click += ShowSelected; _openButton.Click += OpenSelected;
        buttons.Children.Add(_copyButton); buttons.Children.Add(_folderButton); buttons.Children.Add(_openButton);
        Grid.SetRow(buttons, 1); grid.Children.Add(buttons);
        _status.Margin = new Thickness(0, 8, 0, 0); Grid.SetRow(_status, 2); grid.Children.Add(_status);
        return grid;
    }

    private void Populate()
    {
        _deviceName.Text = _settings.DeviceName;
        _saveFolder.Text = _settings.SaveFolder;
        _port.Text = _settings.Port.ToString();
        _language.SelectedValue = _settings.Language;
        _pdfHandling.SelectedValue = _settings.PdfHandlingMode;
        _pdfFormat.SelectedItem = _settings.PdfImageFormat;
        _pdfDpi.Text = _settings.PdfRenderDpi.ToString();
        _autoCopy.IsChecked = _settings.AutoCopyToClipboard;
        _approval.IsChecked = _settings.RequireApproval;
        _startup.IsChecked = _settings.StartWithWindows;
    }

    public async Task RefreshHistoryAsync()
    {
        var items = await _historyRepository.LoadAsync();
        _history.ItemsSource = items;
        _status.Text = items.Count == 0
            ? Localizer.Get(_settings.Language, UiText.NoItems)
            : Localizer.Format(_settings.Language, UiText.ItemsCount, items.Count);
    }

    private async void SaveSettingsAsync(object sender, RoutedEventArgs args)
    {
        if (string.IsNullOrWhiteSpace(_deviceName.Text) || string.IsNullOrWhiteSpace(_saveFolder.Text) ||
            !int.TryParse(_port.Text, out var port) || port is < 1024 or > 65535 ||
            !int.TryParse(_pdfDpi.Text, out var dpi) || dpi is < 72 or > 600 ||
            _language.SelectedValue is not UiLanguage language ||
            _pdfHandling.SelectedValue is not PdfHandlingMode handlingMode ||
            _pdfFormat.SelectedItem is not PdfImageFormat imageFormat)
        {
            MessageBox.Show(this, Localizer.Get(_settings.Language, UiText.InvalidSettings),
                Localizer.Get(_settings.Language, UiText.InvalidSettingsTitle), MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        try
        {
            _settings.DeviceName = _deviceName.Text.Trim(); _settings.SaveFolder = _saveFolder.Text.Trim(); _settings.Port = port;
            _settings.Language = language;
            _settings.PdfHandlingMode = handlingMode; _settings.PdfImageFormat = imageFormat; _settings.PdfRenderDpi = dpi;
            _settings.AutoCopyToClipboard = _autoCopy.IsChecked == true;
            _settings.RequireApproval = _approval.IsChecked == true; _settings.StartWithWindows = _startup.IsChecked == true;
            Directory.CreateDirectory(_settings.SaveFolder);
            await _settingsRepository.SaveAsync(_settings);
            SetStartup(_settings.StartWithWindows);
            await _restart();
            ApplyLocalization();
            MessageBox.Show(this, Localizer.Get(_settings.Language, UiText.SettingsSaved),
                Localizer.Get(_settings.Language, UiText.WindowTitle), MessageBoxButton.OK, MessageBoxImage.Information);
        }
        catch (Exception ex) { MessageBox.Show(this, ex.Message, Localizer.Get(_settings.Language, UiText.SaveError), MessageBoxButton.OK, MessageBoxImage.Error); }
    }

    private void BrowseFolder(object sender, RoutedEventArgs args)
    {
        using var dialog = new Forms.FolderBrowserDialog { SelectedPath = _saveFolder.Text, Description = Localizer.Get(_settings.Language, UiText.FolderPrompt) };
        if (dialog.ShowDialog() == Forms.DialogResult.OK) _saveFolder.Text = dialog.SelectedPath;
    }

    private ReceivedItem? Selected => _history.SelectedItem as ReceivedItem;
    private async void CopySelectedAsync(object sender, RoutedEventArgs args)
    {
        if (Selected is not { } item || !File.Exists(item.FilePath)) return;
        await _clipboard.CopyAsync(new ClipboardPayload(item.FilePath, item.ExtractedText, item.Kind,
            item.PreviewImagePath, item.ConvertedFilePaths));
        _status.Text = Localizer.Format(_settings.Language, UiText.Copied, item.DisplayName);
    }
    private void OpenSelected(object sender, RoutedEventArgs args) { if (Selected is { } i && File.Exists(i.FilePath)) Start(i.FilePath); }
    private void ShowSelected(object sender, RoutedEventArgs args) { if (Selected is { } i && File.Exists(i.FilePath)) Start("explorer.exe", $"/select,\"{i.FilePath}\""); }
    private static void Start(string file, string? arguments = null) => Process.Start(new ProcessStartInfo(file, arguments ?? "") { UseShellExecute = true });

    private void ApplyLocalization()
    {
        var language = _settings.Language;
        Title = Localizer.Get(language, UiText.WindowTitle);
        _historyTab.Header = Localizer.Get(language, UiText.History);
        _settingsTab.Header = Localizer.Get(language, UiText.Settings);
        foreach (var (key, label) in _labels) label.Text = Localizer.Get(language, key);
        foreach (var (key, column) in _columns) column.Header = Localizer.Get(language, key);
        _browseButton.Content = Localizer.Get(language, UiText.Browse);
        _saveButton.Content = Localizer.Get(language, UiText.SaveRestart);
        _autoCopy.Content = Localizer.Get(language, UiText.AutoCopy);
        _approval.Content = Localizer.Get(language, UiText.AskApproval);
        _startup.Content = Localizer.Get(language, UiText.StartWindows);
        _copyButton.Content = Localizer.Get(language, UiText.CopyAgain);
        _folderButton.Content = Localizer.Get(language, UiText.ShowFolder);
        _openButton.Content = Localizer.Get(language, UiText.Open);
        _language.ItemsSource = LanguageOptions();
        _language.SelectedValue = _settings.Language;
        _pdfHandling.ItemsSource = Enum.GetValues<PdfHandlingMode>()
            .Select(x => new DisplayOption<PdfHandlingMode>(x, Localizer.HandlingName(language, x))).ToArray();
        _pdfHandling.SelectedValue = _settings.PdfHandlingMode;
        _ = RefreshHistoryAsync();
    }

    private static IReadOnlyList<DisplayOption<UiLanguage>> LanguageOptions() =>
    [
        new(UiLanguage.English, "English"), new(UiLanguage.Turkish, "Türkçe"),
        new(UiLanguage.German, "Deutsch"), new(UiLanguage.Spanish, "Español"),
        new(UiLanguage.Russian, "Русский"), new(UiLanguage.SimplifiedChinese, "简体中文")
    ];

    private static TextBlock AddRow(Grid grid, int row, UIElement control)
    {
        var text = new TextBlock { Margin = new Thickness(0, 5, 16, 10), VerticalAlignment = VerticalAlignment.Center };
        Grid.SetRow(text, row); Grid.SetColumn(text, 0); grid.Children.Add(text);
        if (control is FrameworkElement element) element.Margin = new Thickness(0, 0, 0, 10);
        Grid.SetRow(control, row); Grid.SetColumn(control, 1); grid.Children.Add(control);
        return text;
    }
    private static void Place(Grid grid, UIElement control, int row) { Grid.SetRow(control, row); Grid.SetColumn(control, 1); grid.Children.Add(control); }

    private static void SetStartup(bool enabled)
    {
        using var key = Registry.CurrentUser.CreateSubKey(@"Software\Microsoft\Windows\CurrentVersion\Run");
        if (enabled) key.SetValue("ArchuraWindrop", $"\"{Environment.ProcessPath}\"");
        else key.DeleteValue("ArchuraWindrop", false);
    }
}
