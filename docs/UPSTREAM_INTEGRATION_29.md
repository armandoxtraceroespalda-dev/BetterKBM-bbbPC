# Upstream integration 29

Upstream: [Awakenchan/LanternEngine](https://github.com/Awakenchan/LanternEngine).
Previous source revision: `1ce45ae7747623b9792d54566cd5290a380555a1` (0.9.37 beta 6).
New revision: `f029d89e934f0f8aac672e80c41e35caa9bd4146` (0.9.37 beta 8), including
`c494fcad5e5591ee79c057f0b1ead90180487ebe` (beta 7).

## Source layout

Upstream beta 7 replaced its engine patch with complete source in `engine/`.
This integration adopts that source layout and vendors the same dependency
sources. `tools/build-better-kbm.sh` prepares a separate
`../BetterKBM-engine-beta8` folder, verifies every native source hash, preserves
local source edits, removes only obsolete unmodified source records and migrates
missing player data. It prefers the engine selected in launcher settings;
`BB_PREVIOUS_ENGINE` explicitly overrides migration. Existing target configuration
and saves are not overwritten. The old base-engine clone and patch application
are no longer needed. Legacy patch files in an existing clone are not applied.

This is a content integration for local testing. No remote commit, push or Git
merge has been performed. Existing custom control profiles and settings remain
in use; the new engine folder must be selected after building.

## Upstream behavior included

- Renderer descriptor/cache fixes, including the beta 7 AMD startup issue.
- GX range allocator sizing and complete GPU crash diagnostics from beta 8.
- Shader-building notification, FSR 3.1 tuning, automatic DLSS model selection
  and dynamic frame-generation support.
- Multiple keyboard/mouse alternatives per action and modified inputs.
- Trophy pictures from the player's game copy; custom MP3/MP4 trophy audio.
- Mods on exFAT, platform paths, certificate handling and Linux runtime changes.
- Frame statistics in beta logs; standalone launcher handover support.

The project's own web deployment files are not deployed by this integration.

## Custom behavior retained

The DS3 default bindings, raw mouse gain of 0.10 degrees/count at sensitivity 5,
keyboard direction/sprint handling, inventory movement, independent gamepad
configuration, native menu organization, localized prompts, language/voice
selection and live settings/resolution synchronization are retained. Negative
mouse sensitivity levels extend the selected custom curve; they do not replace
it with upstream's higher base gain. Capture still accumulates inputs until
all are released. Its Add input action can append an alternative without
replacing existing bindings. Custom layouts, tooltips, audio and cheat phrases
are retained. Glyphs, extracted game assets and personal settings are not part
of the source delivery.

The optional frame-statistics reminder was removed from session summaries.
Active switches now draw the extracted MenuGlowBrush through MenuSwitchGlow,
the same resource-lookup/drawing approach used by the working menu buttons.
The halo is painted behind the thumb without measuring a large image inside
the thumb's layout slot. No blur effect or per-switch decoded bitmap is added.

## Validation and limits

Completed: self-contained Windows x64 launcher publish; portable profiles and
cheat tests; production input, pointer, sprint, wheel, camera, settings sync,
resolution and language/voice regression tests; native asset conversion,
controller queue and Quit to Desktop tests; pipeline archive/cache CPU tests;
Windows-target and Linux syntax checks of the native System menu implementation;
source-deployment and configuration-preservation tests; WPF test compilation.

Not performed: complete Windows Vulkan build/link, GPU validation across vendors,
a Bloodborne game boot, gameplay benchmarks, visual Windows review or RAM
measurements. Those require the user's native test environment. This source
bundle does not claim a measured performance increase or a completed game test.
