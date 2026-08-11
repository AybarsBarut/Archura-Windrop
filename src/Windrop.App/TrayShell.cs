using System.Drawing;
using System.Windows;
using System.Windows.Threading;
using Windrop.Domain;
using Forms = System.Windows.Forms;

namespace Windrop.App;

public enum TrayStatus { Idle, Receiving, Error }

public sealed class TrayShell : IUserNotificationService, IDisposable
{
    private readonly Dispatcher _wpfDispatcher;
    private readonly Thread _trayThread;
    private readonly ManualResetEventSlim _ready = new(false);
    private readonly Icon _idle = SystemIcons.Information;
    private readonly Icon _receiving = SystemIcons.Shield;
    private readonly Icon _error = SystemIcons.Error;
    private Forms.Control? _marshalControl;
    private Forms.NotifyIcon? _icon;
    private Forms.ContextMenuStrip? _menu;
    private Forms.ToolStripItem? _openMenuItem;
    private Forms.ToolStripItem? _exitMenuItem;
    private Exception? _startupError;
    private int _disposed;
    private UiLanguage _language;

    public TrayShell(Dispatcher dispatcher, Action showWindow, Action exit, UiLanguage language)
    {
        _wpfDispatcher = dispatcher;
        _language = language;
        _trayThread = new Thread(() => RunTrayLoop(showWindow, exit))
        {
            IsBackground = true,
            Name = "Windrop Tray UI"
        };
        _trayThread.SetApartmentState(ApartmentState.STA);
        _trayThread.Start();
        if (!_ready.Wait(TimeSpan.FromSeconds(5)))
            throw new TimeoutException("The tray icon message loop did not start.");
        if (_startupError is not null)
            throw new InvalidOperationException("The tray icon could not start.", _startupError);
    }

    private void RunTrayLoop(Action showWindow, Action exit)
    {
        try
        {
            Forms.Application.SetHighDpiMode(Forms.HighDpiMode.PerMonitorV2);
            _marshalControl = new Forms.Control();
            _marshalControl.CreateControl();
            _menu = new Forms.ContextMenuStrip { ShowImageMargin = false };
            _openMenuItem = _menu.Items.Add(Localizer.Get(_language, UiText.OpenWindrop), null,
                (_, _) => _wpfDispatcher.BeginInvoke(showWindow));
            _menu.Items.Add(new Forms.ToolStripSeparator());
            _exitMenuItem = _menu.Items.Add(Localizer.Get(_language, UiText.Exit), null,
                (_, _) => _wpfDispatcher.BeginInvoke(exit));
            _icon = new Forms.NotifyIcon
            {
                Icon = _idle,
                Text = "Archura Windrop",
                Visible = true,
                ContextMenuStrip = _menu
            };
            _icon.DoubleClick += (_, _) => _wpfDispatcher.BeginInvoke(showWindow);
            _ready.Set();
            Forms.Application.Run();
        }
        catch (Exception ex)
        {
            _startupError = ex;
            _ready.Set();
        }
        finally
        {
            if (_icon is not null) { _icon.Visible = false; _icon.Dispose(); }
            _menu?.Dispose();
            _marshalControl?.Dispose();
        }
    }

    public void SetStatus(TrayStatus status, string text) => PostToTray(() =>
    {
        if (_icon is null) return;
        _icon.Icon = status switch { TrayStatus.Receiving => _receiving, TrayStatus.Error => _error, _ => _idle };
        _icon.Text = text.Length > 63 ? text[..63] : text;
    });

    public void SetLanguage(UiLanguage language)
    {
        _language = language;
        PostToTray(() =>
        {
            if (_openMenuItem is not null) _openMenuItem.Text = Localizer.Get(language, UiText.OpenWindrop);
            if (_exitMenuItem is not null) _exitMenuItem.Text = Localizer.Get(language, UiText.Exit);
        });
    }

    public void Received(ReceivedItem item) => PostToTray(() =>
    {
        if (_icon is null) return;
        _icon.Icon = _idle;
        _icon.Text = Localizer.Get(_language, UiText.Ready);
        _icon.ShowBalloonTip(4000, Localizer.Get(_language, UiText.ContentReceived),
            Localizer.Format(_language, UiText.ContentReceivedBody, item.DisplayName), Forms.ToolTipIcon.Info);
    });

    public void Error(string message) => PostToTray(() =>
    {
        if (_icon is null) return;
        _icon.Icon = _error;
        _icon.Text = message.Length > 63 ? message[..63] : message;
        _icon.ShowBalloonTip(5000, Localizer.Get(_language, UiText.Error), message, Forms.ToolTipIcon.Error);
    });

    private void PostToTray(Action action)
    {
        if (Volatile.Read(ref _disposed) != 0) return;
        var control = _marshalControl;
        if (control is null || control.IsDisposed || !control.IsHandleCreated) return;
        try
        {
            if (control.InvokeRequired) control.BeginInvoke(action);
            else action();
        }
        catch (InvalidOperationException) when (Volatile.Read(ref _disposed) != 0) { }
    }

