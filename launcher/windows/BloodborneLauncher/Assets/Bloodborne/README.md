# Local game menu assets

The launcher now has one Bloodborne presentation, with a centered main menu.
Copy the converted local asset pack into LauncherApp, creating
LauncherApp/Assets/Bloodborne. Keep these game-derived assets outside the source
patches. LauncherApp is already excluded from Git.

The source-side exporter is tools/extract-launcher-assets.py. With Pillow and
fonttools installed, it can regenerate the assets from your own game folder:

python tools/extract-launcher-assets.py "D:/path/to/CUSA03173" --out LauncherApp/Assets/Bloodborne

Inputs: dvdroot_ps4/menu/title.tpf.dcx, common.tpf.dcx and
menu/spaes/font.gfx (engus, enggb or spaar are supported fallbacks).
The original font is embedded vector data in a Scaleform DefineFont3 record;
renaming font.gfx to .ttf does not convert it. The exporter creates a TrueType
font from its glyph outlines, character map and advance metrics.

The background comes from MENU_Title_00001. Silver frame/underline ornaments and
the golden selection highlight come from MENU_Common_00003. Black-backed
additive overlays are adapted to PNG transparency for WPF. The native game
files are read only. Restart the launcher after changing assets.
