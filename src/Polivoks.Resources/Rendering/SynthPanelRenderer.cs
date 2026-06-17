using System.Windows;
using Polivoks.Core.Models;
using Polivoks.Resources.Localization;

namespace Polivoks.Resources.Rendering;

public enum SynthRenderMode
{
    Full,
    Interaction,
}

public static class SynthPanelRenderer
{
    private static readonly string[] FootageLabels = ["32'", "16'", "8'", "4'", "2'"];
    private static readonly string[] WaveLabels = ["T", "S", "Q", "N", "P"];
    private static readonly string[] LfoLabels = ["T", "S", "N", "H"];
    private static readonly string[] FilterModeLabels = ["LP", "BP"];
    private const int WaveGraphTopOffset = 26;
    private const int WaveGraphHeight = 70;
    private const int WaveGraphLabelHeight = 18;

    public static void Render(BitmapCanvas canvas, SynthPatch patch, IReadOnlyList<SynthWidget> widgets, IReadOnlySet<int>? pressedKeys = null)
    {
        RenderStatic(canvas);
        RenderDynamic(canvas, patch, widgets, pressedKeys, SynthRenderMode.Full);
    }

    public static void RenderStatic(BitmapCanvas canvas)
    {
        DrawBackground(canvas);
        DrawShell(canvas);
        DrawWaveStripStatic(canvas);

        for (var i = 0; i < SynthPanelLayout.ModuleCount; i++)
        {
            DrawModule(canvas, i);
        }

        DrawKeyboardArea(canvas);
        canvas.Flush();
    }

    public static void RenderDynamic(BitmapCanvas canvas, SynthPatch patch, IReadOnlyList<SynthWidget> widgets, IReadOnlySet<int>? pressedKeys, SynthRenderMode mode)
    {
        DrawWaveGraphs(canvas, patch);
        DrawSignalFlowStrip(canvas, patch);

        foreach (var widget in widgets)
        {
            DrawWidget(canvas, patch, widget, pressedKeys);
        }

        canvas.Flush();
    }

    private static void DrawBackground(BitmapCanvas canvas)
    {
        canvas.Clear(BitmapColor.Chassis);
        canvas.FillRect(0, 0, SynthPanelLayout.DesignWidth, SynthPanelLayout.DesignHeight, BitmapColor.FromRgb(6, 12, 17));

        for (var x = 32; x < SynthPanelLayout.DesignWidth; x += 48)
        {
            canvas.DrawLineSmooth(x, 28, x, SynthPanelLayout.DesignHeight - 28, BitmapColor.FromRgb(9, 23, 28), 0.5);
        }
    }

    private static void DrawShell(BitmapCanvas canvas)
    {
        canvas.FillRoundedRect(16, 16, SynthPanelLayout.DesignWidth - 32, SynthPanelLayout.DesignHeight - 32, 0, BitmapColor.Chassis);
        canvas.DrawLineSmooth(22, SynthPanelLayout.ControlsTop - 8, SynthPanelLayout.DesignWidth - 22, SynthPanelLayout.ControlsTop - 8, BitmapColor.FromRgb(18, 34, 37), 1);
        canvas.DrawLineSmooth(22, SynthPanelLayout.FlowTop - 8, SynthPanelLayout.DesignWidth - 22, SynthPanelLayout.FlowTop - 8, BitmapColor.FromRgb(18, 34, 37), 1);
        canvas.DrawLineSmooth(22, SynthPanelLayout.KeyboardTop - 10, SynthPanelLayout.DesignWidth - 22, SynthPanelLayout.KeyboardTop - 10, BitmapColor.FromRgb(18, 34, 37), 1);
    }

