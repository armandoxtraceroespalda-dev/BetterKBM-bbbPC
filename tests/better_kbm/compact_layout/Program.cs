using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BloodborneLauncher.Controls;
using BloodborneLauncher.Views;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        _ = new Application();
        var columns = new SettingsColumns();
        var first = new Border { Height = 100 };
        var second = new Border { Height = 150 };
        var table = new Border { Height = 200 };
        var last = new Border { Height = 80 };
        SettingsColumns.SetFullWidth(table, true);
        foreach (var child in new[] { first, second, table, last }) columns.Children.Add(child);
        Layout(columns, 1000);
        Check(first.ActualWidth == 488 && second.ActualWidth == 488, "Short sections each occupy one column.");
        Check(VisualTreeHelper.GetOffset(second).X == 512 && VisualTreeHelper.GetOffset(second).Y == 0, "Short sections share the same row.");
        Check(table.ActualWidth == 1000 && VisualTreeHelper.GetOffset(table).Y == 150, "Tables clear both columns.");
        Check(VisualTreeHelper.GetOffset(last).Y == 350, "Sections below a table cannot overlap it.");
        Layout(columns, 650);
        Check(first.ActualWidth == 650 && VisualTreeHelper.GetOffset(second).Y == 100, "Narrow windows stack in source order.");
        second.Visibility = Visibility.Collapsed;
        Layout(columns, 1000);
        Check(VisualTreeHelper.GetOffset(table).Y == 100, "Hidden content does not leave its old height behind.");


        var paired = new SettingsColumns();
        var a = new Border { Height = 80 }; var b = new Border { Height = 160 }; var c = new Border { Height = 40 };
        paired.Children.Add(a); paired.Children.Add(b); paired.Children.Add(c); Layout(paired, 1000);
        Check(VisualTreeHelper.GetOffset(c).Y == 160, "A new row starts below the taller preceding section, not in a masonry gap.");
        var sectionType = typeof(SettingsColumns).Assembly.GetType("BloodborneLauncher.Views.SettingsSection")!;
        var leftBody = new Border { Height = 50 }; var rightBody = new Border { Height = 50 };
        FrameworkElement Section(double headingHeight, UIElement body) => (FrameworkElement)Activator.CreateInstance(sectionType,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null,
            new object[] { new Border { Height = headingHeight }, body }, null)!;
        var headings = new SettingsColumns(); headings.Children.Add(Section(30, leftBody)); headings.Children.Add(Section(70, rightBody));
        Layout(headings, 1000);
        Check(leftBody.ActualWidth == 488 && rightBody.ActualWidth == 488 && VisualTreeHelper.GetOffset(leftBody).Y == VisualTreeHelper.GetOffset(rightBody).Y,
            "Short sections use one column each and align their heading slots.");

        var toolbar = new Grid();
        toolbar.ColumnDefinitions.Add(new ColumnDefinition());
        toolbar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        var resetButton = new System.Windows.Controls.Button { Width = 120, Height = 40 };
        Grid.SetColumn(resetButton, 1); toolbar.Children.Add(resetButton);
        var toolbarBody = new Border { Height = 50 };
        var toolbarSection = (FrameworkElement)Activator.CreateInstance(sectionType,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic, null,
            new object[] { toolbar, toolbarBody }, null)!;
        var toolbarColumns = new SettingsColumns(); SettingsColumns.SetFullWidth(toolbarSection, true);
        toolbarColumns.Children.Add(toolbarSection); Layout(toolbarColumns, 1000);
        Check(toolbar.ActualWidth > 980 && resetButton.TransformToAncestor(toolbarSection).Transform(new Point()).X > 850,
            "A toolbar heading keeps the full row width so Reset all stays at the right edge.");

        var options = new OptionRows();
        for (int i=0;i<8;i++) options.Children.Add(LauncherPage.Card(SymbolRegular.Keyboard24,"Option "+i,null,new CheckBox(),out _));
        var longSection = Section(30, options); var adaptive = new SettingsColumns(); adaptive.Children.Add(longSection);
        Layout(adaptive, 1000);
        Check(longSection.ActualWidth == 1000 && VisualTreeHelper.GetOffset(options.Children[1]).X > 0,
            "Long sections span the viewport and place their options in both columns.");
        Check(ReferenceEquals(LogicalTreeHelper.GetParent(options.Children[0]), options),
            "Splitting options must not reparent controls away from their bound containers.");
        Layout(adaptive, 650);
        Check(VisualTreeHelper.GetOffset(options.Children[1]).X == 0 && VisualTreeHelper.GetOffset(options.Children[1]).Y > 0,
            "Adaptive options return to one column in a narrow window.");
        Layout(adaptive, 1000);
        options.Children.Add(LauncherPage.Card(SymbolRegular.Keyboard24,"Odd option",null,new CheckBox(),out _));
        Layout(adaptive, 1000);
        Check(VisualTreeHelper.GetOffset(options.Children[1]).X > 0,
            "Nine visible options split into two columns.");
        Check(longSection.ActualWidth == 1000, "A long odd group spans both columns.");
        options.Children[8].Visibility = Visibility.Collapsed;
        Layout(adaptive, 1000);
        Check(VisualTreeHelper.GetOffset(options.Children[1]).X > 0,
            "Eight visible options may split even when another option is hidden.");
        options.Children[7].Visibility = Visibility.Collapsed;
        Layout(adaptive, 1000);
        Check(VisualTreeHelper.GetOffset(options.Children[1]).X == 0,
            "Seven visible options never split.");
        Check(longSection.ActualWidth == 488, "A short unpaired section keeps one-column width.");
        var shortSwitch = new ToggleSwitch(); var longSwitch = new ToggleSwitch();
        var shortSwitchRow = LauncherPage.Card(SymbolRegular.Keyboard24, "Short", null, shortSwitch, out _);
        var longSwitchRow = LauncherPage.Card(SymbolRegular.Keyboard24,
            "A much longer title that wraps onto several lines", null, longSwitch, out _);
        Layout(shortSwitchRow, 400); Layout(longSwitchRow, 400);
        Check(Math.Abs(shortSwitch.TransformToAncestor(shortSwitchRow).Transform(new Point()).X -
            longSwitch.TransformToAncestor(longSwitchRow).Transform(new Point()).X) < 0.1,
            "Switches align horizontally regardless of their title length.");
        var sliderGroup = new Grid { Width = 330, Tag = "slider-value-track" };
        var sliderRow = LauncherPage.Card(SymbolRegular.Keyboard24, "Slider", null, sliderGroup, out _);
        Layout(sliderRow, 400);
        Check(sliderGroup.TransformToAncestor(sliderRow).Transform(new Point()).Y > 20 &&
            Math.Abs(sliderGroup.TransformToAncestor(sliderRow).Transform(new Point()).X - 35) < 0.1,
            "The combined value and slider group is centered below the title.");
        var forced = new SettingsColumns();
        var shortOptions = new OptionRows();
        for (int i=0;i<5;i++) shortOptions.Children.Add(LauncherPage.Card(SymbolRegular.Keyboard24,"File "+i,null,new CheckBox(),out _));
        var forcedSection = Section(30, shortOptions); SettingsColumns.SetTwoColumns(forcedSection, true);
        forced.Children.Add(forcedSection); Layout(forced, 1000);
        Check(forcedSection.ActualWidth == 1000 && VisualTreeHelper.GetOffset(shortOptions.Children[1]).X > 0,
            "Screen and Files can explicitly split short groups without changing other groups.");
        Layout(forced, 650);
        Check(VisualTreeHelper.GetOffset(shortOptions.Children[1]).X == 0,
            "Explicit split requests still stack on narrow screens.");
        var letterCell = new MenuBindingCell("Interact", new[]{("E","E",false,(string?)null)},false,()=>{},()=>{});
        Check(!LogicalPaths(letterCell.Content as DependencyObject).Any(), "Single letters do not get a keyboard pictogram.");
        var namedCell = new MenuBindingCell("Dodge", new[]{("Space","Space",false,(string?)null)},false,()=>{},()=>{});
        Check(LogicalPaths(namedCell.Content as DependencyObject).Count()==1, "Named keys retain their keyboard pictogram.");
        var bindingCell = new MenuBindingCell("Strong attack", new[]{("Left Shift + Mouse1","Shift + Mouse1",false,(string?)null)},false,()=>{},()=>{});
        Check(LogicalPaths(bindingCell.Content as DependencyObject).Count()==2,
            "A keyboard-and-mouse chord has both device icons without changing its button style.");

        var row = LauncherPage.Card(SymbolRegular.Keyboard24, "Action", "Contextual description",
            new ComboBox { Width = 400 }, out var note);
        Layout(row, 370);
        Check(!Texts(row).Contains("Contextual description"), "Compact settings do not render permanent descriptions.");
        Check(row.ToolTip as string == "Contextual description", "The entire row exposes its explanation.");
        row.NoteInTooltip = true; note.Tag = row;
        LauncherPage.SetHint(note, "Selected quality note");
        Check(note.Visibility == Visibility.Collapsed && (row.ToolTip as string)!.Contains("Selected quality note"), "Choice notes share the tooltip.");
        MenuHelp.SetCompact(row, false);
        Layout(row, 900);
        Check(Texts(row).Contains("Contextual description") && note.Visibility == Visibility.Visible,
            "Game retains its description and selected-note layout.");
        Console.WriteLine("PASS: responsive columns, table spans, hidden sections, tooltips and Game exception.");
    }

    private static void Layout(FrameworkElement element, double width)
    {
        element.Measure(new Size(width, double.PositiveInfinity));
        element.Arrange(new Rect(0, 0, width, element.DesiredSize.Height));
        element.UpdateLayout();
    }
    private static IEnumerable<string> Texts(DependencyObject root)
    {
        if (root is System.Windows.Controls.TextBlock text && text.Visibility == Visibility.Visible) yield return text.Text;
        for (int i = 0; i < VisualTreeHelper.GetChildrenCount(root); i++)
            foreach (var value in Texts(VisualTreeHelper.GetChild(root, i))) yield return value;
    }
    private static IEnumerable<System.Windows.Shapes.Path> LogicalPaths(DependencyObject? root)
    {
        if (root == null) yield break;
        if (root is System.Windows.Shapes.Path path) yield return path;
        foreach(var child in LogicalTreeHelper.GetChildren(root).OfType<DependencyObject>())
            foreach(var childPath in LogicalPaths(child)) yield return childPath;
    }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
}
