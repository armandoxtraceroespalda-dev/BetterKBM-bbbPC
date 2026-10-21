using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using BloodborneLauncher.Views;
using BloodborneLauncher.Core;

internal static class Program
{
    [STAThread]
    private static void Main()
    {
        var application = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        foreach(var theme in new[]{"LauncherControls","Styles","Bloodborne"})
            application.Resources.MergedDictionaries.Add(new ResourceDictionary {
                Source=new Uri("/Bloodborne Launcher;component/Themes/"+theme+".xaml",UriKind.Relative)
            });
        Require(typeof(BloodborneLauncher.MainWindow).BaseType==typeof(Window),"The launcher must use the native Window, without a UI-framework shell.");
        Require(!typeof(BloodborneLauncher.MainWindow).Assembly.GetReferencedAssemblies().Any(a=>a.Name=="Wpf.Ui"),"The external UI framework must not return through an assembly dependency.");
        CheckScrollBars();
        CheckTransitions();
        CheckAudioImport();
        CheckSearchViewport();
        CheckCheatsHierarchy();
        CheckControllerLabels();
        Require(typeof(MenuAssets).GetProperty("Icon")==null,
            "Local game artwork must not be exposed as a launcher window icon.");
        var launcherIcon = new System.Windows.Media.Imaging.BitmapImage(
            new Uri("pack://application:,,,/Bloodborne Launcher;component/app.ico"));
        Require(launcherIcon.PixelWidth>0,"The launcher must retain its separately compiled icon resource.");
        var minimal=new MinimalPage();minimal.PrepareForDisplay();
        Require(minimal.NoShell,"Start must not construct or retain an unused settings shell.");minimal.Dispose();
        var page = new ProbePage();
        Require(page.Probe.Parent is StackPanel, "Construction starts in the source body.");
        page.PrepareForDisplay();
        var finalParent = page.Probe.Parent;
        Require(finalParent is StackPanel && !ReferenceEquals(finalParent, page.SourceBody),
            "Sections must be built while the page is still detached, before template expansion.");
        page.PrepareForDisplay();
        Require(ReferenceEquals(page.Probe.Parent, finalParent), "Preparing twice must not reparent rows again.");
        var cell = new MenuBindingCell("Strong attack", new[] { ("Left Shift + Mouse1", "Shift + Mouse1", true, (string?)null) }, false,
            () => { }, () => { }, _ => { });
        cell.Measure(new Size(240, double.PositiveInfinity)); cell.Arrange(new Rect(0, 0, 240, cell.DesiredSize.Height));
        cell.ApplyTemplate();
        Require(VisualTreeHelper.GetChildrenCount(cell) == 0 && cell.Template == null,
            "A binding cell must not expand a panel/button/icon tree.");
        Require(System.Windows.Automation.Peers.UIElementAutomationPeer.CreatePeerForElement(cell) is
            System.Windows.Automation.Peers.ButtonAutomationPeer, "The drawn cell must retain native Button automation.");
        var soundType = typeof(BloodborneLauncher.MainWindow).Assembly.GetType("BloodborneLauncher.Core.MenuSounds")!;
        var clickCue = (DependencyProperty)soundType.GetField("ClickCueProperty")!.GetValue(null)!;
        Require((MenuSoundCue)cell.GetValue(clickCue) == MenuSoundCue.Change,
            "Binding activation must use the option/slider cue, not the sidebar cue.");
        var innerTabs = new Segmented(new[] { new Choice("Mods", "mods"), new Choice("Log", "output") });
        foreach (var item in LogicalTreeHelper.GetChildren(innerTabs).OfType<Grid>().SelectMany(grid => grid.Children.OfType<System.Windows.Controls.Button>()))
            Require((MenuSoundCue)item.GetValue(clickCue) == MenuSoundCue.Change, "Nested More actions must use the option cue.");
        var shell = (UIElement)page.Content;
        var scrollerParent = LogicalTreeHelper.GetParent(page.Scroll);
        var overlay = new Grid { Visibility = Visibility.Collapsed };
        page.AddDialog(overlay);
        var root = (Grid)page.Content;
        Require(root.Children.Count == 2 && ReferenceEquals(root.Children[0], shell),
            "The complete decorated settings shell must remain below the capture dialog.");
        Require(ReferenceEquals(root.Children[1], overlay), "The capture dialog must be the top layer.");
        Require(ReferenceEquals(LogicalTreeHelper.GetParent(page.Scroll), scrollerParent),
            "Adding a dialog must not reparent the already-owned scroller.");
        Require(Count(page, page.Scroll) == 1, "The scroller must have one logical owner.");
        Require(Count(page, overlay) == 1, "The overlay must have one logical owner.");
        Console.WriteLine("PASS: decorated shell retained, capture overlay layered above it, scroller ownership unchanged.");
        Require(page is UserControl, "Settings must be direct hosted views, without navigation journals.");
        page.Dispose();page.Dispose();
        Require(page.Content==null && page.Scroll.Content==null,"Discarding a view must detach its rendered tree.");
        application.Shutdown();
    }