    private static void DrawWaveStripStatic(BitmapCanvas canvas)
    {
        var y = SynthPanelLayout.WaveTop;
        canvas.DrawText(Loc("SignalControlShapes"), SynthPanelLayout.DesignWidth / 2, y + 6, 13, BitmapColor.Text, center: true);

        for (var i = 0; i < SynthPanelLayout.ModuleCount; i++)
        {
            var x = i * SynthPanelLayout.ModuleWidth + 10;
            var w = SynthPanelLayout.ModuleWidth - 20;
            var graphY = y + WaveGraphTopOffset;
            var graphH = WaveGraphHeight;
            var labelY = graphY + graphH - WaveGraphLabelHeight;
            canvas.FillRoundedRect(x, graphY, w, graphH, 0, BitmapColor.Glass);

            for (var gy = graphY + 12; gy < labelY - 4; gy += 16)
            {
                canvas.DrawLineSmooth(x + 8, gy, x + w - 8, gy, BitmapColor.Grid, 0.55);
            }

            canvas.DrawLineSmooth(x + 8, labelY, x + w - 8, labelY, BitmapColor.FromRgb(15, 32, 36), 0.8);
            canvas.FillRoundedRect(x + 1, labelY + 1, w - 2, WaveGraphLabelHeight - 2, 0, BitmapColor.FromRgb(7, 15, 19));
            canvas.DrawText(GetSectionSubtitle(i), x + w / 2, labelY + 5, 8, BitmapColor.GoldDim, center: true);
        }
    }

    private static void DrawWaveGraphs(BitmapCanvas canvas, SynthPatch patch)
    {
        var y = SynthPanelLayout.WaveTop;
        for (var i = 0; i < SynthPanelLayout.ModuleCount; i++)
        {
            var x = i * SynthPanelLayout.ModuleWidth + 10;
            var w = SynthPanelLayout.ModuleWidth - 20;
            var graphY = y + WaveGraphTopOffset;
            var graphH = WaveGraphHeight;
            DrawGraph(canvas, patch, i, x + 10, graphY + 8, w - 20, graphH - WaveGraphLabelHeight - 12);
        }
    }

    private static void DrawSignalFlowStrip(BitmapCanvas canvas, SynthPatch patch)
    {
        var y = SynthPanelLayout.FlowTop;
        var h = SynthPanelLayout.FlowHeight;
        canvas.FillRoundedRect(18, y, SynthPanelLayout.DesignWidth - 36, h, 0, BitmapColor.Glass);
        canvas.DrawText(Loc("SignalFlow"), 34, y + 10, 10, BitmapColor.GoldDim);

        var startX = 174;
        var usable = SynthPanelLayout.DesignWidth - startX - 28;
        var cell = usable / SynthPanelLayout.ModuleCount;

        for (var i = 0; i < SynthPanelLayout.ModuleCount; i++)
        {
            var x = startX + i * cell;
            var level = FlowLevel(patch, i);
            var fillWidth = Math.Max(4, (int)((cell - 34) * level));

            canvas.FillRoundedRect(x, y + 8, cell - 24, h - 16, 0, BitmapColor.FromRgb(8, 16, 20));
            canvas.FillRoundedRect(x + 3, y + 12, fillWidth, h - 24, 0, BitmapColor.ButtonActive);
            canvas.DrawText(GetSectionSubtitle(i), x + (cell - 24) / 2, y + 10, 8, BitmapColor.Text, center: true);

            if (i < SynthPanelLayout.ModuleCount - 1)
            {
                var ax = x + cell - 18;
                var ay = y + h / 2;
                canvas.DrawLineSmooth(ax - 10, ay, ax + 8, ay, BitmapColor.Gold, 1.4);
                canvas.DrawLineSmooth(ax + 8, ay, ax + 2, ay - 4, BitmapColor.Gold, 1.2);
                canvas.DrawLineSmooth(ax + 8, ay, ax + 2, ay + 4, BitmapColor.Gold, 1.2);
            }
        }
    }

