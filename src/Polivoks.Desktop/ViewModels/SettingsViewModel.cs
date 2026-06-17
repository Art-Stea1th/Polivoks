using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Polivoks.Audio.Devices;
using Polivoks.Core.Models;
using Polivoks.Desktop.Services;
using Polivoks.Resources.Localization;

namespace Polivoks.Desktop.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly AppServices _services;
    private readonly LanguageOption[] _languages =
    [
        new(AppLanguage.Russian, "Русский"),
        new(AppLanguage.English, "English (US)"),
        new(AppLanguage.ChineseSimplified, "中文 (简体)"),
    ];

    public SettingsViewModel(AppServices services)
    {
        _services = services;
        OutputDevices = new ObservableCollection<AudioOutputDevice>(AudioDeviceCatalog.ListOutputDevices());
        SelectedDeviceId = services.Settings.OutputDeviceId ?? OutputDevices.FirstOrDefault(d => d.IsDefault)?.Id;
        SampleRate = services.Settings.SampleRate;
        BitDepth = services.Settings.BitDepth;
        BufferMilliseconds = services.Settings.BufferMilliseconds;
        UseExclusiveMode = services.Settings.UseExclusiveMode;
        SelectedLanguage = _languages.FirstOrDefault(l => l.Value == services.Settings.Language) ?? _languages[1];
        DefaultMp3BitrateKbps = services.Settings.DefaultMp3BitrateKbps;
    }

    public ObservableCollection<AudioOutputDevice> OutputDevices { get; }

    [ObservableProperty] private string? _selectedDeviceId;
    [ObservableProperty] private int _sampleRate = 48000;
    [ObservableProperty] private AudioBitDepth _bitDepth = AudioBitDepth.Bit16;
    [ObservableProperty] private int _bufferMilliseconds = 10;
    [ObservableProperty] private bool _useExclusiveMode = true;
    [ObservableProperty] private LanguageOption _selectedLanguage = new(AppLanguage.English, "English (US)");
    [ObservableProperty] private int _defaultMp3BitrateKbps = 192;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public int[] SampleRates => SampleRateOptions.Supported;
    public AudioBitDepth[] BitDepths => [AudioBitDepth.Bit16, AudioBitDepth.Bit24, AudioBitDepth.Bit32Float];
    public IReadOnlyList<LanguageOption> Languages => _languages;
    public int[] Mp3Bitrates => Mp3BitrateOptions.Supported;

    [RelayCommand]
    private void RefreshDevices()
    {
        OutputDevices.Clear();
        foreach (var device in AudioDeviceCatalog.ListOutputDevices())
        {
            OutputDevices.Add(device);
        }
    }

    [RelayCommand]
    private async Task TestOutputAsync()
    {
        var draft = BuildSettings();
        try
        {
            StatusMessage = LocalizationManager.Current["TipTestOutput"];
            await _services.AudioTest.PlayTestToneAsync(draft);
            StatusMessage = "OK";
        }
        catch (Exception ex)
        {
            StatusMessage = ex.Message;
        }
    }

    [RelayCommand]
    private void Save()
    {
        var settings = BuildSettings();
        _services.UpdateSettings(settings);
        LocalizationManager.SetLanguage(settings.Language);
        System.Threading.Thread.CurrentThread.CurrentUICulture = LocalizationManager.CultureFor(settings.Language);
        StatusMessage = LocalizationManager.Current["Save"];
    }

    private AppSettings BuildSettings()
    {
        var settings = _services.Settings;
        settings.OutputDeviceId = SelectedDeviceId;
        settings.SampleRate = SampleRate;
        settings.BitDepth = BitDepth;
        settings.BufferMilliseconds = BufferMilliseconds;
        settings.UseExclusiveMode = UseExclusiveMode;
        settings.Language = SelectedLanguage.Value;
        settings.DefaultMp3BitrateKbps = DefaultMp3BitrateKbps;
        return settings;
    }

    public sealed record LanguageOption(AppLanguage Value, string DisplayName);
}
