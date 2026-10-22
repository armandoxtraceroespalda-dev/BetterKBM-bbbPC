using System.Windows;
using System.Windows.Controls;
using BloodborneLauncher.Core;

namespace BloodborneLauncher.Views
{
    /// <summary>The less used pages behind one tab: mods, online, advanced settings and the game's log.</summary>
    public sealed class MorePage : LauncherPage
    {
        // Cheats only once the engine applies them.
        private static readonly (string Key, string Label, Type Page)[] Sections = new (string, string, Type)[]
        {
            ("mods", "Mods", typeof(ModsPage)),
            ("output", "Log", typeof(OutputPage)),
            ("engine", "Engine", typeof(EnginePage)),
        };

        private readonly PageCache<Type, LauncherPage> _pages = new(1,page=>page.Dispose());
        private readonly ContentControl _frame = new() { Focusable=false,HorizontalContentAlignment=HorizontalAlignment.Stretch,VerticalContentAlignment=VerticalAlignment.Stretch };
        private readonly Segmented _tabs;
        private int _current = -1;
        private void RefreshAfterNavigation(LauncherPage page)
        {
            // The parent performs this refresh before starting its entrance animation.
            if (IsVisible && ReferenceEquals(_frame.Content, page)) page.OnShown();
        }

        public MorePage() : base(BloodborneLauncher.Core.UiText.T("More"), "")
        {
            _tabs = new Segmented(Sections.Select(s => new Choice(s.Label, s.Key)).ToArray(),uniform:true)
            {
                HorizontalAlignment = HorizontalAlignment.Center,
                Width = Sections.Length * 140,
                // Keep the entire hit area below the 34-DIP native caption strip.
                Margin = new Thickness(0, 42, 0, 8),
            };
            _tabs.Changed += Open;

            var root = new Grid();
            root.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            root.RowDefinitions.Add(new RowDefinition());
            root.Children.Add(_tabs);
            Grid.SetRow(_frame, 1);
            root.Children.Add(_frame);
            Content = BloodborneLauncher.Core.UiText.T(root);
            Open(0);
            ReleaseWhenDisposed(() => {
                _pages.Clear();
                _frame.Content=null;
            });
        }

        public void ShowSection(string key)
        {
            var index = Array.FindIndex(Sections, s => s.Key == key);
            if (index >= 0) Open(index);
        }

        private void Open(int index)
        {
            _tabs.Select(index, notify: false);
            var type = Sections[index].Page;
            var page=_pages.GetOrCreate(type,()=> (LauncherPage)Activator.CreateInstance(type)!);
            page.PrepareForDisplay();
            if (_current != index) { _frame.Content=BloodborneLauncher.Core.UiText.T(page); MainWindow.Instance?.QueueNavigationCleanup(); }
            _current = index;
            RefreshAfterNavigation(page);
        }

        public override void OnShown()
        {
            if (_current >= 0 && _pages.TryGetValue(Sections[_current].Page, out var page))
                RefreshAfterNavigation(page);
        }
    }
}
