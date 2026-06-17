using System.IO;
using System.Windows;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Melanchall.DryWetMidi.Interaction;
using Microsoft.Win32;
using Polivoks.Audio.Export;
using Polivoks.Core.Models;
using Polivoks.Core.Persistence;
using Polivoks.Desktop.Services;
using Polivoks.Resources.Localization;

namespace Polivoks.Desktop.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly AppServices _services;
    private Melanchall.DryWetMidi.Core.MidiFile? _loadedMidi;
    private bool _patchDirty = true;
    private string _audioInfo = string.Empty;
    private string _patchInfo = string.Empty;

    public MainViewModel(AppServices services)
    {
        _services = services;
        Patch = _services.Engine.Patch;
        LocalizationManager.LanguageChanged += (_, _) => OnPropertyChanged(nameof(WindowTitle));
        RefreshRuntimeInfo();
    }

    public SynthPatch Patch { get; }

    public string WindowTitle => LocalizationManager.Current["AppTitle"];

    public string AudioInfo => _audioInfo;

    public string PatchInfo => _patchInfo;

    [RelayCommand]
    private void OpenSettings()
    {
        var vm = new SettingsViewModel(_services);
        var window = new Views.SettingsWindow
        {
            DataContext = vm,
            Owner = Application.Current.MainWindow,
        };
        window.ShowDialog();
        OnPropertyChanged(nameof(WindowTitle));
    }

    [RelayCommand]
    private void SavePreset()
    {
        var name = Views.InputDialog.Show("Preset", LocalizationManager.Current["PresetSave"], "My Patch");
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        _services.Persistence.SavePreset(new PresetDocument
        {
            Name = name.Trim(),
            Patch = Patch.Clone(),
        });
        StatusMessage = $"Preset saved: {name}";
    }

    [RelayCommand]
    private void LoadPreset()
    {
        var presets = _services.Persistence.ListPresets();
        if (presets.Count == 0)
        {
            StatusMessage = "No presets found.";
            return;
        }

        var name = presets[0].Name;
        ApplyPatch(presets[0].Patch);
        StatusMessage = $"Loaded preset: {name}";
    }

    [RelayCommand]
    private void ToggleRecording()
    {
        if (_services.Recorder.IsRecording)
        {
            _services.Recorder.Stop();
            StatusMessage = "Recording stopped.";
        }
        else
        {
            _services.Recorder.Start();
            StatusMessage = "Recording…";
        }
    }

    [RelayCommand]
    private void ExportRecording()
    {
        var samples = _services.Recorder.ToArray();
        if (samples.Length == 0)
        {
            StatusMessage = "Nothing recorded yet.";
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "WAV|*.wav|MP3|*.mp3|FLAC|*.flac",
            FileName = $"polivoks_{DateTime.Now:yyyyMMdd_HHmmss}",
        };

        if (dialog.ShowDialog(Application.Current.MainWindow) != true)
        {
            return;
        }

        var format = dialog.FileName.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)
            ? AudioExportFormat.Mp3
            : dialog.FileName.EndsWith(".flac", StringComparison.OrdinalIgnoreCase)
                ? AudioExportFormat.Flac
                : AudioExportFormat.Wav;

        try
        {
            AudioExporter.Export(
                samples,
                _services.Settings.SampleRate,
                dialog.FileName,
                format,
                _services.Settings.BitDepth,
                _services.Settings.DefaultMp3BitrateKbps);
            StatusMessage = $"Exported: {dialog.FileName}";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void LoadMidi()
    {
        var dialog = new OpenFileDialog { Filter = "MIDI|*.mid;*.midi" };
        if (dialog.ShowDialog(Application.Current.MainWindow) != true)
        {
            return;
        }

        _loadedMidi = _services.MidiFiles.Load(dialog.FileName);
        _services.State.LastMidiFilePath = dialog.FileName;
        StatusMessage = $"MIDI loaded: {Path.GetFileName(dialog.FileName)}";
    }

    [RelayCommand]
    private async Task PlayMidiAsync()
    {
        if (_loadedMidi is null)
        {
            StatusMessage = "Load a MIDI file first.";
            return;
        }

        StatusMessage = "Playing MIDI…";
        await _services.MidiPreview.PlayAsync(_loadedMidi, _loadedMidi.GetTempoMap(), _services.Settings.SampleRate);
    }

    [RelayCommand]
    private void StopMidi() => _services.MidiPreview.Stop();

    [RelayCommand]
    private void ConvertMidiToAudio()
    {
        if (_loadedMidi is null)
        {
            StatusMessage = "Load a MIDI file first.";
            return;
        }

        var dialog = new SaveFileDialog
        {
            Filter = "WAV|*.wav|MP3|*.mp3",
            FileName = $"polivoks_midi_{DateTime.Now:yyyyMMdd_HHmmss}.wav",
        };

        if (dialog.ShowDialog(Application.Current.MainWindow) != true)
        {
            return;
        }

        var samples = _services.MidiRenderer.Render(_loadedMidi, _services.Engine, _services.Settings.SampleRate);
        var format = dialog.FileName.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase)
            ? AudioExportFormat.Mp3
            : AudioExportFormat.Wav;

        AudioExporter.Export(
            samples,
            _services.Settings.SampleRate,
            dialog.FileName,
            format,
            _services.Settings.BitDepth,
            _services.Settings.DefaultMp3BitrateKbps);
        StatusMessage = $"Rendered MIDI: {dialog.FileName}";
    }

    [RelayCommand]
    private void NoteOn(string note)
    {
        if (int.TryParse(note, out var midi))
        {
            _services.Engine.NoteOn(midi);
        }
    }

    [RelayCommand]
    private void NoteOff(string note)
    {
        if (int.TryParse(note, out var midi))
        {
            _services.Engine.NoteOff(midi);
        }
    }

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public void MarkPatchDirty() => _patchDirty = true;

    public void SyncPatchToEngine()
    {
        if (!_patchDirty)
        {
            return;
        }

        _services.Engine.Patch = Patch.Clone();
        _patchDirty = false;
    }

    public void RefreshRuntimeInfo()
    {
        var audioInfo = $"{_services.Settings.SampleRate} Hz | {_services.Settings.BitDepth}-bit | {_services.Settings.BufferMilliseconds} ms";
        var patchInfo = $"CUT {Patch.FilterCutoff:P0} | RES {Patch.FilterResonance:P0} | VOL {Patch.MasterVolume:P0} | OSC {Patch.MixOsc1:P0}/{Patch.MixOsc2:P0}/{Patch.MixNoise:P0}";
        if (_audioInfo != audioInfo)
        {
            _audioInfo = audioInfo;
            OnPropertyChanged(nameof(AudioInfo));
        }

        if (_patchInfo != patchInfo)
        {
            _patchInfo = patchInfo;
            OnPropertyChanged(nameof(PatchInfo));
        }
    }

    private void ApplyPatch(SynthPatch patch)
    {
        CopyPatch(patch, Patch);
        MarkPatchDirty();
        SyncPatchToEngine();
    }

    private static void CopyPatch(SynthPatch source, SynthPatch target)
    {
        target.MasterTune = source.MasterTune;
        target.MasterVolume = source.MasterVolume;
        target.HeadphoneVolume = source.HeadphoneVolume;
        target.Portamento = source.Portamento;
        target.MainOutputEnabled = source.MainOutputEnabled;
        target.Duophonic = source.Duophonic;
        target.GateOn = source.GateOn;
        target.LfoRate = source.LfoRate;
        target.LfoWaveform = source.LfoWaveform;
        target.Osc1Footage = source.Osc1Footage;
        target.Osc1Waveform = source.Osc1Waveform;
        target.Osc1LfoDepth = source.Osc1LfoDepth;
        target.Osc1FmDepth = source.Osc1FmDepth;
        target.Osc2Footage = source.Osc2Footage;
        target.Osc2Waveform = source.Osc2Waveform;
        target.Osc2Detune = source.Osc2Detune;
        target.Osc2LfoDepth = source.Osc2LfoDepth;
        target.MixOsc1 = source.MixOsc1;
        target.MixOsc2 = source.MixOsc2;
        target.MixNoise = source.MixNoise;
        target.MixExternal = source.MixExternal;
        target.FilterMode = source.FilterMode;
        target.FilterCutoff = source.FilterCutoff;
        target.FilterResonance = source.FilterResonance;
        target.FilterEnvDepth = source.FilterEnvDepth;
        target.FilterLfoDepth = source.FilterLfoDepth;
        target.FilterAttack = source.FilterAttack;
        target.FilterDecay = source.FilterDecay;
        target.FilterSustain = source.FilterSustain;
        target.FilterRelease = source.FilterRelease;
        target.FilterEnvelopeLoop = source.FilterEnvelopeLoop;
        target.AmpAttack = source.AmpAttack;
        target.AmpDecay = source.AmpDecay;
        target.AmpSustain = source.AmpSustain;
        target.AmpRelease = source.AmpRelease;
        target.AmpEnvelopeLoop = source.AmpEnvelopeLoop;
        target.AmpLfoDepth = source.AmpLfoDepth;
    }
}
