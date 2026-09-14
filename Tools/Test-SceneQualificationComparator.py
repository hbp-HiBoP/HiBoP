"""Check that malformed evidence cannot qualify and deltas retain scientific units."""
import hashlib
import importlib.util
from pathlib import Path
import struct
import sys
import tempfile
import unittest


sys.dont_write_bytecode = True
spec = importlib.util.spec_from_file_location('comparator', Path(__file__).with_name('Compare-SceneQualification.py'))
comparator = importlib.util.module_from_spec(spec)
spec.loader.exec_module(comparator)


class EvidenceComparisonTests(unittest.TestCase):
    def write(self, directory, values, **counts):
        data = struct.pack('<' + 'f' * len(values), *values)
        (directory / 'column.bin').write_bytes(data)
        return dict(file='column.bin', sha256=hashlib.sha256(data).hexdigest(),
                    activityCount=counts.get('activity', 1), alphaCount=1,
                    gridPoints=1, colorCount=1)

    def test_quantities_and_maximum_location_are_separate(self):
        with tempfile.TemporaryDirectory() as root:
            left, right = Path(root) / 'left', Path(root) / 'right'
            left.mkdir()
            right.mkdir()
            a = [0.] * 11
            b = [0., .25, 0., .125, 0., 0., 2., 0., 0., 0., .5]
            lc, rc = self.write(left, a), self.write(right, b)
            _, quantities = comparator.compare_segments(left, right, lc, rc)
            for name, maximum, component in [('activity', .25, 1), ('alpha', .125, 1),
                                              ('grid', 2., 2), ('colors', .5, 3)]:
                self.assertEqual(quantities[name]['maximumAbsoluteDelta'], maximum)
                self.assertEqual(quantities[name]['maximumAt']['elementIndex'], 0)
                self.assertEqual(quantities[name]['maximumAt']['componentIndex'], component)

    def test_declared_size_must_match_binary_even_when_hash_is_valid(self):
        with tempfile.TemporaryDirectory() as root:
            directory = Path(root)
            column = self.write(directory, [0.] * 11, activity=2)
            with self.assertRaisesRegex(ValueError, 'length'):
                comparator.compare_segments(directory, directory, column, column)

    def test_corrupted_binary_is_rejected(self):
        with tempfile.TemporaryDirectory() as root:
            directory = Path(root)
            column = self.write(directory, [0.] * 11)
            (directory / 'column.bin').write_bytes(b'corrupt')
            with self.assertRaisesRegex(ValueError, 'hash mismatch'):
                comparator.buffers(directory, column)

    def test_empty_or_duplicate_evidence_is_rejected(self):
        for states in ([], [{'name': 'initial', 'columns': []}],
                       [{'name': 'initial', 'columns': [{'id': 'a'}, {'id': 'a'}]}],
                       [{'name': 'initial', 'columns': [{'id': 'a'}]}] * 2):
            with self.subTest(states=states), self.assertRaises(ValueError):
                comparator.validate_states({'states': states})

    def test_cut_pixels_dimensions_and_completeness_are_checked(self):
        with tempfile.TemporaryDirectory() as root:
            directory = Path(root)
            data = bytes([0, 64, 128, 255])
            (directory / 'cut.rgba').write_bytes(data)
            entries = [dict(cutIndex=0, kind=kind, width=1, height=1,
                            file='cut.rgba', sha256=hashlib.sha256(data).hexdigest())
                       for kind in ('base', 'functional')]
            column = {'cutTextures': entries}
            results = comparator.compare_cut_textures(directory, directory, column, column, 1)
            self.assertTrue(all(result['bytesExact'] for result in results))
            with self.assertRaisesRegex(ValueError, 'Missing'):
                comparator.compare_cut_textures(directory, directory, column, {'cutTextures': entries[:1]}, 1)
            entries[0]['width'] = 2
            with self.assertRaisesRegex(ValueError, 'dimensions'):
                comparator.compare_cut_textures(directory, directory, column, column, 1)
            entries[0]['width'] = 1
            (directory / 'cut.rgba').write_bytes(data[:-1] + b'\x00')
            with self.assertRaisesRegex(ValueError, 'hash mismatch'):
                comparator.compare_cut_textures(directory, directory, column, column, 1)

    def test_nonfinite_mismatch_cannot_disappear_in_rms(self):
        result = comparator.compare([float('nan'), float('inf'), 1.], [0., float('-inf'), 1.])
        self.assertEqual(result['nonfiniteMismatches'], 2)
        self.assertEqual(result['maximumAbsoluteDelta'], 0.)


if __name__ == '__main__':
    unittest.main()
