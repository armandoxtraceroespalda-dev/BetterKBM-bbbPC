using System.IO;
using System.Windows;
using System.Windows.Controls;
using BloodborneLauncher.Core;
using BloodborneLauncher.Controls;
using TextBlock = System.Windows.Controls.TextBlock;

namespace BloodborneLauncher.Views
{
    /// <summary>GPU, screen, upscaling and the game's own rendering effects.</summary>
    public sealed partial class GraphicsPage : LauncherPage
    {
        private readonly TextBlock _upscalerHint, _dlssHint, _frameGenHint;
        private TextBlock? _motionBlurHint;
        private readonly SettingRow _presetCard, _renderScaleCard, _sharpenCard, _sharpnessCard, _mipBiasCard;
        private readonly SettingRow _frameGenFramesCard, _frameGenDynamicCard, _reflexCard, _frameGenCapCard;

        private readonly ComboBox _gpuSelector;
        private readonly OptionRows _pictureOptions = new();
        private void UpdatePictureOptions()
        {
            _pictureOptions.IsEnabled = Ini.Flag("graphics_controls");
            _pictureOptions.Opacity = _pictureOptions.IsEnabled ? 1 : 0.45;
        }
        public GraphicsPage() : base(BloodborneLauncher.Core.UiText.T("Graphics"),
            BloodborneLauncher.Core.UiText.T("Stored in bbport.ini; the upscaler can also be changed in game (Insert or L3 + R3)."))
        {
            var recommend = SmallButton(BloodborneLauncher.Core.UiText.T("Recommended settings"), SymbolRegular.Sparkle24,
                (_, _) => MainWindow.Instance!.OfferBestSettings(firstRun: false));
            recommend.Content = UiText.T("Recommended settings");
            recommend.Icon = null;
            recommend.Tag = "standalone";
            recommend.MinWidth = 220;
            recommend.HorizontalAlignment = HorizontalAlignment.Right;
            recommend.Margin = new Thickness(0, 0, 12, 8);
            Body.Children.Add(recommend);
            SettingsColumns.SetFullWidth(recommend, true);

            var adapters=new[]{new Choice(PcText.Get("Automatic"),"-1")}.Concat(GpuAdapters.All().Select(g=>Choice.Literal(g.Label,g.Index.ToString()))).ToArray();
            _gpuSelector=(ComboBox)ComboRow(SymbolRegular.DeveloperBoard24,BloodborneLauncher.Core.UiText.T(PcText.Get("Rendering GPU")),BloodborneLauncher.Core.UiText.T(PcText.Get("Select the graphics card used by Vulkan")),adapters,
                ()=>GpuAdapters.SelectedIndex(Ini).ToString(),v=>{
                    Ini.Set("gpu_id",v);Ini.Set("gpu_name",GpuAdapters.All().FirstOrDefault(g=>g.Index.ToString()==v)?.Name??"");
                    Hardware.Reset();

                },width:560);
            var gpuRow=(SettingRow)Body.Children[^1];
            gpuRow.StyleNameAsHeading();
            SettingsColumns.SetFullWidth(gpuRow, true);
            // The right edge stays aligned; a wider selector grows left into the row.
            // Reserve label space at smaller window sizes rather than overflowing the card.
            gpuRow.SizeChanged+=(_,_)=>_gpuSelector.MaxWidth=Math.Max(120,gpuRow.ActualWidth-gpuRow.Padding.Left-gpuRow.Padding.Right-180);
            void UpdateGpuTip()=>_gpuSelector.ToolTip=(_gpuSelector.SelectedItem as ComboBoxItem)?.Content?.ToString();
            _gpuSelector.SelectionChanged+=(_,_)=>UpdateGpuTip();
            UpdateGpuTip();

            SettingsColumns.SetTwoColumns(Group(UiText.T("Screen")), true);
            BuildMonitorSetting();
            SettingCombo(SymbolRegular.Gauge24, BloodborneLauncher.Core.UiText.T("Frame rate"), BloodborneLauncher.Core.UiText.T("30, 60, 90, 120 and 144 are the game's frame-rate patches for that rate; Uncapped uses the community uncapped patch"), Choices.FpsModes, "fps_mode");
            OutputResolutionRow();
            IniCombo(SymbolRegular.ArrowSync24, BloodborneLauncher.Core.UiText.T("Live resolution change"),
                BloodborneLauncher.Core.UiText.T("Allow output resolution changes during play. Off saves changes for the next launch. This switch takes effect when the game starts."), Choices.LiveResolution, "live_resolution");
            IniToggle(SymbolRegular.FullScreenMaximize24, BloodborneLauncher.Core.UiText.T("Fullscreen"), BloodborneLauncher.Core.UiText.T("Borderless, so trophy notifications show on top"), "fullscreen");
            ToggleRow(SymbolRegular.RatioOneToOne24, BloodborneLauncher.Core.UiText.T("Widescreen"), BloodborneLauncher.Core.UiText.T("21:9 and 32:9 outputs (3440 × 1440, 5120 × 1440) with the right field of view"),
                () => Ini.Get("widescreen", "1") != "0", v => Ini.Set("widescreen", v ? "1" : "0"));
            SettingCombo(SymbolRegular.Image24, BloodborneLauncher.Core.UiText.T("Vsync"), BloodborneLauncher.Core.UiText.T(PcText.Get("Synchronizes frames with the display to prevent image tearing. The selected mode affects latency and how late frames are displayed.")), Choices.PresentModes, "present_mode");


            Group(BloodborneLauncher.Core.UiText.T("Game effects"));
            Body.Children.Add(Text(BloodborneLauncher.Core.UiText.T("Patches to the game, applied when it starts."), "Faint"));
            IniCombo(SymbolRegular.Cube24, BloodborneLauncher.Core.UiText.T("Model detail"), null, Choices.ModelLod, "model_lod");
            IniToggle(SymbolRegular.CircleHalfFill24, BloodborneLauncher.Core.UiText.T("Vignette"), null, "graphics_vignette");
            foreach (var effect in Choices.Effects.Where(effect => effect.Key is not ("skip_intro" or "debug_camera"))) {
                IniToggle(EffectIcon(effect.Key), UiText.T(effect.Title), UiText.T(effect.Hint), effect.Key,
                    changed: effect.Key == "effect_motion_blur" ? UpdateUpscaler : null);
                if (effect.Key == "effect_motion_blur") _motionBlurHint = ((SettingRow)Body.Children[^1]).Hint;
            }

            // The engine's picture and camera controls (bbport_settings.cpp clamps the same ranges).
            Group(BloodborneLauncher.Core.UiText.T("Picture"));
            var picture = _pictureOptions;
            IniToggle(SymbolRegular.Color24, BloodborneLauncher.Core.UiText.T("Picture tuning"), BloodborneLauncher.Core.UiText.T("Turns the settings below on, live. Also in the game's System menu (PC Graphics, PC Effects, PC Colour) and the Insert menu"), "graphics_controls", changed: UpdatePictureOptions);
            IniSlider(SymbolRegular.DarkTheme24, BloodborneLauncher.Core.UiText.T("Ambient occlusion strength"), 0, 3, "graphics_ao_strength", picture);
            IniSlider(SymbolRegular.WeatherSunny24, BloodborneLauncher.Core.UiText.T("Shadow distance"), 0.5, 4, "graphics_shadow_scale", picture);
            IniSlider(SymbolRegular.Lightbulb24, BloodborneLauncher.Core.UiText.T("Bloom"), 0, 3, "graphics_bloom", picture);
            IniSlider(SymbolRegular.Color24, BloodborneLauncher.Core.UiText.T("Saturation"), 0, 3, "graphics_saturation", picture);
            IniSlider(SymbolRegular.BrightnessHigh24, BloodborneLauncher.Core.UiText.T("Brightness"), 0, 3, "graphics_brightness", picture);
            IniSlider(SymbolRegular.DarkTheme24, BloodborneLauncher.Core.UiText.T("Contrast"), 0.8, 1.2, "graphics_contrast", picture, step: 0.01);
            IniSlider(SymbolRegular.Color24, BloodborneLauncher.Core.UiText.T("Hue"), -180, 180, "graphics_hue", picture, step: 1, fallback: 0);
            IniSlider(SymbolRegular.WeatherFog24, BloodborneLauncher.Core.UiText.T("Fog"), 0, 3, "graphics_fog", picture);
            Body.Children.Add(picture);
            UpdatePictureOptions();

            Group(BloodborneLauncher.Core.UiText.T("Upscaler"));
            // FSR 4.1.1 runs through fsr4vk (in the package) or an fsr4_411 set built from the player's
            // own AMD DLL: offered when either is there. Without both the setting becomes FSR 4, the
            // model it would fall back to (#22).
            var fsr411 = Fsr4VkFound() || Directory.Exists(Fsr411Dir());
            if (!fsr411 && Ini.Get("upscaler") == "fsr411") Ini.Set("upscaler", "fsr4");
            IniCombo(SymbolRegular.Sparkle24, BloodborneLauncher.Core.UiText.T("Upscaler"), null,
                fsr411 ? Choices.Upscalers : Choices.Upscalers.Where(c => c.Value != "fsr411").ToArray(), "upscaler", changed: UpdateUpscaler);
            _upscalerHint = (TextBlock)((SettingRow)Body.Children[^1]).Tag;

            IniCombo(SymbolRegular.ZoomIn24, BloodborneLauncher.Core.UiText.T("Quality preset"), BloodborneLauncher.Core.UiText.T("How far below the output resolution the game renders"),
                Choices.Presets, "preset", changed: UpdateUpscaler);
            _presetCard = (SettingRow)Body.Children[^1];
            // The Custom preset's ratio (render_scale), the engine's PresetScale: 1.0 renders at the
            // output (DLAA with DLSS), 3.0 is Ultra Performance.
            SliderRow(SymbolRegular.SlideSize24, UiText.T("Render scale (Custom preset)"),
                UiText.T("Output size divided by this is the render size: 1.00 renders at the output, 1.30 a little below, 3.00 like Ultra Performance. FSR 4 and DLSS use the nearest preset's model"),
                1.0, 3.0, 0.05, () => ParseDouble(Ini.Get("render_scale", "1.30"), 1.3), v => Ini.Set("render_scale", Fmt(v)), format: "x0.00", valueWidth: 56);
            _renderScaleCard = (SettingRow)Body.Children[^1];



            IniToggle(SymbolRegular.Eyedropper24, BloodborneLauncher.Core.UiText.T("Sharpening (RCAS)"), BloodborneLauncher.Core.UiText.T("Works with every option above, TAA included: turn it up if the picture looks soft"), "sharpen");
            _sharpenCard = (SettingRow)Body.Children[^1];
            SliderRow(SymbolRegular.SlideSize24, BloodborneLauncher.Core.UiText.T("Sharpening strength"), BloodborneLauncher.Core.UiText.T("Up to 1: the upscaler's own sharpening. Above 1: an extra pass"), 0, 2, 0.05,
                () => ParseDouble(Ini.Get("sharpness"), 0.5), v => Ini.Set("sharpness", Fmt(v)));
            _sharpnessCard = (SettingRow)Body.Children[^1];
            SliderRow(SymbolRegular.Image24, UiText.T("Texture mip bias"),
                UiText.T("Added to the texture detail the upscaler asks for: below 0 sharper textures (more shimmer), above 0 softer. 0 is the upscaler's own"),
                -2, 1, 0.05, () => ParseDouble(Ini.Get("mip_bias", "0"), 0), v => Ini.Set("mip_bias", Fmt(v)), format: "+0.00;-0.00;0.00", valueWidth: 56);
            _mipBiasCard = (SettingRow)Body.Children[^1];
            IniToggle(SymbolRegular.ArrowMove24, UiText.T("Object motion vectors"), UiText.T("Less ghosting on characters; costs about 10% FPS"), "object_motion");

            var dlssMore = new OptionRows();
            Group(UiText.T("DLSS options"), dlssMore);
            Body.Children.Add(dlssMore);
            IniCombo(SymbolRegular.Sparkle24, UiText.T("DLSS model"),
                UiText.T("Which NVIDIA model DLSS runs. Also in game (Insert or L3 + R3), where it changes at once"), Choices.DlssModels, "dlss_preset", dlssMore);
            ToggleRow(SymbolRegular.BrightnessHigh24, UiText.T("DLSS auto exposure"),
                UiText.T("DLSS works out each frame's exposure itself, for the game's finished frame too. Can calm ghosting in dark scenes"),
                () => Ini.Get("dlss_auto_exposure", "0") == "1", v => Ini.Set("dlss_auto_exposure", v ? "1" : "0"), dlssMore);
            ToggleRow(SymbolRegular.Image24, UiText.T("DLSS sharper textures"),
                UiText.T("One more mip level of texture detail under DLSS, as NVIDIA's guide asks. Off: the render-to-output ratio only"),
                () => Ini.Get("dlss_mip_bias", "1") != "0", v => Ini.Set("dlss_mip_bias", v ? "1" : "0"), dlssMore);
            Add(Card(SymbolRegular.DeveloperBoard24, "DLSS", "", null, out _dlssHint), dlssMore);

            IniCombo(SymbolRegular.FastForward24, UiText.T("Frame generation"),
                UiText.T("Extra frames between the game's own: smoother motion, a little more input lag. Needs an upscaler. Off by default"),
                Choices.FrameGeneration, "frame_gen", changed: UpdateUpscaler);
            _frameGenHint = (TextBlock)((SettingRow)Body.Children[^1]).Tag;
            IniCombo(SymbolRegular.FastForward24, UiText.T("DLSS generated frames"),
                UiText.T("Frames shown per frame the game renders. More than 2x needs an RTX 50 GPU; the engine keeps to what the GPU allows"),
                Choices.FrameGenFrames, "frame_gen_frames");
            _frameGenFramesCard = (SettingRow)Body.Children[^1];
            IniToggle(SymbolRegular.TopSpeed24, UiText.T("DLSS dynamic frame generation"),
                UiText.T("RTX 50: the driver picks 2x, 3x or 4x moment by moment to reach your refresh rate, up to the generated frames above. Fewer generated frames when the game is fast: less lag, fewer artifacts. Also in game, with a target FPS"), "frame_gen_dynamic");
            _frameGenDynamicCard = (SettingRow)Body.Children[^1];
            IniCombo(SymbolRegular.Flash24, UiText.T("NVIDIA Reflex"),
                UiText.T("Keeps the GPU's queue short for lower input lag. Always on while DLSS Frame Generation runs"),
                Choices.Reflex, "reflex");
            _reflexCard = (SettingRow)Body.Children[^1];
            IniToggle(SymbolRegular.Timer24, UiText.T("Frame generation: even pacing (Uncapped)"),
                UiText.T("With the Uncapped frame rate: the game at the refresh rate divided by the frames shown per game frame, so every generated frame reaches the screen. The fixed rates (30 to 144) are left as they are"), "frame_gen_cap");
            _frameGenCapCard = (SettingRow)Body.Children[^1];

            BuildHdrSettings();
            BuildInfoSettings();

            Group(BloodborneLauncher.Core.UiText.T("Experimental"));
            SettingToggle(SymbolRegular.Broom24, BloodborneLauncher.Core.UiText.T("VRAM cleanup"),
                BloodborneLauncher.Core.UiText.T("Frees video memory held by textures the GPU drew, so long sessions don't fill up VRAM and slow down. Recommended: on"),
                "exp_vram_cleanup");
            SettingToggle(SymbolRegular.Timer24, BloodborneLauncher.Core.UiText.T("Precise frame pacing"),
                BloodborneLauncher.Core.UiText.T("A high-resolution timer and even spacing between frames: steadier frame times and less VRR flicker. Enabled by default"),
                "exp_frame_pacing");
            SettingToggle(SymbolRegular.Storage24, BloodborneLauncher.Core.UiText.T("Use more VRAM for textures"),
                BloodborneLauncher.Core.UiText.T("Keeps textures in video memory up to 80% of the card instead of about 40%, so they aren't thrown out and loaded again (the cause of most stutters on 8 GB cards). Off by default while it is tested"),
                "exp_vram_budget");
            SettingToggle(SymbolRegular.TopSpeed24, BloodborneLauncher.Core.UiText.T("Graphics threads first"),
                BloodborneLauncher.Core.UiText.T("Runs the engine's graphics threads at a higher CPU priority than the game's other threads, so a frame isn't held up behind them. Off by default"),
                "exp_thread_priority");
            SettingToggle(SymbolRegular.Sleep24, BloodborneLauncher.Core.UiText.T("Shorter sleep spin"),
                BloodborneLauncher.Core.UiText.T("The game's many short sleeps busy-wait only as long as the timer needs (about 0.2–0.4 ms instead of 0.6 ms), leaving that CPU time to the graphics threads. Off by default"),
                "exp_sleep_spin");
            SettingToggle(SymbolRegular.FlashFlow24, BloodborneLauncher.Core.UiText.T("Background shader compiling"),
                BloodborneLauncher.Core.UiText.T("New shaders compile on helper threads instead of freezing the frame: much shorter hitches when you reach a new area. On by default"),
                "exp_async_pipelines");
            SettingToggle(SymbolRegular.EyeOff24, BloodborneLauncher.Core.UiText.T("Occlusion culling"),
                BloodborneLauncher.Core.UiText.T("The game skips objects hidden behind walls instead of drawing them all: much higher FPS in dense areas. Turn it off if lamps or glare flicker"),
                "exp_occlusion");
            SettingToggle(SymbolRegular.FastForward24, BloodborneLauncher.Core.UiText.T("Faster draw thread"),
                BloodborneLauncher.Core.UiText.T("Remembers texture and sampler lookups and hands the end of each frame to the draw thread: higher FPS in busy areas. New: turn it off if the game hangs or crashes"),
                "exp_fast_draws");
            SettingToggle(SymbolRegular.Shapes24, BloodborneLauncher.Core.UiText.T("No shader freezes"),
                BloodborneLauncher.Core.UiText.T("While a new shader compiles, the objects that need it appear a moment later instead of the game freezing. New: turn it off if something stays missing"),
                "exp_compile_skip");
            SettingToggle(SymbolRegular.Storage24,"PC memory model",
                "Lets the GPU read the game's memory in place when supported, with the engine's compatibility fallback", "exp_guest_in_place");
            SettingToggle(SymbolRegular.FastForward24,"Native draw calls",
                "Sends supported game draw calls directly to the renderer", "exp_native_calls");
            SettingToggle(SymbolRegular.Timer24,"Faster loading",
                "Uses the upstream engine's faster loading path", "exp_fast_loading");
            SettingToggle(SymbolRegular.Storage24,"Hot memory pages",
                "Avoids repeatedly watching frequently written memory pages", "exp_hot_pages");
            SettingToggle(SymbolRegular.Run24, BloodborneLauncher.Core.UiText.T("Sharper motion (FSR 4)"),
                BloodborneLauncher.Core.UiText.T("Uses FSR 4's balanced model at Performance and Ultra Performance: fewer trails behind moving characters, a little more GPU time"),
                "fsr4_balanced");

            Ws.PortChanged += RefreshGraphics;
            ReleaseWhenDisposed(() => Ws.PortChanged -= RefreshGraphics);
        }

