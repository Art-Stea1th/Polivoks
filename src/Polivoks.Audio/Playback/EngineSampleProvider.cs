using NAudio.Wave;
using Polivoks.Audio.Recording;
using Polivoks.Core.Synth;

namespace Polivoks.Audio.Playback;

public sealed class EngineSampleProvider : ISampleProvider
{
    private readonly PolivoksEngine _engine;
    private readonly WaveFormat _format;
    private readonly float[] _monoBuffer = new float[8192];
    private readonly AudioRecorder? _recorder;

    public EngineSampleProvider(PolivoksEngine engine, WaveFormat format, AudioRecorder? recorder = null)
    {
        _engine = engine;
        _format = WaveFormat.CreateIeeeFloatWaveFormat(format.SampleRate, 1);
        _recorder = recorder;
    }

    public WaveFormat WaveFormat => _format;

    public int Read(float[] buffer, int offset, int count)
    {
        var frames = count;
        if (frames > _monoBuffer.Length)
        {
            frames = _monoBuffer.Length;
        }

        _engine.Render(_monoBuffer.AsSpan(0, frames), _format.SampleRate);
        _recorder?.CaptureBlock(_monoBuffer.AsSpan(0, frames));
        Array.Copy(_monoBuffer, 0, buffer, offset, frames);
        return frames;
    }
}
