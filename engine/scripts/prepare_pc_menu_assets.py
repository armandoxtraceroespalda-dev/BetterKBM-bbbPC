# SPDX-License-Identifier: GPL-3.0-or-later
"""Cached custom movie/localization worker for the native starter."""
from pathlib import Path
import sys
from engine_assets import prepare_menu_assets
if __name__=='__main__':
 if len(sys.argv)!=7:raise SystemExit('Expected GAME ELF CACHE TOOL REBIRTH TWEAK_MENUS')
 root=prepare_menu_assets(*(Path(v) for v in sys.argv[1:5]),rebirth=sys.argv[5]=='1',tweak_menus=sys.argv[6]!='0')
 print(root,flush=True)
