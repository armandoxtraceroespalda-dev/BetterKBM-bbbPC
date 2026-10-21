using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using BloodborneLauncher.Core;
using BloodborneLauncher.Controls;
using Button = BloodborneLauncher.Controls.Button;
using TextBlock = System.Windows.Controls.TextBlock;
using TextBox = BloodborneLauncher.Controls.TextBox;

namespace BloodborneLauncher.Views
{
    /// <summary>
    /// One setting: its title on the left (descriptions in an ⓘ tooltip), its control on the right,
    /// or below it for wide controls such as segmented buttons. Hint is a line under the title for
    /// live status (a path, "not available on this GPU"); it stays hidden until something is set.
    /// </summary>
    public sealed class SettingRow : Border
    {
        private SymbolIcon? _icon;
        private readonly SymbolRegular _symbol;
        private readonly Grid _iconGrid;
        public SymbolIcon Icon
        {
            get
            {
                if (_icon != null) return _icon;
                // Most Bloodborne rows hide icons; allocate a glyph control only when requested.
                _icon = new SymbolIcon { Symbol = _symbol, FontSize = Classic ? 20 : 18,
                    Margin = new Thickness(0, 0, Classic ? 16 : 12, 0), VerticalAlignment = VerticalAlignment.Center,
                    Visibility = Visibility.Collapsed };
                _iconGrid.Children.Add(_icon);
                return _icon;
            }
        }
        public TextBlock Hint { get; }

        public bool ShowIcon
        {
            get => _icon?.Visibility == Visibility.Visible;
            set
            {
                if (value) Icon.Visibility = Visibility.Visible;
                else if (_icon != null) _icon.Visibility = Visibility.Collapsed;
            }
        }

        /// <summary>Classic layout: one card per setting with its icon and description, as before 0.9.4.</summary>
        public static bool Classic => true;

        public SettingRow(SymbolRegular icon, string title, string? info, object? control, bool wide)
        {
            MinHeight = 70;
            CornerRadius = new CornerRadius(0);
            Margin = new Thickness(0, 0, 0, 2);

            _symbol = icon;

            var name = new TextBlock { Text = BloodborneLauncher.Core.UiText.T(title), FontSize = LauncherAppearance.Bloodborne ? 22 : 15, VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis };
            Hint = new TextBlock { FontSize = 16, FontFamily = LauncherAppearance.DetailFont, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 3, 0, 0), Visibility = Visibility.Collapsed };
            Hint.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorTertiaryBrush");

            var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            text.Children.Add(name);
            if (Classic && !string.IsNullOrWhiteSpace(info))
            {
                // Classic shows the description under the title instead of in a tooltip.
                var description = new TextBlock { Text = BloodborneLauncher.Core.UiText.T(info), FontSize = 16, FontFamily = LauncherAppearance.DetailFont, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 2, 0, 0) };
                description.SetResourceReference(TextBlock.ForegroundProperty, "TextFillColorSecondaryBrush");
                text.Children.Add(description);
            }

            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            _iconGrid = grid;
            if (Classic && !LauncherAppearance.Bloodborne) ShowIcon = true;
            Grid.SetColumn(text, 1);
            grid.Children.Add(text);

            if (control is UIElement element)
            {
                if (wide)
                {
                    // Title on its own line, the control across the row, the hint under it.
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    if (element is FrameworkElement fe) fe.Margin = new Thickness(0, 8, 0, 0);
                    Grid.SetRow(element, 1);
                    Grid.SetColumn(element, 1);
                    Grid.SetColumnSpan(element, 2);
                    grid.Children.Add(element);
                    grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                    Grid.SetRow(Hint, 2);
                    Grid.SetColumn(Hint, 1);
                    Grid.SetColumnSpan(Hint, 2);
                    grid.Children.Add(Hint);
                }
                else
                {
                    text.Children.Add(Hint);
                    if (element is FrameworkElement fe) fe.Margin = new Thickness(12, 0, 0, 0);
                    Grid.SetColumn(element, 2);
                    grid.Children.Add(element);
                }
            }
            else
                text.Children.Add(Hint);