        private void OutputResolutionRow(){
            string Current()=>Ini.Get("output_res","1920x1080");
            string Selected()=>Choices.OutputResolutions.Any(c=>c.Value==Current())?Current():"custom";
            var parts=Current().Split('x');
            var width=parts.Length==2&&int.TryParse(parts[0],out var w)?w.ToString():"1920";
            var height=parts.Length==2&&int.TryParse(parts[1],out var h)?h.ToString():"1080";
            SettingRow? widthRow=null,heightRow=null;
            void Show(bool show){if(widthRow!=null)widthRow.Visibility=show?Visibility.Visible:Visibility.Collapsed;if(heightRow!=null)heightRow.Visibility=show?Visibility.Visible:Visibility.Collapsed;}
            void Save(){if(int.TryParse(width,out var x)&&int.TryParse(height,out var y)&&x>=640&&x<=16384&&y>=360&&y<=8640)Ini.Set("output_res",$"{x&~1}x{y&~1}");}
            ComboRow(SymbolRegular.ResizeLarge24,UiText.T("Output resolution"),UiText.T("The upscaler renders below this and fills it in"),Choices.OutputResolutions,Selected,v=>{Show(v=="custom");if(v=="custom")Save();else Ini.Set("output_res",v);});
            TextRow(SymbolRegular.ResizeLarge24,UiText.T("Width"),null,"1920",()=>width,v=>{width=v;Save();});widthRow=(SettingRow)Body.Children[^1];
            TextRow(SymbolRegular.ResizeLarge24,UiText.T("Height"),null,"1080",()=>height,v=>{height=v;Save();});heightRow=(SettingRow)Body.Children[^1];Show(Selected()=="custom");
        }

