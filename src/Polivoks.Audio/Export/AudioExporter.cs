using NAudio.Lame;
using NAudio.Wave;
using Polivoks.Core.Models;

namespace Polivoks.Audio.Export;

public static class AudioExporter
{
    public static void Export(
        float[] monoSamples,
        int sampleRate,
        string outputPath,
        AudioExportFormat format,
        AudioBitDepth bitDepth,
        int mp3BitrateKbps)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        switch (format)
        {
            case AudioExportFormat.Wav:
                WriteWav(monoSamples, sampleRate, outputPath, bitDepth);
                break;
            case AudioExportFormat.Mp3:
                WriteMp3(monoSamples, sampleRate, outputPath, mp3BitrateKbps);
                break;
            case AudioExportFormat.Flac:
                WriteFlac(monoSamples, sampleRate, outputPath, bitDepth);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(format));
        }
    }

    private static void WriteWav(float[] samples, int sampleRate, string path, AudioBitDepth bitDepth)
    {
        if (bitDepth == AudioBitDepth.Bit16)
        {
            using var writer = new WaveFileWriter(path, new WaveFormat(sampleRate, 16, 1));
            var buffer = new byte[samples.Length * 2];
            for (var i = 0; i < samples.Length; i++)
            {
                var value = (short)Math.Clamp(samples[i] * short.MaxValue, short.MinValue, short.MaxValue);
                BitConverter.TryWriteBytes(buffer.AsSpan(i * 2), value);
            }

            writer.Write(buffer, 0, buffer.Length);
            return;
        }

        using var floatWriter = new WaveFileWriter(path, WaveFormat.CreateIeeeFloatWaveFormat(sampleRate, 1));
        var floatBuffer = new byte[samples.Length * 4];
        Buffer.BlockCopy(samples, 0, floatBuffer, 0, floatBuffer.Length);
        floatWriter.Write(floatBuffer, 0, floatBuffer.Length);
    }

    private static void WriteMp3(float[] samples, int sampleRate, string path, int bitrateKbps)
    {
        var tempWav = Path.ChangeExtension(path, ".tmp.wav");
        WriteWav(samples, sampleRate, tempWav, AudioBitDepth.Bit16);
        try
        {
            using var reader = new AudioFileReader(tempWav);
            using var writer = new LameMP3FileWriter(path, reader.WaveFormat, bitrateKbps * 1000);
            reader.CopyTo(writer);
        }
        finally
        {
            if (File.Exists(tempWav))
            {
                File.Delete(tempWav);
            }
        }
    }

    private static void WriteFlac(float[] samples, int sampleRate, string path, AudioBitDepth bitDepth)
    {
        // TODO: wire FlacLibSharp encoder (package is netfx-compatible). WAV fallback for MVP.
        var wavPath = Path.ChangeExtension(path, ".wav");
        WriteWav(samples, sampleRate, wavPath, bitDepth == AudioBitDepth.Bit32Float ? AudioBitDepth.Bit16 : bitDepth);
        throw new NotSupportedException(
            "FLAC export is not wired yet in MVP. A WAV file was written next to the requested path. See README.");
    }
}
