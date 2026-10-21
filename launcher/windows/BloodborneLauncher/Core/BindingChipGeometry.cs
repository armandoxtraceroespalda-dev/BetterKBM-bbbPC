namespace BloodborneLauncher.Core;

/// <summary>Shared geometry for drawing and input; no nested controls per key binding.</summary>
internal static class BindingChipGeometry
{
    internal readonly record struct Box(double X, double Y, double Width, double Height)
    {
        public bool Contains(double x, double y) => x >= X && x < X + Width && y >= Y && y < Y + Height;
    }
    internal readonly record struct Chip(Box Bounds, Box Remove);
    internal sealed record Layout(Chip[] Chips, Box? ChangedDot, double Height);
    internal enum ClickKind { Capture, Remove, Cancel }
    internal readonly record struct ClickAction(ClickKind Kind, int Index = -1);
    public static ClickAction ResolveClick(int pressedRemove, int releasedRemove) => pressedRemove < 0
        ? new(ClickKind.Capture)
        : pressedRemove == releasedRemove ? new(ClickKind.Remove, pressedRemove) : new(ClickKind.Cancel);

    public static Layout Arrange(IReadOnlyList<double> widths, double width, double lineHeight, bool changed, double emptyWidth = 0)
    {
        double available = double.IsFinite(width) ? Math.Max(1, width - 12) : Math.Max(1, widths.Sum() + widths.Count * 6);
        lineHeight = Math.Max(22, lineHeight);
        double left = 6, x = left, y = 4;
        var chips = new Chip[widths.Count];
        for (int i = 0; i < widths.Count; i++)
        {
            double size = Math.Clamp(widths[i], 1, available);
            if (x > left && x + size > left + available) { x = left; y += lineHeight + 4; }
            var bounds = new Box(x, y, size, lineHeight);
            var remove = new Box(x + Math.Max(0, size - 19), y, Math.Min(19, size), lineHeight);
            chips[i] = new(bounds, remove);
            x += size + 6;
        }
        Box? dot = null;
        if (widths.Count == 0 && emptyWidth > 0) x += Math.Min(emptyWidth, available) + 6;
        if (changed)
        {
            if (x + 5 > left + available && x > left) { x = left; y += lineHeight + 4; }
            dot = new Box(x, y + (lineHeight - 5) / 2, Math.Min(5, available), 5);
        }
        return new(chips, dot, y + lineHeight + 4);
    }

    public static int RemoveAt(Layout layout, double x, double y)
    {
        for (int i = 0; i < layout.Chips.Length; i++) if (layout.Chips[i].Remove.Contains(x, y)) return i;
        return -1;
    }
    public static int ChipAt(Layout layout, double x, double y)
    {
        for (int i = 0; i < layout.Chips.Length; i++) if (layout.Chips[i].Bounds.Contains(x, y)) return i;
        return -1;
    }
}
