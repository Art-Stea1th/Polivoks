using System.Windows;
using Polivoks.Core.Diagnostics;

namespace Polivoks.Desktop.Services;

public static class StartupGuard
{
    public static void Register(App app)
    {
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            AppLog.Error("Unhandled AppDomain exception.", args.ExceptionObject as Exception);
        };

        app.DispatcherUnhandledException += (_, args) =>
        {
            AppLog.Error("Unhandled dispatcher exception.", args.Exception);
            MessageBox.Show(
                $"Polivoks crashed during UI execution:{Environment.NewLine}{Environment.NewLine}{args.Exception.Message}",
                "Polivoks",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            args.Handled = true;
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            AppLog.Error("Unobserved task exception.", args.Exception);
            args.SetObserved();
        };
    }
}
