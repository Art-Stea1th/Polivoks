using System.Windows;
using System.Windows.Threading;
using Polivoks.Core.Diagnostics;
using Polivoks.Core.Models;
using Polivoks.Desktop.Services;
using Polivoks.Desktop.ViewModels;
using Polivoks.Resources.Localization;

namespace Polivoks.Desktop;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _syncTimer;
    private readonly AppServices _services;
    private long _lastLoggedRenderVersion;
    private DateTime _nextRenderLogAt = DateTime.MinValue;

    public MainWindow(AppServices services)
    {
        _services = services;

        try
        {
            AppLog.Info("Initializing main window UI.");
            InitializeComponent();
            DataContext = new MainViewModel(services);
            LocalizationManager.SetLanguage(services.Settings.Language);
            AppLog.Info("Main window UI initialized.");
        }
        catch (Exception ex)
        {
            AppLog.Error("Main window UI initialization failed.", ex);
            throw;
        }

        Panel.NotePressed += (_, note) => _services.Engine.NoteOn(note);
        Panel.NoteReleased += (_, note) => _services.Engine.NoteOff(note);
        Panel.PatchEdited += (_, _) =>
        {
            if (DataContext is MainViewModel vm)
            {
                vm.MarkPatchDirty();
            }
        };

        _syncTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(80) };
        _syncTimer.Tick += (_, _) =>
        {
            if (DataContext is MainViewModel vm)
            {
                vm.SyncPatchToEngine();
                vm.RefreshRuntimeInfo();
                LogRenderStatsIfNeeded();
            }
        };
        _syncTimer.Start();

        ContentRendered += OnContentRendered;
        Closing += OnClosing;

        ApplySavedWindowPlacement(services.Settings);
    }

    private void OnContentRendered(object? sender, EventArgs e)
    {
        ContentRendered -= OnContentRendered;
        Panel.Redraw();

        try
        {
            _services.Output.Start();
            AppLog.Info("Audio output started.");
            if (DataContext is MainViewModel vm)
            {
                vm.StatusMessage = "Ready.";
            }
        }
        catch (Exception ex)
        {
            AppLog.Error("Audio output failed to start.", ex);
            if (DataContext is MainViewModel vm)
            {
                vm.StatusMessage = $"Audio error: {ex.Message}. Open Settings to change device/mode.";
            }
        }
    }

    private void OnMinimizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void OnMaximizeRestoreClick(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    private void LogRenderStatsIfNeeded()
    {
        if (Panel.RenderVersion == _lastLoggedRenderVersion || DateTime.UtcNow < _nextRenderLogAt)
        {
            return;
        }

        _lastLoggedRenderVersion = Panel.RenderVersion;
        _nextRenderLogAt = DateTime.UtcNow.AddSeconds(5);
        AppLog.Info(
            $"Panel render: mode={Panel.LastRenderMode}, last={Panel.LastRenderMilliseconds:F1} ms, " +
            $"full={Panel.LastFullRenderMilliseconds:F1} ms, interaction={Panel.LastInteractionRenderMilliseconds:F1} ms");
    }

    private void OnClosing(object? sender, System.ComponentModel.CancelEventArgs e)
    {
        _syncTimer.Stop();
        if (DataContext is MainViewModel vm)
        {
            vm.SyncPatchToEngine();
        }

        SaveWindowPlacement(_services.Settings);
        _services.SaveAll();
        _services.Output.Stop();
    }

    private void ApplySavedWindowPlacement(AppSettings settings)
    {
        var screen = GetVirtualScreenBounds();
        Width = ClampFinite(settings.WindowWidth, MinWidth, screen.Width);
        Height = ClampFinite(settings.WindowHeight, MinHeight, screen.Height);

        var centeredLeft = screen.Left + (screen.Width - Width) / 2;
        var centeredTop = screen.Top + (screen.Height - Height) / 2;
        Left = ClampWindowAxis(settings.WindowLeft, centeredLeft, Width, screen.Left, screen.Right);
        Top = ClampWindowAxis(settings.WindowTop, centeredTop, Height, screen.Top, screen.Bottom);
    }

    private void SaveWindowPlacement(AppSettings settings)
    {
        var bounds = WindowState == WindowState.Normal
            ? new Rect(Left, Top, ActualWidth, ActualHeight)
            : RestoreBounds;

        if (!IsUsableBounds(bounds))
        {
            return;
        }

        settings.WindowWidth = Math.Max(MinWidth, bounds.Width);
        settings.WindowHeight = Math.Max(MinHeight, bounds.Height);
        settings.WindowLeft = bounds.Left;
        settings.WindowTop = bounds.Top;
    }

    private static Rect GetVirtualScreenBounds() => SystemParameters.WorkArea;

    private static double ClampFinite(double value, double min, double max) =>
        double.IsFinite(value) ? Math.Clamp(value, min, max) : min;

    private static double ClampWindowAxis(double value, double fallback, double size, double min, double max)
    {
        var preferred = double.IsFinite(value) ? value : fallback;
        var upper = max - size;
        return upper <= min ? min : Math.Clamp(preferred, min, upper);
    }

    private static bool IsUsableBounds(Rect bounds) =>
        !bounds.IsEmpty
        && double.IsFinite(bounds.Left)
        && double.IsFinite(bounds.Top)
        && double.IsFinite(bounds.Width)
        && double.IsFinite(bounds.Height)
        && bounds.Width > 0
        && bounds.Height > 0;
}
