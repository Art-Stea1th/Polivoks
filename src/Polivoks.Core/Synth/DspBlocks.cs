using Polivoks.Core.Models;

namespace Polivoks.Core.Synth;

internal sealed class PolivoksFilterState
{
    public float Low;
    public float Band;
}

internal static class PolivoksFilter
{
    public static float Process(PolivoksFilterState state, float input, float cutoffNorm, float resonanceNorm, FilterMode mode, int sampleRate)
    {
        var cutoffHz = 80f + cutoffNorm * cutoffNorm * 12000f;
        var q = 0.5f + resonanceNorm * 8f;
        var f = MathF.Tan(MathF.PI * cutoffHz / sampleRate);
        var invQ = 1f / q;
        var g = f / (1f + f * (f + invQ));
        var drive = 1f + resonanceNorm * 2.5f;
        var x = MathF.Tanh(input * drive);

        state.Low += g * (x - state.Low - invQ * state.Band);
        state.Band += g * state.Low;

        var lp = state.Low;
        var bp = state.Band;
        var output = mode == FilterMode.BandPass ? bp : lp;
        return MathF.Tanh(output * (1f + resonanceNorm));
    }
}

internal sealed class AdsrEnvelope
{
    private enum Stage { Idle, Attack, Decay, Sustain, Release, LoopAttack, LoopDecay }

    private Stage _stage = Stage.Idle;
    private float _level;
    private float _attack;
    private float _decay;
    private float _sustain;
    private float _release;
    private bool _loop;

    public float Level => _level;

    public void Trigger(double attack, double decay, double sustain, double release, bool loop)
    {
        _attack = (float)Math.Max(0.001, attack);
        _decay = (float)Math.Max(0.001, decay);
        _sustain = (float)Math.Clamp(sustain, 0, 1);
        _release = (float)Math.Max(0.001, release);
        _loop = loop;
        _stage = Stage.Attack;
    }

    public void Release() => _stage = Stage.Release;

    public float Next(int sampleRate)
    {
        var dt = 1f / sampleRate;
        switch (_stage)
        {
            case Stage.Attack:
                _level += dt / _attack;
                if (_level >= 1f)
                {
                    _level = 1f;
                    _stage = Stage.Decay;
                }

                break;
            case Stage.Decay:
                _level -= dt / _decay * (1f - _sustain);
                if (_level <= _sustain)
                {
                    _level = _sustain;
                    _stage = _loop && _sustain <= 0.001f ? Stage.LoopAttack : Stage.Sustain;
                }

                break;
            case Stage.Sustain:
                break;
            case Stage.Release:
                _level -= dt / _release * _level;
                if (_level <= 0.0001f)
                {
                    _level = 0f;
                    _stage = Stage.Idle;
                }

                break;
            case Stage.LoopAttack:
                _level += dt / _attack;
                if (_level >= 1f)
                {
                    _level = 1f;
                    _stage = Stage.LoopDecay;
                }

                break;
            case Stage.LoopDecay:
                _level -= dt / _decay;
                if (_level <= 0f)
                {
                    _level = 0f;
                    _stage = Stage.LoopAttack;
                }

                break;
        }

        return Math.Clamp(_level, 0f, 1f);
    }
}

internal sealed class Lfo
{
    private double _phase;

    public double NextSample(
        double rateNorm,
        LfoWaveformType waveform,
        int sampleRate,
        ref double noisePhase,
        ref float sampleHold,
        ref int holdCounter,
        Random random)
    {
        var hz = 0.05 + rateNorm * 20.0;
        _phase += hz / sampleRate;
        if (_phase >= 1.0)
        {
            _phase -= 1.0;
        }

        return waveform switch
        {
            LfoWaveformType.Triangle => 4.0 * Math.Abs(_phase - 0.5) - 1.0,
            LfoWaveformType.Square => _phase < 0.5 ? 1.0 : -1.0,
            LfoWaveformType.Noise => random.NextDouble() * 2.0 - 1.0,
            LfoWaveformType.SampleHold => SampleHold(ref holdCounter, ref sampleHold, sampleRate, hz, random),
            _ => Math.Sin(_phase * Math.PI * 2.0),
        };
    }

    private static double SampleHold(ref int counter, ref float value, int sampleRate, double hz, Random random)
    {
        var holdSamples = Math.Max(1, (int)(sampleRate / hz));
        if (counter-- <= 0)
        {
            counter = holdSamples;
            value = (float)(random.NextDouble() * 2.0 - 1.0);
        }

        return value;
    }
}
