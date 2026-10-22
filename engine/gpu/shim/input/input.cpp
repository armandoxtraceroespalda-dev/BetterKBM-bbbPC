// SPDX-License-Identifier: GPL-3.0-or-later
// SDL/Pad bridge for bbhost's native engine mouse camera and menu hit testing.
#include "input.h"
#include "compat.h"
#include "bindings.h"
#include "menu_keyboard_policy.h"
#include "chords.h"
#include "wheel_buttons.h"
#include <array>
#include <chrono>
#include <cmath>
#include "pc_controls.h"
#include "../engine/item_cycle.h"
#include "ini_bindings.h"
#include "mouse_camera_step.h"
#include "../bbport_settings.h"
namespace BbOverlay {bool CapturesInput();bool MenuOpen();}
#include "../engine/option_menu.h"
#include <SDL3/SDL.h>
#include <algorithm>
#include <atomic>
#include <cstdarg>
#include <cstdio>
#include <cstring>
#include <mutex>

extern "C" uint64_t runtime_pad_bindings_serial();
extern "C" int runtime_pad_export_binding(int,char*,size_t,char*,size_t);
namespace {
std::atomic<bool> active{false},relative{false},wanted_relative{false},text_open{false},focused{true};
std::atomic<InputDevice> device{InputDevice::KeyboardMouse};
std::mutex state_lock;
PadState snapshot;
float mouse_x=0,mouse_y=0;
int window_w=1920,window_h=1080;
uint32_t mouse_buttons=0,mouse_edges=0;
float wheel=0;
std::array<bool,512> event_keys{};
WheelButtons::Queue weapon_wheel;
std::atomic<bool> in_window{true}, keyboard_moving{false};
ElfImage image{};
uint64_t saved_serial=0,saved_pad_serial=0;
bool previous_item_was=false;
}
void host_log(const char* fmt,...) {
    std::fputs("Input: ",stdout);va_list a;va_start(a,fmt);std::vprintf(fmt,a);va_end(a);std::putchar('\n');
}
bool input_prologue_hook(ElfImage* i,uint64_t at,const uint8_t* bytes,size_t n,BbEngine::Callback fn) {
    BbEngine::Hook hook;
    if(at<i->mem.slide || !BbEngine::Prepare(hook,reinterpret_cast<uint8_t*>(i->mem.slide),
        i->mem.size,at-i->mem.slide,std::span<const uint8_t>(bytes,n),fn))return false;
    BbEngine::Commit(std::span<BbEngine::Hook>(&hook,1));return true;
}
HostSettings host_settings() {return {BbSettings::Get().mouse_menu.load(),BbSettings::Get().menu_move.load()};}
std::uint32_t input_delivered_buttons() {return host_pad_state().buttons;}
InputDevice host_input_device() {return device.load();}
PadState host_pad_state() {std::lock_guard lock(state_lock);return snapshot;}
bool host_keyboard_moving() {return keyboard_moving.load(std::memory_order_relaxed);}
bool host_options_open() {return BbOverlay::MenuOpen();}
bool ingame_menu_open() {return false;} // bbhost's separate host menu is not installed here.
bool host_text_entry_open() {return text_open.load();}
bool debug_menu_open() {return false;}
bool debug_menu_active() {return false;} // No dedicated native debug-menu controller in this port.
bool host_mouse_relative() {return relative.load();}
bool host_mouse_stage_position(float& x,float& y) {
    std::lock_guard lock(state_lock);
    if(!in_window || !focused || relative || window_w<=0 || window_h<=0)return false;
    // Presenter fits the output; Scaleform fits its native 1920x1080 stage.
    const auto& s=BbSettings::Get();
    const int idx=std::clamp(BbSettings::ResolutionNeedsRestart()?s.startup_output_res:s.output_res.load(),0,BbSettings::OutputCount-1);
    const float ow=float(BbSettings::OutputWidth(idx)),oh=float(BbSettings::OutputHeight(idx));
    const float present=std::min(window_w/ow,window_h/oh);
    if(!s.widescreen){
        x=(mouse_x-(window_w-ow*present)/2)/present*1920/ow;
        y=(mouse_y-(window_h-oh*present)/2)/present*1080/oh;return true;
    }
    const float ui=std::min(ow/1920,oh/1080);
    const float sx=(window_w-ow*present)/2+(ow-1920*ui)*present/2;
    const float sy=(window_h-oh*present)/2+(oh-1080*ui)*present/2;
    x=(mouse_x-sx)/(present*ui);y=(mouse_y-sy)/(present*ui);return true;
}
void host_mouse_describe(char* out,size_t n) {
    std::lock_guard lock(state_lock);std::snprintf(out,n,"pointer %.0f,%.0f window %dx%d",mouse_x,mouse_y,window_w,window_h);
}
bool option_menu_click_decides(uint8_t* c,int row) {return BbEngine::Options::ClickDecides(c,row);}
void option_menu_list_update(uint8_t* c,bool focus) {BbEngine::Options::ListUpdate(c,focus);}

