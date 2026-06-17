using Polivoks.Core.Models;

namespace Polivoks.Core.Synth;

public sealed class PolivoksEngine
{
    private readonly object _sync = new();
    private readonly Random _random = new();
    private SynthPatch _patch = new();
    private readonly VoiceState _voiceLow = new();
    private readonly VoiceState _voiceHigh = new();
    private readonly AdsrEnvelope _filterEnv = new();
    private readonly AdsrEnvelope _ampEnv = new();
    private readonly Lfo _lfo = new();
    private float _portamentoPitch;
    private float _targetPitch;
    private bool _gate;
    private float _sampleHold;
    private int _sampleHoldCounter;
    private double _phaseNoise;

    public SynthPatch Patch
    {
        get => _patch;
        set
        {
            lock (_sync)
            {
                _patch = value.Clone();
            }
        }
    }

    public void NoteOn(int midiNote, float velocity = 1f)
    {
        lock (_sync)
        {
            _gate = true;
            var freq = MidiToFrequency(midiNote);
            if (_patch.Duophonic)
            {
                if (!_voiceHigh.IsActive || midiNote > _voiceHigh.MidiNote)
                {
                    _voiceHigh.Activate(midiNote, freq, velocity);
                }

                if (!_voiceLow.IsActive || midiNote < _voiceLow.MidiNote)
                {
                    _voiceLow.Activate(midiNote, freq, velocity);
                }
            }
            else
            {
                _targetPitch = freq;
                if (_portamentoPitch <= 0)
                {
                    _portamentoPitch = freq;
                }

                _voiceHigh.Activate(midiNote, freq, velocity);
                _voiceLow.Deactivate();
            }

            _filterEnv.Trigger(_patch.FilterAttack, _patch.FilterDecay, _patch.FilterSustain, _patch.FilterRelease, _patch.FilterEnvelopeLoop);
            _ampEnv.Trigger(_patch.AmpAttack, _patch.AmpDecay, _patch.AmpSustain, _patch.AmpRelease, _patch.AmpEnvelopeLoop);
        }
    }

    public void NoteOff(int midiNote)
    {
        lock (_sync)
        {
            if (_patch.Duophonic)
            {
                if (_voiceHigh.MidiNote == midiNote)
                {
                    _voiceHigh.Deactivate();
                }

                if (_voiceLow.MidiNote == midiNote)
                {
                    _voiceLow.Deactivate();
                }

                if (!_voiceHigh.IsActive && !_voiceLow.IsActive)
                {
                    ReleaseGate();
                }
            }
            else if (_voiceHigh.MidiNote == midiNote)
            {
                ReleaseGate();
            }
        }
    }

    public void AllNotesOff()
    {
        lock (_sync)
        {
            _voiceHigh.Deactivate();
            _voiceLow.Deactivate();
            ReleaseGate();
        }
    }

    public void Render(Span<float> buffer, int sampleRate)
    {
        lock (_sync)
        {
            var patch = _patch;
            var master = (float)(patch.MainOutputEnabled ? patch.MasterVolume : 0.0);
            var tuneRatio = Math.Pow(2.0, (patch.MasterTune - 0.5) * 2.0 / 12.0);

            for (var i = 0; i < buffer.Length; i++)
            {
                var lfoValue = _lfo.NextSample(patch.LfoRate, patch.LfoWaveform, sampleRate, ref _phaseNoise, ref _sampleHold, ref _sampleHoldCounter, _random);
                UpdatePortamento(patch, sampleRate);

                var osc1Base = _portamentoPitch * tuneRatio;
                var osc2Base = patch.Duophonic && _voiceLow.IsActive ? _voiceLow.Frequency * tuneRatio : osc1Base;
                var osc2Freq = FrequencyForOsc(patch.Osc2Footage, osc2Base, patch.Osc2LfoDepth, lfoValue);
                osc2Freq *= Math.Pow(2.0, (patch.Osc2Detune - 0.5) * 4.0 / 12.0);
                var osc2 = OscillatorSample(patch.Osc2Waveform, osc2Freq, sampleRate, _voiceHigh, useSecondaryPhase: true) * patch.MixOsc2;

                var fm = Math.Sin(_voiceHigh.PhaseOsc2 * Math.PI * 2.0) * patch.Osc1FmDepth * 4.0;
                var osc1Freq = FrequencyForOsc(patch.Osc1Footage, osc1Base, patch.Osc1LfoDepth, lfoValue) * (1.0 + fm);
                var osc1 = OscillatorSample(patch.Osc1Waveform, osc1Freq, sampleRate, _voiceHigh, useSecondaryPhase: false) * patch.MixOsc1;

                var noise = ((float)_random.NextDouble() * 2f - 1f) * (float)patch.MixNoise;
                var mixed = (float)(osc1 + osc2 + noise);

                var cutoff = (float)(patch.FilterCutoff + _filterEnv.Level * patch.FilterEnvDepth + lfoValue * patch.FilterLfoDepth);
                cutoff = Math.Clamp(cutoff, 0.01f, 1f);
                var filtered = PolivoksFilter.Process(_voiceHigh.FilterState, mixed, cutoff, (float)patch.FilterResonance, patch.FilterMode, sampleRate);

                var amp = _ampEnv.Next(sampleRate) * (1f + (float)(lfoValue * patch.AmpLfoDepth * 0.5));
                buffer[i] = filtered * amp * master * (_gate ? 1f : 0f);
            }
        }
    }

