/* SPDX-License-Identifier: GPL-2.0-or-later */
#ifndef BB_RUNTIME_VOICE_BANKS_H
#define BB_RUNTIME_VOICE_BANKS_H
#include <stddef.h>
#include <string.h>
#include <stdio.h>

/* FMOD's three-letter FSB suffixes differ from the game's four-letter locales. */
static inline const char *runtime_voice_bank_suffix(const char *language) {
    if (!language) return NULL;
    if (!strcmp(language,"0")) return "jaj";
    if (!strcmp(language,"1")) return "eng";
    if (!strcmp(language,"2")) return "frf";
    if (!strcmp(language,"3")) return "ese";
    if (!strcmp(language,"4")) return "ded";
    if (!strcmp(language,"5")) return "iti";
    if (!strcmp(language,"17")) return "ptb";
    if (!strcmp(language,"20")) return "esa";
    return NULL;
}
/* Resolve only localized game sound banks, preserving base banks and save paths.
 * Return 1 for a changed path, 0 for an unchanged path, -1 for insufficient space. */
static inline int runtime_voice_bank_path(const char *path,const char *language,char *out,size_t capacity) {
    const char *suffix=runtime_voice_bank_suffix(language);
    const char *app="/app0/dvdroot_ps4/sound/", *host="/hostapp/dvdroot_ps4/sound/";
    if (!path || !suffix || (strncmp(path,app,strlen(app)) && strncmp(path,host,strlen(host)))) return 0;
    const size_t n=strlen(path);
    if (n<8 || strcmp(path+n-4,".fsb") || path[n-8]!='_') return 0;
    static const char *const known[]={"jaj","eng","frf","ese","ded","iti","ptb","esa"};
    int localized=0;
    for(size_t i=0;i<sizeof(known)/sizeof(*known);++i)if(!strncmp(path+n-7,known[i],3))localized=1;
    if (!localized || !strncmp(path+n-7,suffix,3)) return 0;
    if (n>=capacity) return -1;
    memcpy(out,path,n+1);memcpy(out+n-7,suffix,3);
    return 1;
}
#endif
