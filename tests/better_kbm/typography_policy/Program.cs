using BloodborneLauncher.Core;
static void Check(bool condition,string message){if(!condition)throw new Exception(message);}
Check(WindowTextScale.For(1280,800)==1,"The existing default window keeps the original fonts.");
Check(WindowTextScale.For(10000,10000)==1.3 && WindowTextScale.For(100,100)==0.7,"Scaling is bounded to ±30 percent.");
Check(WindowTextScale.For(1600,800)>1 && WindowTextScale.For(1280,1000)>1,"Either dimension can enlarge text.");
Check(WindowTextScale.For(900,620)<1,"Small supported windows reduce text size.");
foreach(var bad in new[]{double.NaN,double.PositiveInfinity,0d,-1d})
 Check(WindowTextScale.For(bad,800)==1 && WindowTextScale.For(1280,bad)==1,"Invalid or unmeasured dimensions keep the baseline.");
double last=0;
for(int width=900;width<=3840;width+=7){double s=WindowTextScale.For(width,1080);Check(s>=last && s>=0.7 && s<=1.3,"Growth is monotonic and bounded.");last=s;}
foreach(var mode in new[]{"dlss","fsr3","fsr4","fsr411","taa","off"})
 foreach(var preset in new[]{"0","1","2","3","4","5"}){
  var state=UpscalerLayout.For(mode,preset);
  Check(state.DlssVisible==(mode=="dlss"),"DLSS options appear only for DLSS.");
  Check(state.PresetEnabled==(mode is not ("off" or "taa")),"Off and native TAA do not accept upscaler quality presets.");
  Check(!state.CustomScaleVisible || state.PresetEnabled && preset=="5","The custom slider never appears for a fixed preset.");
 }
Check(UpscalerLayout.For("dlss","5").CustomScaleVisible && UpscalerLayout.For("fsr4","5").CustomScaleVisible,"Custom render scale works with NVIDIA and AMD upscalers.");
Console.WriteLine("PASS: bounded window typography and all upscaler/preset visibility transitions.");
