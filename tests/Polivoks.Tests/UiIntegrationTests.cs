using System.Windows;
using Polivoks.Core.Diagnostics;
using Polivoks.Desktop;
using Polivoks.Desktop.Services;
using Polivoks.Resources.Controls;
using Polivoks.Resources.Rendering;
using Xunit;

namespace Polivoks.Tests;

public class UiIntegrationTests
{
    [Fact]
    public void WriteableBitmap_panel_and_main_window_start_without_errors()
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                var app = new Application();
                app.Resources.MergedDictionaries.Add(new ResourceDictionary
                {
                    Source = new Uri("pack://application:,,,/Polivoks.Resources;component/Themes/Generic.xaml"),
                });

                var panel = new SynthPanelView { Patch = new Polivoks.Core.Models.SynthPatch(), Width = 1280, Height = 680 };
                panel.Measure(new Size(1280, 680));
                panel.Redraw();
                Assert.Equal(SynthPanelLayout.DesignWidth, panel.DesiredSize.Width);

                var root = Path.Combine(Path.GetTempPath(), "polivoks-tests", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(root);
                AppLog.Initialize(root);
                try
                {
                    using var services = new AppServices(root);
                    var window = new MainWindow(services);
                    window.ShowInTaskbar = false;
                    window.Show();
                    window.UpdateLayout();
                    window.Close();
                }
                finally
                {
                    Directory.Delete(root, recursive: true);
                }

                app.Shutdown();
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });

        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        thread.Join();

        if (error is not null)
        {
            throw error;
        }
    }
}
