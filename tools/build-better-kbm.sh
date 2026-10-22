#!/usr/bin/env bash
# SPDX-License-Identifier: GPL-3.0-or-later
# Integration 20: a separate engine checkout avoids stale files from prior online layouts.
set -euo pipefail
root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
engine_source="${1:-$root/../BetterKBM-engine}"
engine_target="${2:-$root/../BetterKBM-engine-beta6}"
previous="${BB_PREVIOUS_ENGINE:-$root/../BetterKBM-engine-beta5}"
[[ -d "$previous" ]] || previous="$root/../BetterKBM-engine-expanded"
tag="v0.4.0-darkizuku-preview.1"
patch="$root/engine/bbhost-changes.patch"
[[ ${MSYSTEM:-} == CLANG64 ]] || { echo 'Open MSYS2 CLANG64 and run this script there.' >&2; exit 1; }
for executable in git cmake ninja clang python3 cygpath curl; do
    command -v "$executable" >/dev/null || { printf 'Missing dependency: %s\n' "$executable" >&2; exit 1; }
done
for header in expat.h nlohmann/json.hpp; do
    [[ -f "${MINGW_PREFIX:-/clang64}/include/$header" ]] || {
        echo 'Run: pacman -S --needed mingw-w64-clang-x86_64-expat mingw-w64-clang-x86_64-nlohmann-json' >&2; exit 1;
    }
done
git -C "$engine_source" rev-parse --git-dir >/dev/null
expected="$(git -C "$engine_source" rev-parse "${tag}^{commit}")"
if [[ ! -e "$engine_target" ]]; then
    git -C "$engine_source" worktree add --detach "$engine_target" "$tag"
fi
engine_target="$(cd -- "$engine_target" && pwd)"
[[ "$expected" == "$(git -C "$engine_target" rev-parse HEAD)" ]] || {
    echo 'The target has a different engine base. Choose a new empty directory as argument 2.' >&2; exit 1;
}
if git -C "$engine_target" apply --reverse --check --ignore-space-change "$patch" 2>/dev/null; then
    echo 'Integration 20 is already applied; rebuilding.'
elif git -C "$engine_target" apply --check --ignore-space-change "$patch" 2>/dev/null; then
    git -C "$engine_target" apply --whitespace=nowarn --ignore-space-change "$patch"
else
    echo 'The target contains different source changes. Keep it and choose a new empty directory as argument 2.' >&2
    exit 1
fi
# Verify actual source bytes, including the renderer, before producing a build stamp.
python3 "$root/tools/verify-engine-sources.py" "$engine_target" "$root/engine/better-kbm-manifest.json"
git -C "$engine_target" submodule update --init --recursive
cp -- "$root/engine/win_compat.h" "$engine_target/gpu/win_compat.h"
# Copy player data and provider assets once; never copy old engine binaries or old shader caches.
python3 - "$previous" "$engine_target" <<'PY'
from pathlib import Path
import os,sys,shutil,time
old,new=map(Path,sys.argv[1:])
if old.resolve()!=new.resolve() and old.is_dir():
    for name in ['lantern.ini','bbport.ini','mods.json','patches.json','user','mods','network','engine-assets','fsr4_shaders','fsr4_411','fsr4vk','shader-catalog']:
        src,dst=old/name,new/name
        if not src.exists():continue
        files=[src] if src.is_file() else (p for p in src.rglob('*') if p.is_file())
        for p in files:
            relative=Path(name) if src.is_file() else Path(name)/p.relative_to(src)
            if 'cache' in relative.parts or '__pycache__' in relative.parts:continue
            target=new/relative
            if not target.exists():
                target.parent.mkdir(parents=True,exist_ok=True);shutil.copy2(p,target)
if not (new/'lantern.ini').exists() and (new/'bbport.ini').exists():shutil.copy2(new/'bbport.ini',new/'lantern.ini')
now=time.time()
for p in new.rglob('*'):
    if p.is_file() and '.git' not in p.parts and p.stat().st_mtime>now:os.utime(p,(now,now))
PY
compat_windows="$(cygpath -m "$engine_target/gpu/win_compat.h")"
export CFLAGS="${CFLAGS:-} -Wno-error=unused-but-set-global"
export CXXFLAGS="-include \"$compat_windows\" ${BB_EXTRA_CXXFLAGS:-}"
(
    cd -- "$engine_target"
    # Discard cached compiler flags, then let build.sh choose the upstream build options.
    cmake --fresh -S gpu -B out/gpu -G Ninja
    bash build.sh
    bash tools/fetch_dlss.sh
    # FSR assets are only needed when selected. Existing assets are reused without replacing them.
    if grep -Eq '^upscaler[[:space:]]*=[[:space:]]*fsr4[[:space:]]*$' lantern.ini 2>/dev/null; then
        bash tools/fetch_fsr4_assets.sh
    fi
    if grep -Eq '^frame_gen[[:space:]]*=[[:space:]]*dlss[[:space:]]*$' lantern.ini 2>/dev/null; then
        bash tools/fetch_streamline.sh
    fi
)
for program in bb-probe.exe bb-engine-assets.exe bb-start.exe bb-shader-lab.exe; do
    [[ -s "$engine_target/out/$program" ]] || { printf 'Missing built program: %s\n' "$program" >&2; exit 1; }
done
python3 "$root/tools/deploy-runtime-dlls.py" "$engine_target/out" --dll-dir /clang64/bin
python3 - "$engine_target" <<'PY'
from pathlib import Path
import hashlib,json,sys,datetime
root=Path(sys.argv[1]);out=root/'out'
stamp={'integration':'better-kbm-expanded','version':20,'upstream':'1ce45ae7747623b9792d54566cd5290a380555a1','upstream_release':'0.9.37 beta 6','built_utc':datetime.datetime.now(datetime.timezone.utc).isoformat(),
       'executables':{p.name:hashlib.sha256(p.read_bytes()).hexdigest() for p in [out/'bb-probe.exe',out/'bb-start.exe',out/'bb-shader-lab.exe',out/'bb-engine-assets.exe']}}
(out/'better-kbm-build.json').write_text(json.dumps(stamp),encoding='utf-8')
print('Verified native build:',stamp['upstream_release'],stamp['upstream'])
PY
bash "$root/tools/build-launcher-only.sh"
printf '\nLauncher: %s\nEngine folder: %s\n' \
    "$(cygpath -w "$root/LauncherApp/Bloodborne Launcher.exe")" "$(cygpath -w "$engine_target")"
echo 'IMPORTANT: select the NEW BetterKBM-engine-beta6 folder in the launcher before testing.'
echo 'The session log must say: BetterKBM native engine integration 20 | upstream 1ce45ae7...'