    // Run this fixture on Windows: it measures the real WPF text viewport and caret, not a copy
    // of the layout formula. UI29's doubled padding leaves the typed line outside this viewport.
    private static void CheckScrollBars()
    {
        var bar=new System.Windows.Controls.Primitives.ScrollBar {
            Orientation=Orientation.Vertical,Minimum=0,Maximum=100,ViewportSize=10,Value=25
        };
        var host=new Window {Width=300,Height=240,Content=bar,ShowInTaskbar=false};
        host.Show();host.UpdateLayout();bar.ApplyTemplate();
        foreach(var orientation in new[]{Orientation.Vertical,Orientation.Horizontal})
        {
            bar.Orientation=orientation;host.UpdateLayout();bar.ApplyTemplate();
            var track=(System.Windows.Controls.Primitives.Track)bar.Template.FindName("PART_Track",bar);
            Require(track!=null && track.Orientation==orientation,"Both scrollbar orientations must keep a native PART_Track.");
            Require(track!.IsDirectionReversed==(orientation==Orientation.Vertical),"Scroll direction must remain correct.");
            Require(track.DecreaseRepeatButton.Command==(orientation==Orientation.Vertical ?
                System.Windows.Controls.Primitives.ScrollBar.PageUpCommand:System.Windows.Controls.Primitives.ScrollBar.PageLeftCommand),
                "Paging commands must follow scrollbar orientation.");
            Require(track.Thumb.Style==Application.Current.FindResource("DarkScrollThumb"),"The thumb cannot use white system chrome.");
            var color=((SolidColorBrush)track.Thumb.Background).Color;
            Require(color.R<128 && color.G<128 && color.B<128,"The normal thumb must remain dark grey.");
            bar.Value=40;host.UpdateLayout();Require(track.Value==40,"Programmatic scrolling must update the thumb.");
            track.Value=60;host.UpdateLayout();Require(bar.Value==60,"The thumb must update the scrollbar in both directions.");
        }
        foreach(var key in new[]{"ScrollThumbHoverBrush","ScrollThumbDragBrush","ScrollArrowBrush"})
        {
            var color=((SolidColorBrush)Application.Current.FindResource(key)).Color;
            Require(color.R<160 && color.G<160 && color.B<160,"Hover/drag/arrows must remain grey: "+key);
        }
        host.Close();
        Console.WriteLine("PASS: native vertical/horizontal track, direction, paging, two-way values and dark normal/hover/drag palette.");
    }

