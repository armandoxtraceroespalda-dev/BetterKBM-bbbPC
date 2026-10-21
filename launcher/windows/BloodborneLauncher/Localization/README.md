# Launcher localization

Game > Language > Text language selects both the launcher interface and game
text. The launcher rebuilds its current page immediately. The game's next launch
receives the same text-language ID. Voice language remains independent. There
is no additional launcher-language setting.

Supported IDs: English 1, Japanese 0, French 2, Spanish (Spain) 3,
Spanish (America) 20, German 4, Italian 5, Russian 8, Portuguese (Brazil) 17.
Both Spanish variants share the launcher catalog; the game retains its distinct
regional text selection. No extra languages are inserted into the existing list.

Catalog keys are English source strings, including .NET composite formatting
placeholders. Translate only values; preserve every placeholder and format
specifier. New interface strings should use UiText.T, UiText.F for interpolation,
or UiText.Format. User paths, editable text, binding expressions and configuration
values must remain raw, and are inserted as formatting arguments when needed.
Only the selected catalog is deserialized. CultureInfo.CurrentCulture is never
changed, so configuration parsing and numeric serialization remain stable.

Run tests/better_kbm/localization/LocalizationTests.csproj when editing a catalog.
Catalogs include an initial offline translation pass and curated interface and
input terminology. Native-speaker review of less common diagnostic messages is
welcome. Translation models are development tools, not launcher dependencies.

The redistributable Noto subset in ../Fonts supplies missing Japanese/Cyrillic
characters. It uses the renamed family Launcher UI Fallback and the accompanying
OFL license. It is installed in gui/fonts/<content-hash> and never replaces the
user's locally extracted Bloodborne font. If translations add Japanese glyphs,
regenerate the subset and update UiFontAssets.Hash; keep Fonts/OFL.txt.

## UI29 contextual review

The catalogs were rebuilt using the source UI domains: game commands, input
assignment, rendering/performance, display/HDR, saved games, profiles, online
accounts and diagnostics. Review/ contains curated interface/gameplay terms,
contextual help and the extended Spanish review. These are source-only review
materials, not additional runtime catalogs. Offline model output supplies the
remaining initial draft; uncommon diagnostic wording still benefits from
native-speaker review. No translation service or model is a runtime dependency.

Action means a game command, never a measurement. Movement labels are
infinitive commands in Spanish. Keys and keybindings are physical inputs and
assignments, not cryptographic keys or financial bindings. Saves are game save
data, not money. A render frame is a fotograma/Frame, not a picture frame. Mouse
camera means in-game camera control, not a photographic device. Frame/range
context and restart/live semantics in help must be retained.

Keep case variants of the same reviewed term consistent. Translate the group
name before uppercasing it. Translate interpolated action labels before inserting
them into a help template; never translate raw device/user/binding values.
Do not put HTML entities into plain WPF strings. Language endonyms remain literal.
