using BloodborneLauncher.Core;

static void Check(bool value, string text) { if (!value) throw new Exception(text); }
var access = new CheatAccess();
Check(!access.Unlocked && !access.Feed("git"), "The section starts locked and partial input is insufficient.");
access.ResetInput();
Check(!access.Feed("gud"), "Typing on another page cannot finish a previous prefix.");
Check(access.Feed("xxGITGUD") && access.Unlocked, "The exact phrase is case insensitive and tolerates earlier unrelated input.");
Check(!access.Feed("gitgud"), "An unlocked section does not repeatedly reopen.");
var restored = new CheatAccess();
restored.Restore(true);
Check(restored.Unlocked && !restored.Feed("gitgud"), "A saved unlock restores access without re-entering the phrase.");
restored.Restore(false);
Check(restored.Unlocked, "A session unlock is not lost when a page is recreated.");
var locked = new CheatAccess(); locked.Restore(false);
Check(!locked.Unlocked, "New users with no saved unlock still start locked.");

Check(access.Feed("IGOTGUD") && !access.Unlocked, "The lock phrase hides the section again.");
Check(access.Feed("gitgud") && access.Unlocked, "The unlock phrase works again after locking.");
var resetValues = new Dictionary<string,string> {
    ["cheat_stamina"]="1", ["cheat_vials"]="infinite", ["cheat_bullets"]="50",
    ["cheat_gun_range"]="300", ["cheat_echo_multiplier"]="10", ["cheat_future"]="1",
    ["change_appearance"]="1", ["rebirth"]="1"
};
CheatLaunchNotice.DisableAll((key,value)=>resetValues[key]=value, resetValues.Keys.ToArray());
Check(!CheatLaunchNotice.AnyEnabled((key,fallback)=>resetValues.GetValueOrDefault(key,fallback)), "Locking disables every native cheat type.");
Check(resetValues["cheat_future"]=="0" && resetValues["change_appearance"]=="1" && resetValues["rebirth"]=="1",
    "Reset covers extra cheat keys while retaining gameplay tweaks.");
Check(resetValues["cheat_echo_multiplier"]=="1", "The echo multiplier resets to its neutral value, not zero.");

var ini = new Dictionary<string, string>();
string Get(string key, string fallback) => ini.GetValueOrDefault(key, fallback);
Check(!CheatLaunchNotice.AnyEnabled(Get), "An empty configuration has no active cheats.");
foreach (string key in new[] { "change_appearance", "rebirth", "skip_intro", "debug_camera", "show_fps", "graphics_controls" }) ini[key] = "1";
Check(!CheatLaunchNotice.AnyEnabled(Get), "Gameplay and graphics tweaks must never trigger the notification.");
foreach (var (key, value) in new[] {
    ("cheat_stamina", "1"), ("cheat_stealth", "1"), ("cheat_keep_echoes", "1"),
    ("cheat_no_durability", "1"), ("cheat_consumables", "1"), ("cheat_vials", "infinite"),
    ("cheat_bullets", "12"), ("cheat_gun_range", "150"), ("cheat_echo_multiplier", "2"), ("cheat_gun_range", "0150") })
{
    ini[key] = value; Check(CheatLaunchNotice.AnyEnabled(Get), key + " must trigger."); ini.Remove(key);
}
foreach (var (key, value) in new[] { ("cheat_vials", "100"), ("cheat_bullets", "-1"), ("cheat_gun_range", "100"), ("cheat_echo_multiplier", "1"), ("cheat_unknown", "1") }) ini[key] = value;
Check(!CheatLaunchNotice.AnyEnabled(Get), "Invalid, default and unknown keys do not count as cheats.");

const string ready = "GPU: window and Vulkan presenter ready; SDK 0x02000071";
var notice = new CheatLaunchNotice();
notice.Arm(false); Check(!notice.Observe(ready), "Clean sessions do not notify.");
notice.Arm(true); Check(!notice.Observe("Boot image reused"), "Preparation is not a running game.");
Check(notice.Observe(ready) && !notice.Observe(ready), "Ready output triggers once per launch.");
notice.Arm(true); Check(notice.Observe(ready), "Another launch gets another transient notification.");
notice.Arm(true); notice.Arm(false); Check(!notice.Observe(ready), "A failed or exited session cancels its notice.");
Console.WriteLine("PASS: phrase and saved access restoration, cheat versus tweak classification, invalid values, readiness and once-per-launch notification.");