    private void ReleaseGate()
    {
        _gate = false;
        _filterEnv.Release();
        _ampEnv.Release();
    }

    private void UpdatePortamento(SynthPatch patch, int sampleRate)
    {
        if (patch.Duophonic)
        {
            _portamentoPitch = _voiceHigh.Frequency;
            return;
        }

        var portamento = Math.Max(0.001, patch.Portamento);
        if (portamento <= 0.001)
        {
            _portamentoPitch = _targetPitch;
            return;
        }

        var alpha = 1f - MathF.Exp(-1f / (float)(portamento * sampleRate * 0.25));
        _portamentoPitch += (_targetPitch - _portamentoPitch) * alpha;
        _voiceHigh.Frequency = _portamentoPitch;
    }

    private static double FrequencyForOsc(Footage footage, double baseFrequency, double lfoDepth, double lfoValue)
    {
        var octaveShift = footage switch
        {
            Footage.Foot32 => -2,
            Footage.Foot16 => -1,
            Footage.Foot8 => 0,
            Footage.Foot4 => 1,
            Footage.Foot2 => 2,
            _ => 0,
        };

        var freq = baseFrequency * Math.Pow(2, octaveShift);
        return freq * Math.Pow(2, lfoDepth * lfoValue / 12.0);
    }

    private static double OscillatorSample(WaveformType waveform, double frequency, int sampleRate, VoiceState voice, bool useSecondaryPhase)
    {
        if (useSecondaryPhase)
        {
            voice.PhaseOsc2 += frequency / sampleRate;
            if (voice.PhaseOsc2 >= 1.0)
            {
                voice.PhaseOsc2 -= 1.0;
            }

            return Wave(waveform, voice.PhaseOsc2);
        }

        voice.PhaseOsc1 += frequency / sampleRate;
        if (voice.PhaseOsc1 >= 1.0)
        {
            voice.PhaseOsc1 -= 1.0;
        }

        return Wave(waveform, voice.PhaseOsc1);
    }

    private static double Wave(WaveformType waveform, double phase) =>
        waveform switch
        {
            WaveformType.Triangle => 4.0 * Math.Abs(phase - 0.5) - 1.0,
            WaveformType.Saw => 2.0 * phase - 1.0,
            WaveformType.Square => phase < 0.5 ? 1.0 : -1.0,
            WaveformType.PulseNarrow => phase < 0.25 ? 1.0 : -1.0,
            WaveformType.PulseWide => phase < 0.75 ? 1.0 : -1.0,
            _ => Math.Sin(phase * Math.PI * 2.0),
        };

    private static float MidiToFrequency(int note) => 440f * MathF.Pow(2f, (note - 69) / 12f);

    private sealed class VoiceState
    {
        public bool IsActive { get; private set; }
        public int MidiNote { get; private set; }
        public float Frequency { get; set; }
        public double PhaseOsc1 { get; set; }
        public double PhaseOsc2 { get; set; }
        public PolivoksFilterState FilterState { get; } = new();

        public void Activate(int midiNote, float frequency, float velocity)
        {
            IsActive = true;
            MidiNote = midiNote;
            Frequency = frequency;
            _ = velocity;
        }

        public void Deactivate() => IsActive = false;
    }
}
