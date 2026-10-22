using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using BloodborneLauncher.Core;
using BloodborneLauncher.Controls;

namespace BloodborneLauncher.Views
{
    /// <summary>
    /// Cheats the engine applies live (bbport.ini cheat_*: off, infinite or an amount kept topped up).
    /// Turning one off restores the game values it changed; the items you have stay.
    /// </summary>
    public sealed class CheatsPage : LauncherPage
    {
        private static readonly Choice[] Modes =
        {
            new("Off", "off"),
            new("Infinite", "infinite", "Kept full at the normal carry limit, even if you have none. Items put into storage while it is on come back"),
            new("Custom amount", "custom", "Refilled to the amount below whenever it drops"),
        };

        /// <summary>Engine builds from before the cheats module: they ignore the cheat_* keys.</summary>
        private static readonly string[] EnginesWithoutCheats =
            { "bbhost-0.4.0-fps1", "bbhost-0.4.0-fps2", "bbhost-0.4.0-fps3", "bbhost-0.4.0-fps4" };

        private readonly InfoBar _engine;
        private readonly StackPanel _cheatsPanel = new OptionRows();
        private Window? _inputWindow;

        private void DetachSecretInput()
        {
            if (_inputWindow != null) _inputWindow.PreviewTextInput -= SecretInput;
            _inputWindow = null; CheatAccess.Session.ResetInput();
        }
        private void SecretInput(object sender, TextCompositionEventArgs e)
        {
            if (!IsVisible || !IsKeyboardFocusWithin && Window.GetWindow(this)?.IsActive != true) return;
            // Never inspect account fields, key capture, search text or other editable controls.
            if (Keyboard.FocusedElement is TextBoxBase or PasswordBox) { CheatAccess.Session.ResetInput(); return; }
            if (CheatAccess.Session.Feed(e.Text))
            {
                bool unlocked = CheatAccess.Session.Unlocked;
                S.Set("cheats_unlocked", unlocked);
                if (!unlocked) CheatLaunchNotice.DisableAll((key, value) => Ini.Set(key, value), Ini.Values.Select(p => p.Key).ToArray());
                Ws.SaveNow();
                _cheatsPanel.Visibility = unlocked ? Visibility.Visible : Visibility.Collapsed;
                // Recreate controls after reset so a later unlock never displays stale checked values.
                if (!unlocked) Dispatcher.BeginInvoke(new Action(() => MainWindow.Instance?.ReloadPages()));
                e.Handled = true;
            }
        }

