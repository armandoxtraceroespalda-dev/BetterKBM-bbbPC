using System.Reflection;
using BloodborneLauncher.Core;

var endonyms=new[]{"English","Русский","日本語","Français","Español (España)","Español (América)","Deutsch","Italiano","Português (Brasil)"};
foreach(var id in new[]{"1","8","0","2","3","20","4","5","17"}) {
 UiText.UseTextLanguage(id);
 if(!Choices.GameLanguages.Select(c=>c.Label).SequenceEqual(endonyms))throw new Exception("Language names must remain endonyms in every interface language.");
 foreach(var name in new[]{"Default","Display","Game","NVIDIA GeForce RTX 4090","日本語 Display"}) {
  var device=Choice.Literal(name,"device-id");
  if(device.Label!=name||device.Value!="device-id")throw new Exception("Raw device identity was translated.");
 }
 if(Choices.VoiceLanguages.Any(c=>c.Value=="8")||Choices.GameLanguages.Length!=9)throw new Exception("Existing voice/text ID support must not change.");
}
UiText.UseTextLanguage("1");
var root=Path.Combine(Path.GetTempPath(),"better-kbm-recommend-"+Guid.NewGuid());
Directory.CreateDirectory(root);Paths.PortDir=root;
var variables=new[]{"BB_DLSS_DIR","BB_FSR4_DIR","BB_FSR4_OPT","BB_FSR4_BALANCED"};
var original=variables.ToDictionary(key=>key,Environment.GetEnvironmentVariable);
foreach(var key in variables)Environment.SetEnvironmentVariable(key,null);
int checks=0;
void Require(bool condition,string message){checks++;if(!condition)throw new Exception(message);}
Hardware Card(string name,string vendor,int gb=24,int width=2560,int height=1440)=>
    new(name,vendor,(long)gb<<30,"Ryzen",16,32L<<30,width,height,144);
var settings=new LauncherSettings();
var ini=new IniFile();ini.Set("key.cross","E");ini.Set("gpu_id","1");ini.Set("custom.setting","keep");
UpscalerRecommendation Recommend(Hardware card)=>
    UpscalerRecommendation.For(card,settings,card.ScreenWidth,card.ScreenHeight,1);
