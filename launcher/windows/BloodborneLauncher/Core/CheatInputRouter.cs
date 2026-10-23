using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;

namespace BloodborneLauncher.Core;

/// <summary>One application-wide route for the easter egg, without page subscriptions or polling.</summary>
internal static class CheatInputRouter
{
    private static bool _registered;
    internal static void Register()
    {
        if (_registered) return;
        _registered = true;
        EventManager.RegisterClassHandler(typeof(Window), TextCompositionManager.PreviewTextInputEvent,
            new TextCompositionEventHandler(OnText), true);
        EventManager.RegisterClassHandler(typeof(Window), Keyboard.GotKeyboardFocusEvent,
            new KeyboardFocusChangedEventHandler(OnFocus), true);
        EventManager.RegisterClassHandler(typeof(Window), Keyboard.PreviewKeyDownEvent,
            new KeyEventHandler(OnKey), true);
    }

    private static void OnText(object sender, TextCompositionEventArgs args)
    {
        if (MainWindow.Instance is not { } main) return;
        bool allowed = !main.IsCapturingCheatInput && !IsEditable(Keyboard.FocusedElement as DependencyObject)
            && !IsEditable(args.OriginalSource as DependencyObject)
            && (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Windows)) == 0;
        if (!CheatAccess.Session.Feed(args.Text, allowed)) return;
        main.ApplyCheatAccess();
        args.Handled = true;
    }

    private static void OnFocus(object sender, KeyboardFocusChangedEventArgs args)
    {
        if (IsEditable(args.NewFocus as DependencyObject)) CheatAccess.Session.ResetInput();
    }

    private static void OnKey(object sender, KeyEventArgs args)
    {
        if (args.Key is Key.Escape or Key.Back or Key.Delete ||
            (Keyboard.Modifiers & (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Windows)) != 0)
            CheatAccess.Session.ResetInput();
    }

    // Rich text, password and editable combo controls are excluded even when focus is on an inner view.
    internal static bool IsEditable(DependencyObject? node)
    {
        while (node != null)
        {
            if (node is TextBoxBase or PasswordBox or ComboBox { IsEditable: true }) return true;
            node = node is Visual or Visual3D ? VisualTreeHelper.GetParent(node) : LogicalTreeHelper.GetParent(node);
        }
        return false;
    }
}
