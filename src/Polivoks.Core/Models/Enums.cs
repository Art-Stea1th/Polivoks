namespace Polivoks.Core.Models;

public enum WaveformType
{
    Triangle = 0,
    Saw = 1,
    Square = 2,
    PulseNarrow = 3,
    PulseWide = 4,
}

public enum LfoWaveformType
{
    Triangle = 0,
    Square = 1,
    Noise = 2,
    SampleHold = 3,
}

public enum FilterMode
{
    LowPass = 0,
    BandPass = 1,
}

public enum Footage
{
    Foot32 = 0,
    Foot16 = 1,
    Foot8 = 2,
    Foot4 = 3,
    Foot2 = 4,
}

public enum AudioExportFormat
{
    Wav = 0,
    Flac = 1,
    Mp3 = 2,
}

public enum AudioBitDepth
{
    Bit16 = 16,
    Bit24 = 24,
    Bit32Float = 32,
}

public enum AppLanguage
{
    English = 0,
    Russian = 1,
    ChineseSimplified = 2,
}

public static class SampleRateOptions
{
    public static readonly int[] Supported = [44100, 48000, 88200, 96000];
}

public static class Mp3BitrateOptions
{
    public static readonly int[] Supported = [128, 192, 256, 320];
}
