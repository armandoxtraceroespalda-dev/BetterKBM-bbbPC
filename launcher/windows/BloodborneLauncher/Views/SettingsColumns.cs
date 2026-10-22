using System.Windows;
using System.Windows.Controls;

namespace BloodborneLauncher.Views;

/// <summary>Short sections occupy one column; long option groups span both.</summary>
public sealed class SettingsColumns : Panel
{
    public static readonly DependencyProperty FullWidthProperty = DependencyProperty.RegisterAttached(
        "FullWidth", typeof(bool), typeof(SettingsColumns), new FrameworkPropertyMetadata(false,
            FrameworkPropertyMetadataOptions.AffectsParentMeasure));
    public static void SetFullWidth(DependencyObject target, bool value) => target.SetValue(FullWidthProperty, value);
    public static bool GetFullWidth(DependencyObject target) => (bool)target.GetValue(FullWidthProperty);
    public static readonly DependencyProperty TwoColumnsProperty = DependencyProperty.RegisterAttached(
        "TwoColumns", typeof(bool), typeof(SettingsColumns), new FrameworkPropertyMetadata(false,
            FrameworkPropertyMetadataOptions.AffectsParentMeasure));
    public static void SetTwoColumns(DependencyObject target, bool value) => target.SetValue(TwoColumnsProperty, value);
    public static bool GetTwoColumns(DependencyObject target) => (bool)target.GetValue(TwoColumnsProperty);
    private readonly List<Rect> _positions = new();
    private const double Gap = 24;

    protected override Size MeasureOverride(Size available)
    {
        double width = double.IsInfinity(available.Width) ? 900 : available.Width;
        bool two = width >= 820;
        double column = two ? (width - Gap) / 2 : width;
        double y = 0;
        UIElement? pending = null;
        int pendingIndex = -1;
        _positions.Clear();

        void Flush(UIElement? right = null, int rightIndex = -1)
        {
            if (pending == null) return;
            double heading = Math.Max((pending as SettingsSection)?.NaturalHeading ?? 0,
                (right as SettingsSection)?.NaturalHeading ?? 0);
            if (pending is SettingsSection leftSection)
            {
                leftSection.AlignHeading(heading);
                pending.Measure(new Size(column, double.PositiveInfinity));
            }
            if (right is SettingsSection rightSection)
            {
                rightSection.AlignHeading(heading);
                right.Measure(new Size(column, double.PositiveInfinity));
            }
            _positions[pendingIndex] = new Rect(0, y, column, pending.DesiredSize.Height);
            if (right != null)
                _positions[rightIndex] = new Rect(column + Gap, y, column, right.DesiredSize.Height);
            y += Math.Max(pending.DesiredSize.Height, right?.DesiredSize.Height ?? 0);
            pending = null;
        }

        foreach (UIElement child in InternalChildren)
        {
            int index = _positions.Count;
            _positions.Add(Rect.Empty);
            if (child.Visibility == Visibility.Collapsed) continue;
            int count = (child as SettingsSection)?.OptionCount ?? 0;
            bool split = two && (count > 7 || GetTwoColumns(child));
            bool full = !two || GetFullWidth(child) || split;
            if (child is SettingsSection section)
            {
                OptionRows.SetSplit(section, split);
                if (full) section.AlignHeading(0);
            }
            if (full)
            {
                // An unpaired short section keeps its single-column width.
                Flush();
                child.Measure(new Size(width, double.PositiveInfinity));
                _positions[index] = new Rect(0, y, width, child.DesiredSize.Height);
                y += child.DesiredSize.Height;
            }
            else
            {
                child.Measure(new Size(column, double.PositiveInfinity));
                if (pending == null) { pending = child; pendingIndex = index; }
                else Flush(child, index);
            }
        }
        Flush();
        return new Size(width, y);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        for (int i = 0; i < InternalChildren.Count; i++)
            InternalChildren[i].Arrange(i < _positions.Count && !_positions[i].IsEmpty ? _positions[i] : new Rect());
        return finalSize;
    }
}
