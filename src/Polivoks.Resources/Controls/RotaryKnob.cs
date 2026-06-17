using System.Globalization;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;

namespace Polivoks.Resources.Controls;

public sealed class RotaryKnob : FrameworkElement
{
    public const double DefaultKnobDiameter = 52;
    public const double DefaultLabelHeight = 28;

    private bool _dragging;
    private Point _lastPoint;

    public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(
        nameof(Value), typeof(double), typeof(RotaryKnob),
        new FrameworkPropertyMetadata(0.5, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnVisualChanged));

    public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(
        nameof(Minimum), typeof(double), typeof(RotaryKnob), new PropertyMetadata(0.0, OnVisualChanged));

    public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(
        nameof(Maximum), typeof(double), typeof(RotaryKnob), new PropertyMetadata(1.0, OnVisualChanged));

    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label), typeof(string), typeof(RotaryKnob), new PropertyMetadata(string.Empty, OnVisualChanged));

    public static readonly DependencyProperty KnobDiameterProperty = DependencyProperty.Register(
        nameof(KnobDiameter), typeof(double), typeof(RotaryKnob), new PropertyMetadata(DefaultKnobDiameter, OnVisualChanged));

    public static readonly DependencyProperty LargeKnobProperty = DependencyProperty.Register(
        nameof(LargeKnob), typeof(bool), typeof(RotaryKnob), new PropertyMetadata(false, OnVisualChanged));

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Minimum
    {
        get => (double)GetValue(MinimumProperty);
        set => SetValue(MinimumProperty, value);
    }

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public double KnobDiameter
    {
        get => (double)GetValue(KnobDiameterProperty);
        set => SetValue(KnobDiameterProperty, value);
    }

    public bool LargeKnob
    {
        get => (bool)GetValue(LargeKnobProperty);
        set => SetValue(LargeKnobProperty, value);
    }

    public RotaryKnob()
    {
        Margin = new Thickness(4, 6, 4, 2);
        Cursor = Cursors.SizeNS;
        Focusable = true;
        SnapsToDevicePixels = true;
        MouseLeftButtonDown += OnMouseDown;
        MouseMove += OnMouseMove;
        MouseLeftButtonUp += OnMouseUp;
        MouseWheel += OnMouseWheel;
    }

    private static void OnVisualChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is RotaryKnob knob)
        {
            knob.InvalidateVisual();
            knob.InvalidateMeasure();
        }
    }

    protected override Size MeasureOverride(Size availableSize)
    {
        var diameter = LargeKnob ? 68 : KnobDiameter;
        return new Size(diameter + 12, diameter + DefaultLabelHeight + 8);
    }

    protected override void OnRender(DrawingContext dc)
    {
        var diameter = LargeKnob ? 68 : KnobDiameter;
        var centerX = RenderSize.Width / 2;
        var centerY = diameter / 2 + 4;
        var center = new Point(centerX, centerY);
        var radius = diameter / 2;

        dc.DrawEllipse(
            new RadialGradientBrush(Color.FromRgb(28, 28, 28), Color.FromRgb(12, 12, 12))
            {
                Center = new Point(0.35, 0.35),
                RadiusX = 0.9,
                RadiusY = 0.9,
            },
            new Pen(new SolidColorBrush(Color.FromRgb(201, 162, 39)), 1.2),
            center,
            radius,
            radius);

        var norm = Maximum <= Minimum ? 0 : (Value - Minimum) / (Maximum - Minimum);
        var angle = (-135 + norm * 270) * Math.PI / 180;
        var pointerEnd = new Point(
            center.X + Math.Cos(angle) * (radius - 8),
            center.Y + Math.Sin(angle) * (radius - 8));
        dc.DrawLine(new Pen(new SolidColorBrush(Color.FromRgb(220, 70, 40)), 2.5), center, pointerEnd);
        dc.DrawEllipse(Brushes.Black, null, pointerEnd, 3, 3);

        if (!string.IsNullOrWhiteSpace(Label))
        {
            var text = new FormattedText(
                Label,
                CultureInfo.CurrentUICulture,
                FlowDirection.LeftToRight,
                new Typeface("Segoe UI Semibold"),
                10,
                new SolidColorBrush(Color.FromRgb(201, 162, 39)),
                VisualTreeHelper.GetDpi(this).PixelsPerDip)
            {
                MaxTextWidth = RenderSize.Width,
                TextAlignment = TextAlignment.Center,
            };
            dc.DrawText(text, new Point(centerX - text.Width / 2, diameter + 8));
        }
    }

    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        _dragging = true;
        _lastPoint = e.GetPosition(this);
        CaptureMouse();
        e.Handled = true;
    }

    private void OnMouseMove(object sender, MouseEventArgs e)
    {
        if (!_dragging)
        {
            return;
        }

        var point = e.GetPosition(this);
        var delta = _lastPoint.Y - point.Y;
        _lastPoint = point;
        Value = Math.Clamp(Value + delta * 0.006, Minimum, Maximum);
    }

    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        _dragging = false;
        ReleaseMouseCapture();
    }

    private void OnMouseWheel(object sender, MouseWheelEventArgs e)
    {
        var step = (Maximum - Minimum) * 0.025;
        Value = Math.Clamp(Value + (e.Delta > 0 ? step : -step), Minimum, Maximum);
    }
}
