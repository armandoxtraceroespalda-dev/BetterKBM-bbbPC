using System.IO;
using System.Media;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;

namespace BloodborneLauncher.Core;

/// <summary>Optional local PCM WAV cues; absent assets remain silent and never trigger a system beep.</summary>
internal static class MenuSounds
{
    private static readonly Dictionary<MenuSoundCue, Clip> Clips = new();
    private static MenuSoundGate Gate = new();
    internal static long ResidentWaveBytes => Clips.Values.Sum(clip=>(long)clip.SampleBytes);
    public static bool Muted { get; private set; }
    public const double DefaultVolume = MenuWave.LauncherGain;
    public static readonly DependencyProperty IsSoundEnabledProperty = DependencyProperty.RegisterAttached(
        "IsSoundEnabled", typeof(bool), typeof(MenuSounds), new FrameworkPropertyMetadata(true, FrameworkPropertyMetadataOptions.Inherits));
    public static void SetIsSoundEnabled(DependencyObject control, bool value) => control.SetValue(IsSoundEnabledProperty, value);
    public static bool GetIsSoundEnabled(DependencyObject control) => (bool)control.GetValue(IsSoundEnabledProperty);
    public static void SetMuted(bool muted)
    {
        Muted = muted; Gate = new();
        if (muted) foreach (var clip in Clips.Values) clip.Player.Stop();
    }
    private static Window? _owner;
    private static bool _registered;
    public static readonly DependencyProperty ClickCueProperty = DependencyProperty.RegisterAttached(
        "ClickCue", typeof(MenuSoundCue), typeof(MenuSounds), new PropertyMetadata(MenuSoundCue.Change));
    public static void SetClickCue(DependencyObject control, MenuSoundCue cue) => control.SetValue(ClickCueProperty, cue);
    public static MenuSoundCue GetClickCue(DependencyObject control) => (MenuSoundCue)control.GetValue(ClickCueProperty);

    public static void Attach(Window owner)
    {
        _owner = owner;
        SetMuted(Workspace.Current.Settings.Bool("menu_sound_muted"));
        if (!_registered)
        {
            _registered = true;
            foreach (var type in new[] { typeof(ButtonBase), typeof(ComboBox), typeof(ComboBoxItem), typeof(Slider) })
                EventManager.RegisterClassHandler(type, Mouse.MouseEnterEvent, new MouseEventHandler(Hover), true);
            EventManager.RegisterClassHandler(typeof(ButtonBase), ButtonBase.ClickEvent, new RoutedEventHandler(Click), true);
            EventManager.RegisterClassHandler(typeof(ComboBox), Selector.SelectionChangedEvent, new SelectionChangedEventHandler(Selected), true);
            EventManager.RegisterClassHandler(typeof(Slider), Thumb.DragCompletedEvent, new DragCompletedEventHandler(SliderReleased), true);
        }
        owner.Closed += Closed;
        _ = LoadAsync(owner);
    }

    private static async Task LoadAsync(Window owner)
    {
        foreach (var (cue, file) in new[] { (MenuSoundCue.Hover, "hover.wav"), (MenuSoundCue.Select, "confirm.wav"), (MenuSoundCue.Start, "start.wav"), (MenuSoundCue.Change, "change.wav") })
        {
            Clip? clip = null;
            try
            {
                // IO and WAV parsing never run in a hover/click handler or block the visible page.
                clip = await Task.Run(() => Clip.Load(Path.Combine(MenuAssets.DirectoryPath, "sounds", file)));
                if (clip == null) continue;
                if (!ReferenceEquals(_owner, owner)) { clip.Dispose(); return; }
                if(Clips.Remove(cue,out var previous)) { previous.Player.Stop();previous.Dispose(); }
                Clips[cue] = clip;
            }
            catch (Exception error) when (error is IOException or InvalidDataException or UnauthorizedAccessException or InvalidOperationException or ArgumentException or NotSupportedException or TimeoutException)
            {
                clip?.Dispose();
                LauncherDiagnostics.Record("Menu sound " + file + ": " + error.Message, LogLevel.Warn);
            }
        }
    }

