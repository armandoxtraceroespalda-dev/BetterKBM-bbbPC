namespace BloodborneLauncher.Core;

/// <summary>Recognize only native cheat settings, and notify once when a launched game becomes ready.</summary>
internal sealed class CheatLaunchNotice
{
    private int _pending;
    internal void Arm(bool cheatsEnabled) => Interlocked.Exchange(ref _pending, cheatsEnabled ? 1 : 0);
    internal bool Observe(string line) => line.Contains("GPU: window and Vulkan presenter ready", StringComparison.Ordinal)
        && Interlocked.CompareExchange(ref _pending, 0, 1) == 1;

    internal static void DisableAll(Action<string, string> set, IEnumerable<string> keys)
    {
        var known = new[] { "cheat_stamina", "cheat_stealth", "cheat_keep_echoes", "cheat_no_durability",
            "cheat_consumables", "cheat_vials", "cheat_bullets", "cheat_gun_range", "cheat_echo_multiplier" };
        foreach (var key in keys.Where(k => k.StartsWith("cheat_", StringComparison.Ordinal)).Concat(known).Distinct())
            set(key, key == "cheat_echo_multiplier" ? "1"
                : key is "cheat_vials" or "cheat_bullets" or "cheat_gun_range" ? "off" : "0");
    }

    internal static bool AnyEnabled(Func<string, string, string> get)
    {
        foreach (var key in new[] { "cheat_stamina", "cheat_stealth", "cheat_keep_echoes", "cheat_no_durability", "cheat_consumables" })
            if (int.TryParse(get(key, "0"), out int value) && value != 0) return true;
        foreach (var key in new[] { "cheat_vials", "cheat_bullets" })
        {
            string value = get(key, "off");
            if (value == "infinite" || int.TryParse(value, out int amount) && amount is >= 1 and <= 99) return true;
        }
        return int.TryParse(get("cheat_gun_range", "off"), out int range) && range is 150 or 200 or 300 ||
            int.TryParse(get("cheat_echo_multiplier", "1"), out int multiplier) && multiplier is 2 or 5 or 10;
    }
}
