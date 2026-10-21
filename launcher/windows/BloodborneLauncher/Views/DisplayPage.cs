using BloodborneLauncher.Core;
using BloodborneLauncher.Controls;
using System.Windows;
using System.Windows.Controls;

namespace BloodborneLauncher.Views
{
    /// <summary>Monitor selection, FPS counter details and HDR output.</summary>
    public sealed class DisplayPage : LauncherPage
    {
        private readonly ComboBox _monitor = new() { Width = 450, VerticalAlignment = VerticalAlignment.Center };
        private bool _refreshingMonitors;

        private void RefreshMonitors()
        {
            _refreshingMonitors = true;
            try
            {
                var saved = S.Str("display_device");
                _monitor.Items.Clear();
                _monitor.Items.Add(new ComboBoxItem { Content = "Default (Windows primary monitor)", Tag = "" });
                foreach (var monitor in WindowsMonitors.All())
                    _monitor.Items.Add(new ComboBoxItem { Content = monitor.Label, Tag = monitor.Device,
                        ToolTip = $"{monitor.Width} × {monitor.Height}; {monitor.Device}" });
                var selected = _monitor.Items.Cast<ComboBoxItem>().FirstOrDefault(i => string.Equals(i.Tag as string, saved, StringComparison.OrdinalIgnoreCase));
                if (selected == null && saved.Length > 0)
                {
                    selected = new ComboBoxItem { Content = $"Disconnected monitor ({saved})", Tag = saved };
                    _monitor.Items.Add(selected);
                }
                _monitor.SelectedItem = selected ?? _monitor.Items[0];
            }
            finally { _refreshingMonitors = false; }
        }


        public DisplayPage() : base(BloodborneLauncher.Core.UiText.T("Display"), BloodborneLauncher.Core.UiText.T("FPS counter and HDR output settings."))
        {
            Group(UiText.T("Monitor & Info"));
            Add(Card(SymbolRegular.Desktop24, BloodborneLauncher.Core.UiText.T("Monitor"),
                BloodborneLauncher.Core.UiText.T("The game opens here from the start, including fullscreen and HDR. If this monitor is disconnected, the Windows primary monitor is used. Applied on the next launch."),
                _monitor, out _));
            var monitorRow=(SettingRow)Body.Children[^1];
            monitorRow.SizeChanged+=(_,_)=>_monitor.MaxWidth=Math.Max(120,monitorRow.ActualWidth-monitorRow.Padding.Left-monitorRow.Padding.Right-180);
            RefreshMonitors();
            _monitor.SelectionChanged += (_, _) =>
            {
                if (_refreshingMonitors || _monitor.SelectedItem is not ComboBoxItem item) return;
                S.Set("display_device", item.Tag as string ?? "");
                Ws.SaveSoon();
            };
            IniToggle(SymbolRegular.DataUsage24, BloodborneLauncher.Core.UiText.T(PcText.Get("Show FPS")), null, "show_fps");
            // What the counter shows (on unless turned off).
            foreach (var (icon, key, title) in new[]
                     {
                         (SymbolRegular.TopSpeed24, "show_fps_rate", "Frame rate"),
                         (SymbolRegular.Timer24, "show_fps_frame_time", "Frame time"),
                         (SymbolRegular.ResizeLarge24, "show_fps_upscaler", "Upscaler"),
                         (SymbolRegular.DeveloperBoard24, "show_fps_cpu", "CPU usage"),
                         (SymbolRegular.Gauge24, "show_fps_gpu", "GPU usage"),
                     })
            {
                // CPU and GPU use are off unless chosen (the engine's defaults, bbport_settings.h).
                var fallback = key is "show_fps_cpu" or "show_fps_gpu" ? "0" : "1";
                ToggleRow(icon, BloodborneLauncher.Core.UiText.T(title), BloodborneLauncher.Core.UiText.T("Shown in the FPS counter"), () => Ini.Get(key, fallback) == "1", v => Ini.Set(key, v ? "1" : "0"));
                ((System.Windows.FrameworkElement)Body.Children[^1]).Margin = new System.Windows.Thickness(24, 0, 0, 4);
            }


            Group(BloodborneLauncher.Core.UiText.T("HDR"));
            var hdrHint = WindowsHdr.AnyOn()
                ? "The game's picture on an HDR screen, with brighter highlights. Applied when the game starts"
                : WindowsHdr.AnySupported()
                    ? "Turn on Use HDR in Windows' display settings first (Settings › System › Display)"
                    : "This display shows no HDR";
            IniToggle(SymbolRegular.Hdr24, BloodborneLauncher.Core.UiText.T("HDR output"), BloodborneLauncher.Core.UiText.T(hdrHint), "hdr");
            ComboRow(SymbolRegular.Lightbulb24, BloodborneLauncher.Core.UiText.T("HDR source"),
                BloodborneLauncher.Core.UiText.T("Native: highlights as bright as they are in the game's scene. Expanded: the SDR picture's bright parts made brighter"),
                Choices.HdrMode, () => Ini.Get("hdr_mode", "native"), v => Ini.Set("hdr_mode", v));
            SliderRow(SymbolRegular.BrightnessHigh24, BloodborneLauncher.Core.UiText.T("Paper white"), BloodborneLauncher.Core.UiText.T("How bright the game's white is, in nits. Range: 10–500"),
                10, 500, 1, () => ParseDouble(Ini.Get("hdr_paper_white", "200"), 200),
                v => Ini.Set("hdr_paper_white", v.ToString("0", System.Globalization.CultureInfo.InvariantCulture)),
                format: "0' nits'", valueWidth: 82);
            SliderRow(SymbolRegular.WeatherSunny24, BloodborneLauncher.Core.UiText.T("Peak brightness"), BloodborneLauncher.Core.UiText.T("Your display's peak brightness, in nits. Range: 10–4000"),
                10, 4000, 10, () => ParseDouble(Ini.Get("hdr_peak", "1000"), 1000),
                v => Ini.Set("hdr_peak", v.ToString("0", System.Globalization.CultureInfo.InvariantCulture)),
                format: "0' nits'", valueWidth: 82);
            ComboRow(SymbolRegular.Sparkle24, BloodborneLauncher.Core.UiText.T("Highlight boost"), BloodborneLauncher.Core.UiText.T("Off keeps the game's own look; higher makes lamps, fire and the sky brighter"),
                Choices.HdrBoost, () => Ini.Get("hdr_boost", "0.25"), v => Ini.Set("hdr_boost", v));
            ComboRow(SymbolRegular.TextFont24, BloodborneLauncher.Core.UiText.T("UI brightness"), BloodborneLauncher.Core.UiText.T("Native source only: how bright the HUD, menus and subtitles are, against paper white"),
                Choices.HdrUiBrightness, () => Ini.Get("hdr_ui_brightness", "1"), v => Ini.Set("hdr_ui_brightness", v));
            ComboRow(SymbolRegular.Color24, BloodborneLauncher.Core.UiText.T("UI colour"), BloodborneLauncher.Core.UiText.T("Native source only: Vivid shows the HUD and menus with the colours a wide-gamut monitor gives SDR; Exact keeps sRGB's"),
                Choices.HdrUiColor, () => Ini.Get("hdr_ui_color", "1"), v => Ini.Set("hdr_ui_color", v));


        }
        public override void OnShown() => RefreshMonitors();
    }
}
