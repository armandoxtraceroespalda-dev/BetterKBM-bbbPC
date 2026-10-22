using System.Windows;
using System.Windows.Controls;
using BloodborneLauncher.Core;
using BloodborneLauncher.Controls;
using Button = BloodborneLauncher.Controls.Button;
using PasswordBox = System.Windows.Controls.PasswordBox;

namespace BloodborneLauncher.Views
{
    /// <summary>
    /// Online play with The Hunter's Dream. The engine reads these from bbport.ini (online_*,
    /// scripts/online.py); only the password stays out of it and is passed when the game starts.
    /// </summary>
    public sealed class OnlinePage : LauncherPage
    {
        private readonly ToggleSwitch _online;
        private readonly StackPanel _results = new OptionRows();
        private readonly Button _test;

        /// <summary>Where shadNet accounts are made (the public server's).</summary>
        private const string ShadnetSignUp = "https://shadnet.shadps4.net";

        public OnlinePage() : base(BloodborneLauncher.Core.UiText.T("Online"),
            BloodborneLauncher.Core.UiText.Format("1. Turn on Play online. 2. Enter your account below. 3. In the game, choose online at the title screen: The Hunter\u0027s Dream\u0027s welcome message means you are online. Everyone needs game version 1.09."))
        {
            // One server now: the bbbridge pool and custom servers are gone (scripts/online.py defaults).
            foreach (var key in new[] { "online_host", "online_port", "online_webapi", "online_game_api", "online_thd_account" })
                if (Ini.Get(key).Length > 0) Ini.Set(key, "");
            // With a Hunter's Dream account: its own co-op (the engine's thd backend, with bbhost players).
            Ini.Set("online_pool", S.HuntersDreamToken.Length > 0 && S.Str("hd_name").Length > 0 ? "huntersdream" : "");

            if (HuntersDream.OnlinePaused)
                Body.Children.Add(Text(BloodborneLauncher.Core.UiText.Format("Online play is paused in this version. The game starts offline whatever is set here; your settings are kept for when it returns."), "Faint"));

            Group(BloodborneLauncher.Core.UiText.T("Play"));
            _online = ToggleRow(SymbolRegular.Globe24, BloodborneLauncher.Core.UiText.T("Play online"), BloodborneLauncher.Core.UiText.T("Off: the game stays offline"),
                () => Ini.Get("online_enabled") == "1", v => Ini.Set("online_enabled", v ? "1" : "0"));

            Group(BloodborneLauncher.Core.UiText.T("Server"));
            ComboRow(SymbolRegular.Server24, BloodborneLauncher.Core.UiText.T("Server"), BloodborneLauncher.Core.UiText.T("Messages, bloodstains, ghosts, co-op and invasions"),
                Choices.OnlineServers, () => "huntersdream", _ => { });
            Add(Card(SymbolRegular.Globe24, BloodborneLauncher.Core.UiText.T("The Hunter's Dream"), BloodborneLauncher.Core.UiText.T("Players online, the map and your hunter's profile"),
                SmallButton(BloodborneLauncher.Core.UiText.T("Open website"), SymbolRegular.Open24, (_, _) =>
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(LauncherSettings.OnlineCommunity) { UseShellExecute = true })),
                out _));

            Group(BloodborneLauncher.Core.UiText.T("The Hunter's Dream account"));
            _hdButton = SmallButton(BloodborneLauncher.Core.UiText.T("Connect"), SymbolRegular.Person24, async (_, _) => await LinkAsync());
            Add(Card(SymbolRegular.PersonAccounts24, BloodborneLauncher.Core.UiText.T("Hunter's Dream account"), null, _hdButton, out _hdHint));
            ShowLink();

            Group(BloodborneLauncher.Core.UiText.T("shadNet account"));
            Body.Children.Add(Text(BloodborneLauncher.Core.UiText.Format("Enter the account name you registered (up to 16 characters), not an email address. The password is kept encrypted for your Windows user, not in bbport.ini."), "Faint"));
            Add(Card(SymbolRegular.PersonAdd24, BloodborneLauncher.Core.UiText.T("No account yet?"), BloodborneLauncher.Core.UiText.T("Register one, then enter it below"),
                SmallButton(BloodborneLauncher.Core.UiText.T("Create account"), SymbolRegular.Open24, (_, _) =>
                    System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(ShadnetSignUp) { UseShellExecute = true })),
                out _));
            IniText(SymbolRegular.Person24, "Account name", null, "Account name", "online_username");
            var password = new PasswordBox
            {
                Width = 300,
                Password = S.OnlinePassword,
                VerticalAlignment = VerticalAlignment.Center,
            };
            password.PasswordChanged += (_, _) =>
            {
                S.OnlinePassword = password.Password;
                Ws.SaveSoon();
            };
            Add(Card(SymbolRegular.Password24, BloodborneLauncher.Core.UiText.T("Password"), null, password, out _));

