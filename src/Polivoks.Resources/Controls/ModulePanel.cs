using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Polivoks.Resources.Controls;

public sealed class ModulePanel : ContentControl
{
    public static readonly DependencyProperty TitleProperty = DependencyProperty.Register(
        nameof(Title), typeof(string), typeof(ModulePanel), new PropertyMetadata(string.Empty));

    public static readonly DependencyProperty SubtitleProperty = DependencyProperty.Register(
        nameof(Subtitle), typeof(string), typeof(ModulePanel), new PropertyMetadata(string.Empty));

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    static ModulePanel()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(ModulePanel), new FrameworkPropertyMetadata(typeof(ModulePanel)));
    }
}
