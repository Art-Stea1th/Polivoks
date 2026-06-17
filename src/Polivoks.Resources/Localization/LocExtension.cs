using System.Windows.Markup;
using Polivoks.Resources.Localization;

namespace Polivoks.Resources.Localization;

[MarkupExtensionReturnType(typeof(string))]
public sealed class LocExtension : MarkupExtension
{
    public LocExtension(string key) => Key = key;

    public string Key { get; }

    public override object ProvideValue(IServiceProvider serviceProvider) => LocalizationManager.Current[Key];
}
