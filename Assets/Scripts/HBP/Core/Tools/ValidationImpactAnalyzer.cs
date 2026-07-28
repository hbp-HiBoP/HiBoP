using System;
using System.Collections.Generic;
using System.Linq;
using HBP.Core.Tools;

namespace HBP.Core.Data
{
    public static class ValidationImpactAnalyzer
    {
        public static ValidationRequest ForProtocols(
            IEnumerable<Protocol> before,
            IEnumerable<Protocol> after)
        {
            Dictionary<string, Protocol> beforeByID = ByID(before);
            Dictionary<string, Protocol> afterByID = ByID(after);
            HashSet<string> protocolIDs = new(StringComparer.Ordinal);
            HashSet<string> subBlocIDs = new(StringComparer.Ordinal);

            foreach (string protocolID in beforeByID.Keys
                .Union(afterByID.Keys, StringComparer.Ordinal))
            {
                beforeByID.TryGetValue(protocolID, out Protocol oldProtocol);
                afterByID.TryGetValue(protocolID, out Protocol newProtocol);
                Dictionary<string, string> oldSignatures =
                    GetEpochingSignatures(oldProtocol);
                Dictionary<string, string> newSignatures =
                    GetEpochingSignatures(newProtocol);
                IEnumerable<string> candidates = oldSignatures.Keys
                    .Union(newSignatures.Keys, StringComparer.Ordinal);

                foreach (string subBlocID in candidates)
                {
                    oldSignatures.TryGetValue(subBlocID, out string oldSignature);
                    newSignatures.TryGetValue(subBlocID, out string newSignature);
                    if (!string.Equals(
                        oldSignature,
                        newSignature,
                        StringComparison.Ordinal))
                    {
                        protocolIDs.Add(protocolID);
                        subBlocIDs.Add(subBlocID);
                    }
                }
            }

            return new ValidationRequest(
                subBlocIDs.Count == 0
                    ? ValidationAspect.None
                    : ValidationAspect.Epoching,
                protocolIDs: protocolIDs,
                subBlocIDs: subBlocIDs,
                force: true);
        }

        public static ValidationRequest ForPatients(
            IEnumerable<Patient> before,
            IEnumerable<Patient> after)
        {
            Dictionary<string, Patient> beforeByID = ByID(before);
            Dictionary<string, Patient> afterByID = ByID(after);
            HashSet<string> assetPatientIDs = new(StringComparer.Ordinal);
            HashSet<string> sitePatientIDs = new(StringComparer.Ordinal);

            foreach (string patientID in beforeByID.Keys
                .Union(afterByID.Keys, StringComparer.Ordinal))
            {
                beforeByID.TryGetValue(patientID, out Patient oldPatient);
                afterByID.TryGetValue(patientID, out Patient newPatient);
                if (newPatient == null)
                {
                    continue;
                }
                if (oldPatient == null)
                {
                    assetPatientIDs.Add(patientID);
                    sitePatientIDs.Add(patientID);
                    continue;
                }
                if (!string.Equals(
                    GetAssetSignature(oldPatient),
                    GetAssetSignature(newPatient),
                    StringComparison.Ordinal))
                {
                    assetPatientIDs.Add(patientID);
                }
                if (!string.Equals(
                    GetSiteNameSignature(oldPatient),
                    GetSiteNameSignature(newPatient),
                    StringComparison.Ordinal))
                {
                    sitePatientIDs.Add(patientID);
                }
            }

            ValidationRequest assets = new(
                assetPatientIDs.Count == 0
                    ? ValidationAspect.None
                    : ValidationAspect.PatientAssets,
                patientIDs: assetPatientIDs,
                force: true);
            ValidationRequest sites = new(
                sitePatientIDs.Count == 0
                    ? ValidationAspect.None
                    : ValidationAspect.ChannelMapping,
                patientIDs: sitePatientIDs,
                force: true);
            return assets.Merge(sites);
        }

