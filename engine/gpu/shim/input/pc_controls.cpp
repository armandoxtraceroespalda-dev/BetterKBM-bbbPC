// SPDX-License-Identifier: GPL-3.0-or-later
#include "pc_controls.h"
#include "bindings.h"
#include "../bbport_settings.h"
#include <SDL3/SDL.h>
#include <atomic>
extern "C" int runtime_pad_button(uint32_t,int);
extern "C" void runtime_pad_request_binding(uint32_t,int,int);
namespace { std::atomic<int> capture{-1}; }
namespace BbSettings {
void ApplyDs3Profile(){
 host_bindings_load_defaults();
 const char* pairs[][2]={{"roll","Space"},{"attack","Mouse1"},{"strong_attack","Left Shift + Mouse1"},{"strong_modifier","none"},
 {"firearm","Left Ctrl, Mouse3"},{"transform","F"},{"blood_vial","C"},{"use_item","R"},{"interact","E"},
 {"lock_on","Q, Mouse2"},{"l3","Z"},{"menu","Escape"},{"confirm","Return"},{"back","Backspace"},
 {"dpad_up","T"},{"switch_item","WheelDown"},{"previous_item","WheelUp"},{"switch_right","Left Shift + WheelUp"},{"switch_left","Left Shift + WheelDown"}};
 for(auto& pair:pairs)host_bindings_parse(pair[0],pair[1]);
 Get().ds3_profile=true;Get().pc_controls=true;Get().mouse_menu=true;
}
void RestoreControllerDefaults(){runtime_pad_request_binding(0,0,-2);}
void SelectKeyboardMouse(bool enabled){
 auto& s=Get();s.keyboard_mouse=enabled;s.pc_controls=true;s.mouse_camera=enabled;s.mouse_menu=true;
 s.button_prompts=enabled?PromptsKeyboard:PromptsController;
}
}
namespace NativePages {
std::string ControllerBindingLabel(int action){
 if(action<0||action>=kBindCount)return "Unbound";
 if(action==kBindPreviousItem)return "—";
 auto bit=host_binding_info(action).button;
 if(!bit)return action<=kBindMoveR?"Left stick":action>=kBindLookU&&action<=kBindLookR?"Right stick":"—";
 int b=runtime_pad_button(bit,action==kBindEffects);
 if(b==SDL_GAMEPAD_BUTTON_COUNT)return "L2";
 if(b==SDL_GAMEPAD_BUTTON_COUNT+1)return "R2";
 const char* labels[]={"Cross","Circle","Square","Triangle","Share","PS","Options","L3","R3","L1","R1","Up","Down","Left","Right"};
 if(b>=0&&b<int(sizeof(labels)/sizeof(*labels)))return labels[b];
 if(b==SDL_GAMEPAD_BUTTON_TOUCHPAD)return "Touchpad";
 const auto* name=b>=0?SDL_GetGamepadStringForButton(static_cast<SDL_GamepadButton>(b)):nullptr;
 return name?name:"Unbound";
}
void BeginControllerCapture(int action){if(action>=0&&action<kBindCount&&action!=kBindPreviousItem&&host_binding_info(action).button)capture=action;}
bool ControllerCapturing(){return capture.load()>=0;}
void CancelControllerCapture(){capture=-1;}
void ControllerEvent(int button){int action=capture.exchange(-1);if(action>=0&&button>=0)runtime_pad_request_binding(host_binding_info(action).button,action==kBindEffects,button);}
}