        public CheatsPage() : base(BloodborneLauncher.Core.UiText.T("Tweaks"),
            BloodborneLauncher.Core.UiText.T("Set here for the next start. While playing, change them in the in-game menu (Insert or L3 + R3): they apply live. Turning a cheat off puts the game's values back; items you already have stay."))
        {
            Group(BloodborneLauncher.Core.UiText.T(PcText.Get("Gameplay Tweaks")));
            IniToggle(SymbolRegular.Person24,BloodborneLauncher.Core.UiText.T(PcText.Get("Hunter's dream mirror opens the appearance editor")),null,"change_appearance");
            IniToggle(SymbolRegular.ArrowUndo24,BloodborneLauncher.Core.UiText.T(PcText.Get("The Altar of Despair resets level and attributes with the Yharnam Stone")),null,"rebirth");
            foreach (var effect in Choices.Effects.Where(effect => effect.Key is "skip_intro" or "debug_camera"))
                IniToggle(effect.Key == "skip_intro" ? SymbolRegular.FastForward24 : SymbolRegular.Camera24,
                    BloodborneLauncher.Core.UiText.T(effect.Title), BloodborneLauncher.Core.UiText.T(effect.Hint), effect.Key);
            Group("").Visibility = Visibility.Collapsed;
            var cameraOptions = new OptionRows();
            void UpdateCameraOptions()
            {
                cameraOptions.IsEnabled = Ini.Flag("camera_controls");
                cameraOptions.Opacity = cameraOptions.IsEnabled ? 1 : 0.45;
            }
            IniToggle(SymbolRegular.Camera24, BloodborneLauncher.Core.UiText.T("Camera tuning"), BloodborneLauncher.Core.UiText.T("Turns the three settings below on"), "camera_controls", changed: UpdateCameraOptions);
            IniSlider(SymbolRegular.ZoomOut24, BloodborneLauncher.Core.UiText.T("Field of view"), 1, 1.5, "camera_fov_scale", cameraOptions);
            IniSlider(SymbolRegular.ArrowMaximize24, BloodborneLauncher.Core.UiText.T("Distance"), 0.5, 1.5, "camera_distance_scale", cameraOptions);
            IniSlider(SymbolRegular.ArrowBidirectionalUpDown24, BloodborneLauncher.Core.UiText.T("Height"), 0.5, 1.5, "camera_height_scale", cameraOptions);
            Body.Children.Add(cameraOptions);
            UpdateCameraOptions();

            // Engine features from 0.9.4, each with a switch, as IFreemz's "Bloodborne - Experimental".
            Group(BloodborneLauncher.Core.UiText.T("Cheats"), _cheatsPanel);
            CheatAccess.Session.Restore(S.Bool("cheats_unlocked"));
            _cheatsPanel.Visibility = CheatAccess.Session.Unlocked ? Visibility.Visible : Visibility.Collapsed;
            Body.Children.Add(_cheatsPanel);
            Loaded += (_, _) =>
            {
                DetachSecretInput(); _inputWindow = Window.GetWindow(this);
                if (_inputWindow != null) _inputWindow.PreviewTextInput += SecretInput;
            };
            Unloaded += (_, _) => DetachSecretInput();
            ReleaseWhenDisposed(DetachSecretInput);
            _engine = new InfoBar
            {
                IsClosable = false,
                Severity = InfoBarSeverity.Warning,
                Margin = new Thickness(0, 14, 0, 0),
                Title = BloodborneLauncher.Core.UiText.T("Not in this engine yet"),
                Message = BloodborneLauncher.Core.UiText.T("Your engine doesn't apply cheats yet: they arrive with the next engine update. What you pick here is saved and starts working then."),
            };
            var cheats = new OptionRows();
            cheats.Children.Add(_engine);
            _cheatsPanel.Children.Add(new MenuSurface {
                Child = cheats, Padding = new Thickness(16, 14, 16, 16), Margin = new Thickness(-8, 0, -8, 10)
            });

            Group(BloodborneLauncher.Core.UiText.T("Items"), cheats).Margin = new Thickness(0, 0, 0, 8);
            Cheat(SymbolRegular.Drop24, "Blood Vials", "cheat_vials", "cheat_vials_amount", cheats);
            Cheat(SymbolRegular.Flash24, "Quicksilver Bullets", "cheat_bullets", "cheat_bullets_amount", cheats);
            IniToggle(SymbolRegular.Beaker24, BloodborneLauncher.Core.UiText.T("Infinite consumables"),
                BloodborneLauncher.Core.UiText.T("Molotovs, throwing knives, pebbles, antidotes, papers and the like are not used up (not Blood Vials or Bullets, which are above, and not insight items)"),
                "cheat_consumables", cheats);

            Group(BloodborneLauncher.Core.UiText.T("Character"), cheats);
            IniToggle(SymbolRegular.Run24, BloodborneLauncher.Core.UiText.T("Infinite stamina"), BloodborneLauncher.Core.UiText.T("Your stamina stays full"), "cheat_stamina", cheats);
            IniToggle(SymbolRegular.EyeOff24, BloodborneLauncher.Core.UiText.T("Stealth"), BloodborneLauncher.Core.UiText.T("Enemies don't notice you and your steps make no noise"), "cheat_stealth", cheats);
            IniToggle(SymbolRegular.Shield24, BloodborneLauncher.Core.UiText.T("No weapon wear"), BloodborneLauncher.Core.UiText.T("Equipped weapons are kept at full durability, so no repairs"), "cheat_no_durability", cheats);
            ComboRow(SymbolRegular.Target24, BloodborneLauncher.Core.UiText.T("Firearm range"), BloodborneLauncher.Core.UiText.T("How far firearm bullets travel (pistols, rifles, blunderbuss, cannons)"),
                new[] { new Choice("Normal", "off"), new Choice("150%", "150"), new Choice("200%", "200"), new Choice("300%", "300") },
                () => Ini.Get("cheat_gun_range", "off"), v => Ini.Set("cheat_gun_range", v), cheats);

            Group(BloodborneLauncher.Core.UiText.T("Blood Echoes"), cheats);
            IniToggle(SymbolRegular.Heart24, BloodborneLauncher.Core.UiText.T("Keep echoes on death"), BloodborneLauncher.Core.UiText.T("Dying never drops your Blood Echoes, so there is nothing to pick back up"), "cheat_keep_echoes", cheats);
            ComboRow(SymbolRegular.ArrowTrendingLines24, BloodborneLauncher.Core.UiText.T("Echoes from kills"), BloodborneLauncher.Core.UiText.T("Enemies already loaded give the new amount after you rest at a lamp or reload"),
                new[] { new Choice("Normal", "1"), new Choice("x2", "2"), new Choice("x5", "5"), new Choice("x10", "10") },
                () => Ini.Get("cheat_echo_multiplier", "1"), v => Ini.Set("cheat_echo_multiplier", v), cheats);
            Add(Card(SymbolRegular.Add24, BloodborneLauncher.Core.UiText.T("Add echoes or insight"), BloodborneLauncher.Core.UiText.T("In the in-game menu (Insert or L3 + R3): +1,000, +10,000, +100,000 echoes and +1 insight"), null, out _), cheats);
        }

