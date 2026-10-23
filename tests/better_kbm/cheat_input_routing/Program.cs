using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using BloodborneLauncher.Views;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        _ = new Application();
        var router = typeof(TweaksPage).Assembly.GetType("BloodborneLauncher.Core.CheatInputRouter")!;
        var editable = router.GetMethod("IsEditable", BindingFlags.NonPublic | BindingFlags.Static)!;
        bool Excluded(DependencyObject? value) => (bool)editable.Invoke(null, new object?[] { value })!;
        Check(Excluded(new TextBox()), "Plain text entry is excluded.");
        Check(Excluded(new RichTextBox()), "Rich text entry is excluded.");
        Check(Excluded(new PasswordBox()), "Password entry is excluded.");
        Check(Excluded(new ComboBox { IsEditable = true }), "Editable selector text is excluded.");
        var editor = new RichTextBox();
        var run = new System.Windows.Documents.Run("gitgud");
        editor.Document.Blocks.Add(new System.Windows.Documents.Paragraph(run));
        Check(Excluded(run), "Inner text content follows its editor's exclusion.");
        Check(!Excluded(new Button()) && !Excluded(new ComboBox { IsEditable = false }) && !Excluded(null),
            "Buttons, ordinary selectors and empty focus allow the global phrase.");
        Check(typeof(TweaksPage) != typeof(CheatsPage), "Tweaks and cheats have independent pages.");
        Console.WriteLine("PASS: text, password, rich-text and editable-selector exclusions; independent tweak/cheat pages.");
    }
    private static void Check(bool result, string message) { if (!result) throw new Exception(message); }
}
