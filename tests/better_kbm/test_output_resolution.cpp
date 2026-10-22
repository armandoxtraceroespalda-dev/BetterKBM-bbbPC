// SPDX-License-Identifier: GPL-2.0-or-later
#include "bbport_settings.h"
#include <cassert>
#include <cstdio>
#include <cstdlib>
#include <fstream>
#include <iterator>
#include <string>
#include <unistd.h>

int main() {
    using namespace BbSettings;
    char path[] = "/tmp/bbport-output-policy-XXXXXX";
    int fd = mkstemp(path); assert(fd >= 0); close(fd);
    setenv("BB_CONFIG", path, 1);
    unsetenv("BB_RENDER_RES"); unsetenv("BB_LIVE_RES"); unsetenv("BB_UPSCALER"); unsetenv("BB_UPSCALE_PRESET");
    auto config = [&](const char* output, const char* live, const char* provider = "fsr3") {
        std::ofstream file(path);
        file << "output_res=" << output << "\nlive_resolution=" << live << "\nupscaler=" << provider << "\npreset=1\nbind.roll=Space\n";
        file.close(); Load();
    };
    auto& s = Get();
    config("1920x1080", "0");
    assert(!FixedRenderSession() && !LiveOutputChangesEnabled());
    int startup = s.startup_output_res;
    for (int output = 0; output < OutputCount; ++output) {
        s.output_res = output;
        assert(RenderOutput() == startup);
        assert(ResolutionNeedsRestart() == (output != startup));
    }
    s.output_res = 3; // A new requested 2560x1440 size is saved without changing the applied output.
    Save();
    std::ifstream file(path); std::string saved{std::istreambuf_iterator<char>(file), {}};
    assert(saved.find("output_res=2560x1440") != std::string::npos && saved.find("bind.roll=Space") != std::string::npos);
    assert(RenderOutput() == startup);
    Load(); assert(RenderOutput() == 3 && !ResolutionNeedsRestart());
    s.live_resolution = 1;
    s.output_res = startup;
    assert(!LiveOutputChangesEnabled() && RenderOutput() == 3); // Toggle changes also wait for the next launch.

    config("1920x1080", "1");
    assert(LiveOutputChangesEnabled());
    for (int output = 0; output < OutputCount; ++output) {
        s.output_res = output; assert(RenderOutput() == output && !ResolutionNeedsRestart());
    }
    setenv("BB_LIVE_RES", "0", 1);
    assert(!LiveOutputChangesEnabled() && RenderOutput() == s.startup_output_res);
    config("1920x1080", "auto");
    for (const char* resolved : {"0", "1"}) {
        setenv("BB_LIVE_RES", resolved, 1);
        s.output_res = 3;
        assert(LiveOutputChangesEnabled() == (resolved[0] == '1'));
        assert(RenderOutput() == (resolved[0] == '1' ? 3 : s.startup_output_res));
    }
    setenv("BB_RENDER_RES", "1706x960", 1);
    assert(!LiveOutputChangesEnabled() && RenderOutput() == s.startup_output_res && ResolutionNeedsRestart());
    unsetenv("BB_RENDER_RES"); unsetenv("BB_LIVE_RES");
    config("2560x1440", "0", "taa");
    assert(RenderPreset() == NativeAA && RenderOutput() == 3);
    s.output_res = 1;
    assert(RenderOutput() == 3 && ResolutionNeedsRestart());
    std::remove(path);
    std::puts("PASS: Off/native 1080p, startup snapshots, saved pending output, restart, On, resolved Auto, environment overrides, fixed guest size and TAA.");
}
