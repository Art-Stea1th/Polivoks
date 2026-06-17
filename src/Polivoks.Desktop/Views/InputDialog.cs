using System.Windows;

namespace Polivoks.Desktop.Views;

public static class InputDialog
{
    public static string? Show(string title, string prompt, string defaultValue = "")
    {
        var window = new Window
        {
            Title = title,
            Width = 360,
            Height = 160,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            ResizeMode = ResizeMode.NoResize,
            Owner = Application.Current.MainWindow,
        };

        var promptBlock = new System.Windows.Controls.TextBlock { Text = prompt, Margin = new Thickness(12, 12, 12, 0) };
        var textBox = new System.Windows.Controls.TextBox { Text = defaultValue, Margin = new Thickness(12) };
        var panel = new System.Windows.Controls.StackPanel();
        panel.Children.Add(promptBlock);
        panel.Children.Add(textBox);

        string? result = null;
        var ok = new System.Windows.Controls.Button
        {
            Content = "OK",
            IsDefault = true,
            Margin = new Thickness(12, 0, 12, 12),
        };
        ok.Click += (_, _) =>
        {
            result = textBox.Text;
            window.DialogResult = true;
        };
        panel.Children.Add(ok);
        window.Content = panel;
        window.ShowDialog();
        return result;
    }
}
