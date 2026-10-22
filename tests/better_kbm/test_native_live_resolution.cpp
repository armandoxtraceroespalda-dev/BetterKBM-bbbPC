// SPDX-License-Identifier: GPL-3.0-or-later
#include "live_resolution.h"
#include <cassert>
#include <cstdio>
int main(){
 int calls=0;std::string capability="1";auto probe=[&]{++calls;return capability;};using start::ResolveLiveResolution;
 for(auto value:{"0","1"}){auto r=ResolveLiveResolution(value,"","",probe);assert(r.enabled==(value[0]=='1')&&r.source=="settings");}assert(calls==0);
 auto r=ResolveLiveResolution("auto","","",probe);assert(r.enabled&&calls==1);capability="0";r=ResolveLiveResolution("auto","","",probe);assert(!r.enabled&&calls==2);
 assert(ResolveLiveResolution("0","1","",probe).enabled);assert(!ResolveLiveResolution("1","0","",probe).enabled);
 assert(ResolveLiveResolution("1","0","settings",probe).enabled);capability="1";r=ResolveLiveResolution("0","0","auto-override",probe);assert(r.enabled&&calls==3&&r.source=="auto-override");assert(!ResolveLiveResolution("invalid","","",probe).enabled);
 std::puts("PASS: native On/Off/Auto, explicit overrides and inherited restart values.");
}
