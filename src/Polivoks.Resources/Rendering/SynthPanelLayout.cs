using System.Windows;

namespace Polivoks.Resources.Rendering;

public static class SynthPanelLayout
{
    public const int DesignWidth = 1280;
    public const int DesignHeight = 680;
    public const int WaveTop = 14;
    public const int WaveHeight = 104;
    public const int ControlsTop = 132;
    public const int ControlsHeight = 342;
    public const int FlowTop = 486;
    public const int FlowHeight = 36;
    public const int PanelHeight = ControlsTop + ControlsHeight;
    public const int KeyboardTop = 536;
    public const int KeyboardHeight = 132;
    public const int ModuleCount = 7;
    public const int ModuleWidth = DesignWidth / ModuleCount;

    public static IReadOnlyList<SynthWidget> BuildWidgets()
    {
        var widgets = new List<SynthWidget>();
        BuildModulator(widgets, 0);
        BuildOsc(widgets, 1, "Osc1Footage", "Osc1Waveform", "Osc1LfoDepth", "Osc1FmDepth", false);
        BuildOsc(widgets, 2, "Osc2Footage", "Osc2Waveform", "Osc2Detune", "Osc2LfoDepth", true);
        BuildMixer(widgets, 3);
        BuildFilter(widgets, 4);
        BuildAmp(widgets, 5);
        BuildMaster(widgets, 6);
        BuildKeyboard(widgets);
        return widgets;
    }

    public static IReadOnlyList<SynthInfoZone> BuildInfoZones()
    {
        var zones = new List<SynthInfoZone>();

        for (var i = 0; i < ModuleCount; i++)
        {
            zones.Add(new SynthInfoZone
            {
                Id = $"WaveGraph{i}",
                Bounds = new Int32Rect(i * ModuleWidth + 10, WaveTop + 26, ModuleWidth - 20, 70),
            });

            zones.Add(new SynthInfoZone
            {
                Id = $"Section{i}",
                Bounds = new Int32Rect(i * ModuleWidth + 6, ControlsTop, ModuleWidth - 12, ControlsHeight - 8),
            });
        }

        zones.Add(new SynthInfoZone
        {
            Id = "SignalFlow",
            Bounds = new Int32Rect(18, FlowTop, DesignWidth - 36, FlowHeight),
        });

        zones.Add(new SynthInfoZone
        {
            Id = "Keyboard",
            Bounds = new Int32Rect(14, KeyboardTop - 12, DesignWidth - 28, KeyboardHeight + 24),
        });

        return zones;
    }

    private static void BuildModulator(List<SynthWidget> widgets, int column)
    {
        var x = column * ModuleWidth;
        AddEnumRow(widgets, x, ControlsTop + 50, "LfoWaveform", 4, 0);
        AddKnob(widgets, x + ModuleWidth / 2, ControlsTop + 134, "LfoRate", large: true);
        AddKnob(widgets, x + ModuleWidth / 2, ControlsTop + 254, "Osc1LfoDepth", label: "ModDepth");
    }

    private static void BuildOsc(List<SynthWidget> widgets, int column, string footageId, string waveId, string knob1, string knob2, bool detuneSecond)
    {
        var x = column * ModuleWidth;
        AddEnumRow(widgets, x, ControlsTop + 50, footageId, 5, 0);
        AddEnumRow(widgets, x, ControlsTop + 92, waveId, 5, 0);
        AddKnob(widgets, x + ModuleWidth / 2, ControlsTop + 178, knob1, label: detuneSecond ? "OscDetune" : "OscLfoDepth");
        AddKnob(widgets, x + ModuleWidth / 2, ControlsTop + 258, knob2, label: detuneSecond ? "OscLfoDepth" : "OscFmDepth");
    }

    private static void BuildMixer(List<SynthWidget> widgets, int column)
    {
        var x = column * ModuleWidth;
        var slot = ModuleWidth / 4;
        AddFader(widgets, x + slot, ControlsTop + 66, "MixOsc1");
        AddFader(widgets, x + slot * 2, ControlsTop + 66, "MixOsc2");
        AddFader(widgets, x + slot * 3, ControlsTop + 66, "MixNoise");
    }

    private static void BuildFilter(List<SynthWidget> widgets, int column)
    {
        var x = column * ModuleWidth;
        AddEnumRow(widgets, x, ControlsTop + 50, "FilterMode", 2, 0);
        AddToggle(widgets, x + ModuleWidth - 34, ControlsTop + 80, "FilterEnvelopeLoop", "Loop");
        AddKnob(widgets, x + ModuleWidth / 2, ControlsTop + 122, "FilterCutoff", large: true);
        AddKnob(widgets, x + 38, ControlsTop + 212, "FilterResonance", small: true);
        AddKnob(widgets, x + 91, ControlsTop + 212, "FilterEnvDepth", small: true);
        AddKnob(widgets, x + 144, ControlsTop + 212, "FilterLfoDepth", small: true);
        AddKnob(widgets, x + 38, ControlsTop + 286, "FilterAttack", small: true, label: "EnvA");
        AddKnob(widgets, x + 91, ControlsTop + 286, "FilterDecay", small: true, label: "EnvD");
        AddKnob(widgets, x + 144, ControlsTop + 286, "FilterSustain", small: true, label: "EnvS");
    }