        /// <summary>A mode row and its amount slider for one cheat key.</summary>
        private void Cheat(SymbolRegular icon, string title, string key, string amountKey, Panel into)
        {
            Slider amount = null!;
            string Mode() => Ini.Get(key, "off") switch
            {
                "infinite" => "infinite",
                var v when int.TryParse(v, out var n) && n > 0 => "custom",
                _ => "off",
            };
            int Amount() => Math.Clamp(int.TryParse(Ini.Get(key, ""), out var n) && n > 0 ? n : S.Int(amountKey), 1, 99);

            ComboRow(icon, BloodborneLauncher.Core.UiText.T(title), null, Modes, Mode, v =>
            {
                Ini.Set(key, v == "custom" ? Amount().ToString(CultureInfo.InvariantCulture) : v);
                ShowAmount(amount, v == "custom");
                Ws.Log($"{title}: {(v == "custom" ? BloodborneLauncher.Core.UiText.F($"kept at {Amount()}") : v)}.", LogLevel.Step);
            }, into);
            amount = SliderRow(SymbolRegular.NumberSymbol24, BloodborneLauncher.Core.UiText.T("Amount"), BloodborneLauncher.Core.UiText.F($"How many {title} to keep"), 1, 99, 1, () => Amount(), v =>
            {
                S.Set(amountKey, (int)v);
                if (Mode() == "custom") Ini.Set(key, ((int)v).ToString(CultureInfo.InvariantCulture));
            }, "0", into);
            ShowAmount(amount, Mode() == "custom");
        }

        private void IniSlider(SymbolRegular icon, string title, double min, double max, string key, Panel? into = null) =>
            SliderRow(icon, title, null, min, max, 0.05,
                () => ParseDouble(Ini.Get(key, "1"), 1), v => Ini.Set(key, Fmt(v)), into: into);

        /// <summary>The amount row only shows for Custom amount.</summary>
        private static void ShowAmount(Slider amount, bool show)
        {
            DependencyObject? at = amount;
            while (at != null && at is not SettingRow) at = LogicalTreeHelper.GetParent(at);
            ((UIElement?)at ?? amount).Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        /// <summary>The package's engine applies cheats (newer than the builds listed above).</summary>
        public static bool Supported => (Updater.LocalEngine() is { } engine && !EnginesWithoutCheats.Contains(engine)) || System.IO.File.Exists(System.IO.Path.Combine(Paths.PortDir,"out","better-kbm-build.json"));

        public override void OnShown() => _engine.IsOpen = !Supported;
    }
}
