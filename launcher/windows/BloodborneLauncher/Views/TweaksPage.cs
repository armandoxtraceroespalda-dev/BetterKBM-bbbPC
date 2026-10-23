using System.Windows;
using System.Windows.Controls;
using BloodborneLauncher.Core;
using BloodborneLauncher.Controls;

namespace BloodborneLauncher.Views;

/// <summary>Gameplay and camera tweaks, kept separate from the hidden cheat page.</summary>
public sealed class TweaksPage : LauncherPage
{
    public TweaksPage() : base(UiText.T("Tweaks"), "")
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

    }

    private void IniSlider(SymbolRegular icon, string title, double min, double max, string key, Panel? into = null) =>
        SliderRow(icon, title, null, min, max, 0.05,
            () => ParseDouble(Ini.Get(key, "1"), 1), v => Ini.Set(key, Fmt(v)), into: into);

}
