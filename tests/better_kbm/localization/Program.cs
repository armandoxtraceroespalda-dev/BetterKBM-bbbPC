using BloodborneLauncher.Core;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;
static void Check(bool ok,string message){if(!ok)throw new Exception(message);}
Dictionary<string,string> Read(string code){using var s=Assembly.GetExecutingAssembly().GetManifestResourceStream("BloodborneLauncher.Localization."+code+".json")!;return JsonSerializer.Deserialize<Dictionary<string,string>>(s)!;}
var english=Read("en");var placeholders=new Regex(@"(?<!\{)\{\d+[^{}]*\}(?!\})");int count=0;
foreach(var code in new[]{"es","ru","ja","fr","de","it","pt-BR"}){
 var catalog=Read(code);Check(english.Keys.ToHashSet().SetEquals(catalog.Keys),code+" must cover the complete catalog.");
 foreach(var (key,value) in catalog){
  Check(value.Length>0||key.Length==0,code+" has an empty translation: "+key);
  Check(!Regex.IsMatch(value,@"&(?:quot|amp|lt|gt|nbsp);"),code+" has an HTML entity in plain UI text: "+key);
  var expected=placeholders.Matches(key).Select(m=>m.Value).Order().ToArray();
  Check(expected.SequenceEqual(placeholders.Matches(value).Select(m=>m.Value).Order()),code+" changed a placeholder: "+key);
  if(expected.Length>0){
   // Parse formatting independently of argument types (dates and numbers have different format specifiers).
   _=System.Text.CompositeFormat.Parse(value);
  }
  Check(!Regex.IsMatch(value,@"(.)\1{7,}"),code+" has a repeated-token artifact: "+key);count++;
 }
}
var original=CultureInfo.CurrentCulture;
foreach(var (id,locale) in new[]{("1","en"),("0","ja"),("2","fr"),("3","es-ES"),("20","es-MX"),("4","de"),("5","it"),("8","ru"),("17","pt-BR")}){
 UiText.UseTextLanguage(id);Check(UiText.Locale==locale,"The existing text-language IDs must remain stable.");
 if(id!="1")Check(UiText.T("Settings")!="Settings","The selected launcher language must take effect immediately.");
 Check(UiText.F($"Vulkan GPU {3}").Contains('3'),"Formatting must preserve runtime values.");
 Check(ReferenceEquals(original,CultureInfo.CurrentCulture),"Language changes must not affect numeric INI serialization.");
 const string path=@"C:\Users\Admin\Bloodborne\gui";Check(UiText.T(path)==path,"User paths are not translated.");
 Check(UiText.Format("{0}","Shift + Mouse1")=="Shift + Mouse1","User bindings are inserted without translation.");
}
string Resource(string name){using var input=Assembly.GetExecutingAssembly().GetManifestResourceStream(name)!;using var reader=new StreamReader(input);return reader.ReadToEnd();}
var terms=Resource("Review.Terms").Split('\n').Where(s=>s.Length>0&&!s.StartsWith('#')).Select(s=>s.TrimEnd('\r').Split('\t')).ToArray();
var descriptions=Resource("Review.Descriptions").Split('\n').Where(s=>s.Length>0&&!s.StartsWith('#')).Select(s=>s.TrimEnd('\r').Split('\t')).ToArray();
var spanish=JsonSerializer.Deserialize<Dictionary<string,string>>(Resource("Review.Spanish"))!;
var ids=new[]{"3","8","0","2","4","5","17"};int semanticChecks=0;
for(int index=0;index<ids.Length;index++) {
 var expected=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
 foreach(var row in terms)expected[row[0]]=row[index+1];
 if(index==0)foreach(var pair in spanish)expected[pair.Key]=pair.Value;
 else foreach(var row in descriptions)expected[row[0]]=row[index];
 UiText.UseTextLanguage(ids[index]);
 foreach(var key in english.Keys)if(expected.TryGetValue(key,out var value)) {
  Check(UiText.T(key)==value,$"Context review regressed ({ids[index]}): {key}");semanticChecks++;
 }
 foreach(var key in new[]{"Move forward","Move Forward","Move back","Move left","Move right","Action","Filter actions or keys","Monitor & Info"})
  Check(expected.ContainsKey(key),"Every action/header regression must have a contextual translation: "+key);
}
foreach(var id in new[]{"3","20"}) {
 UiText.UseTextLanguage(id);
 Check(UiText.T("Action")=="Acción"&&UiText.T("Move forward")=="Avanzar"&&UiText.T("Move back")=="Retroceder","Both Spanish choices must use game-action terminology.");
 Check(UiText.T("Tweaks")=="Mejoras", "The sidebar must distinguish improvements from Settings.");
 Check(UiText.T("Square / X")=="Cuadrado / X" && UiText.T("Cross / A")=="Cruz / A", "Controller shapes must not use place names or swap square and cross.");
 Check(UiText.T("L3 (left stick click)")=="L3 (pulsar la palanca izquierda)" &&
       UiText.T("R3 (right stick click)")=="R3 (pulsar la palanca derecha)", "Stick presses must never become mouse clicks.");
 Check(UiText.T("D-pad up")=="D-pad arriba" && UiText.T("D-pad down")=="D-pad abajo", "Preserve D-pad and localize only the direction.");
 Check(UiText.T("Filter actions or keys")=="Filtrar acción y teclas","The requested search wording must not regress.");
}
Console.WriteLine($"PASS: {semanticChecks} context-reviewed entries/case variants, game actions, help, both Spanish variants and localized section headers.");
foreach(var id in new[]{"0","1","2","3","20","4","5","8","17"}) {
 UiText.UseTextLanguage(id);
 foreach(var label in new[]{"Options / Start","Share / Back","Mute / Share (Xbox Series)","L1 / LB","R1 / RB","L2 / LT","R2 / RT"})
  Check(UiText.T(label)==label,"Branded physical button names must stay literal in every language: "+label);
}
UiText.UseTextLanguage("1");Check(UiText.T("Settings")=="Settings","Switching back to English must clear the old catalog.");
Console.WriteLine($"PASS: {count} translations, exact language IDs, format tokens, immediate switching, paths/bindings and invariant process culture.");