    private static bool Active(Control control)
    {
        if (!GetIsSoundEnabled(control) || _owner == null || !control.IsLoaded || !control.IsEnabled) return false;
        var root = control is ComboBoxItem item ? ItemsControl.ItemsControlFromItemContainer(item) : control;
        if(root==null)return false;
        var window=Window.GetWindow(root);
        // Modal windows disable the main window, but still share its option sounds.
        for(var current=window;current!=null;current=current.Owner)
            if(ReferenceEquals(current,_owner))return true;
        return window is Views.Prompt && window.IsActive;
    }
    private static bool IsInnerButton(Control control)
    {
        for (DependencyObject? node = VisualTreeHelper.GetParent(control); node != null; node = VisualTreeHelper.GetParent(node))
            if (node is ComboBox or Slider) return true;
        return false;
    }
    private static void Hover(object sender, MouseEventArgs args)
    {
        if (sender is Control control && Active(control) && (control is not ButtonBase || !IsInnerButton(control)))
            Play(MenuSoundCue.Hover);
    }
    private static void Click(object sender, RoutedEventArgs args)
    {
        if (sender is ButtonBase control && ReferenceEquals(args.Source, control) && Active(control) && !IsInnerButton(control))
            Play(control is ToggleButton ? MenuSoundCue.Change : GetClickCue(control));
    }
    private static void Selected(object sender, SelectionChangedEventArgs args)
    {
        if (sender is ComboBox control && ReferenceEquals(args.Source, control) && Active(control) &&
            (control.IsDropDownOpen || control.IsKeyboardFocusWithin)) Play(MenuSoundCue.Change);
    }
    private static void SliderReleased(object sender, DragCompletedEventArgs args)
    {
        if (!args.Canceled && sender is Slider control && Active(control)) Play(MenuSoundCue.Change);
    }
    private static void Play(MenuSoundCue cue)
    {
        if (Muted) return;
        if (!Clips.TryGetValue(cue, out var clip) || !Gate.TryPlay(cue, Environment.TickCount64, clip.DurationMs)) return;
        try { clip.Player.Play(); }
        catch (Exception error) when (error is InvalidOperationException or IOException or TimeoutException)
        { LauncherDiagnostics.Record("Menu audio playback: " + error.Message, LogLevel.Warn); }
    }
    private static void Closed(object? sender, EventArgs args)
    {
        if (sender is Window owner) owner.Closed -= Closed;
        if(!ReferenceEquals(sender,_owner))return;
        _owner = null;
        foreach (var clip in Clips.Values) { clip.Player.Stop();clip.Dispose(); }
        Clips.Clear();
    }
    private sealed class Clip : IDisposable
    {
        public SoundPlayer Player { get; }
        public int DurationMs { get; }
        public int SampleBytes { get; }
        private readonly WaveLoadStream _stream;
        private Clip(byte[] wave, int durationMs)
        {
            DurationMs = durationMs; SampleBytes=wave.Length;
            _stream = new WaveLoadStream(wave);
            Player = new SoundPlayer(_stream);
            try { Player.Load(); _stream.ReleaseSamples(); }
            catch { Player.Dispose(); _stream.Dispose(); throw; }
        }
        public static Clip? Load(string path)
        {
            if (!File.Exists(path)) return null;
            using var source = File.OpenRead(path);
            if (source.Length is < 44 or > 2 * 1024 * 1024) throw new InvalidDataException(BloodborneLauncher.Core.UiText.T("Expected a short PCM WAV (up to 2 MiB and five seconds)."));
            var wave = new byte[(int)source.Length];
            source.ReadExactly(wave);
            if (!MenuWave.TryDuration(wave, out int durationMs))
                throw new InvalidDataException(BloodborneLauncher.Core.UiText.T("Expected a mono or stereo 16-bit PCM WAV file of up to five seconds."));
            MenuWave.ApplyGain(wave, DefaultVolume);
            return new Clip(wave, durationMs);
        }
        public void Dispose() { Player.Dispose(); _stream.Dispose(); }
    }
}
