"""SCENE-008: six real Desktop modalities, synthetic data only, reproducible IDs.

Reuses the QUEST-020 signal/reference protocol and native loader fixtures. Files
are absolute references for this checkout; regenerate after moving the project.
"""
import copy
import gzip
import json
from pathlib import Path
import runpy
import struct
import zipfile

ROOT = Path(__file__).resolve().parents[1]
runpy.run_path(str(ROOT / 'Tools/Prepare-QuestIEEGFixture.py'))
OUT = ROOT / '.artifacts/scene-008/fixture'
OUT.mkdir(parents=True, exist_ok=True)
# Use the native fixture's actual samples over a synthetic MNI-sized field of view.
# The original 5 mm cube never intersects the full cortical surface.
volumes = {}
for name in ('fmri_4d.nii.gz', 'fmri_3d.nii', 'mask_binary.nii', 'megv_mask.nii'):
    source = ROOT / 'Assets/Tests/Fixtures/Native/Nifti' / name
    data = bytearray(gzip.decompress(source.read_bytes()) if name.endswith('.gz') else source.read_bytes())
    struct.pack_into('<3f', data, 80, 50, 50, 50)
    struct.pack_into('<2h', data, 252, 0, 2)
    struct.pack_into('<12f', data, 280, 50, 0, 0, -100, 0, 50, 0, -125, 0, 0, 50, -75)
    target = OUT / (name.replace('.nii.gz', '').replace('.nii', '') + '-mni.nii')
    target.write_bytes(data)
    volumes[name] = str(target)
with zipfile.ZipFile(ROOT / '.artifacts/quest-020/fixture/quest-mni-ieeg.hibop') as archive:
    entries = {n: json.loads(archive.read(n)) for n in archive.namelist() if not n.endswith('/')}
with zipfile.ZipFile(ROOT / 'Assets/Tests/Fixtures/Projects/Generated/native-fixture-reference.hibop') as archive:
    templates = json.loads(archive.read('Visualizations/visualization-alpha.visualization'))
    native = json.loads(archive.read('Datasets/native-fixture-dataset.dataset'))
    patient_template = json.loads(archive.read('Patients/native-patient-001.patient'))

visualization = entries.pop('Visualizations/MNI iEEG.visualization')
visualization.update(Name='SCENE-008 six modalities', ID='scene-008-visualization')
dataset = entries['Datasets/QUEST-020.dataset']
signal = copy.deepcopy(visualization['Columns'][0])
columns = []
for template in templates['Columns']:
    column = copy.deepcopy(template)
    kind = column['$type'].split(',')[0].split('.')[-1].replace('Column', '')
    column.update(ID='scene-008-' + kind, Name=kind)
    column['BaseConfiguration'] = copy.deepcopy(signal['BaseConfiguration'])
    column['BaseConfiguration']['ID'] = column['ID'] + '-base'
    for site_id, configuration in column['BaseConfiguration'].get('ConfigurationBySite', {}).items():
        configuration['ID'] = column['ID'] + '-site-' + site_id
    if 'Dataset' in column:
        column['Dataset'] = dataset['ID']
    if 'Bloc' in column:
        column['Bloc'] = signal['Bloc']
    if kind == 'IEEG':
        column['DataName'] = signal['DataName']
        column['DynamicConfiguration'] = copy.deepcopy(signal['DynamicConfiguration'])
        column['DynamicConfiguration']['ID'] = column['ID'] + '-dynamic'
    elif kind == 'CCEP':
        column['DataName'] = 'SCENE-008 responses'
    columns.append(column)
visualization['Columns'] = columns

# CCEP responses use the same independently loaded synthetic BrainVision trials.
for item in list(dataset['Data']):
    response = copy.deepcopy(item)
    response.update({'$type': 'HBP.Core.Data.CCEPDataInfo, HBP.Core.Runtime',
                     'Name': 'SCENE-008 responses', 'StimulatedChannel': 'A1',
                     'ID': item['ID'] + '-ccep'})
    response.pop('Normalization')
    response['DataContainer']['ID'] += '-ccep'
    dataset['Data'].append(response)

def resolve(value):
    if isinstance(value, dict):
        return {key: resolve(item) for key, item in value.items()}
    if isinstance(value, list):
        return [resolve(item) for item in value]
    if isinstance(value, str) and value.startswith('[HIBOP_NATIVE_FIXTURES]'):
        name = value.replace('\\', '/').rsplit('/', 1)[-1]
        if name in volumes:
            return volumes[name]
        return str(ROOT / 'Assets/Tests/Fixtures/Native' / value.split(']', 1)[1].lstrip('\\/').replace('\\', '/'))
    return value

for item in native['Data']:
    if any(kind in item['$type'] for kind in ('FMRIDataInfo', 'MEGcDataInfo', 'MEGvDataInfo', 'StaticDataInfo')):
        for index, patient_id in enumerate(visualization['Patients'] if 'Patient' in item else [None]):
            resource = resolve(copy.deepcopy(item))
            resource.update(m_ProtocolID=dataset['Protocol'])
            if patient_id:
                resource['Patient'] = patient_id
            resource['ID'] += '-' + str(index)
            for key in ('DataContainer', 'MaskDataContainer'):
                if key in resource:
                    resource[key]['ID'] += '-' + str(index)
            dataset['Data'].append(resource)

# A small patient-specific anatomical resource accompanies the full local MNI.
first_patient = next(value for key, value in entries.items()
                     if key.endswith('.patient') and value['ID'] == visualization['Patients'][0])
for key in ('Meshes', 'MRIs'):
    if key in patient_template:
        first_patient[key] = resolve(patient_template[key])

entries.pop('quest-mni-ieeg.settings')
entries['scene-008.settings'] = {'Version': '6.1.0', 'ID': 'scene-008-project'}
entries['Visualizations/SCENE-008.visualization'] = visualization
output = OUT / 'scene-008.hibop'
with zipfile.ZipFile(output, 'w', compression=zipfile.ZIP_STORED) as archive:
    for name in ['Patients/', 'Groups/', 'Datasets/', 'Visualizations/']:
        archive.writestr(zipfile.ZipInfo(name, (2000, 1, 1, 0, 0, 0)), '')
    for name, content in entries.items():
        archive.writestr(zipfile.ZipInfo(name, (2000, 1, 1, 0, 0, 0)), json.dumps(content, indent=2) + '\n')
(OUT / 'scene-008.prov').write_bytes((ROOT / '.artifacts/quest-020/fixture/quest-020.prov').read_bytes())
print(output)

# Same scientific modalities on a single patient, exercising patient mesh/MRI transfer.
visualization['Patients'] = visualization['Patients'][:1]
visualization['Configuration']['Mesh'] = first_patient['Meshes'][0]['Name']
visualization['Configuration']['MRI'] = first_patient['MRIs'][0]['Name']
with zipfile.ZipFile(OUT / 'scene-008-patient.hibop', 'w', compression=zipfile.ZIP_STORED) as archive:
    for name in ['Patients/', 'Groups/', 'Datasets/', 'Visualizations/']:
        archive.writestr(zipfile.ZipInfo(name, (2000, 1, 1, 0, 0, 0)), '')
    for name, content in entries.items():
        archive.writestr(zipfile.ZipInfo(name, (2000, 1, 1, 0, 0, 0)), json.dumps(content, indent=2) + '\n')
