using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using BloodborneLauncher.Core;
using Button=System.Windows.Controls.Button;
namespace BloodborneLauncher.Views;

/// <summary>Title action, current output settings and game identity on the local artwork.</summary>
public sealed class MenuHomePage : LauncherPage
{
    private readonly Button _play;
    private readonly List<TextBlock> _statLabels = new(), _statValues = new();
    private readonly MenuSurface _overview, _versionSurface;
    private readonly TextBlock _sessionText;
    private readonly DispatcherTimer _sessionClock = new() { Interval = TimeSpan.FromSeconds(1) };
    private double _infoScale = double.NaN;
    private readonly TextBlock _upscaler,_resolution,_fps,_achievements,_gameVersion;
    public MenuHomePage():base(BloodborneLauncher.Core.UiText.T("Start"),"",settingsShell:false)
    {
        var grid=new Grid { Margin=new Thickness(24) };
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width=new GridLength(3,GridUnitType.Star) });
        grid.ColumnDefinitions.Add(new ColumnDefinition { Width=new GridLength(2,GridUnitType.Star) });
        grid.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition());
        grid.RowDefinitions.Add(new RowDefinition { Height=GridLength.Auto });
        // Content-sized fields form one compact row in reading order.
        var stats=new StackPanel { Orientation=Orientation.Horizontal };
        stats.Children.Add(Stat("Upscaler",out _upscaler));stats.Children.Add(Stat("Output",out _resolution));
        stats.Children.Add(Stat("Frame rate",out _fps));stats.Children.Add(Stat("Achievements",out _achievements));
        var statsScale=new Viewbox { Stretch=Stretch.Uniform,StretchDirection=StretchDirection.DownOnly,Child=stats };
        _sessionText = Text("", "Faint");
        _sessionText.TextAlignment = TextAlignment.Center;
        _sessionText.HorizontalAlignment = HorizontalAlignment.Center;
        _sessionText.TextWrapping = TextWrapping.Wrap;
        _sessionText.Margin = new Thickness(8, 10, 8, 0);
        var overviewContent = new StackPanel();
        overviewContent.Children.Add(statsScale); overviewContent.Children.Add(_sessionText);
        _overview=new MenuSurface { ShadowOpacity=0.42,Padding=new Thickness(12,12,12,12),Margin=new Thickness(0,12,0,0),HorizontalAlignment=HorizontalAlignment.Right,Child=overviewContent };
        Grid.SetColumnSpan(_overview,2);grid.Children.Add(_overview);
        var version=Stat("Game version",out _gameVersion);
        _versionSurface=new MenuSurface { ShadowOpacity=0.42,Padding=new Thickness(16,28,16,28),Width=260,
            HorizontalAlignment=HorizontalAlignment.Left,Child=version,Margin=new Thickness(0,0,0,8) };
        Grid.SetRow(_versionSurface,2);grid.Children.Add(_versionSurface);
        var actionArea=new Grid();actionArea.RowDefinitions.Add(new RowDefinition { Height=new GridLength(2,GridUnitType.Star) });
        actionArea.RowDefinitions.Add(new RowDefinition());Grid.SetRow(actionArea,1);Grid.SetRowSpan(actionArea,2);Grid.SetColumn(actionArea,1);
        _play=new Button { Content=BloodborneLauncher.Core.UiText.T("Play"),Width=280,Height=190 };
        _play.SetResourceReference(StyleProperty,"BloodbornePlayButton");
        MenuSounds.SetClickCue(_play,MenuSoundCue.Start);
        _play.Click+=async(_,_)=>await MainWindow.Instance!.PlayOrStopAsync();
        var scale=new Viewbox { Stretch=Stretch.Uniform,StretchDirection=StretchDirection.Both,Child=_play,HorizontalAlignment=HorizontalAlignment.Right,Margin=new Thickness(0,0,12,20) };
        Grid.SetRow(scale,1);actionArea.Children.Add(scale);grid.Children.Add(actionArea);Content=BloodborneLauncher.Core.UiText.T(grid);
        SizeChanged += (_,_) => ScaleInformation();
        Loaded += (_,_) => { ScaleInformation(); UpdateSessionStatus(); };
        _sessionClock.Tick += (_,_) => UpdateSessionStatus();
        Unloaded += (_,_) => _sessionClock.Stop();
        Ws.GameStateChanged += Refresh;
        ReleaseWhenDisposed(() => { Ws.GameStateChanged -= Refresh; _sessionClock.Stop(); });
        Ws.Saved+=Refresh;Ws.PortChanged+=Refresh;Ws.TrophiesChanged+=Refresh;
        ReleaseWhenDisposed(()=>{Ws.Saved-=Refresh;Ws.PortChanged-=Refresh;Ws.TrophiesChanged-=Refresh;});
    }
    private StackPanel Stat(string title,out TextBlock value)
    {
        var panel=new StackPanel { Margin=new Thickness(8,0,8,0),VerticalAlignment=VerticalAlignment.Center };
        var label=Text(BloodborneLauncher.Core.UiText.T(title),"StatLabel");label.HorizontalAlignment=HorizontalAlignment.Center;
        value=Text("—","StatValue");value.HorizontalAlignment=HorizontalAlignment.Center;
        _statLabels.Add(label);_statValues.Add(value);
        value.TextAlignment=TextAlignment.Center;panel.Children.Add(label);panel.Children.Add(value);return panel;
    }
    private void ScaleInformation()
    {
        var window = Window.GetWindow(this);
        double scale = TabMetrics.SidebarScale(window?.ActualWidth ?? 1280, window?.ActualHeight ?? 800);
        if (Math.Abs(scale - _infoScale) < 0.005) return;
        _infoScale = scale;
        foreach (var label in _statLabels) label.FontSize = 11.375 * scale;
        foreach (var value in _statValues) value.FontSize = 16.625 * scale;
        _versionSurface.Width = 260 * scale;
        _versionSurface.Padding = new Thickness(16 * scale, 28 * scale, 16 * scale, 28 * scale);
        _sessionText.FontSize = 12.25 * scale;
        _sessionText.MaxWidth = 600 * scale;
        _overview.Padding = new Thickness(12 * scale);
        _overview.Margin = new Thickness(0, 12 * scale, 0, 0);
    }
    private static string Label(IReadOnlyList<Choice> choices,string value)=>choices.FirstOrDefault(c=>c.Value==value)?.Label.Split(" (")[0]??value;
    private void Refresh()
    {
        _upscaler.Text=BloodborneLauncher.Core.UiText.T(Label(Choices.Upscalers,Ini.Get("upscaler")));
        var (w,h)=Choices.OutputSize(Ini.Get("output_res","1920x1080"));_resolution.Text=$"{w} × {h}";
        _fps.Text=BloodborneLauncher.Core.UiText.T(Label(Choices.FpsModes,GameEnvironment.Fps(S)));
        _achievements.Text=$"{Ws.Trophies.Count} / {TrophyTracker.All.Length} · {Ws.Trophies.Completion*100:0}%";
        var status=GameCheck.Inspect(GameEnvironment.GameDir(S));
        _gameVersion.Text=status.Health==Health.Bad?BloodborneLauncher.Core.UiText.T("Not found"):BloodborneLauncher.Core.UiText.T($"{status.TitleId} {status.Version}".Trim());
        SetRunning(Ws.Game.Running);
        UpdateSessionStatus();
    }
    public void UpdateSessionStatus()
    {
        if (Ws.Game.Running)
        {
            var up = DateTime.Now - Ws.Game.StartedAt;
            _sessionText.Text = UiText.Format("Running for {0}", $"{(int)up.TotalMinutes}:{up.Seconds:00}");
            if (IsLoaded && IsVisible) _sessionClock.Start(); else _sessionClock.Stop();
        }
        else
        {
            _sessionClock.Stop();
            _sessionText.Text = Ws.Session is { Length.TotalSeconds: > 10 } last
                ? UiText.Format("Last session: {0}", last.Summary) : "";
        }
        _sessionText.Visibility = string.IsNullOrEmpty(_sessionText.Text) ? Visibility.Collapsed : Visibility.Visible;
    }
    public void SetRunning(bool running)=>_play.Content=running?BloodborneLauncher.Core.UiText.T("Stop"):BloodborneLauncher.Core.UiText.T("Play");
    public override void OnShown()=>Refresh();
}