        public static ValidationRequest ForDataInfo(
            DataInfo before,
            DataInfo after)
        {
            if (after == null)
            {
                return new ValidationRequest(ValidationAspect.None);
            }
            if (before == null ||
                before.GetType() != after.GetType() ||
                !string.Equals(
                    DataInfoValidationContext.GetSourceDefinitionSignature(before),
                    DataInfoValidationContext.GetSourceDefinitionSignature(after),
                    StringComparison.Ordinal))
            {
                return new ValidationRequest(
                    ValidationAspect.DataInfoAll,
                    dataInfoIDs: new[] { after.ID },
                    force: true);
            }

            ValidationAspect aspects = ValidationAspect.None;
            if (!string.Equals(before.Name, after.Name, StringComparison.Ordinal))
            {
                aspects |= ValidationAspect.Structure;
            }
            if (!string.Equals(
                before.Protocol?.ID,
                after.Protocol?.ID,
                StringComparison.Ordinal))
            {
                aspects |= ValidationAspect.Epoching;
            }
            if (before is PatientDataInfo oldPatientData &&
                after is PatientDataInfo newPatientData &&
                !string.Equals(
                    oldPatientData.Patient?.ID,
                    newPatientData.Patient?.ID,
                    StringComparison.Ordinal))
            {
                aspects |=
                    ValidationAspect.Structure |
                    ValidationAspect.ChannelMapping;
            }
            if (before is CCEPDataInfo oldCCEP &&
                after is CCEPDataInfo newCCEP &&
                !string.Equals(
                    oldCCEP.StimulatedChannel,
                    newCCEP.StimulatedChannel,
                    StringComparison.Ordinal))
            {
                aspects |= ValidationAspect.ChannelMapping;
            }
            return new ValidationRequest(
                aspects,
                dataInfoIDs: new[] { after.ID },
                force: true);
        }

        public static ValidationRequest ForDatasets(
            IEnumerable<Dataset> before,
            IEnumerable<Dataset> after)
        {
            Dictionary<string, DataInfo> beforeByID =
                (before ?? Enumerable.Empty<Dataset>())
                    .SelectMany(dataset => dataset.Data)
                    .Where(dataInfo => dataInfo != null)
                    .GroupBy(dataInfo => dataInfo.ID, StringComparer.Ordinal)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Last(),
                        StringComparer.Ordinal);
            Dictionary<string, DataInfo> afterByID =
                (after ?? Enumerable.Empty<Dataset>())
                    .SelectMany(dataset => dataset.Data)
                    .Where(dataInfo => dataInfo != null)
                    .GroupBy(dataInfo => dataInfo.ID, StringComparer.Ordinal)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Last(),
                        StringComparer.Ordinal);

