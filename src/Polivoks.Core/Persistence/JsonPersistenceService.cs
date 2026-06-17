using System.Text.Json;
using System.Text.Json.Serialization;
using Polivoks.Core.Models;

namespace Polivoks.Core.Persistence;

public sealed class JsonPersistenceService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly IAppDataPaths _paths;

    public JsonPersistenceService(IAppDataPaths paths) => _paths = paths;

    public AppSettings LoadSettings()
    {
        if (!File.Exists(_paths.SettingsFile))
        {
            return new AppSettings();
        }

        var json = File.ReadAllText(_paths.SettingsFile);
        return JsonSerializer.Deserialize<AppSettings>(json, Options) ?? new AppSettings();
    }

    public void SaveSettings(AppSettings settings) =>
        File.WriteAllText(_paths.SettingsFile, JsonSerializer.Serialize(settings, Options));

    public AppState LoadState()
    {
        if (!File.Exists(_paths.StateFile))
        {
            return new AppState();
        }

        var json = File.ReadAllText(_paths.StateFile);
        return JsonSerializer.Deserialize<AppState>(json, Options) ?? new AppState();
    }

    public void SaveState(AppState state) =>
        File.WriteAllText(_paths.StateFile, JsonSerializer.Serialize(state, Options));

    public IReadOnlyList<PresetDocument> ListPresets()
    {
        if (!Directory.Exists(_paths.PresetsDirectory))
        {
            return [];
        }

        return Directory.EnumerateFiles(_paths.PresetsDirectory, "*.json")
            .Select(path =>
            {
                var json = File.ReadAllText(path);
                return JsonSerializer.Deserialize<PresetDocument>(json, Options);
            })
            .Where(p => p is not null)
            .Cast<PresetDocument>()
            .OrderBy(p => p.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public void SavePreset(PresetDocument preset)
    {
        var safeName = string.Join("_", preset.Name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        var path = Path.Combine(_paths.PresetsDirectory, $"{safeName}.json");
        File.WriteAllText(path, JsonSerializer.Serialize(preset, Options));
    }

    public void DeletePreset(string name)
    {
        var safeName = string.Join("_", name.Split(Path.GetInvalidFileNameChars(), StringSplitOptions.RemoveEmptyEntries));
        var path = Path.Combine(_paths.PresetsDirectory, $"{safeName}.json");
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }
}
