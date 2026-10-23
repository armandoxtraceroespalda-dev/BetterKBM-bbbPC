# Launcher layout 35

## Typography

Settings pages use a shared, inherited text-scale scope. Relative to a 1280×800
window, the geometric mean of the width and height ratios is clamped to 70–130%
and rounded to one-percent steps. Windows dimensions are already logical DIPs;
no second DPI factor is applied. Explicit control/text sizes keep their original
base value; inherited or template-bound fonts follow their owner once. A late
heading style updates the base rather than compounding a previous scale. Fixed
text-entry heights are given room for the line and padding when fonts grow.
Tooltips retain their existing sizes. Start's manually responsive information
uses the same bounded policy; its existing Play layout is retained.

The scope uses inherited dependency-property changes and one static Loaded class
handler, not LayoutUpdated, mouse handlers, per-page polling or a retained element
registry. Per-element base sizes are stored only on the elements that own them.

## Graphics and settings

The order is Screen, Upscaler, Game effects, Picture, HDR, Info, Experimental.
Upscaler explicitly spans both columns on sufficiently wide windows. Its upscaler
and quality selections are native menu buttons across full-width rows, with
content-sized labels and evenly distributed remaining space. Narrow windows wrap
the buttons instead of clipping translated labels. The two-column settings fall
back to one column on narrow viewports, as the existing layout does.

Quality preset and DLSS options have subsection headings. Custom is the concise
preset label, and its render-scale slider appears only for that preset with an
upscaler that uses it. DLSS-specific model/exposure/texture/generated-frame/Reflex
controls are inside Upscaler and visible only with DLSS selected. Shared sharpening,
frame-generation selection and pacing remain available to their supported modes.
Settings are preserved when their controls are hidden. Existing keys and engine
behavior are unchanged. Diagnostics explicitly uses two columns when space permits.

The source patch also includes the hidden Cheats navigation changes from launcher
34, so it can be applied over Integration33 or Launcher34. It contains only files
changed since Integration33: no native engine source, executables, DLLs or game
artwork. The engine remains Integration33 / beta 9.

## Validation

- Self-contained Windows x64 launcher publish completed.
- Portable tests for scaling bounds, baseline/invalid dimensions, monotonic growth
  and all upscaler/preset visibility transitions passed.
- Existing production profile and hidden-cheat regressions passed.
- All fourteen catalogs contain the compact preset labels and DLSS subsection title.
- The WPF tests for explicit/inherited/dynamic font scaling, late styles and narrow/
  wide choice-strip layout build. Their execution and visual review require Windows.
