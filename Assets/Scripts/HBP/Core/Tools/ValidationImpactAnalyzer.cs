using System;
using System.Collections.Generic;
using System.Linq;
using HBP.Core.Tools;

namespace HBP.Core.Data
{
    public static class ValidationImpactAnalyzer
    {
        internal static DatabaseSnapshot CaptureDatabase(IEnumerable<Patient> patients, IEnumerable<DataInfo> dataInfos)
        {
            return new DatabaseSnapshot(patients, dataInfos);
        }

        public static ValidationRequest ForProtocols(IEnumerable<Protocol> before, IEnumerable<Protocol> after)
        {
            var beforeByID = ByID(before);
            var afterByID = ByID(after);
            HashSet<string> protocolIDs = new(StringComparer.Ordinal);
            HashSet<string> subBlocIDs = new(StringComparer.Ordinal);

            foreach (var protocolID in beforeByID.Keys.Union(afterByID.Keys, StringComparer.Ordinal))
            {
                beforeByID.TryGetValue(protocolID, out var oldProtocol);
                afterByID.TryGetValue(protocolID, out var newProtocol);
                var oldSignatures = GetEpochingSignatures(oldProtocol);
                var newSignatures = GetEpochingSignatures(newProtocol);
                var candidates = oldSignatures.Keys.Union(newSignatures.Keys, StringComparer.Ordinal);

                foreach (var subBlocID in candidates)
                {
                    oldSignatures.TryGetValue(subBlocID, out var oldSignature);
                    newSignatures.TryGetValue(subBlocID, out var newSignature);
                    if (!string.Equals(oldSignature, newSignature, StringComparison.Ordinal))
                    {
                        protocolIDs.Add(protocolID);
                        subBlocIDs.Add(subBlocID);
                    }
                }
            }

            return new ValidationRequest(subBlocIDs.Count == 0 ? ValidationAspect.None : ValidationAspect.Epoching, protocolIDs: protocolIDs, subBlocIDs: subBlocIDs, force: true);
        }

        public static ValidationRequest ForPatients(IEnumerable<Patient> before, IEnumerable<Patient> after)
        {
            return ForPatientStates(GetPatientStates(before), after);
        }

        public static ValidationRequest ForDataInfo(DataInfo before, DataInfo after)
        {
            if (after == null) return new ValidationRequest(ValidationAspect.None);

            var aspects = GetDataInfoAspects(before == null ? null : new DataInfoState(before), after);
            return new ValidationRequest(aspects, new[] { after.ID }, force: true);
        }

        public static ValidationRequest ForDatasets(IEnumerable<Dataset> before, IEnumerable<Dataset> after)
        {
            var beforeByID = (before ?? Enumerable.Empty<Dataset>()).SelectMany(dataset => dataset.Data).Where(dataInfo => dataInfo != null).GroupBy(dataInfo => dataInfo.ID, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.Last(), StringComparer.Ordinal);
            var afterByID = (after ?? Enumerable.Empty<Dataset>()).SelectMany(dataset => dataset.Data).Where(dataInfo => dataInfo != null).GroupBy(dataInfo => dataInfo.ID, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.Last(), StringComparer.Ordinal);

            return ForDataInfos(beforeByID.Values, afterByID.Values);
        }

        public static ValidationRequest ForDataInfos(IEnumerable<DataInfo> before, IEnumerable<DataInfo> after)
        {
            return ForDataInfoStates(GetDataInfoStates(before), after);
        }

        public static ValidationRequest ForAliases(IEnumerable<Alias> before, IEnumerable<Alias> after, IEnumerable<DataInfo> dataInfos, IEnumerable<Patient> patients)
        {
            var oldAliases = (before ?? Enumerable.Empty<Alias>()).ToArray();
            var newAliases = (after ?? Enumerable.Empty<Alias>()).ToArray();
            var dataInfoIDs = (dataInfos ?? Enumerable.Empty<DataInfo>()).Where(dataInfo => DataInfoValidationContext.GetSavedSourcePaths(dataInfo).Any(path => ResolutionChanged(path, oldAliases, newAliases))).Select(dataInfo => dataInfo.ID).ToArray();
            var patientIDs = (patients ?? Enumerable.Empty<Patient>()).Where(patient => GetSavedAssetPaths(patient).Any(path => ResolutionChanged(path, oldAliases, newAliases))).Select(patient => patient.ID).ToArray();

            ValidationRequest sources = new(dataInfoIDs.Length == 0 ? ValidationAspect.None : ValidationAspect.SourceAvailability, dataInfoIDs, force: true);
            ValidationRequest assets = new(patientIDs.Length == 0 ? ValidationAspect.None : ValidationAspect.PatientAssets, patientIDs: patientIDs, force: true);
            return sources.Merge(assets);
        }