        private void IniSlider(SymbolRegular icon, string title, double min, double max, string key, Panel? into = null,
            double step = 0.05, double fallback = 1.0) =>
            SliderRow(icon, BloodborneLauncher.Core.UiText.T(title), null, min, max, step, () => ParseDouble(Ini.Get(key, Fmt(fallback)), fallback), v => Ini.Set(key, Fmt(v)), into: into);

        private static SymbolRegular EffectIcon(string key) => key switch
        {
            "skip_intro" => SymbolRegular.FastForward24,
            "debug_camera" => SymbolRegular.Camera24,
            "debug_menu" => SymbolRegular.Bug24,
            _ => SymbolRegular.Color24,
        };

        private void RefreshGraphics()
        {
            var value=GpuAdapters.SelectedIndex(Ini).ToString();
            var selected=_gpuSelector.Items.OfType<ComboBoxItem>().FirstOrDefault(item=>(string?)item.Tag==value);
            if(selected!=null && !ReferenceEquals(_gpuSelector.SelectedItem,selected))_gpuSelector.SelectedItem=selected;
            UpdateUpscaler();
        }
        public override void OnShown() { RefreshGraphics(); RefreshMonitors(); UpdateCounterOptions(); UpdateHdrOptions(); UpdatePictureOptions(); }

