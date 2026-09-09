"""Compare complete DensityBenchmark exports. Reports errors; never assigns a tolerance.

Usage: python Tools/Compare-QuestDensity.py WINDOWS_DIR ANDROID_DIR OUTPUT_JSON
Requires numpy. A zero exit code means inputs/categories match and floats are finite,
not scientific acceptance of any observed numerical discrepancy.
"""
import json
import sys
from pathlib import Path
import numpy as np


def load(root, name):
    metadata = json.loads((root / (name + '.json')).read_text(encoding='utf-8-sig'))
    data = np.fromfile(root / (name + '.bin'), dtype='<f4')
    n, g = metadata['vertices'], metadata['gridPoints']
    if len(data) != 1 + 7 * n + 3 * g:
        raise ValueError(f'{root}/{name}: inconsistent binary length')
    return metadata, {
        'maximum': data[:1],
        'activity': data[1:1 + 2*n].reshape(n, 2),
        'alpha': data[1 + 2*n:1 + 4*n].reshape(n, 2),
        'grid': data[1 + 4*n:1 + 4*n + 3*g].reshape(g, 3),
        'positions': data[1 + 4*n + 3*g:].reshape(n, 3),
    }


def compare(left, right, name):
    lm, la = load(left, name)
    rm, ra = load(right, name)
    exact = {key: lm[key] == rm[key] for key in
             ('inputSha256', 'unity', 'nativeVersion', 'vertices', 'gridPoints', 'sites', 'dimensions', 'masks', 'coverage')}
    for platform, metadata, buffers in (('windows', lm, la), ('android', rm, ra)):
        if name.startswith(('none-', 'masked-')):
            activity_sentinel = np.array([0.5, 1], dtype=np.float32)
            alpha_sentinel = np.array([0.01, 1], dtype=np.float32)
            exact[platform + 'EmptyDensity'] = bool(buffers['maximum'][0] == 0 and
                np.all(buffers['activity'] == activity_sentinel) and np.all(buffers['alpha'] == alpha_sentinel))
        if name.startswith('single-'):
            exact[platform + 'SingleSiteBound'] = bool(0 < buffers['maximum'][0] <= 1)
        if name.startswith('boundary-'):
            exact[platform + 'OutsideVolume'] = bool(metadata['coverage'][2] >= 1 and
                buffers['activity'][-1, 1] == 1 and buffers['alpha'][-1, 1] == 1)
    errors = {}
    for key in la:
        a, b = la[key], ra[key]
        if a.shape != b.shape:
            exact[key + 'Shape'] = False
            continue
        finite = bool(np.isfinite(a).all() and np.isfinite(b).all())
        exact[key + 'Finite'] = finite
        if not finite:
            continue
        if key in ('activity', 'alpha'):
            exact[key + 'Categories'] = bool(np.array_equal(a[:, 1], b[:, 1]))
            sentinels = (a[:, 1] > 0.5) | (b[:, 1] > 0.5)
            exact[key + 'Sentinels'] = bool(np.array_equal(a[sentinels], b[sentinels]))
        if key == 'positions':
            exact['positions'] = bool(np.array_equal(a, b))
        delta = np.abs(a.astype(np.float64) - b.astype(np.float64))
        index = np.unravel_index(int(delta.argmax()), delta.shape) if delta.size else (0,)
        entry = {'maxAbsolute': float(delta.max(initial=0)),
                 'rms': float(np.sqrt(np.mean(delta * delta))) if delta.size else 0,
                 'differentComponents': int(np.count_nonzero(delta)), 'worstIndex': [int(i) for i in index]}
        if delta.size:
            entry.update(windows=float(a[index]), android=float(b[index]))
            if key in ('activity', 'alpha'):
                entry['vertexPositionMm'] = la['positions'][index[0]].tolist()
            if key == 'grid':
                entry['gridPointMm'] = a[index[0]].tolist()
        errors[key] = entry
    return {'exactChecks': exact, 'errors': errors, 'windows': lm, 'android': rm}


def main():
    left, right, output = map(Path, sys.argv[1:])
    for root in (left, right):
        if not json.loads((root / 'complete.json').read_text())['completed']:
            raise ValueError(f'{root}: incomplete benchmark')
    names = sorted(p.stem for p in left.glob('*.bin'))
    if len(names) != 18 or names != sorted(p.stem for p in right.glob('*.bin')):
        raise ValueError('Expected the same 18 benchmark runs')
    runs = {name: compare(left, right, name) for name in names}
    valid = all(all(run['exactChecks'].values()) for run in runs.values())
    report = {'inputsAndCategoriesMatch': valid, 'scientificAcceptance': 'PENDING_OWNER_REVIEW',
              'tolerancesApplied': None, 'runs': runs}
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, indent=2, allow_nan=False) + '\n', encoding='utf-8')
    print(json.dumps({'inputsAndCategoriesMatch': valid, 'runs': len(runs), 'output': str(output)}))
    return 0 if valid else 1


if __name__ == '__main__':
    sys.exit(main())
