namespace Polivoks.Core.Models;

public sealed class SynthPatch
{
    public double MasterTune { get; set; } = 0.5;
    public double MasterVolume { get; set; } = 0.7;
    public double HeadphoneVolume { get; set; } = 0.7;
    public double Portamento { get; set; }
    public bool MainOutputEnabled { get; set; } = true;
    public bool Duophonic { get; set; }
    public bool GateOn { get; set; }

    public double LfoRate { get; set; } = 0.3;
    public LfoWaveformType LfoWaveform { get; set; } = LfoWaveformType.Triangle;

    public Footage Osc1Footage { get; set; } = Footage.Foot8;
    public WaveformType Osc1Waveform { get; set; } = WaveformType.Saw;
    public double Osc1LfoDepth { get; set; }
    public double Osc1FmDepth { get; set; }

    public Footage Osc2Footage { get; set; } = Footage.Foot8;
    public WaveformType Osc2Waveform { get; set; } = WaveformType.Saw;
    public double Osc2Detune { get; set; } = 0.5;
    public double Osc2LfoDepth { get; set; }

    public double MixOsc1 { get; set; } = 0.8;
    public double MixOsc2 { get; set; } = 0.5;
    public double MixNoise { get; set; }
    public double MixExternal { get; set; }

    public FilterMode FilterMode { get; set; } = FilterMode.LowPass;
    public double FilterCutoff { get; set; } = 0.6;
    public double FilterResonance { get; set; } = 0.2;
    public double FilterEnvDepth { get; set; } = 0.5;
    public double FilterLfoDepth { get; set; }
    public double FilterAttack { get; set; } = 0.1;
    public double FilterDecay { get; set; } = 0.3;
    public double FilterSustain { get; set; } = 0.5;
    public double FilterRelease { get; set; } = 0.4;
    public bool FilterEnvelopeLoop { get; set; }

    public double AmpAttack { get; set; } = 0.05;
    public double AmpDecay { get; set; } = 0.2;
    public double AmpSustain { get; set; } = 0.8;
    public double AmpRelease { get; set; } = 0.3;
    public bool AmpEnvelopeLoop { get; set; }
    public double AmpLfoDepth { get; set; }

    public SynthPatch Clone() => (SynthPatch)MemberwiseClone();
}
