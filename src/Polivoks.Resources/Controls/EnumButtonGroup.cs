using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Polivoks.Resources.Controls;

public sealed class EnumButtonGroup : StackPanel
{
    public static readonly DependencyProperty SelectedValueProperty = DependencyProperty.Register(
        nameof(SelectedValue),
        typeof(object),
        typeof(EnumButtonGroup),
        new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnSelectedChanged));

    public static readonly DependencyProperty EnumTypeProperty = DependencyProperty.Register(
        nameof(EnumType),
        typeof(Type),
        typeof(EnumButtonGroup),
        new PropertyMetadata(null, OnStructureChanged));

    public static readonly DependencyProperty LabelsProperty = DependencyProperty.Register(
        nameof(Labels),
        typeof(string),
        typeof(EnumButtonGroup),
        new PropertyMetadata(string.Empty, OnStructureChanged));

    public static readonly DependencyProperty ColumnsProperty = DependencyProperty.Register(
        nameof(Columns),
        typeof(int),
        typeof(EnumButtonGroup),
        new PropertyMetadata(4, OnStructureChanged));

    private bool _updating;
    private UniformGrid? _grid;

    public object? SelectedValue
    {
        get => GetValue(SelectedValueProperty);
        set => SetValue(SelectedValueProperty, value);
    }

    public Type? EnumType
    {
        get => (Type?)GetValue(EnumTypeProperty);
        set => SetValue(EnumTypeProperty, value);
    }

    public string Labels
    {
        get => (string)GetValue(LabelsProperty);
        set => SetValue(LabelsProperty, value);
    }

    public int Columns
    {
        get => (int)GetValue(ColumnsProperty);
        set => SetValue(ColumnsProperty, value);
    }

    public EnumButtonGroup()
    {
        Loaded += (_, _) => Rebuild();
    }

    private static void OnStructureChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is EnumButtonGroup group && group.IsLoaded)
        {
            group.Rebuild();
        }
    }

    private static void OnSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is EnumButtonGroup group && !group._updating)
        {
            group.UpdateSelection();
        }
    }

    private void Rebuild()
    {
        Children.Clear();
        _grid = null;
        if (EnumType is null || !EnumType.IsEnum)
        {
            return;
        }

        var values = Enum.GetValues(EnumType);
        var labels = ParseLabels(values.Length);
        _grid = new UniformGrid { Columns = Math.Max(1, Columns), Margin = new Thickness(0, 2, 0, 2) };
        Children.Add(_grid);

        for (var i = 0; i < values.Length; i++)
        {
            var value = values.GetValue(i)!;
            var button = CreateSquareButton(labels[i], value);
            button.Click += (_, _) =>
            {
                if (_updating)
                {
                    return;
                }

                _updating = true;
                SelectedValue = value;
                _updating = false;
                UpdateSelection();
            };
            _grid.Children.Add(button);
        }

        UpdateSelection();
    }

    private static Button CreateSquareButton(string label, object value)
    {
        return new Button
        {
            Content = label,
            Tag = value,
            Margin = new Thickness(2),
            MinWidth = 28,
            MinHeight = 24,
            Padding = new Thickness(2, 1, 2, 1),
            FontSize = 9,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromRgb(201, 162, 39)),
            Background = new SolidColorBrush(Color.FromRgb(18, 18, 18)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(201, 162, 39)),
            BorderThickness = new Thickness(1),
            Cursor = System.Windows.Input.Cursors.Hand,
        };
    }

    private string[] ParseLabels(int count)
    {
        if (!string.IsNullOrWhiteSpace(Labels))
        {
            var parts = Labels.Split('|', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == count)
            {
                return parts;
            }
        }

        return EnumType is null
            ? Enumerable.Repeat("?", count).ToArray()
            : Enum.GetNames(EnumType);
    }

    private void UpdateSelection()
    {
        if (_grid is null)
        {
            return;
        }

        foreach (var button in _grid.Children.OfType<Button>())
        {
            var selected = button.Tag?.Equals(SelectedValue) == true;
            button.Background = new SolidColorBrush(selected ? Color.FromRgb(70, 58, 18) : Color.FromRgb(18, 18, 18));
        }
    }
}
