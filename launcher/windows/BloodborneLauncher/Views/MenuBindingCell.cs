using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BloodborneLauncher.Core;

namespace BloodborneLauncher.Views;

/// <summary>One drawn button for a binding cell, including its removable chips and conflict hints.</summary>
public sealed class MenuBindingCell : Button
{
    private readonly (string Value, string Label, bool Mouse, string? Warning)[] _tokens;
    private readonly bool _changed;
    private readonly Action _capture, _clear;
    private readonly Action<string> _remove;
    private readonly string _help;
    private FormattedText[]? _labels;
    private RectangleGeometry? _clip;
    private Size _clipSize;
    private FormattedText? _empty;
    private BindingChipGeometry.Layout? _layout;
    private double _layoutWidth = double.NaN, _pixelsPerDip, _lineHeight;
    private int _removeIndex = -1, _hoverIndex = -1, _pressedRemoveIndex = -1;
    private bool _hoverRemove;
    private readonly Brush _fill, _stroke, _foreground, _warning, _hover, _faint, _accent;
    private readonly Pen _strokePen, _warningPen, _faintPen, _accentPen;
    private readonly Typeface _typeface;

    public MenuBindingCell(string action, IEnumerable<(string Value, string Label, bool Mouse, string? Warning)> tokens,
        bool changed, Action capture, Action clear, Action<string> remove)
    {
        _tokens = tokens.ToArray(); _changed = changed; _capture = capture; _clear = clear; _remove = remove;
        // Keep native Button input, command, automation and sound routing, but never expand a template.
        Style = null; Template = null; OverridesDefaultStyle = true;
        Focusable = true; Cursor = Cursors.Hand; HorizontalContentAlignment = HorizontalAlignment.Stretch;
        _help = UiText.F($"Click to set a key or mouse input for {UiText.T(action)}. Right-click to unbind.");
        ToolTip = BloodborneLauncher.Core.UiText.T(_help);
        AutomationProperties.SetName(this, UiText.T(action) + ": " + (_tokens.Length == 0 ? "Not bound" : string.Join(", ", _tokens.Select(t => t.Label))));
        _fill = Brush("ControlFillColorSecondaryBrush", Brushes.DimGray);
        _stroke = Brush("CardStrokeColorDefaultBrush", Brushes.Gray);
        _foreground = Brush("TextFillColorSecondaryBrush", Brushes.Bisque);
        _warning = Brush("WarningTextBrush", Brushes.Goldenrod);
        _hover = Brush("SidebarItemHoverBrush", Brushes.Transparent);
        _faint = Brush("TextFillColorTertiaryBrush", Brushes.Gray);
        _accent = Brush("AccentTextFillColorPrimaryBrush", Brushes.Bisque);
        _strokePen = Line(_stroke); _warningPen = Line(_warning); _faintPen = Line(_faint); _accentPen = Line(_accent);
        _typeface = new Typeface(TryFindResource("MonoFont") as FontFamily ?? new FontFamily("Consolas"),
            FontStyles.Normal, FontWeights.Normal, FontStretches.Normal);
        IsEnabledChanged += (_, _) => InvalidateVisual();
    }

    private Brush Brush(string key, Brush fallback) => TryFindResource(key) as Brush ?? fallback;
    private static Pen Line(Brush brush, double thickness = 1)
    { var pen = new Pen(brush, thickness); if (pen.CanFreeze) pen.Freeze(); return pen; }
    private static Rect Rect(BindingChipGeometry.Box box) => new(box.X, box.Y, box.Width, box.Height);

