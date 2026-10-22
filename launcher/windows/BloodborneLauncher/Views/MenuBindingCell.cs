using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Data;
using Path = System.Windows.Shapes.Path;
using BloodborneLauncher.Core;

namespace BloodborneLauncher.Views;

/// <summary>A keyboard binding uses the same native menu button as a controller binding.</summary>
public sealed class MenuBindingCell : Button
{
    private readonly Action _capture, _clear;
    private static readonly Geometry Keyboard = Shape("M1,3 L17,3 17,13 1,13 Z M4,6 L5,6 M8,6 L9,6 M12,6 L14,6 M4,9 L5,9 M8,9 L9,9 M12,9 L14,9 M5,11 L13,11");
    private static readonly Geometry MouseIcon = Shape("M9,1 C5,1 3,4 3,7 L3,11 C3,16 15,16 15,11 L15,7 C15,4 13,1 9,1 Z M9,1 L9,7 M3,7 L15,7");
    private static Geometry Shape(string path) { var geometry = Geometry.Parse(path); geometry.Freeze(); return geometry; }
    private Path Icon(Geometry geometry)
    {
        var icon = new Path { Data = geometry, Width = 16, Height = 15, Stretch = Stretch.Uniform,
            StrokeThickness = 1, Margin = new Thickness(0,0,5,0), VerticalAlignment = VerticalAlignment.Center, IsHitTestVisible = false };
        icon.SetBinding(Path.StrokeProperty, new Binding("Foreground") { Source = this }); return icon;
    }

    public MenuBindingCell(string action, IEnumerable<(string Value, string Label, bool Mouse, string? Warning)> tokens,
        bool changed, Action capture, Action clear)
    {
        _capture = capture; _clear = clear;
        var entries = tokens.ToArray();
        var label = entries.Length == 0 ? UiText.T("Not bound") : string.Join(", ", entries.Select(t => t.Label));
        // Use the existing style so font, colour, underline, highlight and input sounds agree.
        SetResourceReference(StyleProperty, "BloodborneActionButton");
        FontSize = 15;
        if (changed) FontWeight = FontWeights.SemiBold;
        Padding = new Thickness(8, 3, 10, 3);
        VerticalAlignment = VerticalAlignment.Center;
        HorizontalAlignment = HorizontalAlignment.Left;
        var content = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Center };
        for (int i = 0; i < entries.Length; i++)
        {
            var entry = entries[i];
            if (i > 0) content.Children.Add(new TextBlock { Text = ", ", VerticalAlignment = VerticalAlignment.Center });
            var row = new Grid(); row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); row.ColumnDefinitions.Add(new ColumnDefinition());
            var icons = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            var atoms = BindingExpressions.Atoms(entry.Value);
            if (atoms.Any(a => !InputMap.IsMouse(a) && !(a.Length == 1 && char.IsLetterOrDigit(a[0]))) || entry.Value.Contains('+')) icons.Children.Add(Icon(Keyboard));
            if (entry.Mouse || atoms.Any(InputMap.IsMouse)) icons.Children.Add(Icon(MouseIcon));
            row.Children.Add(icons);
            var text = new TextBlock { Text = entry.Label, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
            Grid.SetColumn(text, 1); row.Children.Add(text); content.Children.Add(row);
        }
        if (entries.Length == 0) content.Children.Add(new TextBlock { Text = label });
        Content = content;
        // Bindings stay readable without hover popups covering neighbouring actions.
        ToolTipService.SetIsEnabled(this, false);
        AutomationProperties.SetName(this, UiText.T(action) + ": " + label);
    }

    protected override void OnClick() { base.OnClick(); _capture(); }
    protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonUp(e);
        if (IsEnabled) { e.Handled = true; _clear(); }
    }
}
