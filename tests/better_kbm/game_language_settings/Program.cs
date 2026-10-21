// SPDX-License-Identifier: GPL-3.0-or-later
using BloodborneLauncher.Core;
var settings=new LauncherSettings();settings.Set("language","3");
GameLanguages.Ensure(settings);
if(GameLanguages.Text(settings)!="3"||GameLanguages.Voice(settings)!="3")throw new Exception("Legacy migration failed");
settings.Set("language","1");
if(GameLanguages.Text(settings)!="3"||GameLanguages.Voice(settings)!="1")throw new Exception("Voice changed text");
settings.Set("text_language","0");
if(GameLanguages.Text(settings)!="0"||GameLanguages.Voice(settings)!="1")throw new Exception("Text changed voice");
GameLanguages.Ensure(settings);
if(GameLanguages.Text(settings)!="0")throw new Exception("Repeated migration overwrote text");
settings.Set("language","8");
if(GameLanguages.Voice(settings)!="1"||GameLanguages.MenuLocale(GameLanguages.Text(settings))!="ja")throw new Exception("Unsupported voice or locale mapping failed");
settings.Set("text_language","20");settings.Set("language","20");
if(GameLanguages.Text(settings)!="20"||GameLanguages.Voice(settings)!="20"||GameLanguages.MenuLocale("20")!="es")throw new Exception("Latin American Spanish was lost");
var launcher=Workspace.Current.Settings;
launcher.Set("ui_language","en");launcher.Set("text_language","20");launcher.Set("language","3");
UiText.UseTextLanguage(GameLanguages.Text(launcher));
if(PcText.Get("Turning speed")!="Velocidad de giro"||GameLanguages.Voice(launcher)!="3")throw new Exception("Text/launcher coupling or independent voice selection failed");
launcher.Set("text_language","1");UiText.UseTextLanguage(GameLanguages.Text(launcher));
if(PcText.Get("Turning speed")!="Turning speed"||GameLanguages.Voice(launcher)!="3")throw new Exception("Text language must own the launcher without changing voices");
Console.WriteLine("PASS: legacy migration, shared game/launcher text, independent voices and fallback.");
namespace BloodborneLauncher.Core {
    public sealed class Workspace {
        public static Workspace Current { get; }=new();
        public LauncherSettings Settings { get; }=new();
    }
    public sealed class LauncherSettings {
        private readonly Dictionary<string,string> values=new();
        public string Str(string key)=>values.GetValueOrDefault(key,"");
        public void Set(string key,string value)=>values[key]=value;
    }
}
