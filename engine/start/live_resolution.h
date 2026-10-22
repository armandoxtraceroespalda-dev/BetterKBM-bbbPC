// SPDX-License-Identifier: GPL-3.0-or-later
#pragma once
#include <string>
namespace start{
struct LiveResolutionSelection{bool enabled;std::string source;};
// Resolve generated values again on restart; retain explicit process overrides.
template<class Probe>LiveResolutionSelection ResolveLiveResolution(std::string setting,std::string value,const std::string& previous,Probe probe){
 if(previous=="settings")value.clear();else if(previous=="auto-override")value="auto";
 const std::string source=value.empty()?"settings":value=="auto"?"auto-override":"";
 if(value.empty())value=setting;if(value=="auto")value=probe();return{value=="1",source};
}
}