    public async Task<bool> ApproveAsync(string source, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        await _wpfDispatcher.InvokeAsync(() =>
        {
            var window = new Window
            {
                Title = Localizer.Get(_language, UiText.ApprovalTitle), Width = 430, Height = 190,
                WindowStartupLocation = WindowStartupLocation.CenterScreen, Topmost = true,
                ResizeMode = ResizeMode.NoResize, ShowInTaskbar = true
            };
            var panel = new System.Windows.Controls.StackPanel { Margin = new Thickness(20) };
            var text = new System.Windows.Controls.TextBlock
            {
                Text = $"{Localizer.Format(_language, UiText.ApprovalQuestion, source)}\n{Localizer.Format(_language, UiText.ApprovalCountdown, timeout.Seconds)}",
                TextWrapping = TextWrapping.Wrap
            };
            var buttons = new System.Windows.Controls.StackPanel { Orientation = System.Windows.Controls.Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Margin = new Thickness(0, 20, 0, 0) };
            var decline = new System.Windows.Controls.Button { Content = Localizer.Get(_language, UiText.Decline), Width = 100, Margin = new Thickness(0, 0, 8, 0) };
            var accept = new System.Windows.Controls.Button { Content = Localizer.Get(_language, UiText.Accept), Width = 100, IsDefault = true };
            buttons.Children.Add(decline); buttons.Children.Add(accept); panel.Children.Add(text); panel.Children.Add(buttons); window.Content = panel;
            var seconds = Math.Max(1, (int)timeout.TotalSeconds);
            var timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Normal, (_, _) =>
            {
                seconds--;
                text.Text = $"{Localizer.Format(_language, UiText.ApprovalQuestion, source)}\n{Localizer.Format(_language, UiText.ApprovalCountdown, seconds)}";
                if (seconds <= 0) { completion.TrySetResult(true); window.Close(); }
            }, _wpfDispatcher);
            accept.Click += (_, _) => { completion.TrySetResult(true); window.Close(); };
            decline.Click += (_, _) => { completion.TrySetResult(false); window.Close(); };
            window.Closed += (_, _) => { timer.Stop(); completion.TrySetResult(false); };
            timer.Start(); window.Show();
        });
        using var registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
        return await completion.Task;
    }

    public async Task<PdfHandlingChoice> ChoosePdfHandlingAsync(
        PdfHandlingContext context, TimeSpan timeout, CancellationToken cancellationToken = default)
    {
        var completion = new TaskCompletionSource<PdfHandlingChoice>(TaskCreationOptions.RunContinuationsAsynchronously);
        await _wpfDispatcher.InvokeAsync(() =>
        {
            var window = new Window
            {
                Title = Localizer.Get(_language, UiText.PdfChoiceTitle), Width = 560, Height = 290,
                WindowStartupLocation = WindowStartupLocation.CenterScreen, Topmost = true,
                ResizeMode = ResizeMode.NoResize, ShowInTaskbar = true
            };
            var panel = new System.Windows.Controls.StackPanel { Margin = new Thickness(22) };
            var summary = new System.Windows.Controls.TextBlock
            {
                Text = Localizer.Format(_language,
                    context.HasVisualContent ? UiText.PdfSummaryVisual : UiText.PdfSummaryText,
                    Math.Max(1, context.PageCount)),
                FontWeight = FontWeights.SemiBold,
                TextWrapping = TextWrapping.Wrap
            };
            var preview = new System.Windows.Controls.TextBlock
            {
                Text = context.TextPreview is null
                    ? Localizer.Get(_language, UiText.NoText)
                    : Localizer.Format(_language, UiText.TextPreview, context.TextPreview),
                TextWrapping = TextWrapping.Wrap,
                MaxHeight = 70,
                Margin = new Thickness(0, 10, 0, 16)
            };
            var countdown = new System.Windows.Controls.TextBlock { Margin = new Thickness(0, 0, 0, 10) };
            var buttons = new System.Windows.Controls.StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            var automatic = new System.Windows.Controls.Button { Content = Localizer.Get(_language, UiText.Automatic), Width = 110, IsDefault = true };
            var image = new System.Windows.Controls.Button { Content = Localizer.Get(_language, UiText.SaveImage), Width = 135, Margin = new Thickness(8, 0, 0, 0) };
            var textOnly = new System.Windows.Controls.Button
            {
                Content = Localizer.Get(_language, UiText.TextOnly), Width = 135, Margin = new Thickness(8, 0, 0, 0), IsEnabled = context.HasText
            };
            buttons.Children.Add(automatic); buttons.Children.Add(image); buttons.Children.Add(textOnly);
            panel.Children.Add(summary); panel.Children.Add(preview); panel.Children.Add(countdown); panel.Children.Add(buttons);
            window.Content = panel;

            var seconds = Math.Max(1, (int)timeout.TotalSeconds);
            countdown.Text = Localizer.Format(_language, UiText.AutoCountdown, seconds);
            var timer = new DispatcherTimer(TimeSpan.FromSeconds(1), DispatcherPriority.Normal, (_, _) =>
            {
                seconds--;
                countdown.Text = Localizer.Format(_language, UiText.AutoCountdown, seconds);
                if (seconds <= 0) { completion.TrySetResult(PdfHandlingChoice.Automatic); window.Close(); }
            }, _wpfDispatcher);
            automatic.Click += (_, _) => { completion.TrySetResult(PdfHandlingChoice.Automatic); window.Close(); };
            image.Click += (_, _) => { completion.TrySetResult(PdfHandlingChoice.Image); window.Close(); };
            textOnly.Click += (_, _) => { completion.TrySetResult(PdfHandlingChoice.TextOnly); window.Close(); };
            window.Closed += (_, _) => { timer.Stop(); completion.TrySetResult(PdfHandlingChoice.Automatic); };
            timer.Start(); window.Show();
        });
        using var registration = cancellationToken.Register(() => completion.TrySetCanceled(cancellationToken));
        return await completion.Task;
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        var control = _marshalControl;
        if (control is not null && !control.IsDisposed && control.IsHandleCreated)
        {
            try
            {
                control.BeginInvoke(() =>
                {
                    if (_icon is not null) _icon.Visible = false;
                    Forms.Application.ExitThread();
                });
            }
            catch (InvalidOperationException) { }
        }
        if (Thread.CurrentThread != _trayThread) _trayThread.Join(TimeSpan.FromSeconds(3));
        _ready.Dispose();
    }
}
