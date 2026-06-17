using Melanchall.DryWetMidi.Core;
using Melanchall.DryWetMidi.Interaction;
using Polivoks.Core.Synth;

namespace Polivoks.Audio.Midi;

public sealed class MidiFileService
{
    public MidiFile Load(string path) => MidiFile.Read(path);

    public IReadOnlyList<TimedNote> ExtractNotes(MidiFile file)
    {
        var notes = file.GetNotes();
        return notes
            .Select(n => new TimedNote((int)n.NoteNumber, n.Time, n.Length, n.Velocity / 127f))
            .OrderBy(n => n.Start)
            .ToList();
    }
}

public readonly record struct TimedNote(int MidiNote, long Start, long Length, float Velocity);

public sealed class MidiPreviewService : IDisposable
{
    private readonly PolivoksEngine _engine;
    private CancellationTokenSource? _cts;

    public MidiPreviewService(PolivoksEngine engine) => _engine = engine;

    public async Task PlayAsync(MidiFile file, TempoMap tempoMap, int sampleRate, Action<int, float>? noteOn = null, CancellationToken cancellationToken = default)
    {
        _cts?.Cancel();
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        var token = _cts.Token;
        var notes = file.GetNotes().OrderBy(n => n.Time).ToList();
        var clock = new MidiClock(tempoMap);

        foreach (var note in notes)
        {
            token.ThrowIfCancellationRequested();
            var startMs = clock.GetAbsoluteTimeInMilliseconds(note.Time);
            await Task.Delay(TimeSpan.FromMilliseconds(startMs), token);
            _engine.NoteOn(note.NoteNumber, note.Velocity / 127f);
            noteOn?.Invoke(note.NoteNumber, note.Velocity / 127f);
            var lengthMs = clock.GetAbsoluteTimeInMilliseconds(note.Time + note.Length) - startMs;
            _ = Task.Run(async () =>
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMilliseconds(Math.Max(1, lengthMs)), token);
                    _engine.NoteOff(note.NoteNumber);
                }
                catch (OperationCanceledException)
                {
                    // ignore
                }
            }, token);
        }
    }

    public void Stop()
    {
        _cts?.Cancel();
        _engine.AllNotesOff();
    }

    public void Dispose() => Stop();
}

public sealed class MidiOfflineRenderer
{
    public float[] Render(MidiFile file, PolivoksEngine engine, int sampleRate)
    {
        var tempoMap = file.GetTempoMap();
        var notes = file.GetNotes().OrderBy(n => n.Time).ToList();
        if (notes.Count == 0)
        {
            return [];
        }

        var lastEnd = notes.Max(n => n.Time + n.Length);
        var clock = new MidiClock(tempoMap);
        var totalMs = clock.GetAbsoluteTimeInMilliseconds(lastEnd) + 500;
        var totalSamples = (int)(totalMs / 1000.0 * sampleRate);
        var buffer = new float[totalSamples];
        var events = new List<(int Sample, int Note, bool On, float Velocity)>();

        foreach (var note in notes)
        {
            var startSample = (int)(clock.GetAbsoluteTimeInMilliseconds(note.Time) / 1000.0 * sampleRate);
            var endSample = (int)(clock.GetAbsoluteTimeInMilliseconds(note.Time + note.Length) / 1000.0 * sampleRate);
            events.Add((startSample, note.NoteNumber, true, note.Velocity / 127f));
            events.Add((endSample, note.NoteNumber, false, 0f));
        }

        events = events.OrderBy(e => e.Sample).ToList();
        var eventIndex = 0;
        var block = new float[512];
        var writePos = 0;

        while (writePos < totalSamples)
        {
            var blockSize = Math.Min(block.Length, totalSamples - writePos);
            while (eventIndex < events.Count && events[eventIndex].Sample <= writePos)
            {
                var ev = events[eventIndex++];
                if (ev.On)
                {
                    engine.NoteOn(ev.Note, ev.Velocity);
                }
                else
                {
                    engine.NoteOff(ev.Note);
                }
            }

            engine.Render(block.AsSpan(0, blockSize), sampleRate);
            Array.Copy(block, 0, buffer, writePos, blockSize);
            writePos += blockSize;
        }

        engine.AllNotesOff();
        return buffer;
    }
}

internal sealed class MidiClock
{
    private readonly TempoMap _tempoMap;

    public MidiClock(TempoMap tempoMap) => _tempoMap = tempoMap;

    public double GetAbsoluteTimeInMilliseconds(long metricTime) =>
        TimeConverter.ConvertTo<MetricTimeSpan>(metricTime, _tempoMap).TotalMicroseconds / 1000.0;
}