    private static double FlowLevel(SynthPatch patch, int index) => index switch
    {
        0 => Math.Clamp(patch.LfoRate * 0.9 + patch.Osc1LfoDepth * 0.1, 0, 1),
        1 => Math.Clamp(patch.MixOsc1, 0, 1),
        2 => Math.Clamp(patch.MixOsc2, 0, 1),
        3 => Math.Clamp((patch.MixOsc1 + patch.MixOsc2 + patch.MixNoise) / 3.0, 0, 1),
        4 => Math.Clamp(patch.FilterCutoff * (0.6 + patch.FilterResonance * 0.4), 0, 1),
        5 => Math.Clamp(patch.AmpSustain * 0.65 + patch.MasterVolume * 0.35, 0, 1),
        _ => Math.Clamp(patch.MasterVolume, 0, 1),
    };

    private static void DrawGraph(BitmapCanvas canvas, SynthPatch patch, int index, int x, int y, int w, int h)
    {
        if (index == 3)
        {
            DrawLevelBars(canvas, x, y, w, h, [patch.MixOsc1, patch.MixOsc2, patch.MixNoise]);
            return;
        }

        if (index == 5)
        {
            DrawEnvelopeGraph(canvas, x, y, w, h, patch.AmpAttack, patch.AmpDecay, patch.AmpSustain, patch.AmpRelease);
            return;
        }

        var points = new List<Point>(72);
        for (var i = 0; i < 72; i++)
        {
            var t = i / 71.0;
            var v = index switch
            {
                0 => LfoSample(patch.LfoWaveform, t, 1.0 + patch.LfoRate * 3.0),
                1 => WaveSample(patch.Osc1Waveform, t),
                2 => WaveSample(patch.Osc2Waveform, t),
                4 => FilterCurve(t, patch.FilterCutoff, patch.FilterResonance),
                _ => Math.Sin(t * Math.PI * 6.0) * patch.MasterVolume,
            };

            points.Add(new Point(x + t * w, y + h * (0.5 - v * 0.42)));
        }

        canvas.DrawPolylineSmooth(points, BitmapColor.GoldDim, 3.2);
        canvas.DrawPolylineSmooth(points, BitmapColor.Gold, 1.5);
    }

    private static void DrawLevelBars(BitmapCanvas canvas, int x, int y, int w, int h, double[] values)
    {
        var slot = w / values.Length;
        for (var i = 0; i < values.Length; i++)
        {
            var value = Math.Clamp(values[i], 0, 1);
            var barH = Math.Max(4, (int)(h * value));
            var bx = x + i * slot + slot / 4;
            var by = y + h - barH;
            canvas.FillRoundedRect(bx, by, slot / 2, barH, 0, BitmapColor.ButtonActive, BitmapColor.Gold, 1);
        }
    }

    private static void DrawEnvelopeGraph(BitmapCanvas canvas, int x, int y, int w, int h, double attack, double decay, double sustain, double release)
    {
        var a = Math.Max(0.08, attack) * 0.26;
        var d = Math.Max(0.08, decay) * 0.24;
        var r = Math.Max(0.08, release) * 0.26;
        var sustainY = y + h * (1.0 - Math.Clamp(sustain, 0, 1));
        var points = new[]
        {
            new Point(x, y + h),
            new Point(x + w * a, y + 4),
            new Point(x + w * (a + d), sustainY),
            new Point(x + w * (1 - r), sustainY),
            new Point(x + w, y + h),
        };
        canvas.DrawPolylineSmooth(points, BitmapColor.GoldDim, 3.2);
        canvas.DrawPolylineSmooth(points, BitmapColor.Gold, 1.5);
    }

    private static double LfoSample(LfoWaveformType waveform, double t, double cycles) => waveform switch
    {
        LfoWaveformType.Noise => Math.Sin((t * 97.0 + Math.Floor(t * cycles * 12.0)) * 11.3),
        LfoWaveformType.Square => ((t * cycles) % 1.0) < 0.5 ? 1.0 : -1.0,
        LfoWaveformType.SampleHold => Math.Sin(Math.Floor(t * cycles * 6.0) * 1.73),
        _ => Math.Sin(t * Math.PI * 2.0 * cycles),
    };

