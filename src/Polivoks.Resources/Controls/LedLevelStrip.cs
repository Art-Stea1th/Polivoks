using System.Globalization;
using System.Windows;
using System.Windows.Media;

namespace Polivoks.Resources.Controls;

public sealed class LedLevelStrip : FrameworkElement
{
    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value),
        typeof(double),
        typeof(LedLevelStrip),
        new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.AffectsRender));

    public static readonly DependencyProperty LedCountProperty = DependencyProperty.Register(
        nameof(LedCount),
        typeof(int),
        typeof(LedLevelStrip),
        new PropertyMetadata(10, (_, _) => { }));

    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label),
        typeof(string),
        typeof(LedLevelStrip),
        new PropertyMetadata(string.Empty, (_, _) => { }));

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public int LedCount
    {
        get => (int)GetValue(LedCountProperty);
        set => SetValue(LedCountProperty, value);
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public LedLevelStrip()
    {
        Height = 36;
        Margin = new Thickness(4, 8, 4, 4);
    }

    protected override Size MeasureOverride(Size availableSize) => new(Math.Max(120, LedCount * 12 + 8), 36);

    protected override void OnRender(DrawingContext dc)
    {
        var count = Math.Max(1, LedCount);
        var lit = (int)Math.Round(Math.Clamp(Value, 0, 1) * count);
        var ledSize = Math.Min(10, (ActualWidth - 8) / count - 2);
        var startX = (ActualWidth - count * (ledSize + 2)) / 2;

        for (var i = 0; i < count; i++)
        {
            var on = i < lit;
            var brush = on
                ? new SolidColorBrush(Color.FromRgb(220, 40, 30))
                : new SolidColorBrush(Color.FromRgb(50, 18, 14));
            var x = startX + i * (ledSize + 2);
            dc.DrawRoundedRectangle(brush, new Pen(Brushes.Black, 0.5), new Rect(x, 10, ledSize, ledSize), 1, 1);
        }

        if (!string.IsNullOrWhiteSpace(Label))
        {
            var text = new FormattedText(
                Label,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI Semibold"),
                8,
                new SolidColorBrush(Color.FromRgb(201, 162, 39)),
                VisualTreeHelper.GetDpi(this).PixelsPerDip)
            {
                MaxTextWidth = ActualWidth,
                TextAlignment = TextAlignment.Center,
            };
            dc.DrawText(text, new Point(ActualWidth / 2 - text.Width / 2, 0));
        }
    }
}
