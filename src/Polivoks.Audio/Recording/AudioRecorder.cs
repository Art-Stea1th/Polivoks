using Polivoks.Core.Synth;

namespace Polivoks.Audio.Recording;

public sealed class AudioRecorder
{
    private readonly List<float> _samples = [];
    private readonly object _sync = new();
    private bool _isRecording;

    public bool IsRecording => _isRecording;

    public void Start()
    {
        lock (_sync)
        {
            _samples.Clear();
            _isRecording = true;
        }
    }

    public void Stop()
    {
        lock (_sync)
        {
            _isRecording = false;
        }
    }

    public void CaptureBlock(ReadOnlySpan<float> monoBlock)
    {
        lock (_sync)
        {
            if (!_isRecording)
            {
                return;
            }

            foreach (var sample in monoBlock)
            {
                _samples.Add(sample);
            }
        }
    }

    public float[] ToArray()
    {
        lock (_sync)
        {
            return _samples.ToArray();
        }
    }
}

public sealed class RecordingTapProvider
{
    private readonly PolivoksEngine _engine;
    private readonly AudioRecorder _recorder;

    public RecordingTapProvider(PolivoksEngine engine, AudioRecorder recorder)
    {
        _engine = engine;
        _recorder = recorder;
    }

    public void Render(Span<float> buffer, int sampleRate)
    {
        _engine.Render(buffer, sampleRate);
        _recorder.CaptureBlock(buffer);
    }
}
