/* SPDX-License-Identifier: GPL-2.0-or-later */
#ifndef BB_RUNTIME_TEXT_LOCALE_H
#define BB_RUNTIME_TEXT_LOCALE_H
#include <stddef.h>
#include <string.h>
/* Preserve an explicit Spanish region if native saved/regional settings request
 * the other Spanish bundle. Do not substitute translations or touch audio. */
static inline int runtime_text_locale_path(const char *path,const char *language,char *out,size_t capacity) {
    if (!path||!language) return 0;
    const char *locale=!strcmp(language,"20")?"spaar":!strcmp(language,"3")?"spaes":NULL;
    if (!locale) return 0;
    const char *prefix="/app0/dvdroot_ps4/msg/";
    size_t offset=strlen(prefix);
    if (strncmp(path,prefix,offset)) {
        prefix="/hostapp/dvdroot_ps4/msg/";offset=strlen(prefix);
        if(strncmp(path,prefix,offset))return 0;
    }
    const char *current=path+offset;
    if(strncmp(current,"spaes/",6)&&strncmp(current,"spaar/",6))return 0;
    if(!strncmp(current,locale,5))return 0;
    const size_t n=strlen(path);
    if(n>=capacity)return -1;
    memcpy(out,path,n+1);memcpy(out+offset,locale,5);return 1;
}
#endif
