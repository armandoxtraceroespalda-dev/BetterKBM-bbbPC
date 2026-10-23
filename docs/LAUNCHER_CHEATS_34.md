# Launcher 34: hidden Cheats navigation

The customized launcher now has separate `TweaksPage` and `CheatsPage` views.
Gameplay and camera tweaks remain in Tweaks. Only actual cheat settings are in
Cheats. Its sidebar entry uses the same native artwork, selection sound, scale
and navigation animation as the existing entries. Sidebar minimum height accounts
for the extra visible entry on short windows.

`gitgud` unlocks the entry from any launcher window. Access is persisted with the
existing `cheats_unlocked` preference, including an unlock saved by older builds.
`igotgud` hides it and saves neutral values for all native cheat keys, including
unknown `cheat_*` keys, without changing tweak settings. If Cheats is being shown,
the launcher returns to Tweaks. A hidden page is also blocked in direct navigation.
Unlocking does not select Cheats, enable cheats or recreate the current page.

One process-wide WPF class handler routes text input instead of attaching listeners
to pages. Plain/rich text editors, passwords, editable selectors and key-binding
capture are excluded. Entering an editor clears a partial phrase; excluded input
cannot bridge a prefix into ordinary navigation. Escape, Backspace, Delete and
modified keyboard shortcuts reset a prefix. There are no timers, mouse listeners
or per-page global subscriptions for recognition.

The Cheater popup remains transient and once per game launch with actual cheats
active; tweaks alone do not trigger it. It does not grant or count an achievement.
The title, description and Cheats label are present in all fourteen launcher
catalogs. Spanish uses Trampas and Tramposo.

## Upstream status

Awakenchan/LanternEngine main was checked against our integrated revision
`08173e6cbfcfd2191594ca28ad8373c7e9ccc895` (0.9.37 beta 9). GitHub reported an
identical comparison, with no subsequent commits. The cumulative bundle retains
Integration33 and its native engine unchanged. No upstream update is claimed for
this launcher-only change. No remote commit, push or pull request was made.

## Validation

- Self-contained Windows x64 launcher publish completed.
- Portable phrase-recognition, editor-interruption, persisted-access, cheat reset,
  cheat/tweak classification and once-per-launch notice tests passed.
- Production device profile and binding tests passed.
- The WPF test project for editable-control ancestry and separate page types builds.
- All fourteen catalogs contain the Cheats, Cheater and description strings.

The WPF test executable, real keyboard focus routing and visual navigation still
need running on Windows; this environment cannot execute WPF. Native sources were
not changed in this version. The earlier full Windows Vulkan/game validation
limits of Integration33 still apply.
