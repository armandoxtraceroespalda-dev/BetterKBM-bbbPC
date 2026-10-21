#include <stdint.h>
#include <stdbool.h>
#include <stdlib.h>
#include <stdio.h>
static int opened, initialized;
static bool pressed=true;
static int16_t stick_axis;
void SDL_TestSetButton(bool value){pressed=value;}
void SDL_TestSetAxis(int16_t value){stick_axis=value;}
static void record(const char *s) { const char *p=getenv("SDL_LIFETIME_LOG");if(!p)return;FILE *f=fopen(p,"a");if(f){fprintf(f,"%s\n",s);fclose(f);} }
__attribute__((constructor))static void load(void){record("load");}
__attribute__((destructor))static void unload(void){record(opened||initialized?"UNSAFE_UNLOAD":"unload");}
bool SDL_SetHint(const char *a,const char *b){return true;}
bool SDL_InitSubSystem(uint32_t f){initialized=1;record("init");return true;}
void SDL_QuitSubSystem(uint32_t f){initialized=0;record("quit-subsystem");}
void SDL_Quit(void){record(opened?"OPEN_AT_SHUTDOWN":"quit");initialized=0;}
uint32_t *SDL_GetGamepads(int *count){*count=1;uint32_t *p=malloc(sizeof(uint32_t));*p=7;return p;}
uint32_t *SDL_GetJoysticks(int *count){return SDL_GetGamepads(count);}
void SDL_free(void *p){free(p);}
void *SDL_OpenGamepad(uint32_t id){opened=1;record("open");return (void*)(uintptr_t)id;}
const char *SDL_GetGamepadNameForID(uint32_t id){return "Fixture pad";}
const char *SDL_GetJoystickNameForID(uint32_t id){return "Fixture joystick";}
bool SDL_IsGamepad(uint32_t id){return true;}
void SDL_UpdateGamepads(void){}
bool SDL_GetGamepadButton(void *pad,int button){return button==0 && pressed;}
int16_t SDL_GetGamepadAxis(void *pad,int axis){return axis==4?20000:axis==0?stick_axis:0;}
#ifndef OMIT_EXPORT
const char *SDL_GetGamepadStringForButton(int button){return "a";}
#endif
void SDL_CloseGamepad(void *pad){opened=0;record("close");}
