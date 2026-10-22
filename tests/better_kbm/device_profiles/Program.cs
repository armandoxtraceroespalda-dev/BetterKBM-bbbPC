using BloodborneLauncher.Core;
using System.Text.Json.Nodes;

static void Check(bool value,string why){if(!value)throw new Exception(why);}
try
{
    Directory.CreateDirectory(Paths.Root);
    var ini=new IniFile();ini.Load();var settings=new LauncherSettings();
    Check(ControlProfiles.BuiltIns.Length==2,"Only DS3 PC and Controller remain built in.");
    var ds3=ControlProfiles.BuiltIns.Single(p=>p.InputDevice=="keyboard_mouse");
    var controller=ControlProfiles.BuiltIns.Single(p=>p.InputDevice=="controller");
    PadMap.Assign(ini,"circle","lefttrigger");var padBefore=PadMap.Current(ini);
    ControlProfiles.Apply(settings,ini,ds3);
    Check(PadMap.Current(ini).OrderBy(p=>p.Key).SequenceEqual(padBefore.OrderBy(p=>p.Key)),"KBM profile changed controller assignments.");
    Check(ini.Get("bind.switch_right")=="Left Shift + WheelUp" && ini.Get("bind.switch_left")=="Left Shift + WheelDown","Weapon wheel chords were lost.");
    Check(ini.Get("bind.switch_item")=="WheelDown" && ini.Get("bind.dpad_up")=="T","DS3 item wheel or Blood Bullets defaults were lost.");
    var keysBefore=ControlProfiles.Current(ini);
    ControlProfiles.Apply(settings,ini,controller);
    Check(ControlProfiles.Current(ini).OrderBy(p=>p.Key).SequenceEqual(keysBefore.OrderBy(p=>p.Key)),"Controller reset changed keyboard assignments.");
    Check(PadMap.IsDefault(ini,"circle") && ini.Get("input_mode")=="controller","Controller defaults did not apply.");
    var keyboardSave=ds3 with{Name="Shared name",BuiltIn=false};
    var padSave=controller with{Name="Shared name",BuiltIn=false,Pad=padBefore};
    string kfile=ControlProfiles.Save(keyboardSave),pfile=ControlProfiles.Save(padSave);
    Check(kfile!=pfile,"Equal profile names must not overwrite another device.");
    Check((string?)JsonNode.Parse(File.ReadAllText(kfile))!["input_device"]=="keyboard_mouse","Keyboard file has no device filter.");
    Check((string?)JsonNode.Parse(File.ReadAllText(pfile))!["input_device"]=="controller","Controller file has no device filter.");
    Check(ControlProfiles.All("keyboard_mouse").Count==2 && ControlProfiles.All("controller").Count==2,"Per-device discovery leaked built-ins or saved profiles.");
    var parsed=ControlProfiles.Parse(File.ReadAllText(pfile));
    Check(parsed.DeviceExplicit && parsed.Profile.InputDevice=="controller" && parsed.Profile.Bindings.Count==0,"Controller roundtrip populated keyboard defaults.");
    ControlProfiles.Apply(settings,ini,parsed.Profile);
    Check(PadMap.Main(ini,"circle")=="lefttrigger","Controller profile failed to restore assignments.");
    Check(ControlProfiles.Current(ini).OrderBy(p=>p.Key).SequenceEqual(keysBefore.OrderBy(p=>p.Key)),"Controller roundtrip rewrote keyboard bindings.");
    var legacy=JsonNode.Parse(ControlProfiles.ToJson(keyboardSave))!.AsObject();legacy.Remove("input_device");legacy["version"]=2;
    var old=ControlProfiles.Parse(legacy.ToJsonString());
    Check(!old.DeviceExplicit && old.Profile.InputDevice=="keyboard_mouse","Legacy keyboard profiles stopped loading.");
    legacy["input_device"]="unsupported";
    try{ControlProfiles.Parse(legacy.ToJsonString());throw new Exception("An unknown profile device was accepted.");}catch(FormatException){}

    string directory=Path.Combine(Paths.EngineDir,"patches");Directory.CreateDirectory(directory);
    File.WriteAllText(Path.Combine(directory,"Bloodborne.xml"),"""
    <Patch><ID>CUSA03173</ID>
      <Metadata Name="Resolution Patch 1280x720 (16:9)" AppVer="01.09" AppElf="eboot.bin"><Line Address="1000" Type="bytes" Value="AA" /></Metadata>
      <Metadata Name="Example patch (READ NOTES)" Author="Original author" Note="Important context" AppVer="01.09" AppElf="eboot.bin"><Line Address="1000" Type="bytes" Value="BB" /><Line Address="23b9190" Type="bytes" Value="01" /></Metadata>
    </Patch>
    """);
    foreach(var locale in new[]{"1","0","2","3","20","4","5","8","17"})
    {
        UiText.UseTextLanguage(locale);
        var patch=Patches.Discover(directory).Single();
        Check(patch.Name=="Example patch (READ NOTES)" && patch.Author=="Original author","XML field names changed with locale "+locale);
        Check(patch.Key=="Bloodborne.xml/Example patch (READ NOTES)","Persistent identity changed with locale.");
        Check(patch.SetsResolution && patch.ChangesRegion,"Patch footprint detection lost raw XML identifiers.");
        Check(PatchTitles.Display(patch.Name,UiText.T)=="Example patch","Read-notes suffix remains in visible title.");
        Patches.Save(new[]{(Patch:patch,On:true)});
        Check(Patches.LoadProfile().Enabled.Contains(patch.Key),"Display cleanup changed patch selections.");
    }
    foreach(string suffix in new[]{"READ NOTE","read notes","Leer las notas","LIRE LES NOTES","Hinweise lesen","Leggi le note","Leia as notas","см. примечания","注記を読む","阅读说明"})
        Check(PatchTitles.Clean("Patch ("+suffix+")")=="Patch","Suffix not cleaned: "+suffix);
    Check(PatchTitles.Clean("Patch (16:9)")=="Patch (16:9)","Meaningful parenthetical text was removed.");
    Console.WriteLine("PASS: production profiles isolate devices, serialize/filter correctly, preserve legacy/chords; XML names/IDs and notes in nine locales.");
}
finally { if(Directory.Exists(Paths.Root))Directory.Delete(Paths.Root,true); }

Check(InputMap.Canonical("Shift+Q")=="Shift+Q" && InputMap.Canonical("Ctrl+Mouse4")=="Ctrl+Mouse4", "Compact upstream modifiers remain valid native tokens.");