    private void EnsureLayout(double width)
    {
        double dpi = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        if (_layout != null && width == _layoutWidth && dpi == _pixelsPerDip) return;
        _layoutWidth = width; _pixelsPerDip = dpi;
        // Text is cached for the current column width/DPI; hover only redraws existing runs.
        _labels = _tokens.Select(t => new FormattedText(t.Label, CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight, _typeface, 14, t.Warning == null ? _foreground : _warning, dpi)).ToArray();
        _empty = _tokens.Length == 0 ? new FormattedText(UiText.T("Not bound"), CultureInfo.CurrentUICulture,
            FlowDirection.LeftToRight, new Typeface(LauncherAppearance.DetailFont, FontStyles.Italic, FontWeights.Normal, FontStretches.Normal), 14, _faint, dpi) : null;
        _lineHeight = _labels.Length == 0 ? 22 : Math.Max(22, Math.Ceiling(_labels.Max(t => t.Height)) + 6);
        _layout = BindingChipGeometry.Arrange(_labels.Select(t => Math.Ceiling(t.WidthIncludingTrailingWhitespace) + 53).ToArray(), width, _lineHeight, _changed,
            _empty?.WidthIncludingTrailingWhitespace ?? 0);
        for (int i = 0; i < _labels.Length; i++)
        {
            _labels[i].MaxTextWidth = Math.Max(1, _layout.Chips[i].Bounds.Width - 53);
            _labels[i].Trimming = TextTrimming.CharacterEllipsis;
            _labels[i].MaxLineCount = 1;
        }
        if (_empty != null) { _empty.MaxTextWidth = Math.Max(1, width - 12); _empty.MaxLineCount = 1; _empty.Trimming = TextTrimming.CharacterEllipsis; }
    }

    protected override Size MeasureOverride(Size constraint)
    {
        double width = double.IsFinite(constraint.Width) ? Math.Max(1, constraint.Width) : 320;
        EnsureLayout(width);
        return new(width, _layout!.Height);
    }

