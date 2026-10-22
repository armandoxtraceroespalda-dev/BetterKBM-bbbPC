// SPDX-License-Identifier: GPL-3.0-or-later
using BloodborneLauncher.Core;
var dir=Path.Combine(Path.GetTempPath(),"ini-sync-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
try{
 Paths.IniPath=Path.Combine(dir,"lantern.ini");File.WriteAllText(Paths.IniPath,"# preserved\noutput_res=1920x1080\nupscaler=fsr3\nbind.roll=Space\nunknown_key=yes\n");
 var launcher=new IniFile();launcher.Load();File.WriteAllText(Paths.IniPath,"# preserved\noutput_res=2560x1440\nupscaler=off\nbind.roll=Space\nunknown_key=yes\n");
 launcher.Set("show_fps","0");launcher.Save();var text=File.ReadAllText(Paths.IniPath);
 Check(text.Contains("output_res=2560x1440")&&text.Contains("upscaler=off"),"External menu edits overwritten");Check(text.Contains("unknown_key=yes")&&text.Contains("# preserved"),"Unknown keys/comments lost");Check(launcher.Get("output_res")=="2560x1440","In-memory state stale");
 launcher.Set("bind.roll",null);launcher.Save();Check(!File.ReadAllText(Paths.IniPath).Contains("bind.roll="),"Default deletion lost");Check(Directory.GetFiles(dir,"*.tmp").Length==0,"Temporary file leaked");
 launcher.Set("output_res","3840x2160");var good=Paths.IniPath;Paths.IniPath=Path.Combine(dir,"blocked");Directory.CreateDirectory(Paths.IniPath);
 try{launcher.Save();throw new Exception("Expected save failure");}catch(IOException){}
 Paths.IniPath=good;launcher.Save();Check(File.ReadAllText(good).Contains("output_res=3840x2160"),"Retry lost dirty value");
 Console.WriteLine("PASS: production INI merge, refreshed values, default deletion, atomic save and retry.");
}finally{Directory.Delete(dir,true);}
static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
namespace BloodborneLauncher.Core{
 public static class Paths{public static string IniPath="";}
 public static class Choices{public static (string Key,bool Default)[] Effects=[("effect_motion_blur",false)];}
}
