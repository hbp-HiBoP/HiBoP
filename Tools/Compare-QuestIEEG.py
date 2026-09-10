"""Compare complete QUEST-023 buffers, without a numerical tolerance.

Exit 0 requires matching inputs/categories and finite floats. Float discrepancies
remain reported and require explanation/acceptance before scientific validation.
Usage: python Tools/Compare-QuestIEEG.py WINDOWS_DIR ANDROID_DIR OUTPUT_JSON
"""
import json
import sys
from pathlib import Path
import numpy as np


def load(root, name):
    meta = json.loads((root / (name + '.json')).read_text(encoding='utf-8-sig'))
    data = np.fromfile(root / (name + '.bin'), dtype='<f4')
    n, g = meta['vertices'], meta['gridPoints']
    if len(data) != 4 * n + 3 * g:
        raise ValueError(f'{root}/{name}: incomplete buffers')
    return meta, {'activity': data[:2*n].reshape(n, 2),
                  'alpha': data[2*n:4*n].reshape(n, 2),
                  'grid': data[4*n:].reshape(g, 3)}


def compare(left, right, name):
    lm, la = load(left, name)
    rm, ra = load(right, name)
    checks = {k: lm[k] == rm[k] for k in (
        'inputSha256', 'unity', 'nativeVersion', 'provenance', 'vertices',
        'gridPoints', 'dimensions', 'masks', 'coverage', 'surfaceValues',
        'siteValues', 'availability', 'diameters', 'colors', 'visible')}
    errors = {}
    for key, a in la.items():
        b = ra[key]
        checks[key + 'Finite'] = bool(np.isfinite(a).all() and np.isfinite(b).all())
        checks[key + 'Shape'] = a.shape == b.shape
        if not checks[key + 'Finite'] or not checks[key + 'Shape']:
            continue
        if key in ('activity', 'alpha'):
            checks[key + 'Categories'] = bool(np.array_equal(a[:, 1], b[:, 1]))
            sentinel = (a[:, 1] > .5) | (b[:, 1] > .5)
            checks[key + 'Sentinels'] = bool(np.array_equal(a[sentinel], b[sentinel]))
        else:
            checks['gridExact'] = bool(np.array_equal(a, b))
        delta = np.abs(a.astype(np.float64) - b.astype(np.float64))
        where = np.unravel_index(int(delta.argmax()), delta.shape) if delta.size else (0, 0)
        errors[key] = dict(maxAbsolute=float(delta.max(initial=0)),
                           rms=float(np.sqrt(np.mean(delta*delta))) if delta.size else 0,
                           differentComponents=int(np.count_nonzero(delta)),
                           worstIndex=[int(i) for i in where],
                           windows=float(a[where]) if delta.size else None,
                           android=float(b[where]) if delta.size else None)
    return dict(exactChecks=checks, errors=errors, windows=lm, android=rm)


def main():
    left, right, output = map(Path, sys.argv[1:])
    names = sorted(p.stem for p in left.glob('*.bin'))
    if len(names) != 36 or names != sorted(p.stem for p in right.glob('*.bin')):
        raise ValueError('Expected identical sets of 36 exports (18 instants, two runs).')
    runs = {name: compare(left, right, name) for name in names}
    repeated = {}
    restored = {}
    changed = {}
    for platform, root in [('windows', left), ('android', right)]:
        for name in names:
            if name.endswith('-0'):
                repeated[platform + ':' + name] = (root / (name + '.bin')).read_bytes() == (root / (name[:-1] + '1.bin')).read_bytes()
            if name.startswith('config0'):
                restored[platform + ':' + name] = (root / (name + '.bin')).read_bytes() == (root / (name.replace('config0', 'config2') + '.bin')).read_bytes()
        a = load(root, 'config0-index50-0')[1]['activity']
        b = load(root, 'config1-index50-0')[1]['activity']
        changed[platform] = not np.array_equal(a, b)
    valid = all(all(r['exactChecks'].values()) for r in runs.values()) and all(repeated.values()) and all(restored.values()) and all(changed.values())
    exact = valid and all(e['maxAbsolute'] == 0 for r in runs.values() for e in r['errors'].values())
    report = dict(inputsAndCategoriesMatch=valid, bitExact=exact, tolerancesApplied=None,
                  scientificAcceptance='BIT_EXACT' if exact else 'PENDING_EXPLANATION_AND_REVIEW',
                  repeated=repeated, restored=restored, changed=changed, runs=runs)
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, indent=2, allow_nan=False) + '\n', encoding='utf-8')
    print(json.dumps(dict(inputsAndCategoriesMatch=valid, bitExact=exact, runs=len(runs), output=str(output))))
    return 0 if valid else 1


if __name__ == '__main__':
    sys.exit(main())
