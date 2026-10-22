// SPDX-License-Identifier: GPL-3.0-or-later
#pragma once
#include <algorithm>
#include <cstdint>
namespace SharedCamera {
struct Values {int sensitivity;bool invert_x,invert_y,auto_adjust;};
enum Field : unsigned {Sensitivity=1,InvertX=2,InvertY=4,AutoAdjust=8,All=15};
// A closed parent dialog can outlive a newer loaded record. Only publish fields
// actually edited in that dialog, rather than replaying its entire old snapshot.
struct RecordChanges {
 Values previous{};bool initialized=false;
 unsigned Observe(const Values& v){
  unsigned changed=All;
  if(initialized)changed=(v.sensitivity!=previous.sensitivity?Sensitivity:0)|
   (v.invert_x!=previous.invert_x?InvertX:0)|(v.invert_y!=previous.invert_y?InvertY:0)|
   (v.auto_adjust!=previous.auto_adjust?AutoAdjust:0);
  previous=v;initialized=true;return changed;
 }
};
// The record copier also receives stack-local factory defaults. Those records
// must never replace the live camera settings used by mouse input.
template<class Reader>
std::uint64_t LiveSource(std::uint64_t slide,Reader read){
 std::uint64_t manager=0,options=0;
 if(!read(slide+0x553b130,&manager,sizeof manager)||!manager||
  !read(manager+0x48,&options,sizeof options))return 0;
 return options;
}
template<class Reader>
bool IsLiveSource(std::uint64_t source,std::uint64_t slide,Reader read){
 return source&&source==LiveSource(slide,read);
}
// Verified original menu record: sensitivity +0, X +9, Y +10,
// auto-target +11, auto-adjust +12. Axis choice 1=normal, 0=inverted.
inline bool DecodeMenu(const std::uint8_t* b,Values& v){
 if(b[0]>10||b[9]>1||b[10]>1||b[12]>1)return false;
 v={b[0],b[9]==0,b[10]==0,b[12]!=0};return true;
}
// Serialized game options contain an additional byte before these flags.
inline bool DecodeGame(const std::uint8_t* b,Values& v){
 if(b[0]>10||b[10]>1||b[11]>1||b[13]>1)return false;
 v={b[0],b[10]==0,b[11]==0,b[13]!=0};return true;
}
}
