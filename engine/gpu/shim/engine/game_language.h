// SPDX-License-Identifier: GPL-3.0-or-later
#pragma once
#include <array>
#include <cstddef>
#include <cstdint>
#include <cstring>
#include <cstdlib>
namespace GameLanguage {
struct Site {std::size_t at;std::array<std::uint8_t,5> expected;};
// Read the voice property consistently and replace the subsequent regional conversion.
// Explicit voice selection must not be folded into a different regional track.
inline constexpr Site sites[] = {
    {0x1fb6ee4,{0xe8,0x17,0x4b,0x13,0x00}},
    {0x2001b85,{0xe8,0x76,0x9e,0x0e,0x00}},
    {0x1fb6f08,{0xe8,0xd3,0x28,0x00,0x00}},
};
inline int Parse(const char* text) {
    if(!text||!*text)return -1;
    char* end=nullptr;const long value=std::strtol(text,&end,10);
    if(*end)return -1;
    switch(value){case 0:case 1:case 2:case 3:case 4:case 5:case 17:case 20:return int(value);default:return -1;}
}
// Platform language IDs are not the game's internal sound-language indexes.
// These indexes select jajp, engb, frfr, itit, dede, eses, esar and ptbr.
inline int VoiceIndex(int language) {
    switch(language) {
        case 0:return 0;case 1:return 1;case 2:return 2;case 3:return 5;
        case 4:return 4;case 5:return 3;case 17:return 7;case 20:return 6;
        default:return -1;
    }
}
inline const char* TrackName(int language) {
    static constexpr const char* tracks[]={"jajp","engb","frfr","itit","dede","eses","esar","ptbr"};
    const int index=VoiceIndex(language);return index>=0?tracks[index]:"unavailable";
}
template<class Writer>
bool Apply(std::uint8_t* image,std::size_t size,int language,Writer write) {
    const int voice=VoiceIndex(language);
    if(voice<0)return false;
    for(const auto& site:sites) {
        if(site.at>size||site.expected.size()>size-site.at||
           std::memcmp(image+site.at,site.expected.data(),site.expected.size()))return false;
    }
    // mov eax, voice replaces the property results and the final voice conversion.
    // Text, subtitles and item descriptions keep their independent platform language.
    const std::array<std::uint8_t,5> replacement={0xb8,std::uint8_t(voice),0,0,0};
    std::size_t committed=0;
    for(const auto& site:sites) {
        if(!write(image+site.at,replacement.data(),replacement.size())) {
            // Restore every attempted site, including a writer's partial failure.
            for(std::size_t i=0;i<=committed;++i)
                write(image+sites[i].at,sites[i].expected.data(),sites[i].expected.size());
            return false;
        }
        ++committed;
    }
    return true;
}
}
