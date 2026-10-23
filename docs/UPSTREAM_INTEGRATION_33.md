# Upstream integration 33 — LanternEngine 0.9.37 beta 9

Upstream: https://github.com/Awakenchan/LanternEngine

- Previous revision: `f029d89e934f0f8aac672e80c41e35caa9bd4146` (beta 8).
- Integrated revision: `08173e6cbfcfd2191594ca28ad8373c7e9ccc895` (beta 9).
- This source bundle includes our later Layout30, Layout31 and Layout32 changes.
- No remote commit, push or pull request was made. This is a source integration for local testing.

## Upstream changes

The native source includes the original renderer, memory, file-system and startup
changes: particle indirect-dispatch limits, refresh-aware presentation pacing,
loading-screen frame-rate recovery, frame generation with HDR, RTX 50 dynamic
multi-frame generation detection, the 640×360 minimum scene size, repaired stale
shader-catalog keys, expanded memory-pool diagnostics and Linux feature setup.
The new Ebrietas trail replacement is generated locally from the player's own
files; a mod that replaces the relevant files wins. `BB_EFFECT_FIXES=0` keeps
the original effect. The engine switch is exposed with the other engine fixes.
These implementations are upstream work, not performance work authored here.

Launcher integration includes the shader-building-note switch under Graphics / Info,
new translated DLSS preset and gamepad extra-button labels, the upstream Simplified
Chinese menu messages and optional custom launcher-window icon. The icon choice is
under Settings / Appearance and accepts PNG, JPG, BMP or ICO. Auto uses the existing
compiled app.ico; the game's extracted Bloodborne icon and the EXE resource are not
replaced. New translated game messages use our retained category and action IDs:
Previous Item is action 29 and Debug Menu is action 30.

## Custom behavior retained

The Bloodborne theme, fixed sidebar, Windows caption buttons, navigation animation,
audio, memory policies, contextual translations, DS3 profiles, chord capture,
wheel weapon/item cycling, inventory movement, sprint direction handling,
controller separation, settings synchronization and menu layout remain in use.
The key-release-delay default stays at zero. Camera tuning disables its dependent
rows. The scrollbar retains its audio-button clearance. Existing saved values are
not reset. Generated game assets, personal settings and build products are not
included in this source bundle.

Upstream's optional Modern sidebar and update Download/Later interface were not
reintroduced: this fork has one requested Bloodborne sidebar and its automatic
updates remain disabled. Its separate Avalonia UI and web FAQ deployment are not
part of this Windows WPF launcher integration. The update backend changes are
retained without enabling update requests or controls.

## Build and safe deployment

Run `bash tools/build-better-kbm.sh` in MSYS2 CLANG64 after copying ARCHIVOS into
the project clone. The script verifies the in-tree native sources, builds into a
separate `../BetterKBM-engine-beta9` folder and copies only missing configuration,
assets and player data from the previously selected engine. Existing target data
and modified source files are protected. Select that new engine folder in the
launcher before testing. The previous beta 8 installation is retained.

The native log must contain:

`BetterKBM native engine integration 33 | upstream 08173e6cbfcfd2191594ca28ad8373c7e9ccc895 (0.9.37 beta 9)`

## Validation

Completed:

- Self-contained Windows x64 WPF launcher publish.
- Production profile/device-isolation, chord, saved-profile and cheat access tests.
- Native input, controller, inventory pointer, sprint, wheel, camera, settings,
  resolution and language/voice regressions.
- Native asset converter, controller queue, save-aware Quit to Desktop and localized
  archive tests using real SDL3 headers and synthetic data.
- Upstream tests for effect asset preparation, shader-catalog cache repair, patches
  and run settings. Four existing patch tests skip without their optional fixtures.
- Native Chinese/English message serialization, Unicode, fallback and preservation
  of existing message groups.
- Windows-target syntax checking of the custom native System menu and startup code.
- Shell syntax and source-manifest integrity checks.

The upstream cache test uses placeholder bytes: only its archive/layout postprocess
is mocked there. Real native archive conversion and the custom layouts are covered
by the separate asset and gameplay-menu regression suites.

Not performed here: full Windows Vulkan build/link, the new GPU indirect-clamp
Vulkan test, game boot, visual review or FPS/RAM benchmarks. Those need the native
Windows/GPU test environment. No measured performance gain is claimed.