    private static double WaveSample(WaveformType waveform, double t) => waveform switch
    {
        WaveformType.Triangle => 1.0 - 4.0 * Math.Abs(t - 0.5),
        WaveformType.Saw => 2.0 * t - 1.0,
        WaveformType.Square => t < 0.5 ? 1.0 : -1.0,
        WaveformType.PulseNarrow => t < 0.25 ? 1.0 : -1.0,
        WaveformType.PulseWide => t < 0.75 ? 1.0 : -1.0,
        _ => Math.Sin(t * Math.PI * 2.0),
    };

    private static double FilterCurve(double t, double cutoff, double resonance)
    {
        var c = Math.Clamp(cutoff, 0.05, 0.95);
        var slope = 1.0 / (1.0 + Math.Exp((t - c) * 18.0));
        var peak = Math.Exp(-Math.Pow((t - c) * 16.0, 2.0)) * resonance * 0.65;
        return Math.Clamp(slope * 0.9 + peak - 0.42, -1.0, 1.0);
    }

    private static void DrawModule(BitmapCanvas canvas, int index)
    {
        var x = index * SynthPanelLayout.ModuleWidth + 6;
        var y = SynthPanelLayout.ControlsTop;
        var w = SynthPanelLayout.ModuleWidth - 12;
        var h = SynthPanelLayout.ControlsHeight - 8;

        canvas.FillRoundedRect(x, y, w, h, 0, BitmapColor.ModuleFace);
        canvas.FillRoundedRect(x + 4, y + 4, w - 8, 30, 0, BitmapColor.ModuleHeader);
        canvas.DrawLineSmooth(x + 8, y + 38, x + w - 8, y + 38, BitmapColor.FromRgb(20, 38, 41), 1);

        var title = GetSectionTitle(index);
        canvas.DrawText(title, x + w / 2, y + 9, 13, BitmapColor.Text, center: true);
    }

    private static string GetSectionTitle(int index) => index switch
    {
        0 => LocalizationManager.Current["SectionModulator"],
        1 => LocalizationManager.Current["SectionOsc1"],
        2 => LocalizationManager.Current["SectionOsc2"],
        3 => LocalizationManager.Current["SectionMixer"],
        4 => LocalizationManager.Current["SectionFilter"],
        5 => LocalizationManager.Current["SectionAmp"],
        _ => LocalizationManager.Current["SectionMaster"],
    };

    private static string GetSectionSubtitle(int index) => index switch
    {
        0 => "MODULATOR",
        1 => "GENERATOR I",
        2 => "GENERATOR II",
        3 => "MIXER",
        4 => "FILTER",
        5 => "AMPLIFIER",
        _ => "MASTER",
    };

    private static void DrawKeyboardArea(BitmapCanvas canvas)
    {
        canvas.FillRoundedRect(14, SynthPanelLayout.KeyboardTop - 12, SynthPanelLayout.DesignWidth - 28, SynthPanelLayout.KeyboardHeight + 24, 0, BitmapColor.Track);
        canvas.DrawText(Loc("KeyboardTitle"), SynthPanelLayout.DesignWidth / 2, SynthPanelLayout.KeyboardTop - 5, 10, BitmapColor.GoldDim, center: true);
    }

    private static void DrawWidget(BitmapCanvas canvas, SynthPatch patch, SynthWidget widget, IReadOnlySet<int>? pressedKeys)
    {
        switch (widget.Kind)
        {
            case WidgetKind.Knob:
                DrawKnob(canvas, patch, widget);
                break;
            case WidgetKind.Fader:
                DrawFader(canvas, patch, widget);
                break;
            case WidgetKind.EnumButton:
                DrawEnumButton(canvas, patch, widget);
                break;
            case WidgetKind.Toggle:
                DrawToggle(canvas, patch, widget);
                break;
            case WidgetKind.PianoKey:
                DrawPianoKey(canvas, widget, pressedKeys?.Contains(widget.MidiNote) == true);
                break;
        }
    }

