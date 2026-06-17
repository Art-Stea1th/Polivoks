using System.Windows;
using Polivoks.Core.Models;

namespace Polivoks.Resources.Rendering;

public enum WidgetKind
{
    Knob,
    Fader,
    EnumButton,
    Toggle,
    PianoKey,
}

public sealed class SynthWidget
{
    public required string Id { get; init; }
    public WidgetKind Kind { get; init; }
    public Int32Rect Bounds { get; init; }
    public string? Label { get; init; }
    public int EnumIndex { get; init; }
    public int EnumCount { get; init; }
    public int MidiNote { get; init; }
    public bool LargeKnob { get; init; }
}

public sealed class SynthInfoZone
{
    public required string Id { get; init; }
    public Int32Rect Bounds { get; init; }
}

public static class PatchEditor
{
    public static double GetDouble(SynthPatch patch, string id) => id switch
    {
        "LfoRate" => patch.LfoRate,
        "Osc1LfoDepth" => patch.Osc1LfoDepth,
        "Osc1FmDepth" => patch.Osc1FmDepth,
        "Osc2Detune" => patch.Osc2Detune,
        "Osc2LfoDepth" => patch.Osc2LfoDepth,
        "MixOsc1" => patch.MixOsc1,
        "MixOsc2" => patch.MixOsc2,
        "MixNoise" => patch.MixNoise,
        "FilterCutoff" => patch.FilterCutoff,
        "FilterResonance" => patch.FilterResonance,
        "FilterEnvDepth" => patch.FilterEnvDepth,
        "FilterLfoDepth" => patch.FilterLfoDepth,
        "FilterAttack" => patch.FilterAttack,
        "FilterDecay" => patch.FilterDecay,
        "FilterSustain" => patch.FilterSustain,
        "FilterRelease" => patch.FilterRelease,
        "AmpAttack" => patch.AmpAttack,
        "AmpDecay" => patch.AmpDecay,
        "AmpSustain" => patch.AmpSustain,
        "AmpRelease" => patch.AmpRelease,
        "AmpLfoDepth" => patch.AmpLfoDepth,
        "MasterTune" => patch.MasterTune,
        "MasterVolume" => patch.MasterVolume,
        "HeadphoneVolume" => patch.HeadphoneVolume,
        "Portamento" => patch.Portamento,
        _ => 0,
    };

    public static void SetDouble(SynthPatch patch, string id, double value)
    {
        value = Math.Clamp(value, 0, 1);
        switch (id)
        {
            case "LfoRate": patch.LfoRate = value; break;
            case "Osc1LfoDepth": patch.Osc1LfoDepth = value; break;
            case "Osc1FmDepth": patch.Osc1FmDepth = value; break;
            case "Osc2Detune": patch.Osc2Detune = value; break;
            case "Osc2LfoDepth": patch.Osc2LfoDepth = value; break;
            case "MixOsc1": patch.MixOsc1 = value; break;
            case "MixOsc2": patch.MixOsc2 = value; break;
            case "MixNoise": patch.MixNoise = value; break;
            case "FilterCutoff": patch.FilterCutoff = value; break;
            case "FilterResonance": patch.FilterResonance = value; break;
            case "FilterEnvDepth": patch.FilterEnvDepth = value; break;
            case "FilterLfoDepth": patch.FilterLfoDepth = value; break;
            case "FilterAttack": patch.FilterAttack = value; break;
            case "FilterDecay": patch.FilterDecay = value; break;
            case "FilterSustain": patch.FilterSustain = value; break;
            case "FilterRelease": patch.FilterRelease = value; break;
            case "AmpAttack": patch.AmpAttack = value; break;
            case "AmpDecay": patch.AmpDecay = value; break;
            case "AmpSustain": patch.AmpSustain = value; break;
            case "AmpRelease": patch.AmpRelease = value; break;
            case "AmpLfoDepth": patch.AmpLfoDepth = value; break;
            case "MasterTune": patch.MasterTune = value; break;
            case "MasterVolume": patch.MasterVolume = value; break;
            case "HeadphoneVolume": patch.HeadphoneVolume = value; break;
            case "Portamento": patch.Portamento = value; break;
        }
    }

    public static bool GetBool(SynthPatch patch, string id) => id switch
    {
        "FilterEnvelopeLoop" => patch.FilterEnvelopeLoop,
        "AmpEnvelopeLoop" => patch.AmpEnvelopeLoop,
        "GateOn" => patch.GateOn,
        "Duophonic" => patch.Duophonic,
        "MainOutputEnabled" => patch.MainOutputEnabled,
        _ => false,
    };

    public static void ToggleBool(SynthPatch patch, string id)
    {
        switch (id)
        {
            case "FilterEnvelopeLoop": patch.FilterEnvelopeLoop = !patch.FilterEnvelopeLoop; break;
            case "AmpEnvelopeLoop": patch.AmpEnvelopeLoop = !patch.AmpEnvelopeLoop; break;
            case "GateOn": patch.GateOn = !patch.GateOn; break;
            case "Duophonic": patch.Duophonic = !patch.Duophonic; break;
            case "MainOutputEnabled": patch.MainOutputEnabled = !patch.MainOutputEnabled; break;
        }
    }

    public static int GetEnumIndex(SynthPatch patch, string id) => id switch
    {
        "LfoWaveform" => (int)patch.LfoWaveform,
        "Osc1Footage" => (int)patch.Osc1Footage,
        "Osc1Waveform" => (int)patch.Osc1Waveform,
        "Osc2Footage" => (int)patch.Osc2Footage,
        "Osc2Waveform" => (int)patch.Osc2Waveform,
        "FilterMode" => (int)patch.FilterMode,
        _ => 0,
    };

    public static void SetEnumIndex(SynthPatch patch, string id, int index)
    {
        switch (id)
        {
            case "LfoWaveform":
                patch.LfoWaveform = (LfoWaveformType)Math.Clamp(index, 0, 3);
                break;
            case "Osc1Footage":
                patch.Osc1Footage = (Footage)Math.Clamp(index, 0, 4);
                break;
            case "Osc1Waveform":
                patch.Osc1Waveform = (WaveformType)Math.Clamp(index, 0, 4);
                break;
            case "Osc2Footage":
                patch.Osc2Footage = (Footage)Math.Clamp(index, 0, 4);
                break;
            case "Osc2Waveform":
                patch.Osc2Waveform = (WaveformType)Math.Clamp(index, 0, 4);
                break;
            case "FilterMode":
                patch.FilterMode = (FilterMode)Math.Clamp(index, 0, 1);
                break;
        }
    }

    public static int GetEnumCount(string id) => id switch
    {
        "LfoWaveform" => 4,
        "Osc1Footage" or "Osc2Footage" => 5,
        "Osc1Waveform" or "Osc2Waveform" => 5,
        "FilterMode" => 2,
        _ => 1,
    };
}
