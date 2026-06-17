using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using System.Diagnostics;
using Polivoks.Core.Models;
using Polivoks.Resources.Localization;
using Polivoks.Resources.Rendering;

namespace Polivoks.Resources.Controls;

public sealed class SynthPanelView : FrameworkElement
{
    private const int RenderScale = 2;
    private const double TooltipMaxWidth = 360;
    private const double TooltipOffset = 18;
    private static readonly TimeSpan TooltipShowDelay = TimeSpan.FromMilliseconds(500);
    private static readonly TimeSpan TooltipLifetime = TimeSpan.FromSeconds(10);

    public static readonly DependencyProperty PatchProperty = DependencyProperty.Register(
        nameof(Patch),
        typeof(SynthPatch),
        typeof(SynthPanelView),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, OnPatchChanged));

    private readonly IReadOnlyList<SynthWidget> _widgets = SynthPanelLayout.BuildWidgets();
    private readonly IReadOnlyList<SynthInfoZone> _infoZones = SynthPanelLayout.BuildInfoZones();
    private BitmapCanvas? _canvas;
    private BitmapCanvas? _staticCanvas;
    private bool _staticDirty = true;
    private SynthWidget? _activeWidget;
    private string? _hoverInfoId;
    private string? _hoverTooltipText;
    private IHoverInfo? _pendingHoverInfo;
    private string? _pendingHoverInfoId;
    private Point _tooltipPoint;
    private Point _pendingTooltipPoint;
    private Point _lastPoint;
    private long _lastRedrawMs;
    private bool _redrawPending;
    private readonly DispatcherTimer _tooltipShowTimer;
    private readonly DispatcherTimer _tooltipTimer;
    private readonly HashSet<int> _pressedKeys = [];

    public double LastRenderMilliseconds { get; private set; }

    public double LastFullRenderMilliseconds { get; private set; }

    public double LastInteractionRenderMilliseconds { get; private set; }

    public string LastRenderMode { get; private set; } = "none";

    public long RenderVersion { get; private set; }

    public SynthPatch? Patch
    {
        get => (SynthPatch?)GetValue(PatchProperty);
        set => SetValue(PatchProperty, value);
    }

    public event EventHandler<int>? NotePressed;

    public event EventHandler<int>? NoteReleased;

    public event EventHandler? PatchEdited;

    public SynthPanelView()
    {
        Focusable = true;
        SnapsToDevicePixels = true;
        ClipToBounds = true;
        Cursor = Cursors.Arrow;
        RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.Fant);
        _canvas = new BitmapCanvas(SynthPanelLayout.DesignWidth, SynthPanelLayout.DesignHeight, RenderScale);
        _staticCanvas = new BitmapCanvas(SynthPanelLayout.DesignWidth, SynthPanelLayout.DesignHeight, RenderScale);
        _tooltipShowTimer = new DispatcherTimer { Interval = TooltipShowDelay };
        _tooltipShowTimer.Tick += (_, _) => ShowPendingTooltip();
        _tooltipTimer = new DispatcherTimer { Interval = TooltipLifetime };
        _tooltipTimer.Tick += (_, _) => ClearHoverTooltip();
        LocalizationManager.LanguageChanged += (_, _) =>
        {
            ClearHoverTooltip();
            _staticDirty = true;
            Redraw();
        };
        Loaded += (_, _) => Redraw();
    }

    private static void OnPatchChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is SynthPanelView view)
        {
            view.Redraw();
        }
    }

    public void Redraw()
    {
        Redraw(SynthRenderMode.Full);
    }

    private void Redraw(SynthRenderMode mode)
    {
        if (_canvas is null || Patch is null)
        {
            return;
        }

        var stopwatch = Stopwatch.StartNew();
        EnsureStaticLayer();
        if (_staticCanvas is not null)
        {
            _canvas.CopyFrom(_staticCanvas);
        }

        SynthPanelRenderer.RenderDynamic(_canvas, Patch, _widgets, _pressedKeys, mode);
        stopwatch.Stop();

        LastRenderMilliseconds = stopwatch.Elapsed.TotalMilliseconds;
        LastRenderMode = mode == SynthRenderMode.Full ? "full" : "interaction";
        RenderVersion++;
        if (mode == SynthRenderMode.Full)
        {
            LastFullRenderMilliseconds = LastRenderMilliseconds;
        }
        else
        {
            LastInteractionRenderMilliseconds = LastRenderMilliseconds;
        }

        _lastRedrawMs = Environment.TickCount64;
        InvalidateVisual();
    }

    private void EnsureStaticLayer()
    {
        if (!_staticDirty || _staticCanvas is null)
        {
            return;
        }

        SynthPanelRenderer.RenderStatic(_staticCanvas);
        _staticDirty = false;
    }

    private void RedrawThrottled()
    {
        var now = Environment.TickCount64;
        if (now - _lastRedrawMs < 16)
        {
            if (!_redrawPending)
            {
                _redrawPending = true;
                Dispatcher.BeginInvoke(() =>
                {
                    _redrawPending = false;
                    Redraw(SynthRenderMode.Interaction);
                }, System.Windows.Threading.DispatcherPriority.Render);
            }

            return;
        }

        Redraw(SynthRenderMode.Interaction);
    }

    protected override Size MeasureOverride(Size availableSize) =>
        new(SynthPanelLayout.DesignWidth, SynthPanelLayout.DesignHeight);

    protected override void OnRender(DrawingContext dc)
    {
        if (_canvas is null)
        {
            return;
        }

        dc.DrawImage(_canvas.Bitmap, new Rect(0, 0, ActualWidth, ActualHeight));
        DrawHoverTooltip(dc);
    }

    protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
    {
        base.OnRenderSizeChanged(sizeInfo);
        RedrawThrottled();
    }

    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        CaptureMouse();
        var point = ToDesign(e.GetPosition(this));
        _lastPoint = point;
        _activeWidget = HitTest(point);
        if (_activeWidget is null || Patch is null)
        {
            return;
        }

        switch (_activeWidget.Kind)
        {
            case WidgetKind.EnumButton:
                PatchEditor.SetEnumIndex(Patch, _activeWidget.Id, _activeWidget.EnumIndex);
                PatchEdited?.Invoke(this, EventArgs.Empty);
                Redraw();
                break;
            case WidgetKind.Toggle:
                PatchEditor.ToggleBool(Patch, _activeWidget.Id);
                PatchEdited?.Invoke(this, EventArgs.Empty);
                Redraw();
                break;
            case WidgetKind.PianoKey:
                PressPianoKey(_activeWidget);
                break;
            case WidgetKind.Knob:
            case WidgetKind.Fader:
                ApplyDrag(_activeWidget, point);
                break;
        }

        e.Handled = true;
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        var point = ToDesign(e.GetPosition(this));

        if (_activeWidget?.Kind is WidgetKind.Knob or WidgetKind.Fader && Patch is not null && IsMouseCaptured)
        {
            ApplyDrag(_activeWidget, point);
            _lastPoint = point;
            e.Handled = true;
            return;
        }

        if (_activeWidget?.Kind == WidgetKind.PianoKey && Patch is not null && IsMouseCaptured)
        {
            var key = HitTest(point);
            if (key?.Kind == WidgetKind.PianoKey && key.MidiNote != _activeWidget.MidiNote)
            {
                ReleaseKey(_activeWidget.MidiNote);
                _activeWidget = key;
                PressPianoKey(key);
            }

            e.Handled = true;
            return;
        }

        UpdateHoverTooltip(point, e.GetPosition(this));
    }

    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        if (_activeWidget?.Kind == WidgetKind.PianoKey)
        {
            ReleaseKey(_activeWidget.MidiNote);
        }
        else
        {
            ReleaseAllKeys();
        }

        if (_activeWidget?.Kind is WidgetKind.Knob or WidgetKind.Fader)
        {
            Redraw();
        }

        _activeWidget = null;
        if (IsMouseCaptured)
        {
            ReleaseMouseCapture();
        }

        e.Handled = true;
    }

    protected override void OnMouseLeave(MouseEventArgs e)
    {
        UpdateHoverTooltip(null, null);

        if (!IsMouseCaptured)
        {
            return;
        }

        ReleaseAllKeys();
        _activeWidget = null;
        ReleaseMouseCapture();
    }

    protected override void OnLostMouseCapture(MouseEventArgs e)
    {
        ReleaseAllKeys();
        _activeWidget = null;
    }

    private void PressPianoKey(SynthWidget key)
    {
        if (Patch is null || key.Kind != WidgetKind.PianoKey)
        {
            return;
        }

        if (_pressedKeys.Add(key.MidiNote))
        {
            NotePressed?.Invoke(this, key.MidiNote);
        }

        RedrawThrottled();
    }

    private void ApplyDrag(SynthWidget widget, Point point)
    {
        if (Patch is null)
        {
            return;
        }

        if (widget.Kind == WidgetKind.Knob)
        {
            var delta = _lastPoint.Y - point.Y;
            var value = PatchEditor.GetDouble(Patch, widget.Id) + delta * 0.004;
            PatchEditor.SetDouble(Patch, widget.Id, value);
        }
        else if (widget.Kind == WidgetKind.Fader)
        {
            var top = widget.Bounds.Y + 8;
            var bottom = widget.Bounds.Y + widget.Bounds.Height - 24;
            var t = 1.0 - (point.Y - top) / (bottom - top);
            PatchEditor.SetDouble(Patch, widget.Id, t);
        }

        PatchEdited?.Invoke(this, EventArgs.Empty);
        RedrawThrottled();
    }

    private void ReleaseAllKeys()
    {
        foreach (var note in _pressedKeys.ToArray())
        {
            ReleaseKey(note);
        }
    }

    private void ReleaseKey(int midiNote)
    {
        if (!_pressedKeys.Remove(midiNote))
        {
            return;
        }

        NoteReleased?.Invoke(this, midiNote);
        Redraw(SynthRenderMode.Interaction);
    }

    private void UpdateHoverTooltip(Point? designPoint, Point? viewPoint)
    {
        var info = designPoint is null ? null : HitTestInfo(designPoint.Value);
        var infoId = info?.Id;

        if (infoId == _hoverInfoId && viewPoint is not null)
        {
            _tooltipPoint = viewPoint.Value;
            InvalidateVisual();
            return;
        }

        if (infoId == _pendingHoverInfoId && viewPoint is not null)
        {
            _pendingTooltipPoint = viewPoint.Value;
            return;
        }

        _tooltipShowTimer.Stop();
        _pendingHoverInfo = info;
        _pendingHoverInfoId = infoId;
        _pendingTooltipPoint = viewPoint ?? default;

        if (info is null)
        {
            ClearHoverTooltip();
            return;
        }

        ClearVisibleTooltip();
        _tooltipShowTimer.Start();
    }

    private void ShowPendingTooltip()
    {
        _tooltipShowTimer.Stop();
        var info = _pendingHoverInfo;
        if (info is null)
        {
            return;
        }

        _hoverInfoId = _pendingHoverInfoId;
        _hoverTooltipText = info switch
        {
            WidgetHoverInfo widgetInfo => ResolveTooltip(widgetInfo.Widget),
            ZoneHoverInfo zoneInfo => ResolveTooltip(zoneInfo.Zone),
            _ => null,
        };
        _tooltipPoint = _pendingTooltipPoint;
        RestartTooltipTimer();
        InvalidateVisual();
    }

    private void RestartTooltipTimer()
    {
        _tooltipTimer.Stop();
        if (!string.IsNullOrWhiteSpace(_hoverTooltipText))
        {
            _tooltipTimer.Start();
        }
    }

    private void ClearHoverTooltip()
    {
        _tooltipShowTimer.Stop();
        _pendingHoverInfo = null;
        _pendingHoverInfoId = null;
        ClearVisibleTooltip();
    }

    private void ClearVisibleTooltip()
    {
        _tooltipTimer.Stop();
        if (_hoverInfoId is null && _hoverTooltipText is null)
        {
            return;
        }

        _hoverInfoId = null;
        _hoverTooltipText = null;
        InvalidateVisual();
    }

    private static string ResolveTooltip(SynthWidget widget)
    {
        var loc = LocalizationManager.Current;
        if (widget.Kind == WidgetKind.PianoKey)
        {
            return string.Format(loc["TipPianoKey"], GetNoteName(widget.MidiNote), widget.MidiNote);
        }

        var tipKey = "Tip" + widget.Id;
        var tip = loc[tipKey];
        if (tip != tipKey && widget.Kind == WidgetKind.EnumButton)
        {
            return $"{tip}\n{string.Format(loc["TipEnumChoice"], ResolveEnumChoice(widget))}";
        }

        if (tip != tipKey)
        {
            return tip;
        }

        var labelKey = widget.Label ?? widget.Id;
        var label = loc[labelKey];
        return label != labelKey ? label : widget.Id;
    }

    private static string ResolveTooltip(SynthInfoZone zone)
    {
        var loc = LocalizationManager.Current;
        var tipKey = "Tip" + zone.Id;
        var tip = loc[tipKey];
        return tip != tipKey ? tip : zone.Id;
    }

    private static string ResolveEnumChoice(SynthWidget widget)
    {
        var loc = LocalizationManager.Current;
        var key = widget.Id switch
        {
            "LfoWaveform" => $"LfoWaveform{widget.EnumIndex}",
            "Osc1Footage" or "Osc2Footage" => $"OscFootage{widget.EnumIndex}",
            "Osc1Waveform" or "Osc2Waveform" => $"OscWaveform{widget.EnumIndex}",
            "FilterMode" => $"FilterMode{widget.EnumIndex}",
            _ => string.Empty,
        };

        return string.IsNullOrEmpty(key) ? widget.EnumIndex.ToString() : loc[key];
    }

    private static string GetNoteName(int midiNote)
    {
        string[] names = ["C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"];
        var octave = midiNote / 12 - 1;
        return names[Math.Clamp(midiNote % 12, 0, names.Length - 1)] + octave;
    }

    private SynthWidget? HitTest(Point point)
    {
        var x = (int)point.X;
        var y = (int)point.Y;
        for (var i = _widgets.Count - 1; i >= 0; i--)
        {
            var w = _widgets[i];
            var b = w.Bounds;
            if (x >= b.X && x < b.X + b.Width && y >= b.Y && y < b.Y + b.Height)
            {
                return w;
            }
        }

        return null;
    }

    private IHoverInfo? HitTestInfo(Point point)
    {
        if (HitTest(point) is { } widget)
        {
            return new WidgetHoverInfo(widget);
        }

        var x = (int)point.X;
        var y = (int)point.Y;
        for (var i = _infoZones.Count - 1; i >= 0; i--)
        {
            var zone = _infoZones[i];
            var b = zone.Bounds;
            if (x >= b.X && x < b.X + b.Width && y >= b.Y && y < b.Y + b.Height)
            {
                return new ZoneHoverInfo(zone);
            }
        }

        return null;
    }

    private void DrawHoverTooltip(DrawingContext dc)
    {
        if (string.IsNullOrWhiteSpace(_hoverTooltipText) || ActualWidth <= 0 || ActualHeight <= 0)
        {
            return;
        }

        var dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        var text = new FormattedText(
            _hoverTooltipText,
            System.Globalization.CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight,
            new Typeface("Segoe UI"),
            12,
            new SolidColorBrush(Color.FromRgb(228, 243, 244)),
            dpi)
        {
            MaxTextWidth = TooltipMaxWidth,
            LineHeight = 16,
        };

        var width = Math.Ceiling(text.Width) + 22;
        var height = Math.Ceiling(text.Height) + 18;
        var x = _tooltipPoint.X + TooltipOffset;
        var y = _tooltipPoint.Y + TooltipOffset;

        if (x + width > ActualWidth - 8)
        {
            x = _tooltipPoint.X - width - TooltipOffset;
        }

        if (y + height > ActualHeight - 8)
        {
            y = _tooltipPoint.Y - height - TooltipOffset;
        }

        x = Math.Clamp(x, 8, Math.Max(8, ActualWidth - width - 8));
        y = Math.Clamp(y, 8, Math.Max(8, ActualHeight - height - 8));

        var rect = new Rect(x, y, width, height);
        dc.DrawRectangle(new SolidColorBrush(Color.FromRgb(7, 12, 16)), null, new Rect(rect.X + 3, rect.Y + 3, rect.Width, rect.Height));
        dc.DrawRectangle(
            new SolidColorBrush(Color.FromRgb(10, 18, 23)),
            new Pen(new SolidColorBrush(Color.FromRgb(45, 72, 77)), 1),
            rect);
        dc.DrawText(text, new Point(x + 11, y + 9));
    }

    private Point ToDesign(Point point)
    {
        if (ActualWidth <= 0 || ActualHeight <= 0)
        {
            return point;
        }

        var x = point.X / ActualWidth * SynthPanelLayout.DesignWidth;
        var y = point.Y / ActualHeight * SynthPanelLayout.DesignHeight;
        return new Point(x, y);
    }

    private interface IHoverInfo
    {
        string Id { get; }
    }

    private sealed class WidgetHoverInfo(SynthWidget widget) : IHoverInfo
    {
        public string Id => "Widget:" + Widget.Id + ":" + Widget.EnumIndex;
        public SynthWidget Widget { get; } = widget;
    }

    private sealed class ZoneHoverInfo(SynthInfoZone zone) : IHoverInfo
    {
        public string Id => "Zone:" + Zone.Id;
        public SynthInfoZone Zone { get; } = zone;
    }
}