    public static void DrawKnob(BitmapCanvas canvas, SynthPatch patch, SynthWidget widget)
    {
        var value = PatchEditor.GetDouble(patch, widget.Id);
        var cx = widget.Bounds.X + widget.Bounds.Width / 2;
        var cy = widget.Bounds.Y + widget.Bounds.Height / 2 - 6;
        var radius = widget.LargeKnob ? 34 : widget.Bounds.Width / 2;

        canvas.FillEllipse(cx + 3, cy + 4, radius, radius, BitmapColor.Shadow);
        canvas.FillEllipse(cx, cy, radius, radius, BitmapColor.KnobFace, BitmapColor.GoldDim, 1.2);
        canvas.FillEllipse(cx - radius / 4, cy - radius / 4, Math.Max(4, radius / 5), Math.Max(4, radius / 5), BitmapColor.FromRgb(39, 47, 53));
        for (var i = -135; i <= 135; i += 18)
        {
            var rad = i * Math.PI / 180;
            var inner = radius + 5;
            var outer = radius + 9;
            canvas.DrawLineSmooth(cx + Math.Cos(rad) * inner, cy + Math.Sin(rad) * inner, cx + Math.Cos(rad) * outer, cy + Math.Sin(rad) * outer, BitmapColor.GoldDim, 1);
        }

        var angle = (-135 + value * 270) * Math.PI / 180;
        var ex = cx + (int)(Math.Cos(angle) * (radius - 8));
        var ey = cy + (int)(Math.Sin(angle) * (radius - 8));
        canvas.DrawLineSmooth(cx, cy, ex, ey, BitmapColor.Pointer, 2.4);
        canvas.FillEllipse(ex, ey, 3, 3, BitmapColor.Pointer);

        var label = ShortLabel(widget);
        canvas.DrawText(label, cx, cy + radius + 3, radius <= 18 ? 8 : radius <= 22 ? 9 : 10, BitmapColor.Gold, center: true);
    }

    public static void DrawFader(BitmapCanvas canvas, SynthPatch patch, SynthWidget widget)
    {
        var value = PatchEditor.GetDouble(patch, widget.Id);
        var x = widget.Bounds.X + widget.Bounds.Width / 2;
        var top = widget.Bounds.Y + 8;
        var bottom = widget.Bounds.Y + widget.Bounds.Height - 24;
        canvas.FillRoundedRect(x - 5, top, 10, bottom - top, 0, BitmapColor.Track, BitmapColor.FromRgb(18, 31, 34), 1);
        canvas.DrawLineSmooth(x, top + 4, x, bottom - 4, BitmapColor.GoldDim, 1);

        var capY = top + (int)((1 - value) * (bottom - top));
        canvas.FillRoundedRect(x - 15, capY - 8, 30, 16, 0, BitmapColor.KnobFace, BitmapColor.Gold, 1);

        var label = ShortLabel(widget);
        canvas.DrawText(label, x, bottom + 6, 10, BitmapColor.Gold, center: true);
    }

    public static void DrawEnumButton(BitmapCanvas canvas, SynthPatch patch, SynthWidget widget)
    {
        var selected = PatchEditor.GetEnumIndex(patch, widget.Id) == widget.EnumIndex;
        var bg = selected ? BitmapColor.ButtonActive : BitmapColor.ButtonFace;
        var b = widget.Bounds;
        canvas.FillRoundedRect(b.X, b.Y, b.Width, b.Height, 0, bg);

        var label = GetEnumLabel(widget.Id, widget.EnumIndex);
        canvas.DrawText(label, b.X + b.Width / 2, b.Y + 3, 10, BitmapColor.Gold, center: true);
    }

