using NAudio.CoreAudioApi;
using NAudio.Wave;
using Polivoks.Core.Models;

namespace Polivoks.Audio.Devices;

public sealed record AudioOutputDevice(string Id, string Name, bool IsDefault);

public static class AudioDeviceCatalog
{
    public static IReadOnlyList<AudioOutputDevice> ListOutputDevices()
    {
        using var enumerator = new MMDeviceEnumerator();
        var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active)
            .Select(d => new AudioOutputDevice(d.ID, d.FriendlyName, false))
            .ToList();

        try
        {
            var defaultId = enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia).ID;
            devices = devices
                .Select(d => d with { IsDefault = d.Id == defaultId })
                .OrderByDescending(d => d.IsDefault)
                .ThenBy(d => d.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch
        {
            // ignore missing default endpoint
        }

        return devices;
    }

    public static MMDevice ResolveDevice(string? deviceId)
    {
        using var enumerator = new MMDeviceEnumerator();
        if (!string.IsNullOrWhiteSpace(deviceId))
        {
            return enumerator.GetDevice(deviceId);
        }

        return enumerator.GetDefaultAudioEndpoint(DataFlow.Render, Role.Multimedia);
    }

    public static WaveFormat CreateWaveFormat(AppSettings settings) =>
        settings.BitDepth switch
        {
            AudioBitDepth.Bit16 => new WaveFormat(settings.SampleRate, 16, 2),
            _ => WaveFormat.CreateIeeeFloatWaveFormat(settings.SampleRate, 2),
        };
}
