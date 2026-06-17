using Polivoks.Core.Models;
using Polivoks.Core.Persistence;
using Xunit;

namespace Polivoks.Tests;

public class PersistenceTests
{
    [Fact]
    public void Settings_and_state_roundtrip_through_json()
    {
        var root = Path.Combine(Path.GetTempPath(), "polivoks-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);

        try
        {
            var paths = new AppDataPaths(root);
            var persistence = new JsonPersistenceService(paths);

            var settings = new AppSettings
            {
                OutputDeviceId = "device-1",
                SampleRate = 48000,
                BitDepth = AudioBitDepth.Bit24,
                BufferMilliseconds = 20,
                UseExclusiveMode = false,
                Language = AppLanguage.Russian,
            };

            var state = new AppState
            {
                Patch = new SynthPatch
                {
                    FilterCutoff = 0.42,
                    Osc1Waveform = WaveformType.Square,
                    Duophonic = true,
                },
            };

            persistence.SaveSettings(settings);
            persistence.SaveState(state);

            var loadedSettings = persistence.LoadSettings();
            var loadedState = persistence.LoadState();

            Assert.Equal(settings.OutputDeviceId, loadedSettings.OutputDeviceId);
            Assert.Equal(settings.SampleRate, loadedSettings.SampleRate);
            Assert.Equal(settings.Language, loadedSettings.Language);
            Assert.Equal(state.Patch.FilterCutoff, loadedState.Patch.FilterCutoff);
            Assert.Equal(state.Patch.Osc1Waveform, loadedState.Patch.Osc1Waveform);
            Assert.True(loadedState.Patch.Duophonic);
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
