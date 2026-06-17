using Polivoks.Audio.Devices;
using Polivoks.Audio.Export;
using Polivoks.Audio.Midi;
using Polivoks.Audio.Playback;
using Polivoks.Audio.Recording;
using Polivoks.Core.Models;
using Polivoks.Core.Persistence;
using Polivoks.Core.Synth;

namespace Polivoks.Desktop.Services;

public sealed class AppServices : IDisposable
{
    public AppServices(string appDataRoot)
    {
        Paths = new AppDataPaths(appDataRoot);
        Persistence = new JsonPersistenceService(Paths);
        Settings = Persistence.LoadSettings();
        State = Persistence.LoadState();

        Engine = new PolivoksEngine { Patch = State.Patch.Clone() };
        Recorder = new AudioRecorder();
        Output = new WasapiExclusiveOutputService(Engine, Recorder);
        Output.ApplySettings(Settings);
        AudioTest = new AudioTestService();
        MidiFiles = new MidiFileService();
        MidiPreview = new MidiPreviewService(Engine);
        MidiRenderer = new MidiOfflineRenderer();
    }

    public IAppDataPaths Paths { get; }
    public JsonPersistenceService Persistence { get; }
    public AppSettings Settings { get; private set; }
    public AppState State { get; private set; }
    public PolivoksEngine Engine { get; }
    public AudioRecorder Recorder { get; }
    public WasapiExclusiveOutputService Output { get; }
    public AudioTestService AudioTest { get; }
    public MidiFileService MidiFiles { get; }
    public MidiPreviewService MidiPreview { get; }
    public MidiOfflineRenderer MidiRenderer { get; }

    public void SaveAll()
    {
        State.Patch = Engine.Patch.Clone();
        Persistence.SaveSettings(Settings);
        Persistence.SaveState(State);
    }

    public void UpdateSettings(AppSettings settings)
    {
        Settings = settings;
        Output.ApplySettings(Settings);
        if (Output.IsRunning)
        {
            Output.Restart();
        }

        Persistence.SaveSettings(Settings);
    }

    public void Dispose()
    {
        SaveAll();
        Output.Dispose();
        MidiPreview.Dispose();
    }
}
