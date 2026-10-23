using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using BloodborneLauncher.Core;

namespace BloodborneLauncher.Views;

/// <summary>Full-width native menu choices; long translations wrap into equally spaced rows.</summary>
public sealed class ChoiceStrip : Border
{
    private readonly List<Button> _items = new();
    public int SelectedIndex { get; private set; } = -1;
    public event Action<int>? Changed;
    public ChoiceStrip(IReadOnlyList<Choice> choices)
    {
        var panel = new ChoiceStripPanel();
        for (int i = 0; i < choices.Count; i++)
        {
            int index = i; var choice = choices[i];
            var button = new Button { Content = Segmented.Short(choice.Label), FontSize = 15.75,
                MinHeight = 42, Padding = new Thickness(10, 5, 10, 5),
                HorizontalContentAlignment = HorizontalAlignment.Center, ToolTip = choice.Note ?? choice.Label };
            button.SetResourceReference(StyleProperty, "BloodborneSegmentButton");
            MenuSounds.SetClickCue(button, MenuSoundCue.Change);
            button.Click += (_, _) => Select(index, true);
            panel.Children.Add(button); _items.Add(button);
        }
        Child = panel;
        PreviewKeyDown += (_, args) =>
        {
            if (args.Key is not (Key.Left or Key.Right) || _items.Count == 0) return;
            int direction = args.Key == Key.Right ? 1 : -1;
            Select((Math.Max(0, SelectedIndex) + direction + _items.Count) % _items.Count, true);
            _items[SelectedIndex].Focus(); args.Handled = true;
        };
    }
    public void Select(int index, bool notify)
    {
        if (index < 0 || index >= _items.Count) return;
        SelectedIndex = index;
        for (int i = 0; i < _items.Count; i++) _items[i].Tag = i == index ? "active" : null;
        if (notify) Changed?.Invoke(index);
    }
}

public sealed class ChoiceStripPanel : Panel
{
    private readonly List<Rect> _positions = new();
    private readonly List<int> _visible = new();
    private readonly List<double> _widths = new();
    private const double Gap = 8;
    protected override Size MeasureOverride(Size available)
    {
        _positions.Clear(); _visible.Clear(); _widths.Clear();
        double natural = 0;
        for (int i = 0; i < InternalChildren.Count; i++)
        {
            _positions.Add(Rect.Empty);
            var child = InternalChildren[i];
            if (child.Visibility == Visibility.Collapsed) continue;
            child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            double preferred = Math.Max(60, child.DesiredSize.Width);
            _visible.Add(i); _widths.Add(preferred); natural += preferred;
        }
        if (_visible.Count == 0) return new Size();
        double width = double.IsFinite(available.Width) ? Math.Max(1, available.Width)
            : natural + Gap * (_visible.Count - 1);
        double y = 0;
        for (int first = 0; first < _visible.Count;)
        {
            int end = first; double used = 0;
            while (end < _visible.Count)
            {
                double next = Math.Min(width, _widths[end]) + (end > first ? Gap : 0);
                if (end > first && used + next > width) break;
                used += next; ++end;
            }
            double extra = Math.Max(0, width - used) / (end - first), height = 0;
            for (int i = first; i < end; i++)
            {
                var child = InternalChildren[_visible[i]];
                child.Measure(new Size(Math.Min(width, _widths[i]) + extra, double.PositiveInfinity));
                height = Math.Max(height, child.DesiredSize.Height);
            }
            double x = 0;
            for (int i = first; i < end; i++)
            {
                double cell = Math.Min(width, _widths[i]) + extra;
                _positions[_visible[i]] = new Rect(x, y, cell, height); x += cell + Gap;
            }
            y += height + Gap; first = end;
        }
        return new Size(width, Math.Max(0, y - Gap));
    }
    protected override Size ArrangeOverride(Size final)
    {
        for (int i = 0; i < InternalChildren.Count; i++)
            InternalChildren[i].Arrange(i >= _positions.Count || _positions[i].IsEmpty ? new Rect() : _positions[i]);
        return final;
    }
}