void FileData(string path,byte[]? bytes=null){Directory.CreateDirectory(Path.GetDirectoryName(path)!);File.WriteAllBytes(path,bytes??new byte[]{1});}
byte[] Shader()=>new byte[]{3,2,35,7,0,0,1,0,0,0,0,0,1,0,0,0,0,0,0,0};
void Model(string directory,string model="quality",string tier="2160"){
    string prefix="fsr4_model_v07_i8_"+model;
    foreach(var name in new[]{"rcas.spv","spd_auto_exposure.spv",$"{prefix}_{tier}_pre.spv",$"{prefix}_{tier}_post.spv"})FileData(Path.Combine(directory,name),Shader());
    for(int pass=1;pass<=12;pass++)FileData(Path.Combine(directory,$"{prefix}_{tier}_pass{pass}.spv"),Shader());
    FileData(Path.Combine(directory,prefix+"_initializers.bin"));FileData(Path.Combine(directory,prefix+"_pre_weights.bin"));
}
try{
    var rtx=Card("NVIDIA GeForce RTX 4090","NVIDIA");
    var amd=Card("AMD Radeon RX 9070 XT","AMD",16);
    Require(Recommend(rtx).Value=="fsr3","Missing DLSS must fall back.");
    var dlss=Path.Combine(Paths.BinariesDir,"nvngx_dlss.dll");FileData(dlss,Array.Empty<byte>());
    Require(Recommend(rtx).Value=="fsr3","An empty runtime must not be recommended.");
    FileData(dlss);
    Require(Recommend(rtx).Value=="dlss","RTX 4090 must prefer installed DLSS.");
    Require(Recommend(Card("NVIDIA GeForce RTX 3090","NVIDIA")).Value=="dlss","RTX 3090 must prefer DLSS.");
    Require(Recommend(Card("NVIDIA GeForce RTX 2060","NVIDIA",6)).Value=="dlss","Super Resolution is not restricted to high-VRAM RTX cards.");
    foreach(var card in new[]{Card("NVIDIA GeForce GTX 1080 Ti","NVIDIA",11),Card("Intel Arc A770","Intel",16),Card("Intel UHD Graphics","Intel",1),Card("AMD Radeon RX 7800 XT","AMD",16),Card("AMD Radeon(TM) Graphics","AMD",1),Card("Unknown GPU","",0)})
        Require(Recommend(card).Value=="fsr3","Conservative fallback for "+card.Gpu);
    Require(Recommend(amd).Value=="fsr3","An empty FSR4 directory must not be enough.");
    var assets=Path.Combine(root,"fsr4_shaders");Directory.CreateDirectory(assets);
    Require(Recommend(amd).Value=="fsr3","A directory without the selected model is incomplete.");
    Model(assets);
    Require(Recommend(amd).Value=="fsr4","Complete RX 9000 model should recommend FSR4.");
    var pass7=Path.Combine(assets,"fsr4_model_v07_i8_quality_2160_pass7.spv");
    FileData(pass7,new byte[20]);Require(Recommend(amd).Value=="fsr3","Invalid shader magic must reject the model.");
    FileData(pass7,Shader());
    var optimized=Path.Combine(assets,"opt",Path.GetFileName(pass7));FileData(optimized,new byte[20]);
    Require(Recommend(amd).Value=="fsr3","A broken preferred optimized pass cannot be bypassed.");
    Environment.SetEnvironmentVariable("BB_FSR4_OPT","0");
    Require(Recommend(amd).Value=="fsr4","Disabled optimization must use the valid base model.");
    Environment.SetEnvironmentVariable("BB_FSR4_OPT",null);File.Delete(optimized);
    Require(Recommend(amd with{ScreenWidth=1920,ScreenHeight=1080}).Value=="fsr3","The other resolution tier must not count as installed.");
    Model(assets,"quality","1080");
    Require(Recommend(amd with{ScreenWidth=1920,ScreenHeight=1080}).Value=="fsr4","The matching 1080 model must be used.");
    var customAssets=Path.Combine(root,"custom-models");Model(customAssets);
    Environment.SetEnvironmentVariable("BB_FSR4_DIR","missing");
    Require(Recommend(amd).Value=="fsr3","An override supersedes the installed default model.");
    settings.Set("extra_env","BB_FSR4_DIR=custom-models");
    Require(Recommend(amd).Value=="fsr4","Launcher overrides must win and relative paths resolve beside the engine.");
    settings.Set("extra_env","");Environment.SetEnvironmentVariable("BB_FSR4_DIR",null);
    File.Delete(dlss);FileData(Path.Combine(root,"custom-dlss","nvngx_dlss.dll"));
    Environment.SetEnvironmentVariable("BB_DLSS_DIR","custom-dlss");
    Require(Recommend(rtx).Value=="dlss","Custom DLSS directory must work.");
    settings.Set("extra_env","BB_DLSS_DIR=missing");
    Require(Recommend(rtx).Value=="fsr3","Missing overriding runtime must be noticed.");
    settings.Set("extra_env","");Environment.SetEnvironmentVariable("BB_DLSS_DIR",null);FileData(dlss);
    var changes=rtx.Recommend(settings,ini,out var info);
    Require(info.Value=="dlss" && info.Label.Contains("DLSS"),"The dialog must receive the chosen upscaler even without an edit.");
    Require(changes.Any(c=>c.Label=="Upscaler"),"The proposed changes must include DLSS.");
    Require(ini.Get("upscaler","")=="","Preparing a recommendation must not change settings.");
    foreach(var change in changes)change.Apply();
    Require(ini.Get("upscaler")=="dlss" && ini.Get("preset")=="1","Apply must install the computed upscaler and quality.");
    Require(ini.Get("key.cross")=="E" && ini.Get("gpu_id")=="1" && ini.Get("custom.setting")=="keep","Recommendation must preserve unrelated values.");
    Require(!rtx.Recommend(settings,ini,out info).Any(c=>c.Label=="Upscaler") && info.Value=="dlss","No-op recommendation still identifies DLSS.");
    var native=new IniFile();foreach(var change in (rtx with{ScreenWidth=1920,ScreenHeight=1080}).Recommend(settings,native))change.Apply();
    Require(native.Get("preset","0")=="0" && native.Get("upscaler")=="dlss","High-tier native 1080p uses DLAA through the DLSS Native AA preset.");
    var fourK=new IniFile();foreach(var change in (rtx with{ScreenWidth=3840,ScreenHeight=2160}).Recommend(settings,fourK))change.Apply();
    Require(fourK.Get("preset")=="2","4K must adapt quality independently of the provider choice.");
    settings.Set("fsr4_balanced",true);
    Require(UpscalerRecommendation.For(amd,settings,2560,1440,3).Value=="fsr3","Balanced-low toggle must check the balanced model.");
    Model(assets,"balanced");
    Require(UpscalerRecommendation.For(amd,settings,2560,1440,3).Value=="fsr4","Installed balanced-low model must work.");
    settings.Set("extra_env","BB_FSR4_BALANCED=0");
    Require(UpscalerRecommendation.For(amd,settings,2560,1440,3).Value=="fsr3","Explicit game environment must override balanced-low setting.");
    settings.Set("extra_env","");
    // Seed the real discovery caches so the cross-platform test does not load Windows drivers.
    typeof(Hardware).GetField("_detected",BindingFlags.NonPublic|BindingFlags.Static)!.SetValue(null,rtx);
    var adapters=new[]{new GpuAdapter(0,"NVIDIA GeForce RTX 3090","NVIDIA",24L<<30,2,0x00403000),new GpuAdapter(1,"NVIDIA GeForce RTX 4090","NVIDIA",24L<<30,2,0x00403000),new GpuAdapter(2,"AMD Radeon RX 9070 XT","AMD",32L<<30,2,0x00403000)};
    typeof(GpuAdapters).GetField("cache",BindingFlags.NonPublic|BindingFlags.Static)!.SetValue(null,adapters);
    Workspace.Current.Ini.Set("gpu_name",adapters[1].Name);
    Require(Hardware.Detect().Gpu==adapters[1].Name && Recommend(Hardware.Detect()).Value=="dlss","Explicit 4090 selection must beat a larger-memory AMD card.");
    Workspace.Current.Ini.Set("gpu_name",adapters[2].Name);
    Require(Hardware.Detect().Vendor=="AMD" && Recommend(Hardware.Detect()).Value=="fsr4","Changing selected GPU must change the recommendation without a restart.");
    Workspace.Current.Ini.Set("gpu_name","");Workspace.Current.Ini.Set("gpu_id","0");
    Require(Hardware.Detect().Gpu==adapters[0].Name,"Numeric Vulkan selection must also be respected.");
    Workspace.Current.Ini.Set("gpu_id","-1");
    Require(Hardware.Detect().Gpu==adapters[2].Name,"Automatic selection must follow engine adapter ordering.");
    foreach(var locale in new[]{"en","es-MX","ru","ja","fr","de","it","pt-BR"}){
        UiText.UseTextLanguage(locale switch{"es-MX"=>"20","ru"=>"8","ja"=>"0","fr"=>"2","de"=>"4","it"=>"5","pt-BR"=>"17",_=>"1"});
        Require(PcText.Get(info.Reason).Length>0 && PcText.Get("Recommended upscaler").Length>0,"Recommendation translations must exist.");
        Require(!PcText.Get("Synchronizes frames with the display to prevent image tearing. The selected mode affects latency and how late frames are displayed.").Contains("Anti-aliasing"),"Vsync help must describe only synchronization.");
    }
    Console.WriteLine($"PASS: {checks} hardware/selected-GPU, runtime/model, proposal/apply and localization checks.");
}finally{
    foreach(var item in original)Environment.SetEnvironmentVariable(item.Key,item.Value);
    Directory.Delete(root,true);
}

namespace BloodborneLauncher.Core{
    public static class Paths{
        public static string PortDir{get;set;}="";
        public static string BinariesDir=>Path.Combine(PortDir,"out");
        public static string IniPath=>Path.Combine(PortDir,"bbport.ini");
        public static string ConfigDir=>PortDir;
        public static string ConfigFile=>Path.Combine(PortDir,"settings.json");
    }
    public sealed class Workspace{
        public static Workspace Current{get;}=new();
        public LauncherSettings Settings{get;}=new();
        public IniFile Ini{get;}=new();
    }
    // Platform-only password/update services are unrelated to the recommendation under test.
    public static class Secret{public static string Protect(string s)=>s;public static string Unprotect(string s)=>s;public static bool IsProtected(string s)=>true;}
    public static class Updater{public const int CurrentBeta=0;}
}
