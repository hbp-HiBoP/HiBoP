"""Deterministic QUEST-020 Desktop fixture; no acquired patient signals.
Uses the shipped VISU/FACE protocol, raw uV without normalization, MNI contacts.
"""
import json
import struct
import zipfile
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
OUTPUT = ROOT / '.artifacts/quest-020/fixture'
OUTPUT.mkdir(parents=True, exist_ok=True)
CONTACTS = ROOT / 'Docs/dev/quest-autonomous/fixtures/mni-contacts'
protocol = json.loads((ROOT / 'Assets/Data/DefaultDatabase/Protocols/VISU.prov').read_text(encoding='utf-8-sig'))
bloc = next(b for b in protocol['Blocs'] if b['Name'] == 'FACE')
protocol.update({'Name': 'QUEST-020 synthetic', 'ID': 'quest-020-protocol', 'Blocs': [bloc]})
bloc['ID'] = 'quest-020-bloc'
bloc['IllustrationPath'] = ''
bloc['SubBlocs'][0]['ID'] = 'quest-020-subbloc'
bloc['SubBlocs'][0]['Events'][0]['ID'] = 'quest-020-event'
(OUTPUT / 'quest-020.prov').write_text(json.dumps(protocol, indent=2) + '\n', encoding='utf-8')
visualization = json.loads((CONTACTS / 'MNI Contacts.visualization').read_text(encoding='utf-8-sig'))
visualization['Name'] = 'MNI iEEG'
visualization['ID'] = 'quest-020-visualization'
column = visualization['Columns'][0]
column.update({'$type': 'HBP.Core.Data.IEEGColumn, HBP.Core.Runtime', 'Name': 'Synthetic uV', 'ID': 'quest-020-column', 'Dataset': 'quest-020-dataset', 'Bloc': bloc['ID'], 'DataName': 'QUEST-020 synthetic'})
column.pop('AnatomicConfiguration')
column['DynamicConfiguration'] = {'ID': 'quest-020-dynamic', 'Site Maximum Influence': 15, 'Span Min': -10, 'Middle': 0, 'Span Max': 10}
column['BaseConfiguration']['ConfigurationBySite'] = {'quest-013-patient-right_A2': {'ID': 'quest-020-blacklist', 'IsBlacklisted': True}}
dataset = {'Name': 'QUEST-020 synthetic', 'ID': 'quest-020-dataset', 'Protocol': protocol['ID'], 'Data': []}
for patient, side in zip(visualization['Patients'], ['left', 'right']):
    stem = 'synthetic-' + side
    header = f'Brain Vision Data Exchange Header File Version 1.0\n[Common Infos]\nDataFile={stem}.eeg\nMarkerFile={stem}.vmrk\nDataFormat=BINARY\nDataOrientation=MULTIPLEXED\nNumberOfChannels=3\nSamplingInterval=10000\n\n[Binary Infos]\nBinaryFormat=IEEE_FLOAT_32\n\n[Channel Infos]\nCh1=A1,,1,uV\nCh2=A2,,1,uV\nCh3=B1,,1,uV\n'
    (OUTPUT / (stem + '.vhdr')).write_text(header, encoding='utf-8')
    (OUTPUT / (stem + '.vmrk')).write_text(f'Brain Vision Data Exchange Marker File, Version 1.0\n[Common Infos]\nDataFile={stem}.eeg\n\n[Marker Infos]\nMk1=New Segment,,1,1,0,00000000000000000000\nMk2=Stimulus,S 20,101,1,0\n', encoding='utf-8')
    with (OUTPUT / (stem + '.eeg')).open('wb') as f:
        for index in range(300):
            amplitude = 10.0 if index in (60, 180) else 1.0 + (index - 100) / 100.0
            f.write(struct.pack('<fff', -amplitude, 0, amplitude))
    dataset['Data'].append({'$type': 'HBP.Core.Data.IEEGDataInfo, HBP.Core.Runtime', 'Patient': patient, 'Name': 'QUEST-020 synthetic', 'ID': 'quest-020-data-' + side, 'm_ProtocolID': protocol['ID'], 'Normalization': 0, 'CorrespondingDatabaseID': '', 'm_Errors': [], 'm_Warnings': [], 'DataContainer': {'$type': 'HBP.Core.Data.Container.BrainVision, HBP.Core.Runtime', 'ID': 'quest-020-container-' + side, 'Header': str(OUTPUT / (stem + '.vhdr')), 'm_Errors': [], 'm_Warnings': []}})
entries = {'quest-mni-ieeg.settings': {'Version': '6.1.0', 'ID': 'quest-020-project'}, 'Datasets/QUEST-020.dataset': dataset, 'Visualizations/MNI iEEG.visualization': visualization}
for path in CONTACTS.glob('*.patient'):
    entries['Patients/' + path.name] = json.loads(path.read_text(encoding='utf-8-sig'))
archive = OUTPUT / 'quest-mni-ieeg.hibop'
with zipfile.ZipFile(archive, 'w', compression=zipfile.ZIP_STORED) as z:
    for name in ['Patients/', 'Groups/', 'Datasets/', 'Visualizations/']:
        z.writestr(zipfile.ZipInfo(name, (2000, 1, 1, 0, 0, 0)), '')
    for name, content in entries.items():
        z.writestr(zipfile.ZipInfo(name, (2000, 1, 1, 0, 0, 0)), json.dumps(content, indent=2, ensure_ascii=False) + '\n')
print(archive)
