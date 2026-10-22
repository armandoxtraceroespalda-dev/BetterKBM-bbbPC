# Bloodborne launcher layout 23

Base: `Bloodborne` commit `de516bd84673ca1ca581e657e1f2b0a5502d9c5b`.
This source package includes layouts 21–23; it changes no native engine files.

## Organization and presentation

Display options are included in Graphics, using the same stored keys. Short
sections occupy paired rows with matching heading slots. Sections containing six
or more visible settings span the viewport and distribute options into two
columns. Narrow windows return to one column. Containers are retained rather
than reparented, preserving device visibility, enabled state and subscriptions.
Keybinding tables, mod lists, logs and achievement records retain their full width.
Game keeps its existing organization and inline explanations.

Ordinary explanations and choice notes use gold-on-black tooltips with the
existing shared nine-slice shadow. Warnings, live paths and state remain visible.
WPF hardware acceleration is permitted by default; `--software-rendering` remains
an explicit diagnostic fallback. Content refresh and layout precede entrance
animation. Wallpaper, fade, rise, glow, local artwork and bounded page ownership
are retained. No fixed resident-memory or animation-frame-rate result is claimed.

## Controls and profiles

Input device contains the profile selector and sharing actions, without a
separate Profiles heading. Only Dark Souls 3 PC and Controller remain built in.
Only the built-in DS3 profile has a descriptive tooltip. Keybindings is the final
section; its buttons retain their native style and use small shared vector
keyboard/mouse icons. Sprint options live with keyboard/mouse settings.

Saved/exported profiles use version 3 and `input_device` (`keyboard_mouse` or
`controller`). Lists are filtered by that literal field, independently of UI
language. Controller profiles apply only pad assignments; keyboard profiles apply
only keyboard/mouse assignments. Each device remembers its chosen profile. Equal
names on different devices use separate filenames. Imports check collisions in
the destination device's list. Existing files are not deleted.

Profiles without device metadata remain readable as keyboard/mouse profiles,
preserving the earlier interpretation; their origin cannot be inferred reliably
from a name. They are not rewritten just by viewing the list. Resaving writes the
explicit field. Shared/imported controller files include a pad map, even when it
is empty and represents the factory layout.

## Patch titles and graphics

XML `Name`/`Author` attributes and resolution template names are read literally.
Only visible titles are localized. Read-notes annotations are removed from
visible patch names; raw identifiers, saved selections and note tooltips remain.
Graphics retains only the Recommended action above GPU selection, without the
Best settings label or a trailing ellipsis.

## Cheats easter egg

Type `cheatsplease` while Tweaks is visible and no editable field owns keyboard
focus. This reveals cheats for the current launcher process, including page
recreation. It writes no unlock preference and enables no cheat by itself.
Existing cheat settings are preserved.

Launching with a known active cheat arms one transient `Im a cheater` popup,
shown on the pinned engine's window-ready log marker. Failed starts and exits
cancel pending notices; tweaks alone do not arm one. A separate notice record
never adds a trophy ID, save entry, unlock, completion percentage or Achievements
row. A future engine changing its ready marker must update the notice policy.

## Validation

The Windows x64 self-contained launcher publishes on the build host. Portable
fixtures link production profiles, bindings, pad mapping, INI, patch discovery,
localization and notice logic. Only platform input enums and local paths are
adapted. Executed checks cover device isolation, serialization/filtering, legacy
profiles, DS3 chords, XML identity in nine locales, transient unlocks and notices.

```sh
dotnet run --project tests/better_kbm/device_profiles
dotnet run --project tests/better_kbm/cheat_easter_egg
dotnet run --project tests/better_kbm/launcher_render_policy
```

WPF layout/icon fixtures compile here but require Windows to execute:

```sh
dotnet run --project tests/better_kbm/compact_layout
```

Check normal/maximized/narrow windows, long translations, profile capture and
selector popups on the target PC. No Windows desktop visual review, gameplay,
RAM measurement or fullscreen frame-time measurement was performed here. No
extracted game artwork, settings, saves or runtime libraries are redistributed.

## Layout 24

Sections now occupy the full viewport. Option rows split only at eight or more
visible settings and an even count; narrow windows use one column. Nested groups
apply the same rule independently. Headings share the cached MenuSurface shadow.
Setting label space scales with available width instead of forcing controls below
a fixed 560-DIP threshold. Slider values reserve their natural measured width in
an Auto column; only the track shrinks. Camera tuning moved from Graphics to
Tweaks without changing keys, defaults or live engine behavior.

## Layout 25

Short and odd-count sections occupy a single column each, paired side by side
with aligned heading slots. Only sections containing more than seven visible
options and an even count span both columns and enable option splitting.
Explicit full-width tables retain their width. Unpaired short sections are never
promoted to full width. Narrow viewports stack sections in source order.

## Layout 26

Any section with more than seven visible options may span both columns, including
odd counts. Short sections remain paired, one column each. Switches use the right
edge of their column regardless of title length; slider value/track groups are
centered on a dedicated row below their titles. The imacheater phrase persists
cheats_unlocked in launcher settings. It only reveals the controls and never
enables cheats. Graphics sections follow Screen, Game effects, Picture, Upscaler,
HDR, Info and Experimental. The monitor selector precedes frame rate in Screen.
Show FPS gates its five detail rows without overwriting their stored selections.

## Layout 27

The persistent cheat unlock phrase is now gitgud. Its transient Cheater notice
is localized, without adding a real achievement. Active switch tracks stay
neutral; the extracted MenuGlowBrush is drawn behind the thumb only when on.
Inactive tracks are darker. Section heading shadows use their measured text
width instead of the section width. Numeric page/theme font sizes are reduced
by 12.5 percent, leaving the tooltip style unchanged. Input device choices are
deliberately larger (32 DIP text, 78 DIP minimum height), without a group title.
Screen and Files explicitly request two option columns at wide viewport sizes.
HDR, picture tuning and FPS detail controls follow their master switches and
retain saved values while disabled. Profile labels use a tight layout; profile
action labels omit trailing ellipses. Alphabetic/numeric bindings omit the
keyboard pictogram while named keys and mouse inputs retain their icons.
Recommended settings is a standalone action above the full-width GPU selector.
Share/import settings are part of Files, with their action buttons below the
label so their two-column cards do not squeeze the buttons beside the title.

## Layout 28

Input choices use 28 DIP text and 66 DIP minimum height. Gamepad replaces
Controller in the input choice and binding column; its options explicitly use
two columns. The mouse option section retains device visibility with no visible
heading. Device labels are literal names only. Toolbar headings keep their full
width while text-only heading shadows remain content-sized, so Reset all aligns
with the right edge. Language labels are subtitle-sized and expose help on the
label itself. Start owns session information and its timer; the timer runs only
while the game and visible Start page are active, and is stopped on unload.
The igotgud phrase clears persistent access and resets every known or stored
cheat_* setting, including the neutral echo multiplier of 1, without altering
gameplay tweaks. Recreating the pages removes stale toggle values after reset.
The transient Cheater notice uses localized Error 404: Skill not found. text.
Online connection settings explicitly split into two columns. Multiple-choice
account dialogs use wider content-aware buttons with wrapping and a card capped
to the owner width, keeping long translated choices readable.
