using BloodborneLauncher.Core;

static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
foreach (double width in new[] { 40.0, 80, 120, 180, 240, 320, 600, 1080 })
foreach (var lengths in new[] { Array.Empty<double>(), new[] { 60.0 }, new[] { 80.0, 120, 220, 500 }, Enumerable.Repeat(65.0, 50).ToArray() })
foreach (bool changed in new[] { false, true })
{
    var layout = BindingChipGeometry.Arrange(lengths, width, 24, changed, emptyWidth: 70);
    Check(layout.Chips.Length == lengths.Length, "Every binding, including a long chord, must retain its own hit target.");
    for (int i = 0; i < layout.Chips.Length; i++)
    {
        var chip = layout.Chips[i]; var b = chip.Bounds; var r = chip.Remove;
        Check(b.X >= 6 && b.X + b.Width <= width - 6 && b.Y >= 0 && b.Y + b.Height <= layout.Height,
            "Narrow and wrapped cells must keep all chips inside the allocated column.");
        Check(r.X >= b.X && r.X + r.Width <= b.X + b.Width && r.Y == b.Y, "Remove cannot overlap another binding.");
        Check(BindingChipGeometry.RemoveAt(layout, r.X + r.Width / 2, r.Y + r.Height / 2) == i,
            "Clicking x must remove precisely the intended binding after wrapping.");
        Check(BindingChipGeometry.ChipAt(layout, b.X + 1, b.Y + 1) == i,
            "Conflict help must identify the intended chip.");
        Check(BindingChipGeometry.RemoveAt(layout, b.X + 1, b.Y + 1) == -1,
            "Clicking the key label must not delete a binding.");
        for (int j = i + 1; j < layout.Chips.Length; j++)
        {
            var other = layout.Chips[j].Bounds;
            Check(b.X + b.Width <= other.X || other.X + other.Width <= b.X || b.Y + b.Height <= other.Y || other.Y + other.Height <= b.Y,
                "Chips must not overlap, including long multi-key combinations.");
        }
    }
    Check(BindingChipGeometry.RemoveAt(layout, 0, 0) == -1, "Cell padding starts capture, not removal.");
    if (layout.ChangedDot is { } dot) Check(dot.X + dot.Width <= width - 6 && dot.Y + dot.Height <= layout.Height,
        "The modified marker must stay within the cell.");
    if (lengths.Length == 0 && layout.ChangedDot is { } emptyDot)
        Check(emptyDot.Y >= 32 || emptyDot.X >= 6 + Math.Min(70, width - 12), "The changed marker must not overlap the unbound label.");
}
var exact = BindingChipGeometry.Arrange(new[] { 80.0, 100 }, 204, 22, false);
Check(exact.Chips[0].Bounds.Y == exact.Chips[1].Bounds.Y, "Exact-fit chips must stay on one line.");
Check(!exact.Chips[0].Remove.Contains(exact.Chips[0].Remove.X + exact.Chips[0].Remove.Width, 5),
    "Hit boxes must use exclusive right boundaries.");
for (int i = 0; i < 20; i++)
{
    var remove = BindingChipGeometry.ResolveClick(i, i);
    Check(remove.Kind == BindingChipGeometry.ClickKind.Remove && remove.Index == i,
        "Matching press/release on x removes exactly one alternative.");
    Check(BindingChipGeometry.ResolveClick(i, -1).Kind == BindingChipGeometry.ClickKind.Cancel &&
        BindingChipGeometry.ResolveClick(i, i + 1).Kind == BindingChipGeometry.ClickKind.Cancel,
        "Dragging off x or onto another x must neither remove a different key nor open capture.");
    Check(BindingChipGeometry.ResolveClick(-1, i).Kind == BindingChipGeometry.ClickKind.Capture,
        "Dragging onto x after pressing the label must not delete an input.");
}
Check(BindingChipGeometry.ResolveClick(-1, -1).Kind == BindingChipGeometry.ClickKind.Capture,
    "Keyboard/automation and ordinary cell activation must enter the existing chord capture.");
Console.WriteLine("PASS: narrow/wide layouts, combinations, wrapping, exact fit, per-binding remove, conflict target and modified marker bounds.");
