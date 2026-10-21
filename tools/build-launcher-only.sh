#!/usr/bin/env bash
set -euo pipefail
root="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/.." && pwd)"
dotnet_command="$(command -v dotnet.exe || command -v dotnet || true)"
if [[ -z "$dotnet_command" && -f '/c/Program Files/dotnet/dotnet.exe' ]]; then
    dotnet_command='/c/Program Files/dotnet/dotnet.exe'
fi
[[ -n "$dotnet_command" ]] || { printf '%s\n' 'Install a Windows .NET SDK (8 or newer), then reopen CLANG64.' >&2; exit 1; }
# MSBuild may remove content it tracked in an earlier publish. Preserve the runtime-generated
# native artwork independently of the source project's content list, even when publishing fails.
gui_backup="$(mktemp -d)"
if [[ -d "$root/LauncherApp/gui" ]]; then
    cp -a -- "$root/LauncherApp/gui" "$gui_backup/gui"
fi
restore_gui() {
    local original_status=$?
    if [[ -d "$gui_backup/gui" ]]; then
        mkdir -p -- "$root/LauncherApp/gui"
        if ! cp -a -- "$gui_backup/gui/." "$root/LauncherApp/gui/"; then
            printf '%s\n' "Could not restore the native GUI cache. Backup: $gui_backup/gui" >&2
            return 1
        fi
    fi
    rm -rf -- "$gui_backup"
    return "$original_status"
}
trap restore_gui EXIT
# Clean WPF intermediates so archive timestamps cannot cause stale UI reuse.
# Publish preserves the game-derived cache already stored in LauncherApp/gui.
project="$root/launcher/windows/BloodborneLauncher/BloodborneLauncher.csproj"
"$dotnet_command" clean "$project" -c Release -r win-x64
"$dotnet_command" publish "$project" -c Release -r win-x64 --self-contained true \
    -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "$root/LauncherApp"
printf '\n%s\n' 'Built Bloodborne UI 32. Open LauncherApp/Bloodborne Launcher.exe.'
printf '%s\n' 'On first start, select your game folder; missing menu assets are generated in LauncherApp/gui.'
