"""Create a visible synthetic patient recipe without changing the native loader fixture."""
import base64
import hashlib
import json
from pathlib import Path
import struct
import xml.etree.ElementTree as ET
import zipfile

ROOT = Path(__file__).resolve().parents[1]
OUT = ROOT / '.artifacts/scene-008/fixture'
SOURCE = OUT / 'scene-008-patient.hibop'
with zipfile.ZipFile(SOURCE) as archive:
    entries = {name: json.loads(archive.read(name)) for name in archive.namelist()
               if not name.endswith('/')}
visualization = entries['Visualizations/SCENE-008.visualization']
patient = next(value for name, value in entries.items()
               if name.endswith('.patient') and value['ID'] == visualization['Patients'][0])
mesh = patient['Meshes'][0]
reference_transform = ROOT / 'Assets/Data/Meshes/MNI.trm'
transform_values = [float(value) for value in reference_transform.read_text().split()]
assert len(transform_values) == 12
translation = transform_values[:3]
linear = [transform_values[3+i*3:6+i*3] for i in range(3)]
provenance = {'description': 'Synthetic visual recipe: MNI-derived surfaces loaded as patient data; no real patient.',
              'coordinateTransform': 'Bake MNI.trm into the source points, then x = 0.92*x + 5 mm; y = 0.96*y; z unchanged',
              'files': [{'source': str(reference_transform), 'sourceSha256': hashlib.sha256(reference_transform.read_bytes()).hexdigest()}]}
for side, key in [('L', 'LeftHemisphere'), ('R', 'RightHemisphere')]:
    source = ROOT / f'Assets/Data/Meshes/MNI_{side}hemi.gii'
    tree = ET.parse(source)
    points = next(a for a in tree.getroot().findall('DataArray')
                  if a.attrib['Intent'] == 'NIFTI_INTENT_POINTSET')
    assert points.attrib['Encoding'] == 'Base64Binary'
    assert points.attrib['DataType'] == 'NIFTI_TYPE_FLOAT32'
    assert points.attrib['Endian'] == 'LittleEndian'
    data = bytearray(base64.b64decode(points.find('Data').text))
    for offset in range(0, len(data), 12):
        source_point = struct.unpack_from('<3f', data, offset)
        x, y, z = [translation[i] + sum(linear[i][j]*source_point[j] for j in range(3)) for i in range(3)]
        struct.pack_into('<3f', data, offset, 0.92*x + 5, 0.96*y, z)
    points.find('Data').text = base64.b64encode(data).decode('ascii')
    target = OUT / f'prototype-patient-{side}.gii'
    tree.write(target, encoding='utf-8', xml_declaration=True)
    mesh[key] = str(target)
    provenance['files'].append({'source': str(source), 'sourceSha256': hashlib.sha256(source.read_bytes()).hexdigest(),
                                'output': str(target), 'sha256': hashlib.sha256(target.read_bytes()).hexdigest(),
                                'vertices': int(points.attrib['Dim0'])})
# The native test parcel arrays have four entries, so they cannot describe these surfaces.
mesh.pop('LeftMarsAtlasHemisphere', None)
mesh.pop('RightMarsAtlasHemisphere', None)
# Coordinates above include MNI's registered frame and its handedness. The native loader
# fixture's 4x4 identity file uses a .trm suffix with a different layout, which
# the TRM reader interprets as a singular transform (one axis collapses to zero).
mesh['Transformation'] = ''
mesh.update(ID='quest-024-visual-patient-mesh', Name='Prototype synthetic patient surface')
source_mri = ROOT / 'Assets/Tests/Fixtures/Native/Nifti/mri_t1.nii'
volume = bytearray(source_mri.read_bytes())
struct.pack_into('<3f', volume, 80, 50, 50, 50)
struct.pack_into('<2h', volume, 252, 0, 2)
struct.pack_into('<12f', volume, 280, 50, 0, 0, -100, 0, 50, 0, -125, 0, 0, 50, -75)
target_mri = OUT / 'prototype-patient-t1.nii'
target_mri.write_bytes(volume)
patient['MRIs'][0].update(ID='quest-024-visual-patient-mri', Name='Prototype synthetic patient MRI', File=str(target_mri))
visualization['Configuration']['Mesh'] = mesh['Name']
visualization['Configuration']['MRI'] = patient['MRIs'][0]['Name']
target_project = OUT / 'scene-008-patient-visual.hibop'
with zipfile.ZipFile(target_project, 'w', compression=zipfile.ZIP_STORED) as archive:
    for name in ['Patients/', 'Groups/', 'Datasets/', 'Visualizations/']:
        archive.writestr(zipfile.ZipInfo(name, (2000, 1, 1, 0, 0, 0)), '')
    for name, value in entries.items():
        archive.writestr(zipfile.ZipInfo(name, (2000, 1, 1, 0, 0, 0)), json.dumps(value, indent=2) + '\n')
for path in [source_mri, target_mri, target_project]:
    provenance['files'].append({'path': str(path), 'sha256': hashlib.sha256(path.read_bytes()).hexdigest()})
(OUT / 'prototype-patient-provenance.json').write_text(json.dumps(provenance, indent=2) + '\n')
print(target_project)
