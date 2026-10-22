#pragma once
#include <string>
namespace NativePages {std::string ControllerBindingLabel(int);void BeginControllerCapture(int);bool ControllerCapturing();void CancelControllerCapture();void ControllerEvent(int);}
