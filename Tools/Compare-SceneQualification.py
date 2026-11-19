"""Compare common-scene evidence; report floating deltas without inventing a tolerance."""
import argparse
from array import array
import hashlib
import json
import math
from pathlib import Path
import sys


def evidence_bytes(root, column):
    data = (root / column['file']).read_bytes()
    if hashlib.sha256(data).hexdigest().lower() != column['sha256'].replace('-', '').lower():
        raise ValueError('Evidence hash mismatch: ' + column['file'])
    return data


def buffers(root, column):
    data = evidence_bytes(root, column)
    values = array('f')
    values.frombytes(data)
    if sys.byteorder != 'little':
        values.byteswap()
    return values


def segments(column):
    """Layout written by SceneQualification.WriteState, in float32 values."""
    counts = (('activity', 'activityCount', 2), ('alpha', 'alphaCount', 2),
              ('grid', 'gridPoints', 3), ('colors', 'colorCount', 4))
    offset = 0
    result = []
    for name, key, components in counts:
        count = column[key]
        if type(count) is not int or count < 0:
            raise ValueError('Invalid buffer count: ' + key)
        length = count * components
        result.append((name, offset, offset + length, components))
        offset += length
    return result


def compare(left, right):
    if len(left) != len(right):
        return {'sameLength': False, 'leftCount': len(left), 'rightCount': len(right)}
    if left == right:
        return {'sameLength': True, 'finiteCount': sum(map(math.isfinite, left)),
                'differentFiniteValues': 0, 'nonfiniteMismatches': 0,
                'maximumAbsoluteDelta': 0.0, 'rmsDelta': 0.0, 'maximumAt': None}
    squared = maximum = 0.0
    different = finite = nonfinite_mismatch = 0
    maximum_at = None
    for index, (a, b) in enumerate(zip(left, right)):
        if not math.isfinite(a) or not math.isfinite(b):
            if not (a == b or math.isnan(a) and math.isnan(b)):
                nonfinite_mismatch += 1
            continue
        delta = abs(a - b)
        if delta > maximum:
            maximum_at = {'floatIndex': index, 'desktop': a, 'quest': b}
        maximum = max(maximum, delta)
        squared += delta * delta
        finite += 1
        different += a != b
    return {'sameLength': True, 'finiteCount': finite, 'differentFiniteValues': different,
            'nonfiniteMismatches': nonfinite_mismatch, 'maximumAbsoluteDelta': maximum,
            'rmsDelta': math.sqrt(squared / finite) if finite else 0.0,
            'maximumAt': maximum_at}


def compare_segments(left_root, right_root, left_column, right_column):
    left, right = buffers(left_root, left_column), buffers(right_root, right_column)
    left_segments, right_segments = segments(left_column), segments(right_column)
    if len(left) != left_segments[-1][2] or len(right) != right_segments[-1][2]:
        raise ValueError('Buffer length does not match declared scientific arrays.')
    result = {}
    for (name, start, end, components), (_, rstart, rend, _) in zip(left_segments, right_segments):
        delta = compare(left[start:end], right[rstart:rend])
        if delta.get('maximumAt') is not None:
            location = delta['maximumAt']
            location['elementIndex'], location['componentIndex'] = divmod(location['floatIndex'], components)
        result[name] = delta
    return compare(left, right), result


def validate_states(report):
    states = report['states']
    if not states or len({s['name'] for s in states}) != len(states):
        raise ValueError('Diagnostic states must be nonempty and uniquely named.')
    for state in states:
        columns = state['columns']
        if not columns or len({c['id'] for c in columns}) != len(columns):
            raise ValueError('Diagnostic columns must be nonempty and uniquely identified.')


def compare_cut_textures(left_root, right_root, left_column, right_column, cut_count):
    expected = [(index, kind) for index in range(cut_count) for kind in ('base', 'functional')]
    left, right = left_column['cutTextures'], right_column['cutTextures']
    for entries in (left, right):
        if [(entry['cutIndex'], entry['kind']) for entry in entries] != expected:
            raise ValueError('Missing, duplicated or unexpected cut texture evidence.')
    results = []
    for lc, rc in zip(left, right):
        lb, rb = evidence_bytes(left_root, lc), evidence_bytes(right_root, rc)
        for entry, data in ((lc, lb), (rc, rb)):
            if any(type(entry[key]) is not int or entry[key] <= 0 for key in ('width', 'height')) or len(data) != entry['width'] * entry['height'] * 4:
                raise ValueError('Cut texture length does not match its RGBA dimensions.')
        results.append({'cutIndex': lc['cutIndex'], 'kind': lc['kind'],
                        'sameDimensions': (lc['width'], lc['height']) == (rc['width'], rc['height']),
                        'bytesExact': lb == rb, 'rgba8': compare(lb, rb)})
    return results


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--desktop', type=Path, required=True)
    parser.add_argument('--quest', type=Path, required=True)
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--surface-only', action='store_true',
                        help='Compare surface buffers against legacy evidence; explicitly exclude cut textures.')
    args = parser.parse_args()
    left = json.loads((args.desktop / 'result.json').read_text(encoding='utf-8-sig'))
    right = json.loads((args.quest / 'result.json').read_text(encoding='utf-8-sig'))
    report = {'desktop': str(args.desktop), 'quest': str(args.quest), 'checks': [],
              'categoricalEquivalent': True, 'buffersExact': True,
              'tolerance': None, 'qualification': 'Exact comparison; any floating difference requires scientific review.'}
    if not left['success'] or not right['success']:
        raise ValueError('Both diagnostic runs must have completed successfully.')
    validate_states(left)
    validate_states(right)
    if not args.surface_only and left.get('schemaVersion', 1) != right.get('schemaVersion', 1):
        raise ValueError('Different diagnostic evidence versions.')
    compare_textures = not args.surface_only and left.get('schemaVersion', 1) >= 2
    report['cutTexturesCompared'] = compare_textures
    report['surfaceOnlyRequested'] = args.surface_only
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
            deltas, quantities = compare_segments(args.desktop, args.quest, lc, rc)
            deltas['bytesExact'] = (lc['sha256'].replace('-', '').lower() == rc['sha256'].replace('-', '').lower())
            textures = compare_cut_textures(args.desktop, args.quest, lc, rc, ls['cuts']) if compare_textures else []
            if any(not texture['sameDimensions'] for texture in textures):
                mismatches.append('cutTextures.dimensions')
            report['checks'].append({'state': ls['name'], 'column': lc['id'],
                                     'categoricalMismatches': mismatches, 'buffer': deltas,
                                     'quantities': quantities,
                                     'cutTextures': textures,
                                     'inactiveMaterialHistoryDifferences': hidden_appearance})
            report['categoricalEquivalent'] &= not mismatches
            report['buffersExact'] &= deltas['bytesExact'] and all(texture['bytesExact'] for texture in textures)
    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(json.dumps(report, indent=2) + '\n', encoding='utf-8')
    print(json.dumps({key: report[key] for key in ('categoricalEquivalent', 'buffersExact', 'tolerance')}))
    return 0 if report['categoricalEquivalent'] and report['buffersExact'] else 2


if __name__ == '__main__':
    raise SystemExit(main())
