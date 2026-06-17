using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.Json;
using Polivoks.Core.Models;

namespace Polivoks.Resources.Localization;

public sealed class LocalizationManager : INotifyPropertyChanged
{
    private static LocalizationManager? _current;
    private readonly Dictionary<string, string> _strings;

    private LocalizationManager(Dictionary<string, string> strings) => _strings = strings;

    public static LocalizationManager Current => _current ??= Load(AppLanguage.English);

    public static event EventHandler? LanguageChanged;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string this[string key] => _strings.TryGetValue(key, out var value) ? value : key;

    public static void SetLanguage(AppLanguage language)
    {
        _current = Load(language);
        _current.OnPropertyChanged(nameof(Current));
        _current.OnPropertyChanged("Item[]");
        LanguageChanged?.Invoke(null, EventArgs.Empty);
    }

    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

    private static LocalizationManager Load(AppLanguage language)
    {
        var file = language switch
        {
            AppLanguage.Russian => "ru.json",
            AppLanguage.ChineseSimplified => "zh-Hans.json",
            _ => "en.json",
        };

        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(file, StringComparison.OrdinalIgnoreCase));

        if (resourceName is null)
        {
            return new LocalizationManager(new Dictionary<string, string>(StringComparer.Ordinal));
        }

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        var json = reader.ReadToEnd();
        var map = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? [];
        return new LocalizationManager(map);
    }

    public static CultureInfo CultureFor(AppLanguage language) => language switch
    {
        AppLanguage.Russian => new CultureInfo("ru-RU"),
        AppLanguage.ChineseSimplified => new CultureInfo("zh-Hans"),
        _ => CultureInfo.GetCultureInfo("en-US"),
    };
}
