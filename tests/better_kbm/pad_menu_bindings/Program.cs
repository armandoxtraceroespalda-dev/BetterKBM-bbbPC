using BloodborneLauncher.Core;
static void Check(bool ok,string why){if(!ok)throw new Exception(why);}
try {
 var ini=new IniFile();ini.Load();
 PadMap.Assign(ini,"circle","lefttrigger");
 Check(PadMap.Main(ini,"l2")=="b","World binding swap failed");
 PadMap.Assign(ini,"menu_confirm","a");PadMap.Assign(ini,"menu_back","b");
 Check(PadMap.Main(ini,"circle")=="lefttrigger"&&PadMap.Main(ini,"l2")=="b"&&PadMap.Main(ini,"cross")=="a","Menu mapping changed gameplay controls");
 PadMap.Assign(ini,"menu_confirm","b");
 Check(PadMap.Main(ini,"menu_back")=="a"&&PadMap.Main(ini,"menu_confirm")=="b","Menu mappings did not swap independently");
 ini.Save();ini.Load();Check(PadMap.Main(ini,"menu_confirm")=="b","Menu mapping did not persist");
 var profile=PadMap.Current(ini);var other=new IniFile();PadMap.Apply(other,profile);
 Check(PadMap.Main(other,"menu_confirm")=="b"&&PadMap.Main(other,"l2")=="b","Profile roundtrip lost assignments");
 PadMap.ResetAll(ini);Check(PadMap.Main(ini,"menu_confirm")==""&&PadMap.Main(ini,"circle")=="b","Factory reset lost independent defaults");
 Console.WriteLine("PASS: production pad mapping: independent menu/gameplay assignments, swaps, save/reload, profiles and defaults.");
}finally{if(Directory.Exists(Paths.Root))Directory.Delete(Paths.Root,true);}
namespace BloodborneLauncher.Core {
 public static class Paths{public static readonly string Root=Path.Combine(Path.GetTempPath(),"betterkbm-pad-"+Guid.NewGuid());public static string IniPath=>Path.Combine(Root,"lantern.ini");}
 public static class UiText{public static string T(string text)=>text;}
 public static class L{public static string T(string text)=>text;}
 public sealed record ControlDef(string? Pad);
 public sealed record Effect(string Key,bool Default);public static class Choices { public static Effect[] Effects=>Array.Empty<Effect>(); }
}