        private bool DlssFrameGenGpu() {
            var selected=GpuAdapters.All().FirstOrDefault(g=>g.Index==GpuAdapters.SelectedIndex(Ini));
            string name=selected?.Name ?? Hardware.Detect().Gpu;
            return System.Text.RegularExpressions.Regex.IsMatch(name, @"RTX\s*(PRO\s*)?[45]0\d\d", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
        }
        private static bool StreamlineFound() => File.Exists(Path.Combine(Paths.BinariesDir,"sl.interposer.dll")) &&
            File.Exists(Path.Combine(Paths.BinariesDir,"sl.dlss_g.dll"));
        private static bool DlssFound() =>
            Paths.DlssRuntime != null; // the engine has DLSS built in; NVIDIA's runtime sits beside it

        private void UpdateUpscaler()
        {
            var value = Ini.Get("upscaler");
            string? hint = value switch
            {
                "fsr4" => Directory.Exists(Path.Combine(Paths.EngineDir, "fsr4_shaders"))
                    ? "Assets found. GPUs without the needed shader features fall back to FSR 3.1"
                    : "fsr4_shaders is missing from the port folder: the game falls back to FSR 3.1",
                "fsr411" when Fsr4VkFound() =>
                    "Through fsr4vk (included, experimental): runs on any GPU with the needed Vulkan features. Prefer FSR 4 if it looks wrong",
                "fsr411" => Fsr411Problem() is { } problem
                    ? BloodborneLauncher.Core.UiText.F($"{problem}: the game falls back to FSR 4")
                    : "Assets for the chosen resolution and preset found",
                "dlss" => DlssFound()
                    ? "Needs an NVIDIA GeForce RTX GPU; others fall back to FSR 3.1" +
                      (Ini.Get("preset", "0") == "0" ? ". With Native AA this is DLAA: DLSS as anti-aliasing at full resolution" : "")
                    : "DLSS files are missing: the game falls back to FSR 3.1",
                "taa" => "Anti-aliasing at the output resolution, without an FSR model. Softer than DLAA or FSR Native AA: add sharpening below",
                _ => null,
            };
            SetHint(_upscalerHint, BloodborneLauncher.Core.UiText.T(hint));
            _presetCard.IsEnabled = value is not ("taa" or "off");
            _sharpenCard.IsEnabled = value != "off";
            _sharpnessCard.IsEnabled = value != "off";
            _mipBiasCard.IsEnabled = value != "off";

            // Frame generation: DLSS's own rows only with DLSS chosen.
            var frameGen = Ini.Get("frame_gen", "off");
            _frameGenFramesCard.IsEnabled = frameGen == "dlss";
            _frameGenDynamicCard.IsEnabled = frameGen == "dlss";
            _reflexCard.IsEnabled = frameGen == "dlss";
            _frameGenCapCard.IsEnabled = frameGen != "off";
            SetHint(_frameGenHint, frameGen switch
            {
                "dlss" when !StreamlineFound() => UiText.T("NVIDIA Streamline (sl.interposer.dll, sl.dlss_g.dll) is missing from the engine folder: the game uses no frame generation"),
                "dlss" when !DlssFrameGenGpu() => UiText.T("Needs an NVIDIA RTX 40 or 50 GPU with Hardware-accelerated GPU scheduling on; other GPUs: choose FSR 3.1"),
                "dlss" => UiText.T("RTX 40: 2x; RTX 50: up to 6x. Loaded when the game starts; in game you can switch it off and on (Insert or L3 + R3)"),
                "fsr3" => UiText.T("Any GPU. Not in menus, movies or with HDR output"),
                _ => null,
            });
            if (frameGen != "off" && value == "off")
                SetHint(_frameGenHint, UiText.T("Frame generation needs an upscaler (or TAA): choose one above"));

            // An upscaler takes the finished frame at outputs other than 1920x1080 and can smear the
            // game's motion blur; More > Engine can leave the blur out there (BB_UPSCALER_NO_BLUR).
            var (w, h) = Choices.OutputSize(Ini.Get("output_res", "1920x1080"));
            if (_motionBlurHint != null)
                SetHint(_motionBlurHint, value != "off" && (w, h) != (1920, 1080) && Ini.Flag("effect_motion_blur")
                    ? UiText.T("With an upscaler at an output other than 1920x1080 the blur can smear. More > Engine > No motion blur smear with upscalers leaves it out there")
                    : null);

            var ngx = Paths.DlssRuntime;
            SetHint(_dlssHint, ngx != null
                ? UiText.F($"NVIDIA DLSS {Paths.DlssVersion(ngx)} is included ({Path.Combine("out", Path.GetFileName(ngx))}). Choose it as the upscaler above")
                : UiText.F($"{Path.Combine("out", "nvngx_dlss.dll")} is missing from the engine folder"));
        }

        /// <summary>The fsr4vk provider the package ships (FSR 4.1.1 on any GPU, vk_fsr4vk.cpp).</summary>
        private static bool Fsr4VkFound() =>
            File.Exists(Path.Combine(Paths.EngineDir, "fsr4vk", "amd_fidelityfx_upscaler_vk.dll"));

        private static string Fsr411Dir()
        {
            var env = Environment.GetEnvironmentVariable("BB_FSR411_DIR");
            if (!string.IsNullOrEmpty(env)) return env;
            var local = Path.Combine(Paths.EngineDir, "fsr4_411");
            return Directory.Exists(local) ? local : Path.Combine(Paths.DataDir, "fsr4_411");
        }

        /// <summary>The FSR 4.1.1 model files the chosen output and preset need (fsr4_411\).</summary>
        private static string? Fsr411Problem()
        {
            var (w, h) = Choices.OutputSize(Ini.Get("output_res", "1920x1080"));
            var model = (w > 1920 || h > 1080 ? "t2160" : "t1080") + (Ini.Get("preset", "4") == "4" ? "_m1" : "_m0");
            var passes = new List<string> { "spd", "prepass", "pass0_post" };
            for (var i = 1; i <= 12; i++)
            {
                passes.Add($"pass{i}");
                passes.Add($"pass{i}_post");
            }
            passes.Add("postpass");
            passes.Add("rcas");

            var dir = Path.Combine(Fsr411Dir(), model);
            foreach (var name in passes)
            {
                var file = new FileInfo(Path.Combine(dir, name + ".spv"));
                if (!file.Exists || file.Length < 20 || file.Length % 4 != 0)
                    return $"Missing or damaged fsr4_411\\{model}\\{name}.spv";
            }
            var init = new FileInfo(Path.Combine(dir, "initializer.bin"));
            return init.Exists && init.Length == 131072 ? null : $"Missing or damaged fsr4_411\\{model}\\initializer.bin";
        }
    }
}
