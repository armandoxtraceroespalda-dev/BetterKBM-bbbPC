#!/usr/bin/env python3
# SPDX-License-Identifier: GPL-3.0-or-later
"""Reject incomplete or stale native engine integrations before building."""
from pathlib import Path
import hashlib,json,sys

def verify(root,manifest):
    errors=[]
    for name,expected in manifest['files'].items():
        path=root/name
        value=None
        if path.is_file():
            data=path.read_bytes()
            if b'\0' not in data:data=data.replace(b'\r\n',b'\n')
            value=hashlib.sha256(data).hexdigest()
        if value!=expected['after']:errors.append(name)
    # Older bridge layouts can leave an untracked source picked up by GLOB_RECURSE.
    legacy=root/'gpu/bbnet/vendor/shadps4/core/libraries/np/np_handler.cpp'
    if legacy.exists():errors.append(str(legacy.relative_to(root)))
    if errors:raise SystemExit('Engine source verification failed:\n'+'\n'.join(errors)+'\nUse a new empty engine target directory; keep the previous installation.')
    print(f"Verified integration {manifest['version']}: {len(manifest['files'])} engine source files; upstream {manifest['launcher_source_revision']}")

if __name__=='__main__':
    verify(Path(sys.argv[1]),json.loads(Path(sys.argv[2]).read_text()))
