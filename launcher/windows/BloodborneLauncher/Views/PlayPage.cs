using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using BloodborneLauncher.Core;
using BloodborneLauncher.Controls;
using Button = BloodborneLauncher.Controls.Button;
using TextBlock = System.Windows.Controls.TextBlock;

namespace BloodborneLauncher.Views
{
    /// <summary>Game folders, saves, languages and session status; launching belongs to Start.</summary>
    public sealed class PlayPage : LauncherPage
    {
        private readonly InfoBar _problem;
        private readonly Button _profileButton;
        private readonly TextBlock _profileHint, _backupHint;
        private readonly Button _restoreButton;
        private readonly TextBlock _gameHint, _dlcHint, _userHint;
        private readonly SymbolIcon _gameIcon;
        private readonly Button _resetUser, _resetDlc;
        private readonly TextBlock _playStatus;
        private readonly DispatcherTimer _clock;

        public PlayPage() : base(BloodborneLauncher.Core.UiText.T("Game"), BloodborneLauncher.Core.UiText.T("Check the game folder, choose where saves go, and set the game languages."))
        {
            Group(BloodborneLauncher.Core.UiText.T("Language"));
            _playStatus = Text("", "Muted");
            _playStatus.Margin = new Thickness(0, 10, 0, 0);
            _playStatus.TextWrapping = TextWrapping.Wrap;
            _playStatus.Visibility = Visibility.Collapsed;

            GameLanguages.Ensure(S);
            var languages = new Grid { VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 8, 0, 0) };
            languages.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            languages.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            languages.ColumnDefinitions.Add(new ColumnDefinition());
            languages.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(16) });
            languages.ColumnDefinitions.Add(new ColumnDefinition());
            languages.Children.Add(LanguagePicker("Text language",
                "Language for the launcher and all game text. The launcher updates immediately; game text changes on the next launch.",
                Choices.GameLanguages, GameLanguages.Text(S), v => {
                    S.Set("text_language", v); Ini.Set("menu_language", GameLanguages.MenuLocale(v));
                    Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ContextIdle,new Action(()=> {
                        UiText.UseTextLanguage(v);MainWindow.Instance?.ApplyLauncherLanguage();
                    }));
                }));
            var voices = LanguagePicker("Voice language",
                "Spoken dialogue language. Requires the selected voice files in your game copy. Applied on the next launch.",
                Choices.VoiceLanguages, GameLanguages.Voice(S), v => S.Set("language", v));
            Grid.SetColumn(voices, 2);
            languages.Children.Add(voices);
            // Stack the selectors in narrow windows instead of clipping their selected language.
            languages.SizeChanged += (_, _) => {
                bool compact = languages.ActualWidth < 560;
                Grid.SetColumn(voices, compact ? 0 : 2);
                Grid.SetRow(voices, compact ? 1 : 0);
                Grid.SetColumnSpan(languages.Children[0], compact ? 3 : 1);
                Grid.SetColumnSpan(voices, compact ? 3 : 1);
                voices.Margin = compact ? new Thickness(0, 14, 0, 0) : new Thickness(0);
            };
            Body.Children.Add(languages);
            Body.Children.Add(_playStatus);

            _problem = new InfoBar { IsClosable = false, IsOpen = false, Margin = new Thickness(0, 12, 0, 0) };
            Body.Children.Add(_problem);

            // Upstream update/Discord cards are intentionally not part of this launcher's UI.

            // ==================== Game ====================
            Group(BloodborneLauncher.Core.UiText.T("Game"));
            var gameCard = Card(SymbolRegular.Folder24, BloodborneLauncher.Core.UiText.T("Game folder"), "", Row(
                SmallButton(BloodborneLauncher.Core.UiText.T("Browse"), SymbolRegular.FolderOpen24, (_, _) => ChooseGame()),
                IconButton(SymbolRegular.Open24, BloodborneLauncher.Core.UiText.T("Open in Explorer"), (_, _) => Paths.OpenFolder(GameEnvironment.GameDir(S)))), out _gameHint);
            _gameIcon = gameCard.Icon;
            gameCard.ShowIcon = true;
            Add(gameCard);

            // The Old Hunters on base-game dumps: the folder of the dumped add-ons (BB_DLC_DIR).
            _resetDlc = IconButton(SymbolRegular.Dismiss24, BloodborneLauncher.Core.UiText.T("No DLC folder"), (_, _) =>
            {
                S.Set("dlc_dir", "");
                Ws.SaveSoon();
                Refresh();
            });
            ToggleRow(SymbolRegular.PuzzlePiece24, BloodborneLauncher.Core.UiText.T("The Old Hunters (DLC)"),
                BloodborneLauncher.Core.UiText.T("Off: the game starts without its DLC. The Game of the Year edition (CUSA03173) always has it"),
                () => S.Bool("dlc_enabled"), v =>
                {
                    S.Set("dlc_enabled", v);
                    Ws.SaveSoon();
                    Refresh();
                });
            Add(Card(SymbolRegular.FolderOpen24, BloodborneLauncher.Core.UiText.T("DLC folder"), "", Row(
                SmallButton(BloodborneLauncher.Core.UiText.T("Browse"), SymbolRegular.FolderOpen24, (_, _) => ChooseDlc()),
                IconButton(SymbolRegular.Open24, BloodborneLauncher.Core.UiText.T("Open in Explorer"), (_, _) => Paths.OpenFolder(GameEnvironment.DlcDir(S))),
                _resetDlc), out _dlcHint));

            // Steam: Bloodborne.exe as a non-Steam game (Core/SteamShortcut.cs).
            Add(Card(SymbolRegular.Games24, BloodborneLauncher.Core.UiText.T("Add to Steam"),
                BloodborneLauncher.Core.UiText.T("Bloodborne as a non-Steam game in your Steam library: Steam Input, the overlay and Big Picture work with it"),
                SmallButton(BloodborneLauncher.Core.UiText.T("Add to Steam"), SymbolRegular.Add24, (_, _) => AddToSteam()), out _));

            // Player profiles: each its own saves and trophies (a button with a menu, not a drop-down,
            // so a click can't pick an entry by itself).
            _profileButton = SmallButton(BloodborneLauncher.Core.UiText.T("Main"), SymbolRegular.Person24, (_, _) => ShowProfileMenu());
            Add(Card(SymbolRegular.People24, BloodborneLauncher.Core.UiText.T("Player profile"),
                BloodborneLauncher.Core.UiText.T("Each profile has its own save files and trophies, so several people can play their own runs on this PC"), Row(
                _profileButton,
                SmallButton(BloodborneLauncher.Core.UiText.T("New…"), SymbolRegular.PersonAdd24, (_, _) => NewProfile()),
                IconButton(SymbolRegular.Rename24, BloodborneLauncher.Core.UiText.T("Rename this profile"), (_, _) => RenameProfile()),
                IconButton(SymbolRegular.PersonDelete24, BloodborneLauncher.Core.UiText.T("Remove this profile from the list (its saves stay on disk)"), (_, _) => RemoveProfile())),
                out _profileHint));

            _resetUser = IconButton(SymbolRegular.ArrowUndo24, BloodborneLauncher.Core.UiText.T("Back to the default folder"), (_, _) =>
            {
                S.Set("user_dir", "");
                Ws.SaveSoon();
                Refresh();
            });
            Add(Card(SymbolRegular.Save24, BloodborneLauncher.Core.UiText.T("Saves folder"), "", Row(
                SmallButton(BloodborneLauncher.Core.UiText.T("Browse"), SymbolRegular.FolderOpen24, (_, _) => ChooseUser()),
                IconButton(SymbolRegular.Open24, BloodborneLauncher.Core.UiText.T("Open in Explorer"), (_, _) => Paths.OpenFolder(GameEnvironment.SavesRoot(S))),
                _resetUser), out _userHint));

            // Save backups: made before each start when the saves changed (Workspace.StartGame).
            _restoreButton = SmallButton(BloodborneLauncher.Core.UiText.T("Restore…"), SymbolRegular.History24, (_, _) => ShowBackupMenu());
            var backupToggle = new ToggleSwitch { IsChecked = S.Bool("save_backups"), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) };
            backupToggle.Click += (_, _) =>
            {
                S.Set("save_backups", backupToggle.IsChecked == true);
                Ws.SaveSoon();
                RefreshBackups();
            };
            Add(Card(SymbolRegular.ArrowSync24, BloodborneLauncher.Core.UiText.T("Save backups"),
                BloodborneLauncher.Core.UiText.F($"Before every start, when this profile's saves changed. The newest {SaveBackups.Keep} are kept"), Row(
                _restoreButton,
                IconButton(SymbolRegular.Open24, BloodborneLauncher.Core.UiText.T("Open the backups folder"), (_, _) =>
                {
                    Directory.CreateDirectory(SaveBackups.Folder(S));
                    Paths.OpenFolder(SaveBackups.Folder(S));
                }),
                backupToggle), out _backupHint));
            Add(Card(SymbolRegular.ArrowImport24, BloodborneLauncher.Core.UiText.T("Import saves"),
                BloodborneLauncher.Core.UiText.T("From shadPS4, another Bloodborne port, or a PS4 save exported decrypted (Apollo Save Tool, Save Wizard). This profile's saves are backed up first"),
                SmallButton(BloodborneLauncher.Core.UiText.T("Import…"), SymbolRegular.FolderArrowRight24, (_, _) => ImportSaves()), out _));


            _clock = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
            _clock.Tick += (_, _) => UpdateRunning();
            Unloaded += (_, _) => _clock.Stop();

            Ws.PortChanged += Refresh;
            ReleaseWhenDisposed(() => Ws.PortChanged -= Refresh);
            Action gameStateChanged = () => { RefreshProfile(); RefreshBackups(); };
            Ws.GameStateChanged += gameStateChanged;
            ReleaseWhenDisposed(() => Ws.GameStateChanged -= gameStateChanged);
            ReleaseWhenDisposed(() => _clock.Stop());
        }

        private StackPanel LanguagePicker(string title, string help, IReadOnlyList<Choice> choices,
            string selected, Action<string> save)
        {
            var panel = new StackPanel { VerticalAlignment = VerticalAlignment.Center };
            var caption = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 0, 0, 6) };
            caption.Children.Add(Text(BloodborneLauncher.Core.UiText.T(PcText.Get(title)), "StatLabel"));
            var info = Icon(SymbolRegular.Info16);
            info.FontSize = 12;
            info.Margin = new Thickness(6, 0, 0, 0);
            info.VerticalAlignment = VerticalAlignment.Center;
            info.ToolTip = BloodborneLauncher.Core.UiText.T(PcText.Get(help));
            ToolTipService.SetInitialShowDelay(info, 150);
            caption.Children.Add(info);
            panel.Children.Add(caption);
            var box = new ComboBox { HorizontalAlignment = HorizontalAlignment.Center,
                MinWidth = 0, MaxDropDownHeight = 360, ToolTip = BloodborneLauncher.Core.UiText.T(PcText.Get(help)) };
            foreach (var choice in choices)
                box.Items.Add(new ComboBoxItem { Content = choice.Label, Tag = choice.Value, ToolTip = choice.Label });
            box.SelectedIndex = Choices.IndexOf(choices, selected);
            if(box.SelectedItem is ComboBoxItem chosen)box.Width=MenuSizing.SelectorWidth(chosen.Content?.ToString()??"",260);
            box.SelectionChanged += (_, _) => {
                if (box.SelectedItem is not ComboBoxItem item || item.Tag is not string value) return;
                box.Width=MenuSizing.SelectorWidth(item.Content?.ToString()??"",260);
                save(value);
                Ws.SaveSoon();
            };
            panel.Children.Add(box);
            return panel;
        }

        public override void OnShown() => Refresh();

        private void Refresh()
        {
            var gameDir = GameEnvironment.GameDir(S);
            var status = GameCheck.Inspect(gameDir);
            SetHint(_gameHint, string.IsNullOrEmpty(gameDir) ? BloodborneLauncher.Core.UiText.T("Not chosen") : BloodborneLauncher.Core.UiText.T(gameDir));
            _gameIcon.Symbol = status.Health == Health.Good ? SymbolRegular.CheckmarkCircle24 : SymbolRegular.Warning24;
            _gameIcon.SetResourceReference(ForegroundProperty, status.Health switch
            {
                Health.Good => "GoodTextBrush",
                Health.Warning => "WarningTextBrush",
                _ => "DangerTextBrush",
            });
            _gameIcon.ToolTip = BloodborneLauncher.Core.UiText.T(status.Summary);

            _problem.IsOpen = status.Health != Health.Good;
            _problem.Severity = status.Health == Health.Bad ? InfoBarSeverity.Error : InfoBarSeverity.Warning;
            _problem.Title = status.Health == Health.Bad ? BloodborneLauncher.Core.UiText.T("The game folder needs attention") : BloodborneLauncher.Core.UiText.T("Check the game");
            _problem.Message = BloodborneLauncher.Core.UiText.T(status.Summary);

            var dlc = GameEnvironment.DlcDir(S);
            var chosen = S.Str("dlc_dir").Length > 0;
            SetHint(_dlcHint, dlc.Length > 0
                ? chosen ? BloodborneLauncher.Core.UiText.T(dlc) : BloodborneLauncher.Core.UiText.F($"Found next to the game: {dlc}")
                : BloodborneLauncher.Core.UiText.Format("Only with a base-game dump: the folder of your dumped add-ons, one folder each (The Old Hunters: SPEXPANSIONDLC03). Found on its own when it is beside the game folder. The Game of the Year edition (CUSA03173) has it already"));
            _resetDlc.IsEnabled = chosen;

            var custom = S.Str("user_dir").Length > 0;
            var root = GameEnvironment.SavesRoot(S);
            SetHint(_userHint, BloodborneLauncher.Core.UiText.Format("{0}  \u00B7  every profile\u0027s saves live here",(custom ? root : BloodborneLauncher.Core.UiText.F($"Default: {root}"))));
            _resetUser.IsEnabled = custom;
            RefreshProfile();
            RefreshBackups();

            UpdateRunning();
        }

        public void UpdateRunning()
        {
            var game = Ws.Game;
            if (game.Running)
            {
                var up = DateTime.Now - game.StartedAt;
                _playStatus.Text = BloodborneLauncher.Core.UiText.F($"Running for {(int)up.TotalMinutes}:{up.Seconds:00}  ·  output on the Game output page");
                if (IsVisible) _clock.Start(); else _clock.Stop();
            }
            else
            {
                _clock.Stop();
                _playStatus.Text = GameCheck.Inspect(GameEnvironment.GameDir(S)).Health == Health.Bad
                    ? BloodborneLauncher.Core.UiText.T("Choose the game folder first")
                    : Ws.Session is { Length.TotalSeconds: > 10 } last
                        ? BloodborneLauncher.Core.UiText.Format("Last session: {0}",last.Summary)
                        : "";
            }
            _playStatus.Visibility = string.IsNullOrEmpty(_playStatus.Text) ? Visibility.Collapsed : Visibility.Visible;
        }

        private void OpenDiscord()
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(LauncherSettings.Discord) { UseShellExecute = true }); }
            catch (Exception e) { Ws.Log(BloodborneLauncher.Core.UiText.Format("Could not open the Discord link: {0}",e.Message), LogLevel.Warn); }
        }

        private void ChooseGame()
        {
            var path = PickFolder("Game folder (the one with eboot.bin)", GameEnvironment.GameDir(S));
            if (path == null) return;
            S.Set("game_dir", path);
            if (!Paths.IsPortDir(Paths.PortDir))
                Ws.ResolvePort(); // the port may sit beside the new game folder
            Ws.SaveSoon();
            Ws.Log(BloodborneLauncher.Core.UiText.Format("Game folder: {0}",path), LogLevel.Step);
            Refresh();
        }

        private void AddToSteam()
        {
            var exe = Path.Combine(Paths.PortDir, "Bloodborne.exe");
            if (!File.Exists(exe))
            {
                Prompt.Tell(BloodborneLauncher.Core.UiText.T("Add to Steam"), BloodborneLauncher.Core.UiText.T("Bloodborne.exe is missing from the Bloodborne PC folder: reinstall the package."));
                return;
            }
            if (SteamShortcut.SteamDir() is null)
            {
                Prompt.Tell(BloodborneLauncher.Core.UiText.T("Add to Steam"), BloodborneLauncher.Core.UiText.T("Steam is not installed for this Windows user."));
                return;
            }
            while (SteamShortcut.SteamRunning())
            {
                if (Prompt.Choose(BloodborneLauncher.Core.UiText.T("Close Steam first"),
                        BloodborneLauncher.Core.UiText.Format("Steam rewrites its list of non-Steam games when it closes, so it has to be closed while Bloodborne is added. Exit Steam (Steam \u203A Exit, or right-click its tray icon \u203A Exit), then press Try again."),
                        BloodborneLauncher.Core.UiText.T("Cancel"), BloodborneLauncher.Core.UiText.T("Try again")) <= 0)
                    return;
            }
            try
            {
                SteamShortcut.Add(exe);
                Ws.Log(BloodborneLauncher.Core.UiText.T("Added Bloodborne to Steam as a non-Steam game"), LogLevel.Good);
                Prompt.Tell(BloodborneLauncher.Core.UiText.T("Added to Steam"),
                    BloodborneLauncher.Core.UiText.Format("Bloodborne is in your Steam library after Steam starts again. It starts the game with this launcher\u0027s settings; Steam shows it as running until the game ends."));
            }
            catch (Exception e)
            {
                Prompt.Tell(BloodborneLauncher.Core.UiText.T("Add to Steam"), BloodborneLauncher.Core.UiText.T(e.Message));
            }
        }

        private void ChooseDlc()
        {
            var path = PickFolder("DLC folder (your dumped add-ons, like SPEXPANSIONDLC03)", GameEnvironment.DlcDir(S));
            if (path == null) return;
            S.Set("dlc_dir", path);
            Ws.SaveSoon();
            Ws.Log(BloodborneLauncher.Core.UiText.Format("DLC folder: {0}",path), LogLevel.Step);
            Refresh();
        }

        // ---------------------------------------------------------------- player profiles

        private void RefreshProfile()
        {
            var profile = PlayerProfiles.Current(S);
            _profileButton.Content = BloodborneLauncher.Core.UiText.T(profile);
            var dir = GameEnvironment.UserDir(S);
            var saves = Path.Combine(dir, "savedata");
            var found = Directory.Exists(saves) && Directory.EnumerateFileSystemEntries(saves).Any();
            var played = SessionStats.Playtime(S, profile);
            SetHint(_profileHint, dir + (found ? "  ·  saves found" : "  ·  no saves yet; the game creates them here") +
                                  $"  ·  {Ws.Trophies.Count} / {TrophyTracker.All.Length} trophies" +
                                  (played.TotalMinutes >= 1 ? $"  ·  {SessionStats.Duration(played)} played" : ""));
        }

        // ---------------------------------------------------------------- save backups

        private void RefreshBackups()
        {
            List<SaveBackups.Backup> list;
            try { list = SaveBackups.List(S); }
            catch { list = new(); }
            _restoreButton.IsEnabled = list.Count > 0;
            SetHint(_backupHint, !S.Bool("save_backups") ? BloodborneLauncher.Core.UiText.T("Off: no backups are made")
                : list.Count == 0 ? BloodborneLauncher.Core.UiText.T("None yet: the first is made when you next press Play")
                : BloodborneLauncher.Core.UiText.F($"{list.Count} for {PlayerProfiles.Current(S)}  ·  newest {list[0].When:d MMM, HH:mm}"));
        }

        private void ShowBackupMenu()
        {
            var menu = new ContextMenu { PlacementTarget = _restoreButton, Placement = PlacementMode.Bottom };
            foreach (var backup in SaveBackups.List(S))
            {
                var item = new System.Windows.Controls.MenuItem
                {
                    Header = $"{backup.When:d MMM yyyy, HH:mm}  ·  {backup.Bytes / 1048576.0:0.0} MB" +
                             (backup.MadeBeforeRestore ? "  ·  before a restore" : ""),
                };
                var chosen = backup;
                item.Click += (_, _) => RestoreBackup(chosen);
                menu.Items.Add(item);
            }
            menu.IsOpen = true;
        }

        private void ImportSaves()
        {
            if (Ws.Game.Running)
            {
                Prompt.Tell(BloodborneLauncher.Core.UiText.T("Import saves"), BloodborneLauncher.Core.UiText.T("Close the game first: it has the saves open."));
                return;
            }
            string? from = null;
            if (SaveImport.Known(S).FirstOrDefault() is { } found)
            {
                var choice = Prompt.Choose(BloodborneLauncher.Core.UiText.T("Import saves"),
                    BloodborneLauncher.Core.UiText.F($"Bloodborne saves found ({found.From}), last saved {found.Saved:d MMM yyyy, HH:mm}:\n{found.Dir}"),
                    BloodborneLauncher.Core.UiText.T("Cancel"), BloodborneLauncher.Core.UiText.T("Choose others…"), "Import these");
                if (choice <= 0) return;
                if (choice == 2) from = found.Dir;
            }
            if (from == null)
            {
                var how = Prompt.Choose(BloodborneLauncher.Core.UiText.T("Import saves"),
                    BloodborneLauncher.Core.UiText.Format("Choose the folder with your saves (another port\u0027s whole folder is fine), or a save file: a .zip of the save, or one of its files (userdata0000)."),
                    BloodborneLauncher.Core.UiText.T("Cancel"), BloodborneLauncher.Core.UiText.T("A folder…"), "A file or .zip…");
                if (how <= 0) return;
                string? folder;
                if (how == 1)
                {
                    folder = PickFolder("The folder with your saves (another port's whole folder is fine)", "");
                }
                else
                {
                    var dialog = new Microsoft.Win32.OpenFileDialog
                    {
                        Title = BloodborneLauncher.Core.UiText.T("A save file: a .zip of the save, or one of its files (userdata0000)"),
                        Filter = "Bloodborne saves (*.zip;userdata*)|*.zip;userdata*|All files|*.*",
                    };
                    try { folder = dialog.ShowDialog() == true ? SaveImport.FolderOfFile(dialog.FileName) : null; }
                    catch (Exception e)
                    {
                        Prompt.Tell(BloodborneLauncher.Core.UiText.T("Import saves"), BloodborneLauncher.Core.UiText.Format("Could not open that file: {0}",e.Message));
                        return;
                    }
                }
                if (folder == null) return;
                if (SaveImport.Find(folder, "chosen folder").FirstOrDefault() is not { } inside)
                {
                    Prompt.Tell(BloodborneLauncher.Core.UiText.T("Import saves"), SaveImport.HasEncryptedPs4Save(folder)
                        ? BloodborneLauncher.Core.UiText.Format("That is a PS4 save as the console copies it to USB: it is encrypted with that PS4\u0027s own keys, which only the console has, so no PC program can open it.\n\nExport it decrypted instead: on a jailbroken PS4 with Apollo Save Tool (Copy save game \u2192 decrypted, to USB), or with Save Wizard for PS4. Then import that folder here.")
                        : BloodborneLauncher.Core.UiText.T("No Bloodborne saves in that folder or the folders under it (a SPRJ0005 folder with userdata files)."));
                    return;
                }
                from = inside.Dir;
            }
            var target = SaveImport.Target(S);
            if (string.Equals(Path.GetFullPath(from), Path.GetFullPath(target), StringComparison.OrdinalIgnoreCase))
            {
                Prompt.Tell(BloodborneLauncher.Core.UiText.T("Import saves"), BloodborneLauncher.Core.UiText.T("Those are already this profile's saves."));
                return;
            }
            var replacing = SaveImport.HasSaves(target)
                ? "\n\nThis profile's current saves are backed up first: Save backups › Restore… brings them back."
                : "";
            if (!Prompt.Confirm(BloodborneLauncher.Core.UiText.T("Import saves"), BloodborneLauncher.Core.UiText.F($"Copy the saves from\n{from}\ninto {PlayerProfiles.Current(S)}'s saves?{replacing}"), BloodborneLauncher.Core.UiText.T("Import")))
                return;
            try
            {
                SaveImport.Import(S, from);
                Ws.Log(BloodborneLauncher.Core.UiText.F($"Saves imported from {from}."), LogLevel.Good);
                Prompt.Tell(BloodborneLauncher.Core.UiText.T("Saves imported"), BloodborneLauncher.Core.UiText.T("Start the game and choose Continue or Load Game."));
            }
            catch (Exception e)
            {
                Ws.Log(BloodborneLauncher.Core.UiText.Format("Could not import the saves: {0}",e.Message), LogLevel.Bad);
                Prompt.Tell(BloodborneLauncher.Core.UiText.T("Import saves"), BloodborneLauncher.Core.UiText.Format("Could not import: {0}",e.Message));
            }
            Refresh();
        }

        private void RestoreBackup(SaveBackups.Backup backup)
        {
            if (Ws.Game.Running)
            {
                Prompt.Tell(BloodborneLauncher.Core.UiText.T("Restore saves"), BloodborneLauncher.Core.UiText.T("Close the game first: it has the saves open."));
                return;
            }
            if (!Prompt.Confirm(BloodborneLauncher.Core.UiText.T("Restore saves"),
                    BloodborneLauncher.Core.UiText.Format("{0}What is there now is backed up first, so you can undo this from the same list.",BloodborneLauncher.Core.UiText.F($"{PlayerProfiles.Current(S)}'s saves go back to {backup.When:d MMM yyyy, HH:mm}. ")), BloodborneLauncher.Core.UiText.T("Restore")))
                return;
            try
            {
                SaveBackups.Restore(S, backup);
                Ws.Log(BloodborneLauncher.Core.UiText.F($"Saves restored from {backup.When:d MMM yyyy, HH:mm}."), LogLevel.Good);
            }
            catch (Exception e)
            {
                Ws.Log(BloodborneLauncher.Core.UiText.Format("Could not restore the saves: {0}",e.Message), LogLevel.Bad);
                Prompt.Tell(BloodborneLauncher.Core.UiText.T("Restore saves"), BloodborneLauncher.Core.UiText.Format("Could not restore: {0}",e.Message));
            }
            Refresh();
        }

        private void ShowProfileMenu()
        {
            var menu = new ContextMenu { PlacementTarget = _profileButton, Placement = PlacementMode.Bottom };
            var current = PlayerProfiles.Current(S);
            foreach (var profile in PlayerProfiles.All(S))
            {
                var name = profile;
                var item = new System.Windows.Controls.MenuItem { Header = BloodborneLauncher.Core.UiText.T(name), IsCheckable = true, IsChecked = name.Equals(current, StringComparison.OrdinalIgnoreCase) };
                item.Click += (_, _) => SwitchProfile(name);
                menu.Items.Add(item);
            }
            menu.IsOpen = true;
        }

        private bool GameBlocksProfiles()
        {
            if (!Ws.Game.Running) return false;
            Prompt.Tell(BloodborneLauncher.Core.UiText.T("Player profile"), BloodborneLauncher.Core.UiText.T("Close the game before changing profiles: it is using this profile's saves."));
            return true;
        }

        private void SwitchProfile(string name)
        {
            if (name.Equals(PlayerProfiles.Current(S), StringComparison.OrdinalIgnoreCase) || GameBlocksProfiles()) return;
            S.Set("player_profile", PlayerProfiles.IsMain(name) ? "" : name);
            Directory.CreateDirectory(GameEnvironment.UserDir(S));
            Ws.Trophies.SetUserDir(GameEnvironment.UserDir(S));
            Ws.SaveSoon();
            Ws.Log(BloodborneLauncher.Core.UiText.F($"Player profile: {name}."), LogLevel.Step);
            Refresh();
        }

        private void NewProfile()
        {
            if (GameBlocksProfiles()) return;
            var values = Prompt.Ask(BloodborneLauncher.Core.UiText.T("New player profile"), BloodborneLauncher.Core.UiText.T("Its own save files and trophies. The game starts a new character the first time."),
                new Prompt.Field(BloodborneLauncher.Core.UiText.T("Name"), "", BloodborneLauncher.Core.UiText.T("e.g. Alex")));
            if (values == null) return;
            var name = PlayerProfiles.Validate(S, values[0], out var error);
            if (name == null) { Prompt.Tell(BloodborneLauncher.Core.UiText.T("New player profile"), BloodborneLauncher.Core.UiText.T(error)); return; }
            PlayerProfiles.Add(S, name);
            SwitchProfile(name);
        }

        private void RenameProfile()
        {
            var current = PlayerProfiles.Current(S);
            if (PlayerProfiles.IsMain(current)) { Prompt.Tell(BloodborneLauncher.Core.UiText.T("Rename profile"), BloodborneLauncher.Core.UiText.T("Main is the original saves folder and keeps its name.")); return; }
            if (GameBlocksProfiles()) return;
            var values = Prompt.Ask(BloodborneLauncher.Core.UiText.T("Rename profile"), null, new Prompt.Field(BloodborneLauncher.Core.UiText.T("Name"), current));
            if (values == null) return;
            var name = PlayerProfiles.Validate(S, values[0], out var error, renaming: current);
            if (name == null) { Prompt.Tell(BloodborneLauncher.Core.UiText.T("Rename profile"), BloodborneLauncher.Core.UiText.T(error)); return; }
            try
            {
                PlayerProfiles.Rename(S, current, name);
            }
            catch (Exception e)
            {
                Prompt.Tell(BloodborneLauncher.Core.UiText.T("Rename profile"), BloodborneLauncher.Core.UiText.Format("Could not rename its folder: {0}",e.Message));
                return;
            }
            Ws.Trophies.SetUserDir(GameEnvironment.UserDir(S));
            Ws.SaveSoon();
            Refresh();
        }

        private void RemoveProfile()
        {
            var current = PlayerProfiles.Current(S);
            if (PlayerProfiles.IsMain(current)) { Prompt.Tell(BloodborneLauncher.Core.UiText.T("Remove profile"), BloodborneLauncher.Core.UiText.T("Main is the original saves folder and can't be removed.")); return; }
            if (GameBlocksProfiles()) return;
            if (!Prompt.Confirm(BloodborneLauncher.Core.UiText.T("Remove profile"),
                    BloodborneLauncher.Core.UiText.F($"{current} is taken off the list and the launcher switches to Main. Its saves and trophies are not deleted: ") +
                    BloodborneLauncher.Core.UiText.F($"they stay in {GameEnvironment.UserDir(S)}, and adding a profile with the same name brings them back."), BloodborneLauncher.Core.UiText.T("Remove")))
                return;
            PlayerProfiles.Remove(S, current);
            S.Set("player_profile", "");
            Ws.Trophies.SetUserDir(GameEnvironment.UserDir(S));
            Ws.SaveSoon();
            Refresh();
        }

        private void ChooseUser()
        {
            if (GameBlocksProfiles()) return;
            var path = PickFolder("Saves folder", GameEnvironment.SavesRoot(S));
            if (path == null) return;
            S.Set("user_dir", path);
            Ws.SaveSoon();
            Refresh();
        }
    }
}
