using System.IO;
using System.Windows;
using System.Windows.Controls;
using BloodborneLauncher.Core;
using BloodborneLauncher.Controls;
using TextBox = System.Windows.Controls.TextBox;

namespace BloodborneLauncher.Views
{
    /// <summary>The game's own output (run.py and bb-probe), live while it runs.</summary>
    public sealed class OutputPage : LauncherPage
    {
        private readonly TextBox _output;
        private readonly ToggleSwitch _follow;

        public OutputPage() : base(BloodborneLauncher.Core.UiText.T("Game output"), BloodborneLauncher.Core.UiText.T("What run.py and the game print. Copy or export it when reporting a problem."))
        {
            var bar = new Grid { Margin = new Thickness(0, 18, 0, 8) };
            bar.ColumnDefinitions.Add(new ColumnDefinition());
            bar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            _follow = new ToggleSwitch { IsChecked = true, Content = BloodborneLauncher.Core.UiText.T("Follow new output"), VerticalAlignment = VerticalAlignment.Center };
            bar.Children.Add(_follow);

            var buttons = Row(
                SmallButton(BloodborneLauncher.Core.UiText.T("Copy"), SymbolRegular.Copy24, (_, _) => Copy()),
                SmallButton(BloodborneLauncher.Core.UiText.T("Export…"), SymbolRegular.Save24, (_, _) => Export()),
                SmallButton(BloodborneLauncher.Core.UiText.T("Clear"), SymbolRegular.Broom24, (_, _) => Ws.ClearOutput()));
            Grid.SetColumn(buttons, 1);
            bar.Children.Add(buttons);
            Body.Children.Add(bar);

            SettingToggle(SymbolRegular.WindowConsole20, BloodborneLauncher.Core.UiText.T("Show this page when the game starts"), null, "switch_to_output");

            _output = new TextBox { Height = 520, Margin = new Thickness(0, 4, 0, 0) };
            _output.SetResourceReference(StyleProperty, "CodeBox");
            Body.Children.Add(_output);

            // One append per batch; while the page is hidden nothing is laid out, and it catches up
            // in one go when shown.
            Action<IReadOnlyList<string>> outputChanged = batch =>
            {
                if (!IsVisible)
                {
                    _stale = true;
                    return;
                }
                _lines += batch.Count;
                if (_lines > Workspace.MaxOutputLines + 500)
                {
                    Rebuild();
                    return;
                }
                _output.AppendText(string.Join("\n", batch) + "\n");
                if (_follow.IsChecked == true)
                    _output.ScrollToEnd();
            };
            Action outputCleared = () =>
            {
                _output.Clear();
                _lines = 0;
            };
            Ws.GameOutput += outputChanged;
            Ws.OutputCleared += outputCleared;
            ReleaseWhenDisposed(() => { Ws.GameOutput -= outputChanged; Ws.OutputCleared -= outputCleared; });
            IsVisibleChanged += (_, _) => { if (IsVisible && _stale) Rebuild(); };
            Rebuild();
        }

        private int _lines;
        private bool _stale;

        private void Rebuild()
        {
            _output.Text = Ws.OutputLines.Count > 0 ? string.Join("\n", Ws.OutputLines) + "\n" : "";
            _lines = Ws.OutputLines.Count;
            _stale = false;
            if (_follow.IsChecked == true)
                _output.ScrollToEnd();
        }

        public override void OnShown()
        {
            if (_stale) Rebuild();
            else if (_follow.IsChecked == true)
                _output.ScrollToEnd();
        }

        private void Copy()
        {
            if (_output.Text.Length == 0) return;
            Clipboard.SetText(_output.Text);
            Ws.Log(BloodborneLauncher.Core.UiText.T("Game output copied."), LogLevel.Good);
        }

        private void Export()
        {
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                Title = BloodborneLauncher.Core.UiText.T("Export the game output"),
                FileName = $"bbport-{DateTime.Now:yyyyMMdd_HHmmss}.log",
                Filter = "Log files|*.log|Text files|*.txt|All files|*.*",
            };
            if (dialog.ShowDialog() != true) return;
            try
            {
                File.WriteAllText(dialog.FileName, _output.Text);
                Ws.Log(BloodborneLauncher.Core.UiText.Format("Game output saved: {0}",dialog.FileName), LogLevel.Good);
            }
            catch (Exception e)
            {
                Ws.Log(BloodborneLauncher.Core.UiText.Format("Could not save the output: {0}",e.Message), LogLevel.Bad);
            }
        }
    }
}
