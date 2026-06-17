using NAudio.CoreAudioApi;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using Polivoks.Audio.Devices;
using Polivoks.Core.Models;

namespace Polivoks.Audio.Devices;

public sealed class AudioTestService
{
    public async Task PlayTestToneAsync(AppSettings settings, int durationMs = 1200, CancellationToken cancellationToken = default)
    {
        var device = AudioDeviceCatalog.ResolveDevice(settings.OutputDeviceId);
        var format = AudioDeviceCatalog.CreateWaveFormat(settings);
        var tone = new SignalGenerator(format.SampleRate, format.Channels)
        {
            Type = SignalGeneratorType.Sin,
            Frequency = 440,
            Gain = 0.2,
        }.Take(TimeSpan.FromMilliseconds(durationMs));

        using var output = new WasapiOut(device, settings.UseExclusiveMode ? AudioClientShareMode.Exclusive : AudioClientShareMode.Shared, false, settings.BufferMilliseconds);
        output.Init(tone);
        output.Play();

        try
        {
            await Task.Delay(durationMs, cancellationToken);
        }
        finally
        {
            output.Stop();
        }
    }
}
