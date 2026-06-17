using System.Windows;
using System.Windows.Controls.Primitives;

namespace Polivoks.Resources.Controls;

public sealed class SynthToggle : ToggleButton
{
    public static readonly DependencyProperty LabelProperty = DependencyProperty.Register(
        nameof(Label),
        typeof(string),
        typeof(SynthToggle),
        new PropertyMetadata(string.Empty));

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    static SynthToggle()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(SynthToggle), new FrameworkPropertyMetadata(typeof(SynthToggle)));
    }
}
