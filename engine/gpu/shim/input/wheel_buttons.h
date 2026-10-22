// SPDX-License-Identifier: GPL-3.0-or-later
#pragma once
#include <algorithm>
#include <cstdint>
#include <deque>

namespace WheelButtons {
// A wheel event must survive intervening pad reads and have a release between notches.
// The owner serializes this queue with the input-state lock.
struct Queue {
    std::deque<std::uint32_t> pending;
    std::uint32_t held = 0;
    std::int64_t until = 0, ready = 0;
    void Reset() { pending.clear(); held = 0; until = ready = 0; }
    void Push(std::uint32_t buttons, int notches) {
        if (!buttons) return;
        const int count = std::clamp(notches, 1, 32);
        for (int i = 0; i < count && pending.size() < 32; ++i) pending.push_back(buttons);
    }
    std::uint32_t Poll(std::int64_t now_ms) {
        if (held) {
            if (now_ms < until) return held;
            held = 0;
            ready = now_ms + 50;
            return 0;
        }
        if (pending.empty() || now_ms < ready) return 0;
        held = pending.front(); pending.pop_front();
        until = now_ms + 80;
        return held;
    }
};
}
