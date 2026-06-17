using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Polivoks.Resources.Controls;

public sealed class VerticalFader : FrameworkElement
{
    private bool _dragging;

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(double), typeof(VerticalFader),
        new FrameworkPropertyMetadata(0.5, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged));

    private static void OnValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is VerticalFader fader)
        {
            fader.InvalidateVisual();
        }
    }

    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label), typeof(string), typeof(VerticalFader), new PropertyMetadata(string.Empty));

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public VerticalFader()
    {
        Width = 44;
        Height = 120;
        Margin = new Thickness(6, 4, 6, 2);
        Cursor = Cursors.SizeNS;
        MouseLeftButtonDown += (_, e) => { _dragging = true; CaptureMouse(); SetFromPoint(e.GetPosition(this)); };
        MouseMove += (_, e) => { if (_dragging) SetFromPoint(e.GetPosition(this)); };
        MouseLeftButtonUp += (_, _) => { _dragging = false; ReleaseMouseCapture(); };
    }

    private void SetFromPoint(Point p)
    {
        var trackTop = 8;
        var trackBottom = ActualHeight - 28;
        var y = Math.Clamp(p.Y, trackTop, trackBottom);
        Value = 1.0 - (y - trackTop) / (trackBottom - trackTop);
        InvalidateVisual();
    }

    protected override void OnRender(DrawingContext dc)
    {
        var trackX = ActualWidth / 2;
        var trackTop = 8.0;
        var trackBottom = ActualHeight - 28;
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(201, 162, 39)), 1), new Point(trackX, trackTop), new Point(trackX, trackBottom));

        var capY = trackTop + (1.0 - Value) * (trackBottom - trackTop);
        dc.DrawRectangle(
            new SolidColorBrush(Color.FromRgb(20, 20, 20)),
            new Pen(Brushes.Gold, 1),
            new Rect(trackX - 10, capY - 6, 20, 12));

        if (!string.IsNullOrWhiteSpace(Label))
        {
            var text = new FormattedText(
                Label,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI"),
                9,
                new SolidColorBrush(Color.FromRgb(201, 162, 39)),
                VisualTreeHelper.GetDpi(this).PixelsPerDip)
            {
                MaxTextWidth = ActualWidth,
                TextAlignment = TextAlignment.Center,
            };
            dc.DrawText(text, new Point(ActualWidth / 2 - text.Width / 2, ActualHeight - 22));
        }
    }
}