    private static void BuildAmp(List<SynthWidget> widgets, int column)
    {
        var x = column * ModuleWidth;
        AddKnob(widgets, x + 38, ControlsTop + 82, "AmpAttack", small: true, label: "EnvA");
        AddKnob(widgets, x + 91, ControlsTop + 82, "AmpDecay", small: true, label: "EnvD");
        AddKnob(widgets, x + 144, ControlsTop + 82, "AmpSustain", small: true, label: "EnvS");
        AddKnob(widgets, x + ModuleWidth / 2, ControlsTop + 166, "AmpRelease", small: true, label: "EnvR");
        AddKnob(widgets, x + ModuleWidth / 2, ControlsTop + 246, "AmpLfoDepth", small: true);
        AddToggle(widgets, x + ModuleWidth / 2 - 40, ControlsTop + 292, "AmpEnvelopeLoop", "Loop");
        AddToggle(widgets, x + ModuleWidth / 2 + 40, ControlsTop + 292, "GateOn", "GateOn");
    }

    private static void BuildMaster(List<SynthWidget> widgets, int column)
    {
        var x = column * ModuleWidth;
        AddKnob(widgets, x + 62, ControlsTop + 84, "MasterTune", small: true);
        AddKnob(widgets, x + 120, ControlsTop + 84, "MasterVolume", small: true);
        AddKnob(widgets, x + 62, ControlsTop + 190, "HeadphoneVolume", small: true);
        AddKnob(widgets, x + 120, ControlsTop + 190, "Portamento", small: true);
        AddToggle(widgets, x + ModuleWidth / 2 - 40, ControlsTop + 292, "Duophonic", "Duophonic");
        AddToggle(widgets, x + ModuleWidth / 2 + 40, ControlsTop + 292, "MainOutputEnabled", "MainOutput");
    }

    private static void BuildKeyboard(List<SynthWidget> widgets)
    {
        const int startNote = 48;
        const int octaves = 3;
        const int keyboardInset = 16;
        var whiteWidth = (DesignWidth - keyboardInset * 2) / (octaves * 7);
        var whiteNotes = new[] { 0, 2, 4, 5, 7, 9, 11 };

        for (var i = 0; i < octaves * 7; i++)
        {
            var octave = i / 7;
            var midi = startNote + octave * 12 + whiteNotes[i % 7];
            widgets.Add(new SynthWidget
            {
                Id = $"Key{midi}",
                Kind = WidgetKind.PianoKey,
                Bounds = new Int32Rect(keyboardInset + i * whiteWidth, KeyboardTop, whiteWidth - 1, KeyboardHeight),
                MidiNote = midi,
            });
        }

        int[] blackOffsets = [1, 3, 6, 8, 10];
        int[] whiteIndices = [0, 1, 3, 4, 5];
        for (var octave = 0; octave < octaves; octave++)
        {
            for (var b = 0; b < blackOffsets.Length; b++)
            {
                var midi = startNote + octave * 12 + blackOffsets[b];
                var whiteIndex = octave * 7 + whiteIndices[b];
                var x = keyboardInset + whiteIndex * whiteWidth + (int)(whiteWidth * 0.68);
                widgets.Add(new SynthWidget
                {
                    Id = $"Key{midi}",
                    Kind = WidgetKind.PianoKey,
                    Bounds = new Int32Rect(x, KeyboardTop, Math.Max(10, (int)(whiteWidth * 0.55)), (int)(KeyboardHeight * 0.62)),
                    MidiNote = midi,
                });
            }
        }
    }

    private static void AddKnob(List<SynthWidget> widgets, int cx, int cy, string id, bool large = false, bool small = false, string? label = null)
    {
        var r = large ? 31 : small ? 18 : 24;
        widgets.Add(new SynthWidget
        {
            Id = id,
            Kind = WidgetKind.Knob,
            Bounds = new Int32Rect(cx - r, cy - r, r * 2, r * 2 + 14),
            Label = label ?? id,
            LargeKnob = large,
        });
    }

    private static void AddFader(List<SynthWidget> widgets, int cx, int y, string id)
    {
        widgets.Add(new SynthWidget
        {
            Id = id,
            Kind = WidgetKind.Fader,
            Bounds = new Int32Rect(cx - 14, y, 28, 264),
            Label = id,
        });
    }

    private static void AddEnumRow(List<SynthWidget> widgets, int moduleX, int y, string id, int count, int selected)
    {
        var buttonW = Math.Min(30, (ModuleWidth - 16) / count);
        var totalW = buttonW * count;
        var startX = moduleX + (ModuleWidth - totalW) / 2;
        for (var i = 0; i < count; i++)
        {
            widgets.Add(new SynthWidget
            {
                Id = id,
                Kind = WidgetKind.EnumButton,
                Bounds = new Int32Rect(startX + i * buttonW, y, buttonW - 2, 22),
                EnumIndex = i,
                EnumCount = count,
            });
        }
    }

    private static void AddToggle(List<SynthWidget> widgets, int cx, int y, string id, string? label = null)
    {
        widgets.Add(new SynthWidget
        {
            Id = id,
            Kind = WidgetKind.Toggle,
            Bounds = new Int32Rect(cx - 24, y, 48, 28),
            Label = label ?? id,
        });
    }
}
