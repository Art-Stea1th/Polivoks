using System.Reflection;
using System.Text.Json;
using Polivoks.Core.Models;
using Polivoks.Resources.Localization;
using Xunit;

namespace Polivoks.Tests;

public class LocalizationTests
{
    private static readonly string[] RequiredKeys =
    [
        "AppTitle",
        "SectionModulator",
        "SectionFilter",
        "LfoRate",
        "ModDepth",
        "AmpLfoDepth",
        "TipLfoRate",
    ];

    [Theory]
    [InlineData(AppLanguage.English, "en.json")]
    [InlineData(AppLanguage.Russian, "ru.json")]
    [InlineData(AppLanguage.ChineseSimplified, "zh-Hans.json")]
    public void Embedded_localization_contains_required_keys(AppLanguage language, string fileName)
    {
        LocalizationManager.SetLanguage(language);
        var map = LoadEmbeddedMap(fileName);

        foreach (var key in RequiredKeys)
        {
            Assert.True(map.ContainsKey(key), $"Missing key '{key}' in {fileName}");
            Assert.False(string.IsNullOrWhiteSpace(map[key]), $"Empty value for '{key}' in {fileName}");
        }

        Assert.False(string.IsNullOrWhiteSpace(LocalizationManager.Current["AppTitle"]));
    }

    private static Dictionary<string, string> LoadEmbeddedMap(string fileName)
    {
        var assembly = typeof(LocalizationManager).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .First(name => name.EndsWith(fileName, StringComparison.OrdinalIgnoreCase));

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);
        return JsonSerializer.Deserialize<Dictionary<string, string>>(reader.ReadToEnd()) ?? [];
    }
}