            SettingsColumns.SetTwoColumns(Group(UiText.T("Connection")), true);
            ToggleRow(SymbolRegular.Router24, BloodborneLauncher.Core.UiText.T("UPnP"), BloodborneLauncher.Core.UiText.T("Open the port on the router automatically; turn off with a VPN"),
                () => Ini.Get("online_upnp", "1") == "1", v => Ini.Set("online_upnp", v ? "1" : "0"));
            IniText(SymbolRegular.Router24, "Local P2P port", "Empty: 3658, the PS4's (UPnP opens it on the router)", "3658", "online_p2p_port", 120);
            ComboRow(SymbolRegular.Timer24, BloodborneLauncher.Core.UiText.T("Summon sign timeout"), BloodborneLauncher.Core.UiText.T("How long a sign you touched waits for its host before giving up"),
                Choices.SignTimeout, () => Ini.Get("online_sign_timeout", "30"), v => Ini.Set("online_sign_timeout", v));
            _test = SmallButton(BloodborneLauncher.Core.UiText.T("Test"), SymbolRegular.PlayCircle24, async (_, _) => await TestAsync());
            Add(Card(SymbolRegular.Pulse24, BloodborneLauncher.Core.UiText.T("Test the connection"), BloodborneLauncher.Core.UiText.T("Checks The Hunter's Dream and the online servers"), _test, out _));
            Body.Children.Add(_results);
        }

        private readonly Button _hdButton;
        private readonly System.Windows.Controls.TextBlock _hdHint;
        private CancellationTokenSource? _linking;

        private void ShowLink()
        {
            var name = S.Str("hd_name");
            var linked = name.Length > 0 && S.HuntersDreamToken.Length > 0;
            SetHint(_hdHint, linked ? BloodborneLauncher.Core.UiText.F($"Connected as {name}") : BloodborneLauncher.Core.UiText.T("Not connected. Connect with Discord, or create an account here without it"));
            if (_linking is null) _hdButton.Content = linked ? BloodborneLauncher.Core.UiText.T("Disconnect") : BloodborneLauncher.Core.UiText.T("Connect");
        }

        /// <summary>The device link: the code goes to the clipboard and the website opens; pressing the
        /// button again while it waits cancels. Connected, the button disconnects.</summary>
        /// <summary>The Hunter's Dream account without Discord (#37): created with a name, or recovered
        /// with the name and its recovery code. A new recovery code is shown once.</summary>
        private async Task AccountWithoutDiscordAsync(bool recover)
        {
            var fields = recover
                ? new[] { new Prompt.Field(BloodborneLauncher.Core.UiText.T("Account name")), new Prompt.Field(BloodborneLauncher.Core.UiText.T("Recovery code")) }
                : new[] { new Prompt.Field(BloodborneLauncher.Core.UiText.T("Account name"), "", BloodborneLauncher.Core.UiText.T("Your online name (letters, digits, - and _)")) };
            var answer = Prompt.Ask(recover ? BloodborneLauncher.Core.UiText.T("Recover the account") : BloodborneLauncher.Core.UiText.T("Create an account"),
                recover ? BloodborneLauncher.Core.UiText.T("The name and the recovery code you wrote down. Other PCs signed in to this account are signed out.")
                        : BloodborneLauncher.Core.UiText.T("An account held by this PC. You get a recovery code: write it down, it is the only way back in on another PC or after signing out."),
                fields);
            if (answer is null || answer[0].Trim().Length == 0) return;
            try
            {
                var (name, token, code) = recover
                    ? await HuntersDream.RecoverAsync(answer[0].Trim(), answer[1].Trim(), CancellationToken.None)
                    : await HuntersDream.CreateAccountAsync(answer[0].Trim(), CancellationToken.None);
                S.HuntersDreamToken = token;
                S.Set("hd_name", name);
                Ini.Set("online_pool", "huntersdream");
                Ws.SaveNow();
                Ws.Log(BloodborneLauncher.Core.UiText.F($"The Hunter's Dream: signed in as {name}"), LogLevel.Good);
                if (code.Length > 0)
                {
                    try { Clipboard.SetText(code); } catch (Exception) { }
                    Prompt.Tell(BloodborneLauncher.Core.UiText.T("Your recovery code"), BloodborneLauncher.Core.UiText.Format("{0}Write it down now: it is shown only once, and it is the only way back into this account.",BloodborneLauncher.Core.UiText.F($"Signed in as {name}.\n\nRecovery code (copied to the clipboard):\n\n{code}\n\n")));
                }
            }
            catch (Exception e)
            {
                Prompt.Tell(recover ? BloodborneLauncher.Core.UiText.T("Recovering failed") : BloodborneLauncher.Core.UiText.T("Creating the account failed"), BloodborneLauncher.Core.UiText.T(e.Message));
            }
            finally
            {
                ShowLink();
            }
        }

        private async Task LinkAsync()
        {
            if (_linking is { } running)
            {
                running.Cancel();
                return;
            }
            if (S.HuntersDreamToken is { Length: > 0 } token)
            {
                await HuntersDream.LogoutAsync(token);
                S.HuntersDreamToken = "";
                S.Set("hd_name", "");
                Ini.Set("online_pool", "");
                Ws.SaveNow();
                Ws.Log(BloodborneLauncher.Core.UiText.T("The Hunter's Dream: disconnected"), LogLevel.Info);
                ShowLink();
                return;
            }
            var how = Prompt.Choose(BloodborneLauncher.Core.UiText.T("The Hunter's Dream account"),
                BloodborneLauncher.Core.UiText.T("Sign in with Discord, or use an account held by this PC (a name and a recovery code, no Discord needed)."),
                BloodborneLauncher.Core.UiText.T("Cancel"), BloodborneLauncher.Core.UiText.T("Discord"), "Without Discord…", "Recover with a code…");
            if (how <= 0) return;
            if (how >= 2)
            {
                await AccountWithoutDiscordAsync(recover: how == 3);
                return;
            }
            _linking = new CancellationTokenSource();
            _hdButton.Content = BloodborneLauncher.Core.UiText.T("Cancel");
            try
            {
                var link = await HuntersDream.StartLinkAsync(_linking.Token);
                try { Clipboard.SetText(link.UserCode); } catch (Exception) { }
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(link.Page) { UseShellExecute = true });
                SetHint(_hdHint, BloodborneLauncher.Core.UiText.F($"Enter the code {link.UserCode} on the page that opened (it is on the clipboard), then approve"));
                var (name, linked) = await HuntersDream.WaitLinkAsync(link, _linking.Token);
                S.HuntersDreamToken = linked;
                S.Set("hd_name", name);
                Ini.Set("online_pool", "huntersdream");
                Ws.SaveNow();
                Ws.Log(BloodborneLauncher.Core.UiText.F($"The Hunter's Dream: connected as {name}"), LogLevel.Good);
            }
            catch (OperationCanceledException) { }
            catch (Exception e)
            {
                Prompt.Tell(BloodborneLauncher.Core.UiText.T("Connecting failed"), BloodborneLauncher.Core.UiText.T(e.Message));
            }
            finally
            {
                _linking.Dispose();
                _linking = null;
                ShowLink();
            }
        }

        private void IniText(SymbolRegular icon, string title, string? hint, string placeholder, string key, double width = 300) =>
            TextRow(icon, BloodborneLauncher.Core.UiText.T(title), BloodborneLauncher.Core.UiText.T(hint), BloodborneLauncher.Core.UiText.T(placeholder), () => Ini.Get(key), v => Ini.Set(key, v.Trim()), width: width);

        public override void OnShown() => _online.IsChecked = Ini.Get("online_enabled") == "1";

        private async Task TestAsync()
        {


            _results.Children.Clear();
            _test.IsEnabled = false;
            _test.Content = BloodborneLauncher.Core.UiText.T("Testing…");
            try
            {
                var results = await Online.CheckAsync(Online.GameServer(Ini.Get("online_game_api")));
                foreach (var r in results)
                {
                    var said = r.Ok ? (r.Detail.Length > 0 ? BloodborneLauncher.Core.UiText.F($"Reachable: {r.Detail}") : "Reachable") : BloodborneLauncher.Core.UiText.F($"Not reachable ({r.Detail})");
                    var card = Card(r.Ok ? SymbolRegular.CheckmarkCircle24 : SymbolRegular.ErrorCircle24,
                        $"{r.Name}: {r.Address}", BloodborneLauncher.Core.UiText.T(said), null, out _);
                    card.ShowIcon = true;
                    card.Icon.SetResourceReference(ForegroundProperty, r.Ok ? "GoodTextBrush" : "DangerTextBrush");
                    SetHint(card.Hint, BloodborneLauncher.Core.UiText.T(said));
                    card.Margin = new Thickness(24, 0, 0, 4);
                    _results.Children.Add(card);
                    Ws.Log($"{r.Name} {r.Address}: {(r.Ok ? "reachable" : "not reachable " + r.Detail)}", r.Ok ? LogLevel.Good : LogLevel.Warn);
                }
            }
            finally
            {
                _test.IsEnabled = true;
                _test.Content = BloodborneLauncher.Core.UiText.T("Test");
            }
        }
    }
}
