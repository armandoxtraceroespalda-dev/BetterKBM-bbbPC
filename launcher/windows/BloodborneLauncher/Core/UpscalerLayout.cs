namespace BloodborneLauncher.Core;

public readonly record struct UpscalerLayout(bool PresetEnabled, bool CustomScaleVisible, bool DlssVisible)
{
    public static UpscalerLayout For(string upscaler, string preset)
    {
        bool presetEnabled = upscaler is not ("off" or "taa");
        return new(presetEnabled, presetEnabled && preset == "5", upscaler == "dlss");
    }
}
