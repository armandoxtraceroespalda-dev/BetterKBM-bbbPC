using BloodborneLauncher.Core;
using System.Text.Json.Nodes;
static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
Directory.CreateDirectory(Paths.ConfigDir);
try {
    File.WriteAllText(Paths.ConfigFile,"""{"legacy_shaders":true,"legacy_shaders_default_98":true,"exp_frame_pacing":false,"exp_fast_loading":false,"exp_native_calls":false,"env:BB_NATIVE_CALLS":"0","game_dir":"my-game","extra_user_value":"keep"}""");
    var s=new LauncherSettings();s.Load();var env=GameEnvironment.Build(s);
    Check(s.Str("extra_user_value")=="keep"&&s.Str("game_dir")=="my-game","Migration lost unrelated settings");
    Check(env["BB_NATIVE_CALLS"]=="1"&&env["BB_GUEST_IN_PLACE"]=="1"&&env["BB_PRECISE_PACING"]=="1","New features disabled or shadowed");
    Check(!env.ContainsKey("BB_NATIVE_SHADERS"),"Legacy compiler remained forced");
    s.Set("exp_native_calls",false);s.Set("legacy_shaders",true);s.Save();s.Load();env=GameEnvironment.Build(s);
    Check(!env.ContainsKey("BB_NATIVE_CALLS")&&env["BB_NATIVE_SHADERS"]=="0","User choices lost on subsequent loads");
    s.Set("safe_mode_next",true);env=GameEnvironment.Build(s);
    Check(!env.ContainsKey("BB_GUEST_IN_PLACE")&&env["BB_PRECISE_PACING"]=="0","Safe mode did not disable features");
    s.Set("extra_env","BB_NATIVE_CALLS=verify");env=GameEnvironment.Build(s);
    Check(env["BB_NATIVE_CALLS"]=="verify","Explicit environment override lost precedence");
    Console.WriteLine("PASS: actual launcher defaults, one-time beta4 migration, unshadowed native calls, preserved user settings, safe mode and explicit overrides.");
}finally{Directory.Delete(Paths.ConfigDir,true);}
namespace BloodborneLauncher.Core {
    public record Choice(string Label,string Value,string? Note=null);
    public static class L {public static string T(string s)=>s;public static string F(string s)=>s;}
    public static class Updater{public static int CurrentBeta=>5;}
    public static class Paths{
        public static string ConfigDir=Path.Combine(Path.GetTempPath(),"betterkbm-defaults-"+Guid.NewGuid());
        public static string ConfigFile=>Path.Combine(ConfigDir,"settings.json");
        public static string DataDir=>ConfigDir;public static string EngineDir=>ConfigDir;public static string IniPath=>Path.Combine(ConfigDir,"lantern.ini");public static string Expand(string s)=>s;
    }
    public static class Secret{public static bool IsProtected(string s)=>true;public static string Protect(string s)=>s;public static string Unprotect(string s)=>s;}
    public static class GameLanguages{public static void Ensure(LauncherSettings s){} public static string Text(LauncherSettings s)=>"1";public static string Voice(LauncherSettings s)=>"1";public static string MenuLocale(string s)=>"en";}
    public static class PlayerProfiles{public static string Current(LauncherSettings s)=>"default";public static string Folder(LauncherSettings s,string profile)=>Path.Combine(Paths.DataDir,"user");}
    public static class GameRunner{public static string SessionLog=>Path.Combine(Paths.DataDir,"last_session.log");}
    public sealed class Workspace{public static Workspace Current {get;}=new();public object Ini {get;}=new();}
    public static class GpuAdapters{public static int SelectedIndex(object ini)=>0;}
    public static class WindowsMonitors{public static object All()=>new();public static void ApplyEnvironment(string device,object all,Dictionary<string,string> env){} }
}
