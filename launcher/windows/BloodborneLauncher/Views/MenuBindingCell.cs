using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using BloodborneLauncher.Core;

namespace BloodborneLauncher.Views;

/// <summary>A keyboard binding uses the same native menu button as a controller binding.</summary>
public sealed class MenuBindingCell : Button
{
    private readonly Action _capture, _clear;

    public MenuBindingCell(string action, IEnumerable<(string Value, string Label, bool Mouse, string? Warning)> tokens,
        bool changed, Action capture, Action clear)
    {
        _capture = capture; _clear = clear;
        var entries = tokens.ToArray();
        var label = entries.Length == 0 ? UiText.T("Not bound") : string.Join(", ", entries.Select(t => t.Label));
        // Use the existing style so font, colour, underline, highlight and input sounds agree.
        SetResourceReference(StyleProperty, "BloodborneActionButton");
        FontSize = 12.5;
        if (changed) FontWeight = FontWeights.SemiBold;
        Padding = new Thickness(8, 3, 10, 3);
        VerticalAlignment = VerticalAlignment.Center;
        HorizontalAlignment = HorizontalAlignment.Left;
        Content = new TextBlock { Text = label, TextWrapping = TextWrapping.Wrap, TextAlignment = TextAlignment.Center };
        var help = UiText.F($"Click to set a key or mouse input for {UiText.T(action)}. Right-click to unbind.");
        var warnings = entries.Select(t => t.Warning).Where(t => !string.IsNullOrEmpty(t)).Distinct().ToArray();
        ToolTip = warnings.Length == 0 ? help : help + "\n" + string.Join("\n", warnings);
        AutomationProperties.SetName(this, UiText.T(action) + ": " + label);
    }

    protected override void OnClick() { base.OnClick(); _capture(); }
    protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
    {
        base.OnMouseRightButtonUp(e);
        if (IsEnabled) { e.Handled = true; _clear(); }
    }
}
