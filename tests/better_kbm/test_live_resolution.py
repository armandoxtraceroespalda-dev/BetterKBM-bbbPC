"""Run the production Windows startup resolver without launching the game or a GPU process."""
import contextlib
import importlib.util
import io
import os
from pathlib import Path
import sys
import tempfile
import unittest
from unittest.mock import patch

ENGINE = Path(os.environ['BB_KBM_TEST_ENGINE']).resolve()
sys.path.insert(0, str(ENGINE / 'scripts'))
spec = importlib.util.spec_from_file_location('tested_run_windows', ENGINE / 'scripts/run_windows.py')
runner = importlib.util.module_from_spec(spec)
spec.loader.exec_module(runner)

class LiveResolution(unittest.TestCase):
    def resolve(self, selected, override=None, capabilities='1'):
        with tempfile.TemporaryDirectory() as temporary, patch.dict(os.environ, {}, clear=True):
            root = Path(temporary)
            config = root / 'bbport.ini'
            config.write_text(f'live_resolution={selected}\noutput_res=1920x1080\nupscaler=taa\n')
            if override is not None:
                os.environ['BB_LIVE_RES'] = override
            with patch.object(runner, 'run', return_value=capabilities) as gpu, contextlib.redirect_stdout(io.StringIO()):
                live = runner.configure_live_resolution(config, root / 'out')
                self.assertEqual(os.environ['BB_LIVE_RES'], live)
                return live, gpu.call_args_list

    def test_explicit_modes_need_no_gpu_probe_even_at_1080p_taa(self):
        for selected in ('0', '1'):
            live, calls = self.resolve(selected)
            self.assertEqual(live, selected)
            self.assertEqual(calls, [])

    def test_auto_resolves_both_gpu_outcomes(self):
        for capabilities in ('0', '1', ''):
            live, calls = self.resolve('auto', capabilities=capabilities)
            self.assertEqual(live, '1' if capabilities == '1' else '0')
            self.assertEqual(len(calls), 1)
            self.assertEqual(calls[0].args[0][-1], '--live-resolution')

    def test_explicit_environment_override(self):
        self.assertEqual(self.resolve('0', '1')[0], '1')
        self.assertEqual(self.resolve('1', '0')[0], '0')
        self.assertEqual(self.resolve('0', 'auto', '1')[0], '1')

    def test_restart_reloads_setting_instead_of_inherited_resolved_flag(self):
        with tempfile.TemporaryDirectory() as temporary, patch.dict(os.environ, {}, clear=True), contextlib.redirect_stdout(io.StringIO()):
            root = Path(temporary)
            config = root / 'bbport.ini'
            with patch.object(runner, 'run', return_value='0') as gpu:
                for selected in ('auto', '1', '0', '1'):
                    config.write_text(f'live_resolution={selected}\n')
                    self.assertEqual(runner.configure_live_resolution(config, root), '0' if selected == 'auto' else selected)
                self.assertEqual(gpu.call_count, 1)

    def test_explicit_auto_override_is_probed_again_after_restart(self):
        with tempfile.TemporaryDirectory() as temporary, patch.dict(os.environ, {'BB_LIVE_RES': 'auto'}, clear=True), contextlib.redirect_stdout(io.StringIO()):
            root = Path(temporary)
            config = root / 'bbport.ini'
            config.write_text('live_resolution=0\n')
            with patch.object(runner, 'run', side_effect=['0', '1']) as gpu:
                self.assertEqual(runner.configure_live_resolution(config, root), '0')
                self.assertEqual(runner.configure_live_resolution(config, root), '1')
                self.assertEqual(gpu.call_count, 2)

    def test_invalid_setting_is_safely_off(self):
        self.assertEqual(self.resolve('unexpected')[0], '0')

if __name__ == '__main__':
    unittest.main()