    protected override void OnRender(DrawingContext dc)
    {
        EnsureLayout(ActualWidth);
        var layout = _layout!;
        dc.DrawRoundedRectangle(IsMouseOver || IsKeyboardFocused ? _hover : Brushes.Transparent, null,
            new Rect(RenderSize), 5, 5);
        if (_clip == null || _clipSize != RenderSize)
        { _clipSize=RenderSize;_clip=new RectangleGeometry(new Rect(RenderSize));_clip.Freeze(); }
        dc.PushClip(_clip);
        if (!IsEnabled) dc.PushOpacity(0.4);
        for (int i = 0; i < _tokens.Length; i++)
        {
            var chip = layout.Chips[i]; var bounds = Rect(chip.Bounds);
            var ink = _tokens[i].Warning == null ? _faintPen : _warningPen;
            dc.DrawRoundedRectangle(_fill, _tokens[i].Warning == null ? _strokePen : _warningPen, bounds, 4, 4);
            double cy = bounds.Y + bounds.Height / 2, iconX = bounds.X + 8;
            // Tiny vector input icons replace a SymbolIcon control/font binding for every chip.
            if (_tokens[i].Mouse)
            {
                dc.DrawRoundedRectangle(null, ink, new Rect(iconX + 2, cy - 5, 7, 10), 3, 3);
                dc.DrawLine(ink, new Point(iconX + 5.5, cy - 4), new Point(iconX + 5.5, cy - 1));
            }
            else
            {
                dc.DrawRectangle(null, ink, new Rect(iconX, cy - 4, 11, 8));
                dc.DrawLine(ink, new Point(iconX + 2, cy + 1), new Point(iconX + 9, cy + 1));
            }
            dc.DrawText(_labels![i], new Point(bounds.X + 24, cy - _labels[i].Height / 2));
            var remove = Rect(chip.Remove);
            if (IsMouseOver && _hoverIndex == i && _hoverRemove)
                dc.DrawRoundedRectangle(_hover, null, remove, 3, 3);
            double x = remove.X + remove.Width / 2;
            dc.DrawLine(ink, new Point(x - 2.5, cy - 2.5), new Point(x + 2.5, cy + 2.5));
            dc.DrawLine(ink, new Point(x - 2.5, cy + 2.5), new Point(x + 2.5, cy - 2.5));
        }
        if (_empty != null) dc.DrawText(_empty, new Point(6, 4 + (_lineHeight - _empty.Height) / 2));
        if (layout.ChangedDot is { } dot) dc.DrawEllipse(_accent, null, new Point(dot.X + dot.Width / 2, dot.Y + 2.5), dot.Width / 2, 2.5);
        if (IsKeyboardFocused) dc.DrawRoundedRectangle(null, _accentPen, new Rect(1, 1, Math.Max(0, ActualWidth - 2), Math.Max(0, ActualHeight - 2)), 4, 4);
        if (!IsEnabled) dc.Pop();
        dc.Pop();
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e); EnsureLayout(ActualWidth);
        var point = e.GetPosition(this); int index = BindingChipGeometry.ChipAt(_layout!, point.X, point.Y);
        if (index < 0 && _layout!.ChangedDot is { } dot && dot.Contains(point.X, point.Y)) index = _tokens.Length;
        bool remove = BindingChipGeometry.RemoveAt(_layout!, point.X, point.Y) >= 0;
        if (_hoverIndex != index || _hoverRemove != remove)
        {
            _hoverIndex = index; _hoverRemove = remove;
            ToolTip = remove ? BloodborneLauncher.Core.UiText.T("Remove") : index == _tokens.Length ? BloodborneLauncher.Core.UiText.T("Changed from the default") :
                index >= 0 ? _tokens[index].Label + "\n" + (_tokens[index].Warning ?? _help) : BloodborneLauncher.Core.UiText.T(_help);
            InvalidateVisual();
        }
    }
    protected override void OnMouseEnter(MouseEventArgs e) { base.OnMouseEnter(e); InvalidateVisual(); }
    protected override void OnMouseLeave(MouseEventArgs e) { base.OnMouseLeave(e); _hoverIndex = -1; ToolTip = BloodborneLauncher.Core.UiText.T(_help); InvalidateVisual(); }
    protected override void OnGotKeyboardFocus(KeyboardFocusChangedEventArgs e) { base.OnGotKeyboardFocus(e); InvalidateVisual(); }
    protected override void OnLostKeyboardFocus(KeyboardFocusChangedEventArgs e) { base.OnLostKeyboardFocus(e); InvalidateVisual(); }
    protected override void OnMouseLeftButtonDown(MouseButtonEventArgs e)
    {
        EnsureLayout(ActualWidth); var point = e.GetPosition(this);
        _pressedRemoveIndex = BindingChipGeometry.RemoveAt(_layout!, point.X, point.Y);
        base.OnMouseLeftButtonDown(e);
    }
    protected override void OnMouseLeftButtonUp(MouseButtonEventArgs e)
    {
        EnsureLayout(ActualWidth); var point = e.GetPosition(this);
        _removeIndex = BindingChipGeometry.RemoveAt(_layout!, point.X, point.Y);
        base.OnMouseLeftButtonUp(e);
        _removeIndex = _pressedRemoveIndex = -1;
    }
    protected override void OnKeyDown(KeyEventArgs e) { _removeIndex = _pressedRemoveIndex = -1; base.OnKeyDown(e); }
    protected override void OnClick()
    {
        int remove = _removeIndex, pressed = _pressedRemoveIndex;
        _removeIndex = _pressedRemoveIndex = -1;
        base.OnClick();
        var action = BindingChipGeometry.ResolveClick(pressed, remove);
        if (action.Kind == BindingChipGeometry.ClickKind.Remove) _remove(_tokens[action.Index].Value);
        else if (action.Kind == BindingChipGeometry.ClickKind.Capture) _capture();
    }
    protected override void OnMouseRightButtonUp(MouseButtonEventArgs e)
    { base.OnMouseRightButtonUp(e); if (IsEnabled) { e.Handled = true; _clear(); } }
}
