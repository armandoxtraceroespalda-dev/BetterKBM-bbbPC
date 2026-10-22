// SPDX-License-Identifier: GPL-3.0-or-later
#pragma once
#include "bindings.h"
#include <cstdint>

namespace BbInputPolicy {
// The title/front end retains its normal WASD navigation. This policy applies
// only to native PC input with mouse menus enabled inside the loaded world.
inline bool WorldMovement(bool mouse_menu, bool front_end, bool navigating) {
    return mouse_menu && !front_end && navigating;
}
inline std::uint32_t MenuKeyboard(bool* held, bool world_movement, std::uint32_t arrows) {
    for (int a = kBindLookU; a <= kBindLookR; ++a) held[a] = false;
    if (world_movement) {
        // Circle is also menu Back: Space must not dismiss the inventory.
        // The actual controller and RMB/Backspace are handled separately.
        held[kBindRoll] = false;
    } else {
        if (held[kBindMoveF]) arrows |= 0x10;
        if (held[kBindMoveB]) arrows |= 0x40;
        if (held[kBindMoveL]) arrows |= 0x80;
        if (held[kBindMoveR]) arrows |= 0x20;
        for (int a = kBindMoveF; a <= kBindMoveR; ++a) held[a] = false;
    }
    return arrows;
}
inline bool FilterList(bool mouse_menu, bool front_end, bool keyboard_moving, bool controller) {
    return mouse_menu && !front_end && keyboard_moving && !controller;
}
}
