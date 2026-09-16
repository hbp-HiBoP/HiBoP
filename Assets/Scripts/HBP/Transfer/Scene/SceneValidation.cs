using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using HBP.Core.Data;
using HBP.Core.Data.Processed;
using HBP.Core.Enums;
using HBP.Core.Tools;
using DynamicData = HBP.Core.Data.Processed.DynamicData;

namespace HBP.Transfer.Scene
{
    internal static class SceneValidation
    {
        public static void Validate(ScenePayload payload, SceneArchive archive)
        {
            if (payload == null || payload.Version != archive.ContentVersion) Fail("Unknown visualization format.");
            if (archive.Globals == null || payload.GlobalContextId != archive.Globals.Id) Fail("Visualization global context does not match pairing.");
            if (string.IsNullOrWhiteSpace(payload.TransferId) || payload.TransferId.Length > 128 || string.IsNullOrWhiteSpace(payload.SessionId) || payload.SessionId.Length > 128) Fail("Invalid delivery identity.");
            var visualization = payload.Visualization;
            if (visualization?.Columns == null || visualization.Patients == null || visualization.Configuration == null || visualization.Columns.Count == 0 || visualization.Columns.Count > 4096 || payload.Columns?.Count != visualization.Columns.Count) Fail("Invalid visualization structure.");
            if (visualization.Patients.Any(p => p == null) || visualization.Columns.Any(c => c == null)) Fail("Missing graph node.");
            Unique(visualization.Patients.Select(p => p.ID));
            Unique(visualization.Columns.Select(c => c.ID));
            var patients = visualization.Patients.Select(p => p.ID).ToHashSet();
            if (payload.Meshes == null || !payload.Meshes.Any(m => m.PatientId == null) || payload.MRIs == null || !payload.MRIs.Any(m => m.PatientId == null) || payload.StandardFiles == null) Fail("Missing anatomy.");
            foreach (var entry in payload.StandardFiles)
                if (!(entry.Key.StartsWith("Atlases/", StringComparison.Ordinal) || entry.Key.StartsWith("IRM/", StringComparison.Ordinal) || entry.Key.StartsWith("Meshes/", StringComparison.Ordinal)) || entry.Value == null || entry.Value.Length != 64 || entry.Value.Any(c => !"0123456789abcdef".Contains(c)))
                    Fail("Invalid standard reference.");
            foreach (string required in new[] { "IRM/MNI.nii", "Meshes/MNI.trm", "Meshes/MNI_Lhemi.gii", "Meshes/MNI_Rhemi.gii", "Meshes/MNI_Lwhite.gii", "Meshes/MNI_Rwhite.gii" })
                if (!payload.StandardFiles.ContainsKey(required))
                    Fail("Missing standard identity.");

            void FileReference(string name)
            {
                if (name == null || !archive.ContainsResource(name)) Fail("Missing content resource.");
            }

            void NativeReference(string name)
            {
                FileReference(name);
                if (name.EndsWith(".pair", StringComparison.Ordinal)) archive.ReadNativePair(name);
                else if (StandardData.CompanionFile(name) != null) Fail("Native paired images require an explicit pair identity.");
            }

            foreach (var mesh in payload.Meshes)
            {
                if (mesh == null || string.IsNullOrEmpty(mesh.Name) || (mesh.PatientId != null && !patients.Contains(mesh.PatientId)) || !Enum.IsDefined(typeof(MeshType), mesh.Type)) Fail("Invalid mesh reference.");
                if (mesh.Standard != null && mesh.Standard != "grey" && mesh.Standard != "white") Fail("Unknown standard mesh.");
                if (!Enum.IsDefined(typeof(HBP.Core.Object3D.SurfaceRepresentation), mesh.Representation)) Fail("Invalid surface representation.");
                if (mesh.Standard == null) FileReference(mesh.Both);
                else
                {
                    if (mesh.Type != MeshType.MNI) Fail("Standard mesh must use MNI coordinates.");
                    Mask(mesh.StandardBothMask);
                    Mask(mesh.StandardLeftMask);
                    Mask(mesh.StandardRightMask);
                }

                if (mesh.SourceMRI != null && (mesh.Type != MeshType.Patient || mesh.Standard != null || mesh.Left != null || !payload.MRIs.Any(mri => mri.PatientId == mesh.PatientId && mri.Name == mesh.SourceMRI))) Fail("Invalid transient MRI surface origin.");
                FileReference(mesh.SimplifiedBoth);
                if (mesh.Standard != null || mesh.Left != null)
                {
                    FileReference(mesh.SimplifiedLeft);
                    FileReference(mesh.SimplifiedRight);
                }

                if (mesh.Representation == HBP.Core.Object3D.SurfaceRepresentation.Inflated && mesh.InflatedBoth == null) Fail("Missing inflated surface.");
                if (mesh.InflatedBoth != null)
                {
                    FileReference(mesh.InflatedSimplifiedBoth);
                    if (!Enum.IsDefined(typeof(HBP.Core.Object3D.SurfaceInflationPreset), mesh.InflationPreset) || !Enum.IsDefined(typeof(HBP.Core.DLL.SurfaceInflationCoordinateSpace), mesh.InflatedCoordinates) || !Enum.IsDefined(typeof(HBP.Core.DLL.SurfaceInflationMethod), mesh.InflationOptions.Method) || !Enum.IsDefined(typeof(HBP.Core.DLL.SurfaceInflationRescale), mesh.InflationOptions.Rescale)) Fail("Invalid prepared inflation metadata.");
                }

                if (mesh.InflatedLeft != null)
                {
                    FileReference(mesh.InflatedSimplifiedLeft);
                    FileReference(mesh.InflatedSimplifiedRight);
                }

                if ((mesh.Left == null) != (mesh.Right == null) || (mesh.InflatedLeft == null) != (mesh.InflatedRight == null)) Fail("Incomplete hemispheres.");
                foreach (string name in new[] { mesh.Left, mesh.Right, mesh.InflatedBoth, mesh.InflatedLeft, mesh.InflatedRight })
                    if (name != null)
                        FileReference(name);
            }

            foreach (var mri in payload.MRIs)
            {
                if (mri == null || (mri.PatientId != null && !patients.Contains(mri.PatientId)) || (mri.Standard != null && mri.Standard != "MNI")) Fail("Invalid MRI reference.");
                if (mri.Standard == null) NativeReference(mri.File);
            }

            var configuration = visualization.Configuration;
            if (configuration.ErasedTriangles != null) Mask(configuration.ErasedTriangles);
            if (configuration.ErasedSimplifiedTriangles != null) Mask(configuration.ErasedSimplifiedTriangles);
            if ((configuration.ErasedTriangles == null) != (configuration.ErasedSimplifiedTriangles == null)) Fail("Incomplete configured erasure.");
            if (!Enum.IsDefined(typeof(MeshPart), configuration.MeshPart) || !Enum.IsDefined(typeof(HBP.Core.Object3D.SurfaceRepresentation), configuration.SurfaceRepresentation)) Fail("Invalid selected mesh configuration.");
            bool selectedMeshAvailable = configuration.PreviewMRIName == null ? payload.Meshes.Any(m => m.PatientId == null && m.Name == configuration.MeshName) : payload.Meshes.Any(m => m.PatientId == null && m.SourceMRI == configuration.PreviewMRIName);
            if (!selectedMeshAvailable || !payload.MRIs.Any(m => m.PatientId == null && m.Name == configuration.MRIName)) Fail("Selected anatomy is unavailable.");
            for (int i = 0; i < payload.Columns.Count; i++)
            {
                var column = visualization.Columns[i];
                var current = payload.Columns[i];
                if (current == null || current.Id != column.ID || current.Functional == null || column.BaseConfiguration == null) Fail("Invalid column identity/configuration.");
                if (column is IEEGColumn ieeg)
                {
                    if (ieeg.Data?.ProcessedValuesByChannel == null || ieeg.DynamicConfiguration == null) Fail("Missing iEEG data.");
                    ValidateDynamic(ieeg.Data, ieeg.Bloc, ieeg.Data.ProcessedValuesByChannel.Values);
                    ValidateTrials(ieeg.Data.DataByChannelID.Values, ieeg.Bloc);
                }
                else if (column is CCEPColumn ccep)
                {
                    if (ccep.Data?.ProcessedValuesByChannelIDByStimulatedChannelID == null || ccep.CCEPConfiguration == null) Fail("Missing CCEP data.");
                    ValidateDynamic(ccep.Data, ccep.Bloc, ccep.Data.ProcessedValuesByChannelIDByStimulatedChannelID.Values.SelectMany(d => d.Values));
                    ValidateTrials(ccep.Data.DataByChannelIDByStimulatedChannelID.Values.SelectMany(d => d.Values), ccep.Bloc);
                    var source = ccep.CCEPConfiguration;
                    if (source?.SiteID != null && !ccep.Data.ProcessedValuesByChannelIDByStimulatedChannelID.ContainsKey(source.SiteID)) Fail("Unknown stimulation source.");
                    if (source != null && source.MarsAtlasLabel < -1) Fail("Invalid stimulation area.");
                }
                else if (column is StaticColumn stat)
                {
                    if (stat.Data?.ValueByChannelIDByLabel == null || stat.StaticConfiguration == null || stat.StaticConfiguration.SelectedResourceIndex < 0 || stat.StaticConfiguration.SelectedResourceIndex >= stat.Data.ValueByChannelIDByLabel.Count) Fail("Invalid static column.");
                }
                else if (column is FMRIColumn || column is MEGColumn)
                {
                    int index = column is FMRIColumn fmri ? fmri.FMRIConfiguration?.SelectedResourceIndex ?? -1 : ((MEGColumn)column).MEGConfiguration?.SelectedResourceIndex ?? -1;
                    if (index < 0 || index >= current.Functional.Count) Fail("Invalid functional column.");
                    foreach (var functional in current.Functional)
                    {
                        if (functional == null || (functional.PatientId != null && !patients.Contains(functional.PatientId))) Fail("Invalid functional patient.");
                        if (functional.File != null) NativeReference(functional.File);
                        else if (column is FMRIColumn) Fail("Missing functional image.");
                        if (functional.Mask != null) NativeReference(functional.Mask);
                        if (column is MEGColumn && (functional.Values == null || functional.Units == null || (functional.Values.Count > 0 && functional.Frequency <= 0) || float.IsNaN(functional.Frequency) || float.IsInfinity(functional.Frequency))) Fail("Invalid MEG channel metadata.");
                    }
                }
                else if (column is not AnatomicColumn) Fail("Unsupported modality.");
            }
        }

