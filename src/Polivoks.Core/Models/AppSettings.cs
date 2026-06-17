namespace Polivoks.Core.Models;

public sealed class AppSettings
{
    public string? OutputDeviceId { get; set; }
    public int SampleRate { get; set; } = 48000;
    public AudioBitDepth BitDepth { get; set; } = AudioBitDepth.Bit16;
    public int BufferMilliseconds { get; set; } = 10;
    public bool UseExclusiveMode { get; set; } = true;
    public AppLanguage Language { get; set; } = AppLanguage.English;
    public double WindowWidth { get; set; } = 1280;
    public double WindowHeight { get; set; } = 820;
    public double WindowLeft { get; set; } = 100;
    public double WindowTop { get; set; } = 80;
    public string RecordingsDirectory { get; set; } = "AppData/recordings";
    public string PresetsDirectory { get; set; } = "AppData/presets";
    public AudioExportFormat DefaultExportFormat { get; set; } = AudioExportFormat.Wav;
    public int DefaultMp3BitrateKbps { get; set; } = 192;
}

public sealed class AppState
{
    public SynthPatch Patch { get; set; } = new();
    public string? LastPresetName { get; set; }
    public string? LastMidiFilePath { get; set; }
}

public sealed class PresetDocument
{
    public required string Name { get; init; }
    public DateTimeOffset SavedAt { get; init; } = DateTimeOffset.UtcNow;
    public required SynthPatch Patch { get; init; }
}