            BorderThickness = new Thickness(0);
            Background = Brushes.Transparent;
            Padding = new Thickness(18, 12, 18, 16);
            var surface = new Grid();
            // Rows retain their classic title/description arrangement; only controls glow.
            surface.Children.Add(grid);
            var line = new System.Windows.Shapes.Rectangle { Height = 1, VerticalAlignment = VerticalAlignment.Bottom,
                IsHitTestVisible = false, Opacity = 0.65 };
            line.SetResourceReference(System.Windows.Shapes.Shape.FillProperty, "CardStrokeColorDefaultBrush");
            surface.Children.Add(line);
            Child = surface;
            if (LauncherAppearance.Bloodborne)
            {
                if (control is Control item) item.FontFamily = item is System.Windows.Controls.TextBox ? LauncherAppearance.DetailFont : LauncherAppearance.MenuFont;
            }
        }
    }

    /// <summary>Native underline choices with mouse and keyboard activation.</summary>
    public sealed class Segmented : Border
    {
        private readonly List<System.Windows.Controls.Button> _items=new();
        public int SelectedIndex { get; private set; }=-1;
        public event Action<int>? Changed;
        public Segmented(IReadOnlyList<Choice> choices,Func<Choice,bool>? enabled=null,double spacing=4,bool uniform=false)
        {
            Background=Brushes.Transparent;
            var grid=new Grid { HorizontalAlignment=uniform?HorizontalAlignment.Stretch:HorizontalAlignment.Center };
            for(int i=0;i<choices.Count;i++)
            {
                grid.ColumnDefinitions.Add(new ColumnDefinition { Width=uniform?new GridLength(1,GridUnitType.Star):GridLength.Auto });int index=i;var choice=choices[i];
                var item=new System.Windows.Controls.Button { Content=Short(choice.Label),MinHeight=42,Margin=new Thickness(spacing,0,spacing,0),
                    HorizontalAlignment=HorizontalAlignment.Stretch,HorizontalContentAlignment=HorizontalAlignment.Center,
                    ToolTip=choice.Note??choice.Label,IsEnabled=enabled?.Invoke(choice)??true };
                item.SetResourceReference(StyleProperty,"BloodborneSegmentButton");
                item.Click+=(_,_)=>Select(index,true);Grid.SetColumn(item,i);grid.Children.Add(item);_items.Add(item);
            }
            Child=grid;
            PreviewKeyDown+=(_,e)=>{
                if(e.Key is not (Key.Left or Key.Right))return;
                int direction=e.Key==Key.Right?1:-1;
                for(int n=1;n<=_items.Count;n++)
                {
                    int i=(System.Math.Max(0,SelectedIndex)+direction*n+_items.Count*2)%_items.Count;
                    if(!_items[i].IsEnabled)continue;Select(i,true);_items[i].Focus();e.Handled=true;break;
                }
            };
        }
        public static string Short(string label)=>label.Split(" (")[0];
        public void Select(int index,bool notify)
        {
            if(index<0 || index>=_items.Count || !_items[index].IsEnabled)return;
            SelectedIndex=index;
            for(int i=0;i<_items.Count;i++)_items[i].Tag=i==index?"active":null;
            if(notify)Changed?.Invoke(index);
        }
    }

    /// <summary>
    /// A settings page: groups of compact rows, each group one rounded box (Sectionize puts the
    /// rows of a group into it once the page is built). The helpers bind a control straight to a
    /// settings.json or bbport.ini key and save on change.
    /// </summary>
    public abstract class LauncherPage : UserControl, IDisposable
    {
        protected static Workspace Ws => Workspace.Current;
        protected static LauncherSettings S => Ws.Settings;
        protected static IniFile Ini => Ws.Ini;

        protected StackPanel Body { get; }
        protected ScrollViewer Scroller { get; }
        private bool _sectioned;
        private readonly bool _settingsShell;
        private readonly PageLifetime _lifetime = new();
        private bool _disposed;
        protected bool IsDisposed => _disposed;
        protected void ReleaseWhenDisposed(Action release) => _lifetime.Add(release);
        public void Dispose()
        {
            if(_disposed)return;
            _disposed=true;
            try { _lifetime.Dispose(); }
            finally
            {
                Content=null;
                Body?.Children.Clear();
                if(Scroller!=null)Scroller.Content=null;
            }
        }

        protected LauncherPage(string title, string subtitle, bool settingsShell=true)
        {
            _settingsShell=settingsShell;
            var lease=ViewRetention.Register(this);
            ReleaseWhenDisposed(lease.Retire);
            Background = Brushes.Transparent;
            HorizontalContentAlignment=HorizontalAlignment.Stretch;
            VerticalContentAlignment=VerticalAlignment.Stretch;
            if (LauncherAppearance.Bloodborne) FontFamily = LauncherAppearance.MenuFont;
            SetResourceReference(ForegroundProperty, "TextFillColorPrimaryBrush");
            SetResourceReference(System.Windows.Documents.TextElement.ForegroundProperty, "TextFillColorPrimaryBrush");

            FontSize=21;
            if(!settingsShell) { Body=null!;Scroller=null!;return; }

            // The tab says which page this is; no page title.
            Body = new StackPanel { Margin = new Thickness(22, 8, 22, 24), MaxWidth = 1080 };
            Scroller = new ScrollViewer { VerticalScrollBarVisibility = ScrollBarVisibility.Auto, Content = BloodborneLauncher.Core.UiText.T(Body) };
            var shell = new Grid { Margin = new Thickness(24, 8, 24, 12) };
            shell.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            shell.RowDefinitions.Add(new RowDefinition());
            shell.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            var heading = new Grid { Height = 44, MaxWidth = 1080 };
            var label = new TextBlock { Text = BloodborneLauncher.Core.UiText.T(title), FontSize = 25, HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Bottom, Margin = new Thickness(0, 0, 0, 2) };
            label.SetResourceReference(TextBlock.ForegroundProperty, "SectionHeadingBrush");
            heading.Children.Add(label);shell.Children.Add(heading);
            Grid.SetRow(Scroller, 1);shell.Children.Add(Scroller);
            Content = BloodborneLauncher.Core.UiText.T(shell);
            PageScroll.Attach(Scroller);
            Loaded += (_, _) =>
            {
                PrepareForDisplay();
            };
        }

        /// <summary>Build the final section tree before WPF expands templates in a visible host.</summary>
        public void PrepareForDisplay()
        {
            if (_sectioned || _disposed) return;
            _sectioned = true;
            if(_settingsShell)Sectionize();
        }

        /// <summary>Layer a dialog over the entire settings shell without reparenting its scroller.</summary>
        protected void AttachOverlay(UIElement overlay)
        {
            if (Content is not UIElement settings)
                throw new InvalidOperationException(BloodborneLauncher.Core.UiText.T("The settings shell must exist before adding an overlay."));
            var root = new Grid();
            Content = null;
            root.Children.Add(settings);
            root.Children.Add(overlay);
            Content = BloodborneLauncher.Core.UiText.T(root);
        }

        /// <summary>Called each time the page is shown; refresh anything read from disk.</summary>
        public virtual void OnShown() { }

        /// <summary>
        /// Puts the rows that follow each group header into one rounded box. Rows, panels of rows
        /// and short captions go in; anything else (an info bar, a table, a summary card) ends the box.
        /// </summary>
        private void Sectionize()
        {
            var children=Body.Children.Cast<UIElement>().ToList();Body.Children.Clear();
            StackPanel? box=null;
            foreach(var child in children)
            {
                if(child is TextBlock { Tag:"group" }) { Body.Children.Add(child);box=null;continue; }
                if(child is MenuSurface) { Body.Children.Add(child);box=null;continue; }
                if(box==null)
                {
                    box=new StackPanel();
                    Body.Children.Add(new MenuSurface { Child=box,Padding=new Thickness(16,14,16,16),Margin=new Thickness(-8,0,-8,10) });
                }
                box.Children.Add(child);
            }
        }

        // ---------------------------------------------------------------- building blocks

        public static TextBlock Text(string text, string style)
        {
            var block = new TextBlock { Text = BloodborneLauncher.Core.UiText.T(text) };
            block.SetResourceReference(StyleProperty, style);
            return block;
        }

        protected TextBlock Group(string text, Panel? into = null)
        {
            var header = Text(BloodborneLauncher.Core.UiText.T(text), "GroupHeader");
            header.Tag = "group";
            (into ?? Body).Children.Add(header);
            return header;
        }

        public static SymbolIcon Icon(SymbolRegular symbol) => new() { Symbol = symbol };

        /// <summary>A setting row. info goes in the ⓘ tooltip; hintBlock is a visible status line, empty until set.</summary>
        public static SettingRow Card(SymbolRegular icon, string title, string? info, object? control, out TextBlock hintBlock, bool wide = false)
        {
            var row = new SettingRow(icon, title, info, control, wide);
            hintBlock = row.Hint;
            return row;
        }

        public static void SetHint(TextBlock hint, string? text)
        {
            hint.Text = BloodborneLauncher.Core.UiText.T(text);
            hint.Visibility = string.IsNullOrEmpty(text) ? Visibility.Collapsed : Visibility.Visible;
        }

        protected SettingRow Add(SettingRow row, Panel? into = null)
        {
            (into ?? Body).Children.Add(row);
            return row;
        }

        public static Button SmallButton(string text, SymbolRegular icon, RoutedEventHandler click, ControlAppearance look = ControlAppearance.Secondary)
        {
            var button = new Button
            {
                Content = BloodborneLauncher.Core.UiText.T(text),
                Icon = Icon(icon),
                Appearance = look,
                Margin = new Thickness(6, 0, 0, 0),
                Padding = new Thickness(10, 5, 10, 5),
                VerticalAlignment = VerticalAlignment.Center,
            };
            button.Click += click;
            return button;
        }

        public static Button IconButton(SymbolRegular icon, string tooltip, RoutedEventHandler click)
        {
            var button = new Button
            {
                Icon = Icon(icon),
                ToolTip = BloodborneLauncher.Core.UiText.T(tooltip),
                Appearance = ControlAppearance.Transparent,
                Padding = new Thickness(6),
                Margin = new Thickness(2, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
            };
            button.Click += click;
            return button;
        }

        public static StackPanel Row(params UIElement[] children)
        {
            var panel = new StackPanel { Orientation = Orientation.Horizontal, VerticalAlignment = VerticalAlignment.Center };
            foreach (var child in children)
                panel.Children.Add(child);
            return panel;
        }

        // ---------------------------------------------------------------- bound rows

        /// <summary>A choice: segmented buttons for a few short options, a drop-down otherwise.
        /// The chosen option's note shows under the row.</summary>
        protected FrameworkElement ComboRow(SymbolRegular icon, string title, string? hint, IReadOnlyList<Choice> choices,
            Func<string> get, Action<string> set, Panel? into = null, double width = 240, Action? changed = null,
            Func<Choice, bool>? enabled = null)
        {
            TextBlock note = null!;
            void ShowNote(int index) => SetHint(note, index >= 0 ? BloodborneLauncher.Core.UiText.T(choices[index].Note) : null);
            void Picked(int index)
            {
                set(choices[index].Value);
                ShowNote(index);
                Ws.SaveSoon();
                changed?.Invoke();
            }

            var shortLabels = choices.Sum(c => Segmented.Short(c.Label).Length);
            if (!SettingRow.Classic && choices.Count is >= 2 and <= 6 && shortLabels <= 72)
            {
                var segmented = new Segmented(choices, enabled);
                var row = Add(Card(icon, BloodborneLauncher.Core.UiText.T(title), BloodborneLauncher.Core.UiText.T(hint), segmented, out note, wide: true), into);
                segmented.Select(Choices.IndexOf(choices, get()), notify: false);
                ShowNote(segmented.SelectedIndex);
                segmented.Changed += Picked;
                row.Tag = note;
                return segmented;
            }

            var box = new ComboBox { Width = width, VerticalAlignment = VerticalAlignment.Center };
            foreach (var choice in choices)
                box.Items.Add(new ComboBoxItem { Content = choice.Label, Tag = choice.Value });
            var card = Add(Card(icon, BloodborneLauncher.Core.UiText.T(title), BloodborneLauncher.Core.UiText.T(hint), box, out note), into);
            box.SelectedIndex = Choices.IndexOf(choices, get());
            ShowNote(box.SelectedIndex);
            void FitSelection() { if(box.SelectedIndex>=0)box.Width=MenuSizing.SelectorWidth(choices[box.SelectedIndex].Label,width); }
            FitSelection();
            box.SelectionChanged += (_, _) => { if (box.SelectedIndex >= 0) { FitSelection();Picked(box.SelectedIndex); } };
            card.Tag = note;
            return box;
        }

        protected FrameworkElement SettingCombo(SymbolRegular icon, string title, string? hint, IReadOnlyList<Choice> choices,
            string key, Panel? into = null, Action? changed = null) =>
            ComboRow(icon, BloodborneLauncher.Core.UiText.T(title), BloodborneLauncher.Core.UiText.T(hint), choices, () => S.Str(key), v => S.Set(key, v), into, changed: changed);

        protected FrameworkElement IniCombo(SymbolRegular icon, string title, string? hint, IReadOnlyList<Choice> choices,
            string key, Panel? into = null, Action? changed = null, Func<Choice, bool>? enabled = null) =>
            ComboRow(icon, BloodborneLauncher.Core.UiText.T(title), BloodborneLauncher.Core.UiText.T(hint), choices, () => Ini.Get(key), v => Ini.Set(key, v), into, changed: changed, enabled: enabled);

        protected ToggleSwitch ToggleRow(SymbolRegular icon, string title, string? hint, Func<bool> get, Action<bool> set,
            Panel? into = null, Action? changed = null)
        {
            var toggle = new ToggleSwitch { IsChecked = get(), VerticalAlignment = VerticalAlignment.Center };
            toggle.Click += (_, _) =>
            {
                set(toggle.IsChecked == true);
                Ws.SaveSoon();
                changed?.Invoke();
            };
            Add(Card(icon, BloodborneLauncher.Core.UiText.T(title), BloodborneLauncher.Core.UiText.T(hint), toggle, out _), into);
            return toggle;
        }

        protected ToggleSwitch SettingToggle(SymbolRegular icon, string title, string? hint, string key, Panel? into = null, Action? changed = null) =>
            ToggleRow(icon, BloodborneLauncher.Core.UiText.T(title), BloodborneLauncher.Core.UiText.T(hint), () => S.Bool(key), v => S.Set(key, v), into, changed);

        protected ToggleSwitch IniToggle(SymbolRegular icon, string title, string? hint, string key, Panel? into = null, Action? changed = null) =>
            ToggleRow(icon, BloodborneLauncher.Core.UiText.T(title), BloodborneLauncher.Core.UiText.T(hint), () => Ini.Flag(key), v => Ini.Set(key, v ? "1" : "0"), into, changed);

        /// <summary>A slider with its value printed beside it.</summary>
        protected Slider SliderRow(SymbolRegular icon, string title, string? hint, double min, double max, double step,
            Func<double> get, Action<double> set, string format = "0.00", Panel? into = null, double valueWidth = 44)
        {
            var slider = new Slider
            {
                Minimum = min,
                Maximum = max,
                SmallChange = step,
                LargeChange = step * 4,
                TickFrequency = step,
                IsSnapToTickEnabled = true,
                Width = 260,
                VerticalAlignment = VerticalAlignment.Center,
                Value = Math.Clamp(get(), min, max),
            };
            var value = new TextBlock
            {
                Width = valueWidth,
                TextAlignment = TextAlignment.Right,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(0, 0, 10, 0),
            };
            value.SetResourceReference(TextBlock.FontFamilyProperty, "MonoFont");
            value.Text = BloodborneLauncher.Core.UiText.T(slider.Value.ToString(format, System.Globalization.CultureInfo.InvariantCulture));
            slider.ValueChanged += (_, _) =>
            {
                value.Text = BloodborneLauncher.Core.UiText.T(slider.Value.ToString(format, System.Globalization.CultureInfo.InvariantCulture));
                set(slider.Value);
                Ws.SaveSoon();
            };
            Add(Card(icon, BloodborneLauncher.Core.UiText.T(title), BloodborneLauncher.Core.UiText.T(hint), Row(value, slider), out _), into);
            return slider;
        }

        /// <summary>A text field row; saves as you type.</summary>
        protected TextBox TextRow(SymbolRegular icon, string title, string? hint, string placeholder,
            Func<string> get, Action<string> set, Panel? into = null, double width = 300)
        {
            var box = new TextBox
            {
                Text = get(),
                PlaceholderText = BloodborneLauncher.Core.UiText.T(placeholder),
                PlaceholderEnabled = true,
                Width = width,
                VerticalAlignment = VerticalAlignment.Center,
            };
            box.TextChanged += (_, _) =>
            {
                set(box.Text);
                Ws.SaveSoon();
            };
            Add(Card(icon, BloodborneLauncher.Core.UiText.T(title), BloodborneLauncher.Core.UiText.T(hint), box, out _), into);
            return box;
        }

        protected static string Fmt(double value) => value.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture);

        protected static double ParseDouble(string text, double fallback) =>
            double.TryParse(text, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var v)
                ? v
                : fallback;

        protected static string? PickFolder(string title, string current)
        {
            var dialog = new Microsoft.Win32.OpenFolderDialog { Title = BloodborneLauncher.Core.UiText.T(title) };
            if (System.IO.Directory.Exists(current))
                dialog.InitialDirectory = current;
            return dialog.ShowDialog() == true ? dialog.FolderName : null;
        }
    }
}