        private static void ValidateDynamic(DynamicData data, Bloc bloc, IEnumerable<float[]> signals)
        {
            if (data?.Timeline == null || data.ProjectionTimeline == null || bloc == null) Fail("Missing dynamic data.");
            foreach (var timeline in new[] { data.Timeline, data.ProjectionTimeline })
            {
                if (timeline.Length < 1 || timeline.Frequency == null || timeline.Frequency.Value <= 0 || timeline.SubTimelinesBySubBloc == null) Fail("Invalid timeline.");
                foreach (var entry in timeline.SubTimelinesBySubBloc)
                {
                    if (!bloc.SubBlocs.Any(sub => ReferenceEquals(sub, entry.Key)) || entry.Value == null || entry.Value.Length < 1 || entry.Value.StatisticsByEvent.Keys.Any(e => !entry.Key.Events.Any(ev => ReferenceEquals(ev, e)))) Fail("Timeline protocol identities do not match the column.");
                }
            }

            if (signals.Any(values => values == null || (values.Length != 0 && values.Length != data.ProjectionTimeline.Length))) Fail("Signal dimensions do not match the projection timeline.");
        }

        private static void Mask(int[] mask)
        {
            if (mask == null || mask.Any(value => value != 0 && value != 1)) Fail("Invalid visibility mask.");
        }

        private static void ValidateTrials(IEnumerable<BlocChannelData> channels, Bloc bloc)
        {
            foreach (var channel in channels)
            {
                if (channel?.Trials == null) Fail("Missing channel trials.");
                foreach (var trial in channel.Trials)
                {
                    if (trial?.ChannelSubTrialBySubBloc == null) Fail("Missing subtrials.");
                    foreach (var entry in trial.ChannelSubTrialBySubBloc)
                    {
                        if (!bloc.SubBlocs.Any(sub => ReferenceEquals(sub, entry.Key)) || entry.Value.Values == null || entry.Value.InformationsByEvent == null || entry.Value.InformationsByEvent.Keys.Any(e => !entry.Key.Events.Any(ev => ReferenceEquals(ev, e)))) Fail("Trial protocol identities do not match the column.");
                    }
                }
            }
        }

        private static void Unique(IEnumerable<string> ids)
        {
            var values = new HashSet<string>(StringComparer.Ordinal);
            foreach (string id in ids)
                if (string.IsNullOrEmpty(id) || !values.Add(id))
                    Fail("Missing or repeated identity.");
        }

        private static void Fail(string message) => throw new InvalidDataException(message);
    }
}