namespace BbInput {
bool Enabled(){return active && BbSettings::Get().pc_controls;}
bool Install(uint8_t* base,size_t size) {
    if(!BbSettings::Get().pc_controls)return false;
    image={{reinterpret_cast<uint64_t>(base),size},kEboot109Sha256};
    host_bindings_load_defaults();
    if(FILE* f=std::fopen((std::getenv("BB_CONFIG")?std::getenv("BB_CONFIG"):"lantern.ini"),"r")) {
        char line[512];
        while(std::fgets(line,sizeof line,f)) {
            char* eq=std::strchr(line,'=');if(!eq)continue;*eq++=0;
            char* end=line+std::strlen(line);while(end>line && (end[-1]==' '||end[-1]=='\t'))*--end=0;
            if(!std::strncmp(line,"bind.",5)){eq[std::strcspn(eq,"\r\n")]=0;host_bindings_parse(line+5,eq);}
        }
        std::fclose(f);
    }
    BbInputConfig::append_bindings=[](BbInputConfig::Items& items) {for(int a=0;a<kBindDebugMenu;a++)items.emplace_back(std::string("bind.")+host_binding_info(a).key,host_binding_ini_value(a));for(int i=0;i<19;++i){char name[64],value[256];if(runtime_pad_export_binding(i,name,sizeof name,value,sizeof value))items.emplace_back(std::string("pad.")+name,value);}};
    saved_serial=host_bindings_serial();
    mouse_camera_install(&image);menu_pointer_install(&image);BbEngine::ItemCycle::Install(base,size);
    active=true;
    host_log("keyboard/mouse controls enabled, camera hook %s",mouse_camera_installed()?"ready":"unavailable");
    return true;
}
void Tick() {
    if(!active)return;
    if(host_bindings_serial()!=saved_serial || runtime_pad_bindings_serial()!=saved_pad_serial){BbSettings::Save();saved_serial=host_bindings_serial();saved_pad_serial=runtime_pad_bindings_serial();}
}
void Event(const SDL_Event& e,bool blocked) {
    if(!active)return;
    if(e.type==SDL_EVENT_WINDOW_FOCUS_LOST) {host_bindings_reset_key_events();keyboard_moving=false;focused=false;wanted_relative=false;mouse_camera::g_counts=0;
        std::lock_guard lock(state_lock);mouse_buttons=mouse_edges=0;wheel=0;event_keys.fill(false);weapon_wheel.Reset();return;}
    if(e.type==SDL_EVENT_WINDOW_FOCUS_GAINED)focused=true;
    if(e.type==SDL_EVENT_WINDOW_MOUSE_LEAVE)in_window=false;
    if(e.type==SDL_EVENT_WINDOW_MOUSE_ENTER)in_window=true;
    if(!BbSettings::Get().keyboard_mouse && (e.type==SDL_EVENT_GAMEPAD_BUTTON_DOWN ||
        (e.type==SDL_EVENT_GAMEPAD_AXIS_MOTION && std::abs(e.gaxis.value)>8000)))device=InputDevice::Pad;
    if(NativePages::ControllerCapturing()){
        if(e.type==SDL_EVENT_KEY_DOWN&&e.key.scancode==SDL_SCANCODE_ESCAPE)NativePages::CancelControllerCapture();
        if(e.type==SDL_EVENT_GAMEPAD_BUTTON_DOWN)NativePages::ControllerEvent(e.gbutton.button);
        if(e.type==SDL_EVENT_GAMEPAD_AXIS_MOTION&&e.gaxis.value>16000){
            if(e.gaxis.axis==SDL_GAMEPAD_AXIS_LEFT_TRIGGER)NativePages::ControllerEvent(SDL_GAMEPAD_BUTTON_COUNT);
            if(e.gaxis.axis==SDL_GAMEPAD_AXIS_RIGHT_TRIGGER)NativePages::ControllerEvent(SDL_GAMEPAD_BUTTON_COUNT+1);
        }
        return;
    }
    if(e.type==SDL_EVENT_KEY_DOWN || e.type==SDL_EVENT_KEY_UP) {
        host_bindings_key_event(e.key.scancode, e.type==SDL_EVENT_KEY_DOWN);
        const int sc=e.key.scancode;
        if(sc>=0 && sc<int(event_keys.size()))event_keys[sc]=e.type==SDL_EVENT_KEY_DOWN;
    }
    if(blocked)return;
    if(e.type==SDL_EVENT_KEY_DOWN){device=InputDevice::KeyboardMouse;host_bind_capture_key(e.key.scancode,e.key.mod);
        if(e.key.scancode==SDL_SCANCODE_DELETE)host_bind_note_clear_key();}
    if(e.type==SDL_EVENT_KEY_UP)host_bind_capture_key_up(e.key.scancode); // bbport: a modifier bound alone
    if(e.type==SDL_EVENT_MOUSE_MOTION){
        device=InputDevice::KeyboardMouse;std::lock_guard lock(state_lock);mouse_x=e.motion.x;mouse_y=e.motion.y;
        if(relative && focused)mouse_camera::add(mouse_camera::g_counts,e.motion.xrel,e.motion.yrel);
    }
    if(e.type==SDL_EVENT_MOUSE_BUTTON_DOWN || e.type==SDL_EVENT_MOUSE_BUTTON_UP){
        device=InputDevice::KeyboardMouse;
        const int b=e.button.button;const uint32_t bit=b>0&&b<=5?1u<<(b-1):0;
        const bool capture=e.type==SDL_EVENT_MOUSE_BUTTON_DOWN && host_bind_capture_mouse(b);
        std::lock_guard lock(state_lock);
        mouse_x=e.button.x;mouse_y=e.button.y;
        if(e.type==SDL_EVENT_MOUSE_BUTTON_DOWN){mouse_buttons|=bit;if(!capture)mouse_edges|=bit;}
        else mouse_buttons&=~bit;
    }
    if(e.type==SDL_EVENT_MOUSE_WHEEL){device=InputDevice::KeyboardMouse;
        if(!host_bind_capture_mouse(e.wheel.y>0?6:7)){
            std::lock_guard lock(state_lock);
            const unsigned direction=e.wheel.y>0?32:e.wheel.y<0?64:0;
            std::uint32_t weapons=0;
            if(direction && !menu_pointer_in_menu()) {
                for(const int action:{kBindSwitchRight,kBindSwitchLeft}) {
                    const auto chord=Chords::Get(action);
                    const bool match=chord.active()
                        ? (chord.mouse&direction) && Chords::Matches(chord,event_keys.data(),int(event_keys.size()),mouse_buttons|direction)
                        : host_binding_mouse(action)==(direction==32?6:7);
                    if(match)weapons|=host_binding_info(action).button;
                }
            }
            // Resolve modifiers now, before a later key-up can change their meaning.
            if(weapons)weapon_wheel.Push(weapons,std::max(1,int(std::abs(e.wheel.y))));
            else wheel+=e.wheel.y;
        }}
}
void Pump(SDL_Window* w,bool editing) {
    text_open=editing;if(!active)return;
    // bbport: focus back without its event (another screen used, then the game again).
    if(!focused && (SDL_GetWindowFlags(w)&SDL_WINDOW_INPUT_FOCUS))focused=true;
    int ww,hh;SDL_GetWindowSize(w,&ww,&hh);
    {std::lock_guard lock(state_lock);window_w=ww;window_h=hh;}
    const bool want=wanted_relative && BbSettings::Get().pc_controls && !editing && !BbOverlay::CapturesInput() && focused;
    if(relative!=want){
        if(SDL_SetWindowRelativeMouseMode(w,want)){relative=want;mouse_camera::g_counts=0;
            mouse_camera::g_flick_counts=0;mouse_camera::g_flick_updates=0;
            if(want)SDL_HideCursor();else SDL_ShowCursor();}
    }
}
}

