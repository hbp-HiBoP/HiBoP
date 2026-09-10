"""Compare common-scene evidence; report floating deltas without inventing a tolerance."""
import argparse
from array import array
import hashlib
import json
import math
from pathlib import Path
import sys


def buffers(root, column):
    data = (root / column['file']).read_bytes()
    if hashlib.sha256(data).hexdigest().lower() != column['sha256'].replace('-', '').lower():
        raise ValueError('Evidence hash mismatch: ' + column['file'])
    values = array('f')
    values.frombytes(data)
    if sys.byteorder != 'little':
        values.byteswap()
    return values


def compare(left, right):
    if len(left) != len(right):
        return {'sameLength': False, 'leftCount': len(left), 'rightCount': len(right)}
    squared = maximum = 0.0
    different = finite = nonfinite_mismatch = 0
    for a, b in zip(left, right):
        if not math.isfinite(a) or not math.isfinite(b):
            if not (a == b or math.isnan(a) and math.isnan(b)):
                nonfinite_mismatch += 1
            continue
        delta = abs(a - b)
        maximum = max(maximum, delta)
        squared += delta * delta
        finite += 1
        different += a != b
    return {'sameLength': True, 'finiteCount': finite, 'differentFiniteValues': different,
            'nonfiniteMismatches': nonfinite_mismatch, 'maximumAbsoluteDelta': maximum,
            'rmsDelta': math.sqrt(squared / finite) if finite else 0.0}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--desktop', type=Path, required=True)
    parser.add_argument('--quest', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    args = parser.parse_args()
    left = json.loads((args.desktop / 'result.json').read_text(encoding='utf-8-sig'))
    right = json.loads((args.quest / 'result.json').read_text(encoding='utf-8-sig'))
    report = {'desktop': str(args.desktop), 'quest': str(args.quest), 'checks': [],
              'categoricalEquivalent': True, 'buffersExact': True,
              'tolerance': None, 'qualification': 'Exact comparison; any floating difference requires scientific review.'}
    if not left['success'] or not right['success']:
        raise ValueError('Both diagnostic runs must have completed successfully.')
    if [s['name'] for s in left['states']] != [s['name'] for s in right['states']]:
        raise ValueError('Different diagnostic scenarios.')
    for ls, rs in zip(left['states'], right['states']):
        if len(ls['columns']) != len(rs['columns']):
            raise ValueError('Different column counts.')
        for lc, rc in zip(ls['columns'], rs['columns']):
            keys = ('id', 'type', 'timeIndex', 'timeLength', 'vertices', 'activityCount', 'alphaCount',
                    'gridPoints', 'gridDimensions', 'colorCount', 'masks', 'resourceIndex', 'sourceSite')
            mismatches = [key for key in keys if lc.get(key) != rc.get(key)]
            hidden_appearance = []
            if len(lc['sites']) != len(rc['sites']):
                mismatches.append('sites.count')
            for lsite, rsite in zip(lc['sites'], rc['sites']):
                for key in set(lsite) | set(rsite):
                    if lsite.get(key) == rsite.get(key):
                        continue
                    # Disabled renderers retain their last material on Desktop.
                    # Record this history-dependent value; compare all visible appearances.
                    if key in ('material', 'materialColor') and not lsite['visible'] and not rsite['visible']:
                        hidden_appearance.append({'site': lsite['id'], 'field': key,
                                                  'desktop': lsite.get(key), 'quest': rsite.get(key)})
                    else:
                        mismatches.append('sites.' + lsite['id'] + '.' + key)
            mismatches += ['scene.' + key for key in ('sceneId', 'mesh', 'mri', 'cuts') if ls[key] != rs[key]]
            deltas = compare(buffers(args.desktop, lc), buffers(args.quest, rc))
            deltas['bytesExact'] = (lc['sha256'].replace('-', '').lower() == rc['sha256'].replace('-', '').lower())
            report['checks'].append({'state': ls['name'], 'column': lc['id'],
                                     'categoricalMismatches': mismatches, 'buffer': deltas,
                                     'inactiveMaterialHistoryDifferences': hidden_appearance})
            report['categoricalEquivalent'] &= not mismatches
            report['buffersExact'] &= deltas['bytesExact']
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({key: report[key] for key in ('categoricalEquivalent', 'buffersExact', 'tolerance')}))
    return 0 if report['categoricalEquivalent'] and report['buffersExact'] else 2


if __name__ == '__main__':
    raise SystemExit(main())
