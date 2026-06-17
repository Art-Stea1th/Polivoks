using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Polivoks.Audio.Devices;
using Polivoks.Audio.Recording;
using Polivoks.Core.Diagnostics;
using Polivoks.Core.Models;
using Polivoks.Core.Synth;

namespace Polivoks.Audio.Playback;

public sealed class WasapiExclusiveOutputService : IDisposable
{
    private readonly PolivoksEngine _engine;
    private readonly AudioRecorder? _recorder;
    private WasapiOut? _output;
    private AppSettings _settings = new();

    public WasapiExclusiveOutputService(PolivoksEngine engine, AudioRecorder? recorder = null)
    {
        _engine = engine;
        _recorder = recorder;
    }

    public bool IsRunning => _output?.PlaybackState == PlaybackState.Playing;

    public void ApplySettings(AppSettings settings)
    {
        _settings = settings;
        if (IsRunning)
        {
            Restart();
        }
    }

    public void Start()
    {
        Stop();

        if (_settings.UseExclusiveMode)
        {
            try
            {
                StartInternal(exclusive: true);
                return;
            }
            catch (Exception ex)
            {
                AppLog.Warn($"Exclusive WASAPI failed: {ex.Message}");
            }
        }

        StartInternal(exclusive: false);
    }

    private void StartInternal(bool exclusive)
    {
        var device = AudioDeviceCatalog.ResolveDevice(_settings.OutputDeviceId);
        var format = AudioDeviceCatalog.CreateWaveFormat(_settings);
        ISampleProvider graph = new EngineSampleProvider(_engine, format, _recorder);
        if (format.Channels > 1)
        {
            graph = new MonoToStereoSampleProvider(graph);
        }

        _output = new WasapiOut(
            device,
            exclusive ? AudioClientShareMode.Exclusive : AudioClientShareMode.Shared,
            false,
            _settings.BufferMilliseconds)
        {
            Volume = 1f,
        };
        _output.Init(graph.ToWaveProvider16());
        _output.Play();
        AppLog.Info($"WASAPI started ({(exclusive ? "exclusive" : "shared")}) on '{device.FriendlyName}'.");
    }

    public void Stop()
    {
        if (_output is null)
        {
            return;
        }

        _output.Stop();
        _output.Dispose();
        _output = null;
    }

    public void Restart()
    {
        if (!IsRunning)
        {
            Start();
            return;
        }

        Stop();
        Start();
    }

    public void Dispose() => Stop();
}