            ValidationRequest result =
                new(ValidationAspect.None);
            foreach (string dataInfoID in afterByID.Keys)
            {
                beforeByID.TryGetValue(dataInfoID, out DataInfo oldDataInfo);
                result = result.Merge(
                    ForDataInfo(oldDataInfo, afterByID[dataInfoID]));
            }
            return result;
        }

        public static ValidationRequest ForDataInfos(
            IEnumerable<DataInfo> before,
            IEnumerable<DataInfo> after)
        {
            Dictionary<string, DataInfo> beforeByID =
                (before ?? Enumerable.Empty<DataInfo>())
                    .Where(dataInfo => dataInfo != null)
                    .GroupBy(dataInfo => dataInfo.ID, StringComparer.Ordinal)
                    .ToDictionary(
                        group => group.Key,
                        group => group.Last(),
                        StringComparer.Ordinal);
            ValidationRequest result = new(ValidationAspect.None);
            foreach (DataInfo dataInfo in after ??
                Enumerable.Empty<DataInfo>())
            {
                beforeByID.TryGetValue(dataInfo.ID, out DataInfo oldDataInfo);
                result = result.Merge(ForDataInfo(oldDataInfo, dataInfo));
            }
            return result;
        }

        public static ValidationRequest ForAliases(
            IEnumerable<Alias> before,
            IEnumerable<Alias> after,
            IEnumerable<DataInfo> dataInfos,
            IEnumerable<Patient> patients)
        {
            Alias[] oldAliases =
                (before ?? Enumerable.Empty<Alias>()).ToArray();
            Alias[] newAliases =
                (after ?? Enumerable.Empty<Alias>()).ToArray();
            string[] dataInfoIDs =
                (dataInfos ?? Enumerable.Empty<DataInfo>())
                    .Where(dataInfo =>
                        DataInfoValidationContext
                            .GetSavedSourcePaths(dataInfo)
                            .Any(path =>
                                ResolutionChanged(
                                    path,
                                    oldAliases,
                                    newAliases)))
                    .Select(dataInfo => dataInfo.ID)
                    .ToArray();
            string[] patientIDs =
                (patients ?? Enumerable.Empty<Patient>())
                    .Where(patient =>
                        GetSavedAssetPaths(patient).Any(path =>
                            ResolutionChanged(
                                path,
                                oldAliases,
                                newAliases)))
                    .Select(patient => patient.ID)
                    .ToArray();

            ValidationRequest sources = new(
                dataInfoIDs.Length == 0
                    ? ValidationAspect.None
                    : ValidationAspect.SourceAvailability,
                dataInfoIDs: dataInfoIDs,
                force: true);
            ValidationRequest assets = new(
                patientIDs.Length == 0
                    ? ValidationAspect.None
                    : ValidationAspect.PatientAssets,
                patientIDs: patientIDs,
                force: true);
            return sources.Merge(assets);
        }

        private static Dictionary<string, T> ByID<T>(
            IEnumerable<T> values)
            where T : BaseData
        {
            return (values ?? Enumerable.Empty<T>())
                .Where(value =>
                    value != null &&
                    !string.IsNullOrEmpty(value.ID))
                .GroupBy(value => value.ID, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group => group.Last(),
                    StringComparer.Ordinal);
        }

        private static Dictionary<string, string> GetEpochingSignatures(
            Protocol protocol)
        {
            if (protocol == null)
            {
                return new Dictionary<string, string>(StringComparer.Ordinal);
            }
            return protocol.Blocs
                .SelectMany(bloc => bloc.SubBlocs)
                .Where(subBloc => !string.IsNullOrEmpty(subBloc.ID))
                .GroupBy(subBloc => subBloc.ID, StringComparer.Ordinal)
                .ToDictionary(
                    group => group.Key,
                    group =>
                    {
                        SubBloc subBloc = group.Last();
                        Event mainEvent = subBloc.MainEvent;
                        string codes = mainEvent == null
                            ? string.Empty
                            : string.Join(
                                ",",
                                mainEvent.Codes
                                    .Distinct()
                                    .OrderBy(code => code));
                        bool epochable =
                            subBloc.Window.Length > 0 &&
                            mainEvent != null &&
                            mainEvent.Codes.Count > 0;
                        return $"{subBloc.Type}|{epochable}|{mainEvent?.ID}|{codes}";
                    },
                    StringComparer.Ordinal);
        }

        private static string GetSiteNameSignature(Patient patient)
        {
            return patient == null
                ? string.Empty
                : string.Join(
                    "|",
                    patient.Sites
                        .Select(site => site.Name ?? string.Empty)
                        .OrderBy(name => name, StringComparer.Ordinal));
        }

        private static string GetAssetSignature(Patient patient)
        {
            if (patient == null)
            {
                return string.Empty;
            }
            IEnumerable<string> meshes = patient.Meshes.Select(mesh =>
                mesh switch
                {
                    SingleMesh single =>
                        $"{mesh.ID}:{mesh.Name}:{single.SavedPath}",
                    LeftRightMesh leftRight =>
                        $"{mesh.ID}:{mesh.Name}:{leftRight.SavedLeftHemisphere}:{leftRight.SavedRightHemisphere}",
                    _ => $"{mesh.ID}:{mesh.Name}"
                });
            IEnumerable<string> MRIs = patient.MRIs.Select(MRI =>
                $"{MRI.ID}:{MRI.Name}:{MRI.SavedFile}");
            return string.Join(
                "|",
                meshes.Concat(MRIs)
                    .OrderBy(value => value, StringComparer.Ordinal));
        }

        private static IEnumerable<string> GetSavedAssetPaths(
            Patient patient)
        {
            IEnumerable<string> meshPaths = patient.Meshes.SelectMany(mesh =>
                mesh switch
                {
                    SingleMesh single =>
                        new[] { single.SavedPath },
                    LeftRightMesh leftRight =>
                        new[]
                        {
                            leftRight.SavedLeftHemisphere,
                            leftRight.SavedRightHemisphere
                        },
                    _ => Array.Empty<string>()
                });
            return meshPaths.Concat(
                patient.MRIs.Select(MRI => MRI.SavedFile));
        }

        private static bool ResolutionChanged(
            string savedPath,
            IEnumerable<Alias> before,
            IEnumerable<Alias> after)
        {
            return !string.Equals(
                Resolve(savedPath, before),
                Resolve(savedPath, after),
                StringComparison.OrdinalIgnoreCase);
        }

        private static string Resolve(
            string savedPath,
            IEnumerable<Alias> aliases)
        {
            string resolved = savedPath ?? string.Empty;
            foreach (Alias alias in aliases)
            {
                alias?.ConvertKeyToValue(ref resolved);
            }
            return resolved.StandardizeToEnvironement();
        }
    }
}
