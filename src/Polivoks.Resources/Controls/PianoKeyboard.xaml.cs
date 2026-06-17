using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Polivoks.Resources.Controls;

public partial class PianoKeyboard : UserControl
{
    public static readonly DependencyProperty StartNoteProperty = DependencyProperty.Register(
        nameof(StartNote),
        typeof(int),
        typeof(PianoKeyboard),
        new PropertyMetadata(48, OnStructureChanged));

    public static readonly DependencyProperty OctaveCountProperty = DependencyProperty.Register(
        nameof(OctaveCount),
        typeof(int),
        typeof(PianoKeyboard),
        new PropertyMetadata(3, OnStructureChanged));

    public event EventHandler<int>? NotePressed;
    public event EventHandler<int>? NoteReleased;

    private static readonly int[] BlackOffsets = [1, 3, 6, 8, 10];

    public int StartNote
    {
        get => (int)GetValue(StartNoteProperty);
        set => SetValue(StartNoteProperty, value);
    }

    public int OctaveCount
    {
        get => (int)GetValue(OctaveCountProperty);
        set => SetValue(OctaveCountProperty, value);
    }

    public int WhiteKeyCount => WhiteKeys?.Children.Count ?? 0;

    public int BlackKeyCount => BlackKeys?.Children.Count ?? 0;

    public PianoKeyboard()
    {
        InitializeComponent();
        Loaded += (_, _) => SafeBuildKeys();
        SizeChanged += (_, _) => PositionBlackKeys();
    }

    private static void OnStructureChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is PianoKeyboard keyboard && keyboard.IsLoaded)
        {
            keyboard.SafeBuildKeys();
        }
    }

    private void SafeBuildKeys()
    {
        if (!IsLoaded)
        {
            return;
        }

        BuildKeys();
    }

    private void BuildKeys()
    {
        WhiteKeys.Children.Clear();
        BlackKeys.Children.Clear();

        var whiteCount = OctaveCount * 7;
        WhiteKeys.Columns = whiteCount;

        for (var octave = 0; octave < OctaveCount; octave++)
        {
            var baseNote = StartNote + octave * 12;
            var whiteNotes = new[] { 0, 2, 4, 5, 7, 9, 11 };
            foreach (var offset in whiteNotes)
            {
                WhiteKeys.Children.Add(CreateKey(baseNote + offset, false));
            }
        }

        for (var octave = 0; octave < OctaveCount; octave++)
        {
            var baseNote = StartNote + octave * 12;
            foreach (var offset in BlackOffsets)
            {
                BlackKeys.Children.Add(CreateKey(baseNote + offset, true));
            }
        }

        PositionBlackKeys();
    }

    private Button CreateKey(int midiNote, bool black)
    {
        var button = new Button
        {
            Tag = midiNote,
            Margin = new Thickness(black ? 0 : 1, 0, black ? 0 : 1, 0),
            MinWidth = black ? 16 : 28,
            Padding = new Thickness(0, black ? 6 : 8, 0, black ? 0 : 4),
            Background = new SolidColorBrush(black ? Color.FromRgb(30, 30, 30) : Color.FromRgb(216, 208, 184)),
            Foreground = new SolidColorBrush(black ? Color.FromRgb(204, 204, 204) : Color.FromRgb(42, 42, 42)),
            BorderBrush = new SolidColorBrush(black ? Color.FromRgb(68, 68, 68) : Color.FromRgb(138, 133, 112)),
            BorderThickness = new Thickness(1),
        };
        Panel.SetZIndex(button, black ? 2 : 0);

        button.PreviewMouseLeftButtonDown += (_, e) =>
        {
            NotePressed?.Invoke(this, midiNote);
            e.Handled = true;
        };
        button.PreviewMouseLeftButtonUp += (_, e) =>
        {
            NoteReleased?.Invoke(this, midiNote);
            e.Handled = true;
        };
        button.MouseLeave += (_, _) => NoteReleased?.Invoke(this, midiNote);

        return button;
    }

    private void PositionBlackKeys()
    {
        if (WhiteKeys.ActualWidth <= 0)
        {
            return;
        }

        var whiteWidth = WhiteKeys.ActualWidth / Math.Max(1, WhiteKeys.Columns);
        var blackWidth = whiteWidth * 0.55;
        var blackHeight = ActualHeight * 0.62;
        var index = 0;

        for (var octave = 0; octave < OctaveCount; octave++)
        {
            foreach (var offset in BlackOffsets)
            {
                if (index >= BlackKeys.Children.Count)
                {
                    return;
                }

                var whiteIndex = octave * 7 + WhiteIndex(offset);
                var x = whiteIndex * whiteWidth + whiteWidth * 0.68;
                if (BlackKeys.Children[index] is Button button)
                {
                    button.Width = blackWidth;
                    button.Height = blackHeight;
                    Canvas.SetLeft(button, x);
                    Canvas.SetTop(button, 0);
                }

                index++;
            }
        }
    }

    private static int WhiteIndex(int semitoneOffset) => semitoneOffset switch
    {
        1 => 0,
        3 => 1,
        6 => 3,
        8 => 4,
        10 => 5,
        _ => 0,
    };
}