        private static Dictionary<string, T> ByID<T>(IEnumerable<T> values) where T : BaseData
        {
            return (values ?? Enumerable.Empty<T>()).Where(value => value != null && !string.IsNullOrEmpty(value.ID)).GroupBy(value => value.ID, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.Last(), StringComparer.Ordinal);
        }

        private static Dictionary<string, PatientState> GetPatientStates(IEnumerable<Patient> patients)
        {
            return ByID(patients).ToDictionary(pair => pair.Key, pair => new PatientState(pair.Value), StringComparer.Ordinal);
        }

        private static Dictionary<string, DataInfoState> GetDataInfoStates(IEnumerable<DataInfo> dataInfos)
        {
            return ByID(dataInfos).ToDictionary(pair => pair.Key, pair => new DataInfoState(pair.Value), StringComparer.Ordinal);
        }

        private static ValidationRequest ForPatientStates(IReadOnlyDictionary<string, PatientState> beforeByID, IEnumerable<Patient> after)
        {
            var afterByID = ByID(after);
            HashSet<string> assetPatientIDs = new(StringComparer.Ordinal);
            HashSet<string> sitePatientIDs = new(StringComparer.Ordinal);

            foreach (var pair in afterByID)
            {
                var patientID = pair.Key;
                var newPatient = pair.Value;
                if (!beforeByID.TryGetValue(patientID, out var oldPatient))
                {
                    assetPatientIDs.Add(patientID);
                    sitePatientIDs.Add(patientID);
                    continue;
                }

                if (!string.Equals(oldPatient.AssetSignature, GetAssetSignature(newPatient), StringComparison.Ordinal)) assetPatientIDs.Add(patientID);

                if (!string.Equals(oldPatient.SiteNameSignature, GetSiteNameSignature(newPatient), StringComparison.Ordinal)) sitePatientIDs.Add(patientID);
            }

            ValidationRequest assets = new(assetPatientIDs.Count == 0 ? ValidationAspect.None : ValidationAspect.PatientAssets, patientIDs: assetPatientIDs, force: true);
            ValidationRequest sites = new(sitePatientIDs.Count == 0 ? ValidationAspect.None : ValidationAspect.ChannelMapping, patientIDs: sitePatientIDs, force: true);
            return assets.Merge(sites);
        }

        private static ValidationRequest ForDataInfoStates(IReadOnlyDictionary<string, DataInfoState> beforeByID, IEnumerable<DataInfo> after)
        {
            Dictionary<ValidationAspect, HashSet<string>> dataInfoIDsByAspects = new();
            foreach (var dataInfo in ByID(after).Values)
            {
                beforeByID.TryGetValue(dataInfo.ID, out var oldDataInfo);
                var aspects = GetDataInfoAspects(oldDataInfo, dataInfo);
                if (aspects == ValidationAspect.None) continue;

                if (!dataInfoIDsByAspects.TryGetValue(aspects, out var dataInfoIDs))
                {
                    dataInfoIDs = new HashSet<string>(StringComparer.Ordinal);
                    dataInfoIDsByAspects.Add(aspects, dataInfoIDs);
                }

                dataInfoIDs.Add(dataInfo.ID);
            }

            ValidationRequest result = new(ValidationAspect.None);
            foreach (var pair in dataInfoIDsByAspects) result = result.Merge(new ValidationRequest(pair.Key, pair.Value, force: true));

            return result;
        }

        private static ValidationAspect GetDataInfoAspects(DataInfoState before, DataInfo after)
        {
            if (before == null || before.Type != after.GetType() || !string.Equals(before.SourceDefinitionSignature, DataInfoValidationContext.GetSourceDefinitionSignature(after), StringComparison.Ordinal)) return ValidationAspect.DataInfoAll;

            var aspects = ValidationAspect.None;
            if (!string.Equals(before.Name, after.Name, StringComparison.Ordinal)) aspects |= ValidationAspect.Structure;

            if (!string.Equals(before.ProtocolID, after.Protocol?.ID, StringComparison.Ordinal)) aspects |= ValidationAspect.Epoching;

            if (after is PatientDataInfo patientDataInfo && !string.Equals(before.PatientID, patientDataInfo.Patient?.ID, StringComparison.Ordinal)) aspects |= ValidationAspect.Structure | ValidationAspect.ChannelMapping;

            if (after is CCEPDataInfo CCEPDataInfo && !string.Equals(before.StimulatedChannel, CCEPDataInfo.StimulatedChannel, StringComparison.Ordinal)) aspects |= ValidationAspect.ChannelMapping;

            return aspects;
        }

