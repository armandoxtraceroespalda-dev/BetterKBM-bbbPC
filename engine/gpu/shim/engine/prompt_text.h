#pragma once
#include <string>
namespace PcPrompts {
inline std::u16string wide(const std::string& s) {
 std::u16string out;
 for(std::size_t i=0;i<s.size();) {
  unsigned c=static_cast<unsigned char>(s[i++]);unsigned need=0;
  if(c>=0xf0){c&=7;need=3;}else if(c>=0xe0){c&=15;need=2;}else if(c>=0xc0){c&=31;need=1;}
  while(need-- && i<s.size())c=(c<<6)|(static_cast<unsigned char>(s[i++])&63);
  if(c>0xffff){c-=0x10000;out.push_back(char16_t(0xd800+(c>>10)));out.push_back(char16_t(0xdc00+(c&1023)));}
  else out.push_back(char16_t(c));
 }
 return out;
}
}
