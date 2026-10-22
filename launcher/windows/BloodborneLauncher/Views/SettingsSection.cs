using System.Windows;
using System.Windows.Controls;

namespace BloodborneLauncher.Views;

/// <summary>Measures headings naturally, then reserves an equal heading slot for each paired row.</summary>
internal sealed class SettingsSection : Panel
{
    private readonly UIElement? _heading;
    private readonly UIElement _body;
    private readonly bool _textHeading;
    private double _alignedHeading;
    internal double NaturalHeading { get; private set; }
    internal int OptionCount => Count(_body);
    private static int Count(DependencyObject node)
    {
        if (node is UIElement { Visibility: Visibility.Collapsed }) return 0;
        if (node is SettingRow) return 1;
        int count = 0;
        foreach (var child in LogicalTreeHelper.GetChildren(node).OfType<DependencyObject>()) count += Count(child);
        return count;
    }
    internal SettingsSection(UIElement? heading, UIElement body)
    {
        _textHeading = heading is TextBlock;
        _heading = heading == null || heading.Visibility == Visibility.Collapsed ? null
            : _textHeading ? new MenuSurface { Child = heading,
                Padding = new Thickness(10, 2, 10, 2), Margin = new Thickness(0, 0, 0, 4) } : heading;
        _body = body;
        if (heading is FrameworkElement title) title.Margin = new Thickness(4, 8, 4, 8);
        if (_heading != null) Children.Add(_heading);
        Children.Add(body);
    }
    internal void AlignHeading(double height)
    {
        if (Math.Abs(_alignedHeading - height) < 0.01) return;
        _alignedHeading = height; InvalidateMeasure();
    }
    protected override Size MeasureOverride(Size available)
    {
        _heading?.Measure(new Size(available.Width, double.PositiveInfinity));
        NaturalHeading = _heading?.DesiredSize.Height ?? 0;
        _body.Measure(new Size(available.Width, double.PositiveInfinity));
        return new Size(available.Width, Math.Max(NaturalHeading, _alignedHeading) + _body.DesiredSize.Height);
    }
    protected override Size ArrangeOverride(Size final)
    {
        double slot = Math.Max(NaturalHeading, _alignedHeading);
        _heading?.Arrange(new Rect(0, 0, _textHeading ? Math.Min(final.Width, _heading.DesiredSize.Width) : final.Width, NaturalHeading));
        _body.Arrange(new Rect(0, slot, final.Width, Math.Max(0, final.Height - slot)));
        return final;
    }
}