    private static void CheckAudioImport()
    {
        using var wave=new MemoryStream();using(var writer=new BinaryWriter(wave,System.Text.Encoding.UTF8,true))
        {
            writer.Write("RIFF"u8);writer.Write(88200+36);writer.Write("WAVEfmt "u8);writer.Write(16);
            writer.Write((ushort)1);writer.Write((ushort)1);writer.Write(44100);writer.Write(88200);
            writer.Write((ushort)2);writer.Write((ushort)16);writer.Write("data"u8);writer.Write(88200);writer.Write(new byte[88200]);
        }
        var type=typeof(MenuAssets).Assembly.GetType("BloodborneLauncher.Core.WaveLoadStream")!;
        using var stream=(Stream)Activator.CreateInstance(type,System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic,
            null,new object[]{wave.ToArray()},null)!;
        using var player=new System.Media.SoundPlayer(stream);player.Load();
        type.GetMethod("ReleaseSamples",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.Invoke(stream,null);
        Require(!stream.CanRead,"The import stream must release its redundant PCM array.");
        player.Load();Require(player.IsLoadCompleted,"Actual SoundPlayer must reuse its cache after import storage is released.");
    }

    private static void CheckTransitions()
    {
        var viewport=new ContentControl();var references=new List<WeakReference<object>>();
        for(int index=0;index<100;index++)references.Add(InterruptTransition(viewport));
        var frame=new System.Windows.Threading.DispatcherFrame();
        System.Windows.Threading.Dispatcher.CurrentDispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.ApplicationIdle,
            new Action(()=>frame.Continue=false));
        System.Windows.Threading.Dispatcher.PushFrame(frame);
        GC.Collect();GC.WaitForPendingFinalizers();GC.Collect();
        Require(!DependencyPropertyHelper.GetValueSource(viewport,UIElement.OpacityProperty).IsAnimated &&
                viewport.RenderTransform is TranslateTransform {HasAnimatedProperties:false},
            "Reset must remove both transition clocks.");
        Require(references.All(reference=>!reference.TryGetTarget(out _)),
            "Interrupted transition callbacks must not retain their captured pages.");
    }
    [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
    private static WeakReference<object> InterruptTransition(FrameworkElement viewport)
    {
        var token=new object();MenuTransition.Enter(viewport,true,()=>GC.KeepAlive(token));
        MenuTransition.Reset(viewport);return new(token);
    }

    private static void CheckSearchViewport()
    {
        var search = new BloodborneLauncher.Controls.TextBox {
            PlaceholderText="Filter actions or keys", Width=240, Height=38,
            FontFamily=new FontFamily("Segoe UI"), FontSize=17,
            VerticalContentAlignment=VerticalAlignment.Center
        };
        var window=new Window { Content=search, Width=340, Height=160, ShowInTaskbar=false };
        window.Show(); search.ApplyTemplate(); window.UpdateLayout();
        var host=(ScrollViewer)search.Template.FindName("PART_ContentHost",search);
        var placeholder=(TextBlock)search.Template.FindName("Placeholder",search);
        Require(host.VerticalAlignment==VerticalAlignment.Stretch && host.Padding==search.Padding,
            "The editor must occupy the available viewport with exactly one content padding.");
        Require(placeholder.IsVisible,"An empty search must show its placeholder.");
        foreach(var text in new[]{"W","Parry","Movimiento","Shift + Mouse1",new string('W',120)}) {
            search.Text=text; search.CaretIndex=text.Length; search.ScrollToEnd(); window.UpdateLayout();
            var rect=search.GetRectFromCharacterIndex(text.Length,true);
            Require(!rect.IsEmpty && rect.Top>=0 && rect.Bottom<=search.ActualHeight && rect.Height>0,
                "Typed text and the caret must remain inside the search field: "+text);
            Require(!placeholder.IsVisible,"The placeholder must disappear when typing.");
        }
        search.Clear(); window.UpdateLayout();
        Require(placeholder.IsVisible,"Clearing the filter must restore the placeholder.");
        search.AcceptsReturn=true; search.Height=110; search.Text="First line\nSecond line";
        window.UpdateLayout();
        Require(search.VerticalContentAlignment==VerticalAlignment.Top,
            "The shared editor template must preserve multiline top alignment.");
        window.Close();
        Console.WriteLine("PASS: real search viewport, typed-line/caret bounds, long text, placeholder and multiline editor.");
    }

    private static void CheckCheatsHierarchy()
    {
        UiText.UseTextLanguage("1");
        var page=new CheatsPage(); page.PrepareForDisplay();
        var body=(StackPanel)typeof(LauncherPage).GetProperty("Body",
            System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic)!.GetValue(page)!;
        Require(body.Children.OfType<TextBlock>().Select(t=>t.Text).SequenceEqual(new[]{"Gameplay Tweaks","Cheats"}),
            "Only Gameplay Tweaks and Cheats are top-level sections.");
        var surfaces=body.Children.OfType<MenuSurface>().ToArray();
        Require(surfaces.Length==2,"A hidden warning must not create an empty standalone shadow.");
        var cheats=(StackPanel)surfaces[1].Child;
        Require(cheats.Children.OfType<TextBlock>().Select(t=>t.Text).SequenceEqual(new[]{"Items","Character","Blood Echoes"}),
            "All three categories must be children of Cheats.");
        Require(cheats.Children.OfType<SettingRow>().Count()==12,
            "All cheat controls, including amount rows, must remain inside the Cheats surface.");
        cheats.Children.OfType<BloodborneLauncher.Controls.InfoBar>().Single().IsOpen=false;
        Require(cheats.Children.OfType<SettingRow>().Any(row=>row.Visibility==Visibility.Visible),
            "Hiding an unsupported-engine warning must leave actual controls in the surface.");
        page.Dispose();
    }

    private static void CheckControllerLabels()
    {
        foreach(var id in new[]{"3","20"}) {
            UiText.UseTextLanguage(id);
            Require(PadMap.DisplayLabel("leftstick")=="L3 (pulsar la palanca izquierda)" &&
                    PadMap.DisplayLabel("rightstick")=="R3 (pulsar la palanca derecha)",
                "Both Spanish variants must describe a stick press.");
            Require(PadMap.DisplayLabel("x")=="Cuadrado / X" && PadMap.DisplayLabel("a")=="Cruz / A",
                "Square and Cross must remain distinct.");
            Require(PadMap.GameLabel("Options")=="Options" && PadMap.GameLabel("Square")=="Cuadrado",
                "Controller tooltip labels need the same context as the controller column.");
            Require(PadMap.DisplayLabel("start")=="Options / Start" && PadMap.DisplayLabel("back")=="Share / Back",
                "Branded controller buttons must not be translated as generic UI commands.");
            Require(PadMap.DisplayLabel("dpup")=="D-pad arriba" && PadMap.DisplayLabel("Unknown device input")=="Unknown device input",
                "D-pad directions are localized; unknown device identifiers remain literal.");
        }
        UiText.UseTextLanguage("1");
    }

    private static int Count(DependencyObject node, DependencyObject sought)
    {
        int count = ReferenceEquals(node, sought) ? 1 : 0;
        foreach (var child in LogicalTreeHelper.GetChildren(node).OfType<DependencyObject>())
            count += Count(child, sought);
        return count;
    }
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    private sealed class MinimalPage:LauncherPage {
        public MinimalPage():base("Start","",settingsShell:false){}
        public bool NoShell=>Body==null&&Scroller==null&&Content==null;
    }
    private sealed class ProbePage : LauncherPage
    {
        public System.Windows.Controls.Button Probe { get; } = new() { Content = "Probe" };
        public StackPanel SourceBody => Body;
        public ProbePage() : base("Controls", "") { Group("Probe"); Body.Children.Add(Probe); }
        public ScrollViewer Scroll => Scroller;
        public void AddDialog(UIElement overlay) => AttachOverlay(overlay);
    }
}