extern "C" int bbgpu_menu_buttons(uint32_t* confirm,uint32_t* back) {
    if(!active)return 0;
    *confirm=menu_confirm_button();*back=menu_back_button();
    return menu_pointer_navigating()?1:0;
}

extern "C" int bbgpu_pc_input(BbPcPad* d,const bool* keys,int nkeys,int gamepad_present) {
    if(!active || !BbSettings::Get().pc_controls){keyboard_moving=false;wanted_relative=false;return 0;}
    auto& s=BbSettings::Get();
    // Keyboard/mouse mode ignores physical pads; controller mode retains keyboard fallback.
    const bool allow_pad=gamepad_present && !s.keyboard_mouse;
    PadState p;
    if(allow_pad){p.buttons=d->buttons;
    p.lx=d->lx;p.ly=d->ly;p.rx=d->rx;p.ry=d->ry;p.l2=d->l2;p.r2=d->r2;
    
    p.touch_count=std::min<uint8_t>(d->touch_count,2);
    for(int i=0;i<p.touch_count;i++)p.touch[i]={d->touch[i].x,d->touch[i].y,d->touch[i].id,true};}
    else device=InputDevice::KeyboardMouse;
    bool moving=false;
    const bool menu=menu_pointer_in_menu();
    const bool camera=s.keyboard_mouse && s.mouse_camera && mouse_camera_installed() && !menu && !host_text_entry_open() && focused;
    // Match the SDL device-switch threshold; resting stick noise must not disable mouse look.
    const bool stick_camera = allow_pad && (std::abs(int(d->rx)-128)>32 || std::abs(int(d->ry)-128)>32);
    const bool mouse_active = camera && device==InputDevice::KeyboardMouse && !stick_camera;
    wanted_relative=mouse_active;
    mouse_camera_publish(s.mouse_sensitivity,s.mouse_invert_x,s.mouse_invert_y,
        mouse_active && !s.mouse_auto_rotation, mouse_active);
    uint32_t buttons,edges;float scroll;
    {std::lock_guard lock(state_lock);buttons=mouse_buttons;edges=mouse_edges;scroll=wheel;mouse_edges=0;wheel=0;}
    bool any=buttons!=0;for(int i=0;keys&&i<nkeys;i++)any|=keys[i];
    host_bind_capture_note_held(any);
    if(!focused || BbOverlay::CapturesInput() || host_text_entry_open() || host_bind_capture_blocking() || NativePages::ControllerCapturing()){p=PadState{};wanted_relative=false;std::lock_guard lock(state_lock);weapon_wheel.Reset();}
    else {
        bool held[kBindCount]={};
        if(keys)host_bindings_keys_held(keys,nkeys,held);
        static uint64_t settle_until=0;
        static uint32_t dpad_blocked=0,last_buttons=0;
        uint32_t dpad_held=0;
        const unsigned mouse=buttons|edges|(scroll>0?32:scroll<0?64:0);
        bool mh[kBindCount]={};host_bindings_mouse_held(mouse,mh);
        if(!menu)for(int a=0;a<kBindCount;++a)held[a]=held[a]||mh[a];
        bool suppressed[kBindCount]={};Chords::Resolve(keys,nkeys,menu?0:mouse,held,suppressed);
        host_bindings_resolve_movement(held);
        if(!menu){held[kBindConfirm]=false;held[kBindBack]=false;}
        if(!menu && held[kBindPreviousItem] && !previous_item_was)native_item_cycle_request(true);
        if(menu)native_item_cycle_cancel();else menu_pointer_scroll(0);
        previous_item_was=!menu&&held[kBindPreviousItem];
        if(held[kBindPreviousItem])held[kBindSwitchItem]=false;
        const bool navigating=menu && menu_pointer_navigating();
        const bool world_movement=BbInputPolicy::WorldMovement(s.mouse_menu,
            menu_pointer_front_end(),navigating);
        if(navigating) {
            const auto down=[&](int sc){return keys && sc>=0 && sc<nkeys && keys[sc];};
            uint32_t arrows=0;
            if(down(SDL_SCANCODE_UP))arrows|=0x10;
            if(down(SDL_SCANCODE_DOWN))arrows|=0x40;
            if(down(SDL_SCANCODE_LEFT))arrows|=0x80;
            if(down(SDL_SCANCODE_RIGHT))arrows|=0x20;
            dpad_held=BbInputPolicy::MenuKeyboard(held,world_movement,arrows);
            dpad_blocked&=dpad_held;
            // The dialog guard blocks list taps, never world movement in inventory.
            if(SDL_GetTicks()>=settle_until)p.buttons|=dpad_held&~dpad_blocked;
            if(down(SDL_SCANCODE_RETURN))p.buttons|=menu_confirm_button();
            if(down(SDL_SCANCODE_BACKSPACE))p.buttons|=menu_back_button();
        }
        moving=world_movement &&
            (held[kBindMoveF]||held[kBindMoveB]||held[kBindMoveL]||held[kBindMoveR]);
        host_bindings_apply(held,held[kBindStrongMod],p);
        {
            std::lock_guard lock(state_lock);
            if(menu)weapon_wheel.Reset();
            else p.buttons|=weapon_wheel.Poll(std::chrono::duration_cast<std::chrono::milliseconds>(
                std::chrono::steady_clock::now().time_since_epoch()).count());
        }
        if(menu) {
            if(s.mouse_menu){
                if(edges&1)menu_pointer_click();
                if(edges&SDL_BUTTON_RMASK)p.buttons|=menu_back_button();
                if(navigating && scroll)menu_pointer_scroll(scroll>0?-1:1);
                p.buttons|=menu_pointer_take_press();
            }
            const uint32_t closing=menu_confirm_button()|menu_back_button();
            if(p.buttons&~last_buttons&closing){dpad_blocked|=dpad_held;settle_until=SDL_GetTicks()+250;}
        }else {

            float x,y;if(mouse_active && mouse_camera_flick(x,y)){
                p.rx=uint8_t(std::clamp(127+int(127*x),0,254));p.ry=uint8_t(std::clamp(127+int(127*y),0,254));}
        }
        // The camera is off in menus unless menu_move leaves it the mouse (see camera above).
        float x,y;if(camera && mouse_camera_flick(x,y)){
            p.rx=uint8_t(std::clamp(127+int(127*x),0,254));p.ry=uint8_t(std::clamp(127+int(127*y),0,254));}
        last_buttons=p.buttons;
    }
    keyboard_moving.store(moving,std::memory_order_relaxed);
    {std::lock_guard lock(state_lock);snapshot=p;}
    d->buttons=p.buttons;d->lx=p.lx;d->ly=p.ly;d->rx=p.rx;d->ry=p.ry;d->l2=p.l2;d->r2=p.r2;
    d->touch_count=p.touch_count;for(int i=0;i<p.touch_count;i++)d->touch[i]={p.touch[i].x,p.touch[i].y,p.touch[i].id};
    return 1;
}

