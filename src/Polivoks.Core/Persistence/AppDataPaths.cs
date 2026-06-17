namespace Polivoks.Core.Persistence;

public interface IAppDataPaths
{
    string RootDirectory { get; }
    string SettingsFile { get; }
    string StateFile { get; }
    string PresetsDirectory { get; }
    string RecordingsDirectory { get; }
    string KeyBindingsFile { get; }
}

public sealed class AppDataPaths : IAppDataPaths
{
    public AppDataPaths(string rootDirectory)
    {
        RootDirectory = rootDirectory;
        Directory.CreateDirectory(rootDirectory);
        Directory.CreateDirectory(PresetsDirectory);
        Directory.CreateDirectory(RecordingsDirectory);
    }

    public string RootDirectory { get; }
    public string SettingsFile => Path.Combine(RootDirectory, "settings.json");
    public string StateFile => Path.Combine(RootDirectory, "app-state.json");
    public string PresetsDirectory => Path.Combine(RootDirectory, "presets");
    public string RecordingsDirectory => Path.Combine(RootDirectory, "recordings");
    public string KeyBindingsFile => Path.Combine(RootDirectory, "keybindings.json");
}
