using System.IO;
using System.Windows;
using Polivoks.Core.Diagnostics;
using Polivoks.Desktop.Services;
using Polivoks.Resources.Localization;

namespace Polivoks.Desktop;

public partial class App : Application
{
    private AppServices? _services;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        StartupGuard.Register(this);

        try
        {
            var root = AppDataRoot.Resolve();
            Directory.CreateDirectory(root);
            AppLog.Initialize(root);
            AppLog.Info("Application startup.");

            _services = new AppServices(root);
            LocalizationManager.SetLanguage(_services.Settings.Language);
            AppLog.Info($"Language={_services.Settings.Language}, exclusive={_services.Settings.UseExclusiveMode}");

            var window = new MainWindow(_services);
            MainWindow = window;
            window.Show();
            AppLog.Info("Main window shown.");
        }
        catch (Exception ex)
        {
            AppLog.Error("Startup failed.", ex);
            MessageBox.Show(
                $"Polivoks failed to start:{Environment.NewLine}{Environment.NewLine}{ex.Message}",
                "Polivoks",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        AppLog.Info($"Application exit code={e.ApplicationExitCode}.");
        _services?.Dispose();
        base.OnExit(e);
    }
}
