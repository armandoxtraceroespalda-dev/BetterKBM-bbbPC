Run `dotnet run --project tests/better_kbm/launcher_memory/LauncherMemoryTests.csproj -c Release`.
Optionally pass the published launcher DLL path as the first program argument to
inspect metadata and verify that update/install entry points and hidden release
controls are excluded from the compiled assembly. This does not load WPF.

The regression covers LRU reuse and the production active-only page budget,
subscription cleanup, failed construction, saved settings and caption-based glow
geometry under different row heights. It does not measure Windows native memory.
Use tools/measure-launcher-memory.ps1 and optional BB_LAUNCHER_UI_TRACE=1 for real
Windows measurements; the 60 MiB total-process target is not asserted by this test.

UI 10 adds weak diagnostic lease collection and metadata checks for UserControl
views. Forced collections occur only in the isolated weak-reference regression,
never in the production launcher. The automatic Windows report is now
%APPDATA%/bbport-launcher/launcher-diagnostics.log; environment setup is unnecessary.

UI17 changes the earlier production policy: after 12 seconds without navigation,
a nonblocking generation-2 collection may be requested when retired views remain
and managed memory exceeds 24 MiB, with a 45-second cooldown. No working-set trim
is used. Runtime GC.ConserveMemory=7 reduces heap slack; total Windows memory is
not capped. The policy boundaries are covered by the menu_audio tests. Detached
premultiplied PNG cache behavior is covered by launcher_bitmap on Windows.

UI21 supersedes the UI17 idle policy: navigation now queues an ApplicationIdle
cleanup after 1.5 seconds, with retired views, an 8-MiB threshold and eight-second
cooldown. Nested More navigation participates. No working-set trim is used.

UI22 retains UI21's retired-tree cleanup and separately checks excess GC commit
after five seconds without interaction. The one-shot aggressive compacting
release is subject to 32-MiB commit, 16-MiB spare, 2:1 commit/live thresholds and
a thirty-second cooldown. It may briefly pause the UI; it never trims the Windows
working set. `heap_release` tests the production policy and actual .NET decommit.
Actual Windows resident savings and the 60 MiB target remain unverified.