        private static Dictionary<string, string> GetEpochingSignatures(Protocol protocol)
        {
            if (protocol == null) return new Dictionary<string, string>(StringComparer.Ordinal);

            return protocol.Blocs.SelectMany(bloc => bloc.SubBlocs).Where(subBloc => !string.IsNullOrEmpty(subBloc.ID)).GroupBy(subBloc => subBloc.ID, StringComparer.Ordinal).ToDictionary(group => group.Key, group =>
            {
                var subBloc = group.Last();
                var mainEvent = subBloc.MainEvent;
                var codes = mainEvent == null ? string.Empty : string.Join(",", mainEvent.Codes.Distinct().OrderBy(code => code));
                var epochable = subBloc.Window.Length > 0 && mainEvent != null && mainEvent.Codes.Count > 0;
                return $"{subBloc.Type}|{epochable}|{mainEvent?.ID}|{codes}";
            }, StringComparer.Ordinal);
        }

        private static string GetSiteNameSignature(Patient patient)
        {
            return patient == null ? string.Empty : string.Join("|", patient.Sites.Select(site => site.Name ?? string.Empty).OrderBy(name => name, StringComparer.Ordinal));
        }

        private static string GetAssetSignature(Patient patient)
        {
            if (patient == null) return string.Empty;

            var meshes = patient.Meshes.Select(mesh => mesh switch
            {
                SingleMesh single => $"{mesh.ID}:{mesh.Name}:{single.SavedPath}",
                LeftRightMesh leftRight => $"{mesh.ID}:{mesh.Name}:{leftRight.SavedLeftHemisphere}:{leftRight.SavedRightHemisphere}",
                _ => $"{mesh.ID}:{mesh.Name}"
            });
            var MRIs = patient.MRIs.Select(MRI => $"{MRI.ID}:{MRI.Name}:{MRI.SavedFile}");
            return string.Join("|", meshes.Concat(MRIs).OrderBy(value => value, StringComparer.Ordinal));
        }

        private static IEnumerable<string> GetSavedAssetPaths(Patient patient)
        {
            var meshPaths = patient.Meshes.SelectMany(mesh => mesh switch
            {
                SingleMesh single => new[] { single.SavedPath },
                LeftRightMesh leftRight => new[]
                {
                    leftRight.SavedLeftHemisphere,
                    leftRight.SavedRightHemisphere
                },
                _ => Array.Empty<string>()
            });
            return meshPaths.Concat(patient.MRIs.Select(MRI => MRI.SavedFile));
        }

        private static bool ResolutionChanged(string savedPath, IEnumerable<Alias> before, IEnumerable<Alias> after)
        {
            return !string.Equals(Resolve(savedPath, before), Resolve(savedPath, after), StringComparison.OrdinalIgnoreCase);
        }

        private static string Resolve(string savedPath, IEnumerable<Alias> aliases)
        {
            var resolved = savedPath ?? string.Empty;
            foreach (var alias in aliases) alias?.ConvertKeyToValue(ref resolved);

            return resolved.StandardizeToEnvironement();
        }

        internal sealed class DatabaseSnapshot
        {
            private readonly Dictionary<string, DataInfoState> m_DataInfos;
            private readonly Dictionary<string, PatientState> m_Patients;

            internal DatabaseSnapshot(IEnumerable<Patient> patients, IEnumerable<DataInfo> dataInfos)
            {
                m_Patients = GetPatientStates(patients);
                m_DataInfos = GetDataInfoStates(dataInfos);
            }

            internal ValidationRequest Compare(IEnumerable<Patient> patients, IEnumerable<DataInfo> dataInfos)
            {
                return ForPatientStates(m_Patients, patients).Merge(ForDataInfoStates(m_DataInfos, dataInfos));
            }
        }

        private sealed class PatientState
        {
            public PatientState(Patient patient)
            {
                AssetSignature = GetAssetSignature(patient);
                SiteNameSignature = GetSiteNameSignature(patient);
            }

            public string AssetSignature { get; }
            public string SiteNameSignature { get; }
        }

        private sealed class DataInfoState
        {
            public DataInfoState(DataInfo dataInfo)
            {
                Type = dataInfo.GetType();
                SourceDefinitionSignature = DataInfoValidationContext.GetSourceDefinitionSignature(dataInfo);
                Name = dataInfo.Name;
                ProtocolID = dataInfo.Protocol?.ID;
                PatientID = (dataInfo as PatientDataInfo)?.Patient?.ID;
                StimulatedChannel = (dataInfo as CCEPDataInfo)?.StimulatedChannel;
            }

            public Type Type { get; }
            public string SourceDefinitionSignature { get; }
            public string Name { get; }
            public string ProtocolID { get; }
            public string PatientID { get; }
            public string StimulatedChannel { get; }
        }
    }
}
