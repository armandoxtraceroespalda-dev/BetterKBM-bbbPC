// SPDX-License-Identifier: GPL-3.0-or-later
#include "bbport_settings.h"
#include <cassert>
#include <chrono>
#include <filesystem>
#include <fstream>
#include <sstream>
#include <cstdlib>
#include <cstdio>
int main(){
 using namespace BbSettings;
 const auto path=std::filesystem::temp_directory_path()/("bb-sync-"+std::to_string(std::chrono::steady_clock::now().time_since_epoch().count())+".ini");
#ifdef _WIN32
 _putenv_s("BB_CONFIG",path.string().c_str());
#else
 setenv("BB_CONFIG",path.string().c_str(),1);
#endif
 auto write=[&](const char* output,const char* provider,const char* bloom,const char* live="1"){
  std::ofstream f(path);f<<"output_res="<<output<<"\nupscaler="<<provider<<"\npreset=1\ngraphics_bloom="<<bloom<<"\nlive_resolution="<<live<<"\n# preserved\ncustom_key=yes\n";
 };
 auto read=[&]{std::ifstream f(path);std::ostringstream s;s<<f.rdbuf();return s.str();};
 write("1920x1080","fsr3","1");Load();auto& s=Get();const auto initial=s.startup_output_res;
 write("2560x1440","off","0.75");RefreshExternalSettings();assert(RenderOutputWidth()==2560&&RenderOutputHeight()==1440);assert(s.upscaler==UpscalerOff&&s.graphics_bloom==0.75f&&s.startup_output_res==initial);
 s.graphics_bloom=0.6f;write("3840x2160","dlss","0.75");Save();assert(RenderOutputWidth()==3840&&s.upscaler==UpscalerDlss&&s.graphics_bloom==0.6f);auto saved=read();assert(saved.find("graphics_bloom=0.60")!=std::string::npos&&saved.find("output_res=3840x2160")!=std::string::npos&&saved.find("custom_key=yes")!=std::string::npos&&saved.find("# preserved")!=std::string::npos);
 write("2560x1440","off","1","0");Load();write("3840x2160","off","1","0");RefreshExternalSettings();assert(RenderOutputWidth()==2560&&OutputWidth(s.output_res)==3840&&ResolutionNeedsRestart());Load();assert(RenderOutputWidth()==3840&&!ResolutionNeedsRestart());
 // Custom dimensions share an index but must still change the physical output.
 write("3000x2000","off","1");Load();write("2800x1800","off","1");RefreshExternalSettings();assert(s.output_res==OutputCustom&&RenderOutputWidth()==2800&&RenderOutputHeight()==1800);
 write("3000x2000","off","1","0");Load();write("2800x1800","off","1","0");RefreshExternalSettings();assert(RenderOutputWidth()==3000&&RenderOutputHeight()==2000&&ResolutionNeedsRestart());
 // Beta 5 settings must survive the canonical serializer and external refresh.
 {std::ofstream f(path);f<<"preset=5\nrender_scale=2.25\nmip_bias=-0.75\nframe_gen=dlss\nframe_gen_frames=3\nreflex=boost\ndlss_preset=e\nhdr_peak_auto=0\n";}
 Load();Save();auto modern=read();
 assert(modern.find("render_scale=2.25")!=std::string::npos&&modern.find("frame_gen_frames=3")!=std::string::npos);
 assert(modern.find("reflex=boost")!=std::string::npos&&modern.find("dlss_preset=e")!=std::string::npos);
 {std::ofstream f(path);f<<"render_scale=1.75\nmip_bias=-0.25\nframe_gen_frames=1\nreflex=on\n";}
 RefreshExternalSettings();assert(s.render_scale==1.75f&&s.mip_bias==-0.25f&&s.frame_gen_frames==1&&s.reflex==ReflexOn);
 std::filesystem::remove(path);std::puts("PASS: external graphics edits, pending local edits, comments, unknown keys, startup snapshots and custom output dimensions.");
}
