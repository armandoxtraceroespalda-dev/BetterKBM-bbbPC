using System.Windows;
using System.Windows.Controls;

namespace BloodborneLauncher.Views;

/// <summary>Long sections share their options across both columns without reparenting bound controls.</summary>
public sealed class OptionRows : StackPanel
{
    public static readonly DependencyProperty SplitProperty = DependencyProperty.RegisterAttached(
        "Split", typeof(bool), typeof(OptionRows), new FrameworkPropertyMetadata(false,
            FrameworkPropertyMetadataOptions.Inherits | FrameworkPropertyMetadataOptions.AffectsMeasure));
    public static bool GetSplit(DependencyObject target) => (bool)target.GetValue(SplitProperty);
    public static void SetSplit(DependencyObject target, bool value) => target.SetValue(SplitProperty, value);
    private readonly List<Rect> _positions = new();
    private bool _split;
    protected override Size MeasureOverride(Size available)
    {
        int count = Children.OfType<SettingRow>().Count(row => row.Visibility != Visibility.Collapsed && !SettingsColumns.GetFullWidth(row));
        _split = count >= 2 && Orientation == Orientation.Vertical && GetSplit(this) && available.Width >= 760 && double.IsFinite(available.Width);
        if (!_split) return base.MeasureOverride(available);
        double width = available.Width, column = (width - 24) / 2, y = 0, height = 0;
        int pending = -1; _positions.Clear();
        foreach (UIElement child in InternalChildren)
        {
            int index = _positions.Count; _positions.Add(Rect.Empty);
            if (child.Visibility == Visibility.Collapsed) continue;
            bool full = child is not SettingRow || SettingsColumns.GetFullWidth(child);
            if (full && pending >= 0) { y += height; height = 0; pending = -1; }
            child.Measure(new Size(full ? width : column, double.PositiveInfinity));
            if (full) { _positions[index] = new Rect(0, y, width, child.DesiredSize.Height); y += child.DesiredSize.Height; }
            else if (pending < 0)
            {
                pending = index; height = child.DesiredSize.Height;
                _positions[index] = new Rect(0, y, column, height);
            }
            else
            {
                _positions[index] = new Rect(column + 24, y, column, child.DesiredSize.Height);
                y += Math.Max(height, child.DesiredSize.Height); height = 0; pending = -1;
            }
        }
        if (pending >= 0) y += height;
        return new Size(width, y);
    }
    protected override Size ArrangeOverride(Size final)
    {
        if (!_split) return base.ArrangeOverride(final);
        for (int i = 0; i < InternalChildren.Count; i++)
            InternalChildren[i].Arrange(i < _positions.Count && !_positions[i].IsEmpty ? _positions[i] : new Rect());
        return final;
    }
}
