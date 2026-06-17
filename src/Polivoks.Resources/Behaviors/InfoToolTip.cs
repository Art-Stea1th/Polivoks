using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Polivoks.Resources.Behaviors;

public static class InfoToolTip
{
    public static readonly DependencyProperty TextProperty = DependencyProperty.RegisterAttached(
        "Text",
        typeof(string),
        typeof(InfoToolTip),
        new PropertyMetadata(null, OnTextChanged));

    private static readonly DependencyProperty PopupProperty = DependencyProperty.RegisterAttached(
        "Popup",
        typeof(Popup),
        typeof(InfoToolTip),
        new PropertyMetadata(null));

    public static void SetText(DependencyObject element, string value) => element.SetValue(TextProperty, value);
    public static string GetText(DependencyObject element) => (string)element.GetValue(TextProperty)!;

    private static void OnTextChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not FrameworkElement element)
        {
            return;
        }

        element.MouseEnter -= ElementOnMouseEnter;
        element.MouseLeave -= ElementOnMouseLeave;
        element.MouseEnter += ElementOnMouseEnter;
        element.MouseLeave += ElementOnMouseLeave;
    }

    private static void ElementOnMouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is not FrameworkElement element)
        {
            return;
        }

        var text = GetText(element);
        if (string.IsNullOrWhiteSpace(text))
        {
            return;
        }

        ClosePopup(element);
        var popup = new Popup
        {
            AllowsTransparency = true,
            StaysOpen = true,
            Placement = PlacementMode.Right,
            PlacementTarget = element,
            IsOpen = true,
            Child = new Border
            {
                Background = System.Windows.Media.Brushes.Black,
                BorderBrush = System.Windows.Media.Brushes.Gold,
                BorderThickness = new Thickness(1),
                Padding = new Thickness(8),
                MaxWidth = 320,
                Child = new TextBlock
                {
                    Text = text,
                    Foreground = System.Windows.Media.Brushes.White,
                    TextWrapping = TextWrapping.Wrap,
                },
            },
        };

        element.SetValue(PopupProperty, popup);
    }

    private static void ElementOnMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
    {
        if (sender is FrameworkElement element)
        {
            ClosePopup(element);
        }
    }

    private static void ClosePopup(FrameworkElement element)
    {
        if (element.GetValue(PopupProperty) is Popup popup)
        {
            popup.IsOpen = false;
            element.SetValue(PopupProperty, null);
        }
    }
}
