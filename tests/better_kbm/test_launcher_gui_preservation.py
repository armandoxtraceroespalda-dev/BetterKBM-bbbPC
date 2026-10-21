"""Publishing must preserve locally extracted GUI files, including on build failure."""
from pathlib import Path
import os, shutil, subprocess, tempfile
source = Path(__file__).resolve().parents[2] / 'tools' / 'build-launcher-only.sh'
for fail in (False, True):
    with tempfile.TemporaryDirectory() as directory:
        root=Path(directory);(root/'tools').mkdir();shutil.copyfile(source,root/'tools/build-launcher-only.sh')
        project=root/'launcher/windows/BloodborneLauncher';project.mkdir(parents=True);(project/'BloodborneLauncher.csproj').write_text('<Project/>')
        gui=root/'LauncherApp/gui';gui.mkdir(parents=True)
        fixtures={'background.png':b'native background','menu-frame.png':b'native frame','BloodborneMenu.ttf':b'native font','assets-manifest.json':b'{"cache":"native"}','game-icon.png':b'native title icon','game-icon.ico':b'native Windows icon','sounds/hover.wav':b'local native tick','sounds/select.wav':b'legacy custom confirmation','sounds/confirm.wav':b'local soft confirmation','sounds/start.wav':b'local native start','sounds/change.wav':b'local native change','sounds/assets-manifest.json':b'native audio cache metadata'}
        for name,data in fixtures.items():
            path=gui/name;path.parent.mkdir(parents=True,exist_ok=True);path.write_bytes(data)
        fake=root/'fake';fake.mkdir();exe=fake/'dotnet.exe';exe.write_text('#!/usr/bin/env bash\nrm -rf -- "$GUI_TEST_ROOT/LauncherApp/gui"\nif [[ "$1" == publish && "$GUI_TEST_FAIL" == 1 ]]; then exit 23; fi\n')
        exe.chmod(0o755)
        env=dict(os.environ,PATH=str(fake)+os.pathsep+os.environ['PATH'],GUI_TEST_ROOT=str(root),GUI_TEST_FAIL='1' if fail else '0')
        result=subprocess.run(['bash',str(root/'tools/build-launcher-only.sh')],env=env,capture_output=True,text=True)
        assert result.returncode==(23 if fail else 0),(result.stdout,result.stderr)
        assert {name:(gui/name).read_bytes() for name in fixtures}==fixtures
print('PASS: native GUI cache restored after successful publish and failed publish; exit status preserved.')
