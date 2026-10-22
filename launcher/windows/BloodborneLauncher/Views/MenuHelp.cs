using System.Windows;
using System.Windows.Controls;

namespace BloodborneLauncher.Views;

/// <summary>Page-scoped compact settings. Game keeps its existing description layout.</summary>
public static class MenuHelp
{
    public static readonly DependencyProperty CompactProperty = DependencyProperty.RegisterAttached(
        "Compact", typeof(bool), typeof(MenuHelp), new FrameworkPropertyMetadata(true,
            FrameworkPropertyMetadataOptions.Inherits, Changed));
    public static bool GetCompact(DependencyObject target) => (bool)target.GetValue(CompactProperty);
    public static void SetCompact(DependencyObject target, bool value) => target.SetValue(CompactProperty, value);
    private static void Changed(DependencyObject target, DependencyPropertyChangedEventArgs args)
    { if (target is SettingRow row) row.UpdateLayoutMode(); }

    public static void Attach(FrameworkElement target, object? content)
    {
        target.ToolTip = content;
        ToolTipService.SetInitialShowDelay(target, 450);
        ToolTipService.SetBetweenShowDelay(target, 120);
        ToolTipService.SetShowDuration(target, 30000);
        ToolTipService.SetShowOnDisabled(target, true);
    }
}