    public static void DrawToggle(BitmapCanvas canvas, SynthPatch patch, SynthWidget widget)
    {
        var on = PatchEditor.GetBool(patch, widget.Id);
        var b = widget.Bounds;
        canvas.FillRoundedRect(b.X, b.Y + 6, b.Width, 10, 0, BitmapColor.Track);
        canvas.FillRoundedRect(on ? b.X + b.Width / 2 : b.X + 2, b.Y + 8, b.Width / 2 - 4, 6, 0, on ? BitmapColor.Gold : BitmapColor.FromRgb(18, 33, 38));

        var label = ShortLabel(widget);
        canvas.DrawText(label, b.X + b.Width / 2, b.Y + 20, 9, BitmapColor.Gold, center: true);
    }

    public static void DrawPianoKey(BitmapCanvas canvas, SynthWidget widget, bool pressed)
    {
        var b = widget.Bounds;
        var black = b.Height < SynthPanelLayout.KeyboardHeight - 10;
        var baseColor = black ? BitmapColor.KeyBlack : BitmapColor.KeyWhite;
        var color = pressed ? BitmapColor.AlphaBlend(baseColor, BitmapColor.Gold, 90) : baseColor;
        canvas.FillRoundedRect(b.X, b.Y, b.Width, b.Height, 0, color, black ? BitmapColor.FromRgb(46, 58, 62) : BitmapColor.FromRgb(102, 124, 126), 1);
        if (!black)
        {
            canvas.DrawLineSmooth(b.X + 4, b.Y + 8, b.X + b.Width - 5, b.Y + 8, BitmapColor.FromRgb(238, 244, 242), 0.8);
        }
    }

    private static string GetEnumLabel(string id, int index) => id switch
    {
        "LfoWaveform" => LfoLabels[Math.Clamp(index, 0, LfoLabels.Length - 1)],
        "Osc1Footage" or "Osc2Footage" => FootageLabels[Math.Clamp(index, 0, FootageLabels.Length - 1)],
        "Osc1Waveform" or "Osc2Waveform" => WaveLabels[Math.Clamp(index, 0, WaveLabels.Length - 1)],
        "FilterMode" => FilterModeLabels[Math.Clamp(index, 0, FilterModeLabels.Length - 1)],
        _ => index.ToString(),
    };

    private static string ShortLabel(SynthWidget widget) => (widget.Label ?? widget.Id) switch
    {
        "LfoRate" => "RATE",
        "ModDepth" => "MOD",
        "OscLfoDepth" or "Osc1LfoDepth" or "Osc2LfoDepth" => "LFO",
        "OscFmDepth" or "Osc1FmDepth" => "FM",
        "OscDetune" or "Osc2Detune" => "DET",
        "MixOsc1" => "OSC I",
        "MixOsc2" => "OSC II",
        "MixNoise" => "NOISE",
        "FilterCutoff" => "CUT",
        "FilterResonance" => "RES",
        "FilterEnvDepth" => "ENV",
        "FilterLfoDepth" => "LFO",
        "AmpLfoDepth" => "LFO",
        "MasterTune" => "TUNE",
        "MasterVolume" => "VOL",
        "HeadphoneVolume" => "HP",
        "Portamento" => "PORT",
        "Duophonic" => "DUO",
        "MainOutput" or "MainOutputEnabled" => "OUT",
        "GateOn" => "GATE",
        "Loop" => "LOOP",
        "EnvA" => "A",
        "EnvD" => "D",
        "EnvS" => "S",
        "EnvR" => "R",
        var label when label.Length <= 8 => label.ToUpperInvariant(),
        var label => label[..Math.Min(8, label.Length)].ToUpperInvariant(),
    };

    private static string Loc(string key) => LocalizationManager.Current[key];
}
