using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using HBP.Core.Object3D;
using HBP.Data.Module3D;
using HBP.Transfer.Scene;

namespace HBP.Sync.Scene
{
    /// <summary>Exact, delivery-bound references for resources already present in a prepared scene.</summary>
    public sealed class PreparedSceneResourceCatalog
    {
        private readonly Base3DScene m_Scene;
        private readonly string m_ManifestHash;
        private readonly Mesh3D[] m_Meshes;
        private readonly MRI3D[] m_Mris;
        private readonly Implantation3D[] m_Implantations;
        private readonly string[] m_MeshRefs;
        private readonly string[] m_MriRefs;
        private readonly string[] m_ImplantationRefs;
        private readonly FMRI m_Ibc;
        private readonly string[] m_IbcContrastRefs;
        private readonly KeyValuePair<string, FMRI>[] m_DifumoAtlases;
        private readonly string[] m_DifumoRefs;
        private readonly (LocalizerProtocol Resource, string Reference)[] m_LocalizerProtocols;
        private readonly (LocalizerProtocol Protocol, LocalizerData Resource, string Reference)[] m_LocalizerDatas;
        private readonly (LocalizerProtocol Protocol, LocalizerData Data, LocalizerBloc Resource, string Reference)[] m_LocalizerBlocs;
        private readonly Dictionary<string, (Column3D Column, object[] Resources, string[] References)> m_ColumnResources = new();
        private PreparedSceneManifest m_DeliveryManifest;

        public PreparedSceneResourceCatalog(Base3DScene scene, string manifestHash)
        {
            m_Scene = scene ? scene : throw new ArgumentNullException(nameof(scene));
            if (manifestHash == null || manifestHash.Length != 64 || manifestHash.Any(character => character < '0' || character > '9' && (character < 'a' || character > 'f')))
                throw new ArgumentException("Invalid delivery manifest hash.", nameof(manifestHash));
            m_ManifestHash = manifestHash;
            m_Meshes = scene.MeshManager.Meshes.ToArray();
            m_Mris = scene.MRIManager.MRIs.ToArray();
            m_Implantations = scene.ImplantationManager.Implantations.ToArray();
            m_MeshRefs = MakeReferences("mesh", manifestHash, m_Meshes.Length);
            m_MriRefs = MakeReferences("mri", manifestHash, m_Mris.Length);
            m_ImplantationRefs = m_Implantations.Select(ComputeImplantationReference).ToArray();
            m_Ibc = Object3DManager.IBC.Loaded ? Object3DManager.IBC.FMRI : null;
            string ibcLabels = m_Ibc == null ? null : IbcLabelsFingerprint();
            m_IbcContrastRefs = m_Ibc == null ? Array.Empty<string>() : Enumerable.Range(0, m_Ibc.Volumes.Count).Select(index => ContentReference("ibc", index.ToString(CultureInfo.InvariantCulture), m_Ibc.SourceHash, ibcLabels)).ToArray();
            m_DifumoAtlases = Object3DManager.DiFuMo.FMRIs.Where(entry => Object3DManager.DiFuMo.IsLoaded(entry.Key)).OrderBy(entry => entry.Key, StringComparer.Ordinal).ToArray();
            m_DifumoRefs = m_DifumoAtlases.Select(entry => ContentReference("difumo", entry.Key, entry.Value.SourceHash, DifumoLabelsFingerprint(entry.Key))).ToArray();
            LocalizerProtocol[] protocols = PreparedLocalizerProtocols();
            var datas = protocols.SelectMany(protocol => protocol.Datas.Where(data => data.Blocs.Count > 0 && data.Loaded).OrderBy(data => data.Name, StringComparer.Ordinal).Select(data => (Protocol: protocol, Resource: data))).ToArray();
            var blocs = datas.SelectMany(entry => entry.Resource.Blocs.Where(bloc => bloc.Loaded).OrderBy(bloc => bloc.Name, StringComparer.Ordinal).Select(bloc => (entry.Protocol, Data: entry.Resource, Resource: bloc))).ToArray();
            m_LocalizerBlocs = blocs.Select(entry => (entry.Protocol, entry.Data, entry.Resource, ContentReference("localizer-bloc", entry.Protocol.Name, entry.Data.Name, entry.Resource.Name, entry.Resource.FMRI.SourceHash, entry.Resource.FMRI.SourceCompanionHash, entry.Resource.FMRI.MaskHash, entry.Resource.FMRI.MaskCompanionHash))).ToArray();
            m_LocalizerDatas = datas.Select(entry => (entry.Protocol, entry.Resource, ContentReference("localizer-data", entry.Protocol.Name, entry.Resource.Name, string.Join("|", m_LocalizerBlocs.Where(bloc => ReferenceEquals(bloc.Data, entry.Resource)).Select(bloc => bloc.Reference))))).ToArray();
            m_LocalizerProtocols = protocols.Select(resource => (resource, ContentReference("localizer-protocol", resource.Name, string.Join("|", m_LocalizerDatas.Where(data => ReferenceEquals(data.Protocol, resource)).Select(data => data.Reference))))).ToArray();
            foreach (Column3D column in scene.Columns)
            {
                string id = column.ColumnData.ID;
                object[] resources = ColumnResources(column);
                string kind = column is Column3DStatic ? "static" : "functional";
                string[] references = column is Column3DStatic staticColumn ? staticColumn.Labels.Select(label => ContentReference("static", id, label, StaticLabelFingerprint(staticColumn, label))).ToArray() : MakeReferences(kind, manifestHash + ":" + id, resources.Length);
                m_ColumnResources.Add(id, (column, resources, references));
            }
        }

        public void AssertPreparedRoster()
        {
            if (!m_Meshes.SequenceEqual(m_Scene.MeshManager.Meshes) || !m_Mris.SequenceEqual(m_Scene.MRIManager.MRIs) || !m_Implantations.SequenceEqual(m_Scene.ImplantationManager.Implantations))
                throw new InvalidDataException("Prepared scene resource roster changed during the synchronization epoch.");
            if (!m_Implantations.Select(ComputeImplantationReference).SequenceEqual(m_ImplantationRefs))
                throw new InvalidDataException("Prepared implantation data changed during the synchronization epoch.");
            if (m_Ibc != null && (!Object3DManager.IBC.Loaded || !ReferenceEquals(Object3DManager.IBC.FMRI, m_Ibc)) || m_Ibc == null && Object3DManager.IBC.Loaded)
                throw new InvalidDataException("Prepared IBC resource changed.");
            if (m_Ibc != null)
            {
                string ibcLabels = IbcLabelsFingerprint();
                if (!Enumerable.Range(0, m_IbcContrastRefs.Length).Select(index => ContentReference("ibc", index.ToString(CultureInfo.InvariantCulture), m_Ibc.SourceHash, ibcLabels)).SequenceEqual(m_IbcContrastRefs))
                    throw new InvalidDataException("Prepared IBC labels changed.");
            }

            var difumo = Object3DManager.DiFuMo.FMRIs.Where(entry => Object3DManager.DiFuMo.IsLoaded(entry.Key)).OrderBy(entry => entry.Key, StringComparer.Ordinal);
            if (!m_DifumoAtlases.SequenceEqual(difumo))
                throw new InvalidDataException("Prepared DiFuMo resources changed.");
            if (!m_DifumoAtlases.Select(entry => ContentReference("difumo", entry.Key, entry.Value.SourceHash, DifumoLabelsFingerprint(entry.Key))).SequenceEqual(m_DifumoRefs))
                throw new InvalidDataException("Prepared DiFuMo labels changed.");
            LocalizerProtocol[] protocols = PreparedLocalizerProtocols();
            if (!m_LocalizerProtocols.Select(entry => entry.Resource).SequenceEqual(protocols)) throw new InvalidDataException("Prepared localizer protocols changed.");
            var datas = protocols.SelectMany(protocol => protocol.Datas.Where(data => data.Blocs.Count > 0 && data.Loaded).OrderBy(data => data.Name, StringComparer.Ordinal).Select(data => (Protocol: protocol, Resource: data)));
            if (!m_LocalizerDatas.Select(entry => (entry.Protocol, entry.Resource)).SequenceEqual(datas)) throw new InvalidDataException("Prepared localizer data changed.");
            var blocs = datas.SelectMany(entry => entry.Resource.Blocs.Where(bloc => bloc.Loaded).OrderBy(bloc => bloc.Name, StringComparer.Ordinal).Select(bloc => (entry.Protocol, Data: entry.Resource, Resource: bloc)));
            if (!m_LocalizerBlocs.Select(entry => (entry.Protocol, entry.Data, entry.Resource)).SequenceEqual(blocs)) throw new InvalidDataException("Prepared localizer blocs changed.");
            if (!m_ColumnResources.Keys.OrderBy(id => id, StringComparer.Ordinal).SequenceEqual(m_Scene.Columns.Select(column => column.ColumnData.ID).OrderBy(id => id, StringComparer.Ordinal)))
                throw new InvalidDataException("Prepared column resource roster changed.");
            foreach (var entry in m_ColumnResources.Values)
            {
                if (!ColumnResources(entry.Column).SequenceEqual(entry.Resources))
                    throw new InvalidDataException("Prepared column resources changed.");
                if (entry.Column is Column3DStatic staticColumn && !staticColumn.Labels.Select(label => ContentReference("static", entry.Column.ColumnData.ID, label, StaticLabelFingerprint(staticColumn, label))).SequenceEqual(entry.References))
                    throw new InvalidDataException("Prepared static values changed.");
            }

            if (m_DeliveryManifest != null)
                foreach (var state in m_DeliveryManifest.Columns)
                    if (m_ColumnResources[state.Id].Column is Column3DMEG meg)
                        for (int i = 0; i < state.Functional.Count; i++)
                            AssertMegContent(meg.ColumnMEGData.Data.MEGItems[i], state.Functional[i]);
        }

        public void AssertDeliveryManifest(PreparedSceneManifest manifest)
        {
            if (manifest == null)
                throw new InvalidDataException("Published scene resource roster is missing.");
            PreparedSceneManifest.MeshEntry[] meshes = manifest.Meshes.Where(resource => resource.PatientId == null).ToArray();
            PreparedSceneManifest.VolumeEntry[] mris = manifest.MRIs.Where(resource => resource.PatientId == null).ToArray();
            if (meshes.Length != m_Meshes.Length || mris.Length != m_Mris.Length || manifest.Columns.Count != m_Scene.Columns.Count)
                throw new InvalidDataException("Prepared scene resource counts differ from the published delivery.");
            for (int i = 0; i < meshes.Length; i++)
            {
                PreparedSceneManifest.MeshEntry resource = meshes[i];
                Mesh3D mesh = m_Meshes[i];
                if (resource.Name != mesh.Name || resource.Type != (int)mesh.Type || (mesh is RuntimeSingleMesh3D preview ? preview.SourceMRIName : null) != resource.SourceMRI)
                    throw new InvalidDataException("Prepared mesh differs from the published delivery.");
                AssertMeshContent(mesh, resource);
            }

            for (int i = 0; i < mris.Length; i++)
            {
                PreparedSceneManifest.VolumeEntry resource = mris[i];
                MRI3D mri = m_Mris[i];
                if (resource.Name != mri.Name || resource.Standard == null && resource.File == null || resource.File != null && !MatchesCapturedFile(resource.File, mri.Volume.SourceFilePath, mri.Volume.SourceFileSha256, mri.Volume.SourceCompanionSha256))
                    throw new InvalidDataException($"Prepared MRI '{mri.Name}' differs from the published delivery (descriptor={resource.File ?? resource.Standard}, sourceHash={mri.Volume.SourceFileSha256}, sourceExtension={Path.GetExtension(mri.Volume.SourceFilePath)}).");
            }

            for (int i = 0; i < manifest.Columns.Count; i++)
            {
                PreparedSceneManifest.ColumnEntry state = manifest.Columns[i];
                Column3D column = m_Scene.Columns[i];
                if (state.Id != column.ColumnData.ID)
                    throw new InvalidDataException("Prepared column differs from the published delivery.");
                if (column is Column3DFMRI fmri && (state.Functional == null || !fmri.ColumnFMRIData.Data.FMRIs.Select(item => (item.Item1.Name, Patient: item.Item2?.ID)).SequenceEqual(state.Functional.Select(resource => (resource.Name, resource.PatientId)))))
                    throw new InvalidDataException("Prepared fMRI resources differ from the published delivery.");
                if (column is Column3DMEG meg && (state.Functional == null || !meg.ColumnMEGData.Data.MEGItems.Select(item => (item.Label, Patient: item.Patient?.ID)).SequenceEqual(state.Functional.Select(resource => (resource.Name, resource.PatientId)))))
                    throw new InvalidDataException("Prepared MEG resources differ from the published delivery.");
                if (column is not (Column3DFMRI or Column3DMEG) && state.Functional?.Count > 0)
                    throw new InvalidDataException("Unexpected functional resource in the published delivery.");
                if (column is Column3DFMRI functionalColumn)
                    for (int resourceIndex = 0; resourceIndex < state.Functional.Count; resourceIndex++)
                        AssertFunctionalResource(functionalColumn.ColumnFMRIData.Data.FMRIs[resourceIndex].Item1, state.Functional[resourceIndex]);
                if (column is Column3DMEG megColumn)
                    for (int resourceIndex = 0; resourceIndex < state.Functional.Count; resourceIndex++)
                    {
                        AssertFunctionalResource(megColumn.ColumnMEGData.Data.MEGItems[resourceIndex].FMRI, state.Functional[resourceIndex]);
                        AssertMegContent(megColumn.ColumnMEGData.Data.MEGItems[resourceIndex], state.Functional[resourceIndex]);
                    }
            }

            // Both endpoints hash the same final metadata descriptors, including resolved
            // surface and numeric-buffer references from the delivered scene.
            for (int i = 0; i < meshes.Length; i++) m_MeshRefs[i] = DescriptorReference("mesh", "", i, meshes[i].DescriptorHash);
            for (int i = 0; i < mris.Length; i++) m_MriRefs[i] = DescriptorReference("mri", "", i, mris[i].DescriptorHash);
            for (int i = 0; i < manifest.Columns.Count; i++)
            {
                var entry = m_ColumnResources[manifest.Columns[i].Id];
                if (entry.Column is Column3DFMRI or Column3DMEG)
                    for (int resourceIndex = 0; resourceIndex < entry.References.Length; resourceIndex++)
                        entry.References[resourceIndex] = DescriptorReference("functional", manifest.Columns[i].Id, resourceIndex, manifest.Columns[i].Functional[resourceIndex].DescriptorHash);
            }

            m_DeliveryManifest = manifest;
        }

        private static void AssertMegContent(HBP.Core.Data.Processed.MEGItem item, PreparedSceneManifest.FunctionalEntry resource)
        {
            if (!resource.MatchesMegContent(item.ValuesByChannel, item.UnitByChannel, item.Frequency.RawValue))
                throw new InvalidDataException("Prepared MEG channel values differ from the published delivery.");
        }

        private static void AssertMeshContent(Mesh3D mesh, PreparedSceneManifest.MeshEntry resource)
        {
            if (string.IsNullOrEmpty(resource.GeometryHash) || SceneArchive.MeshGeometryFingerprint(mesh) != resource.GeometryHash)
                throw new InvalidDataException("Prepared mesh geometry differs from the published delivery.");
        }

        private string DescriptorReference(string kind, string owner, int index, string descriptorHash)
        {
            using SHA256 sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes($"hbp-sync-v1:{m_ManifestHash}:{kind}:{owner}:{index}:{descriptorHash}"));
            return $"{kind}:{BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant()}:1";
        }

        private static void AssertFunctionalResource(FMRI fmri, PreparedSceneManifest.FunctionalEntry resource)
        {
            if (resource == null || !MatchesCapturedFile(resource.File, fmri.SourceFile, fmri.SourceHash, fmri.SourceCompanionHash) || !MatchesCapturedFile(resource.Mask, fmri.MaskFile, fmri.MaskHash, fmri.MaskCompanionHash))
                throw new InvalidDataException("Prepared functional data differs from the published delivery.");
        }

        private static bool MatchesCapturedFile(string descriptor, string sourcePath, string hash, string companionHash)
        {
            if (string.IsNullOrEmpty(sourcePath)) return descriptor == null;
            if (string.IsNullOrEmpty(hash) || string.IsNullOrEmpty(descriptor)) return false;
            string extension = sourcePath.EndsWith(".nii.gz", StringComparison.OrdinalIgnoreCase) ? ".nii.gz" : Path.GetExtension(sourcePath).ToLowerInvariant();
            string name = hash.Replace("-", "").ToLowerInvariant() + extension;
            if (Path.GetExtension(sourcePath).Equals(".img", StringComparison.OrdinalIgnoreCase) || Path.GetExtension(sourcePath).Equals(".hdr", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(companionHash)) return false;
                string companionExtension = Path.GetExtension(sourcePath).Equals(".img", StringComparison.OrdinalIgnoreCase) ? ".hdr" : ".img";
                using SHA256 sha = SHA256.Create();
                string pair = BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(name + "\n" + companionHash.Replace("-", "").ToLowerInvariant() + companionExtension))).Replace("-", "").ToLowerInvariant();
                return descriptor == pair + ".pair";
            }

            return descriptor == name;
        }

        public string MeshReference(Mesh3D mesh) => Reference(m_Meshes, m_MeshRefs, mesh);
        public string MriReference(MRI3D mri) => Reference(m_Mris, m_MriRefs, mri);
        public string ImplantationReference(Implantation3D implantation) => Reference(m_Implantations, m_ImplantationRefs, implantation);

        public Mesh3D ResolveMesh(string reference) => Resolve(m_Meshes, m_MeshRefs, reference, mesh => mesh.IsLoaded);
        public MRI3D ResolveMri(string reference) => Resolve(m_Mris, m_MriRefs, reference, mri => mri.IsLoaded);
        public Implantation3D ResolveImplantation(string reference) => Resolve(m_Implantations, m_ImplantationRefs, reference, implantation => implantation.IsLoaded);

        public string IbcContrastReference(int index) => m_Ibc != null && index >= 0 && index < m_IbcContrastRefs.Length ? m_IbcContrastRefs[index] : "";

        public int ResolveIbcContrast(string reference)
        {
            if (reference.Length == 0) return -1;
            int index = Array.IndexOf(m_IbcContrastRefs, reference);
            if (index < 0 || m_Ibc == null || !Object3DManager.IBC.Loaded) throw new InvalidDataException("IBC contrast is absent from the prepared atlas.");
            return index;
        }

        public string DifumoReference(string atlas)
        {
            for (int i = 0; i < m_DifumoAtlases.Length; i++)
                if (m_DifumoAtlases[i].Key == atlas)
                    return m_DifumoRefs[i];
            return "";
        }

        public string ResolveDifumo(string reference)
        {
            if (reference.Length == 0) return "";
            int index = Array.IndexOf(m_DifumoRefs, reference);
            if (index < 0 || !Object3DManager.DiFuMo.IsLoaded(m_DifumoAtlases[index].Key)) throw new InvalidDataException("DiFuMo atlas is absent from the prepared resources.");
            return m_DifumoAtlases[index].Key;
        }

        public int DifumoVolumeCount(string atlas) => m_DifumoAtlases.Single(entry => entry.Key == atlas).Value.Volumes.Count;

        public string LocalizerProtocolReference(string name) => m_LocalizerProtocols.FirstOrDefault(entry => entry.Resource.Name == name).Reference ?? "";

        public string LocalizerDataReference(string protocolName, string dataName) => m_LocalizerDatas.FirstOrDefault(entry => entry.Protocol.Name == protocolName && entry.Resource.Name == dataName).Reference ?? "";

        public string LocalizerBlocReference(string protocolName, string dataName, string blocName) => m_LocalizerBlocs.FirstOrDefault(entry => entry.Protocol.Name == protocolName && entry.Data.Name == dataName && entry.Resource.Name == blocName).Reference ?? "";

        public FMRI ResolveLocalizer(string protocolReference, string dataReference, string blocReference)
        {
            if (protocolReference.Length == 0 && dataReference.Length == 0 && blocReference.Length == 0) return null;
            var protocol = m_LocalizerProtocols.FirstOrDefault(entry => entry.Reference == protocolReference).Resource;
            var data = m_LocalizerDatas.FirstOrDefault(entry => entry.Reference == dataReference && ReferenceEquals(entry.Protocol, protocol)).Resource;
            var bloc = m_LocalizerBlocs.FirstOrDefault(entry => entry.Reference == blocReference && ReferenceEquals(entry.Protocol, protocol) && ReferenceEquals(entry.Data, data)).Resource;
            if (protocol == null || data == null || bloc == null || !bloc.Loaded) throw new InvalidDataException("Localizer selection is absent from the prepared atlas.");
            return bloc.FMRI;
        }

        public (string Protocol, string Data) ResolveLocalizerNames(string protocolReference, string dataReference)
        {
            if (protocolReference.Length == 0 && dataReference.Length == 0) return ("", "");
            var protocol = m_LocalizerProtocols.FirstOrDefault(entry => entry.Reference == protocolReference).Resource;
            var data = m_LocalizerDatas.FirstOrDefault(entry => entry.Reference == dataReference && ReferenceEquals(entry.Protocol, protocol)).Resource;
            if (protocol == null || data == null) throw new InvalidDataException("Localizer source is absent from the prepared atlas.");
            return (protocol.Name, data.Name);
        }

        public string ResolveLocalizerBlocName(string protocolReference, string dataReference, string blocReference)
        {
            if (protocolReference.Length == 0 && dataReference.Length == 0 && blocReference.Length == 0) return "";
            var protocol = m_LocalizerProtocols.FirstOrDefault(entry => entry.Reference == protocolReference).Resource;
            var data = m_LocalizerDatas.FirstOrDefault(entry => entry.Reference == dataReference && ReferenceEquals(entry.Protocol, protocol)).Resource;
            var bloc = m_LocalizerBlocs.FirstOrDefault(entry => entry.Reference == blocReference && ReferenceEquals(entry.Protocol, protocol) && ReferenceEquals(entry.Data, data)).Resource;
            if (bloc == null || !bloc.Loaded) throw new InvalidDataException("Localizer bloc is absent from the prepared atlas.");
            return bloc.Name;
        }

        private static LocalizerProtocol[] PreparedLocalizerProtocols() => Object3DManager.Localizers.Protocols.Where(protocol => protocol.Datas.Count > 0 && protocol.Loaded).OrderBy(protocol => protocol.Name, StringComparer.Ordinal).ToArray();

        public string ColumnReference(Column3D column)
        {
            var entry = m_ColumnResources[column.ColumnData.ID];
            if (!ReferenceEquals(entry.Column, column)) throw new InvalidDataException("Column is outside the prepared delivery.");
            int index = SelectedColumnResourceIndex(column);
            return index < 0 ? "" : entry.References[index];
        }

        public int ResolveColumnIndex(Column3D column, string reference)
        {
            var entry = m_ColumnResources[column.ColumnData.ID];
            if (!ReferenceEquals(entry.Column, column)) throw new InvalidDataException("Column is outside the prepared delivery.");
            if (reference.Length == 0 && entry.Resources.Length == 0) return -1;
            int index = Array.IndexOf(entry.References, reference);
            if (index < 0) throw new InvalidDataException("Column resource is absent from the prepared delivery.");
            if (entry.Resources[index] is FMRI fmri && !fmri.Loaded) throw new InvalidDataException("Prepared functional resource is not loaded.");
            if (entry.Resources[index] is HBP.Core.Data.Processed.MEGItem meg && !meg.FMRI.Loaded && meg.ValuesByChannel.Count == 0) throw new InvalidDataException("Prepared MEG resource has no loaded volume or channel values.");
            return index;
        }

        private static object[] ColumnResources(Column3D column) =>
            column switch
            {
                Column3DStatic staticColumn => staticColumn.Labels.Cast<object>().ToArray(),
                Column3DFMRI fmri => fmri.ColumnFMRIData.Data.FMRIs.Select(item => (object)item.Item1).ToArray(),
                Column3DMEG meg => meg.ColumnMEGData.Data.MEGItems.Cast<object>().ToArray(),
                _ => Array.Empty<object>()
            };

        private static int SelectedColumnResourceIndex(Column3D column) =>
            column switch
            {
                Column3DStatic staticColumn when staticColumn.Labels.Length > 0 => staticColumn.SelectedLabelIndex,
                Column3DFMRI fmri when fmri.ColumnFMRIData.Data.FMRIs.Count > 0 => fmri.SelectedFMRIIndex,
                Column3DMEG meg when meg.ColumnMEGData.Data.MEGItems.Count > 0 => meg.SelectedMEGIndex,
                _ => -1
            };

        private string ComputeImplantationReference(Implantation3D implantation)
        {
            IEnumerable<string> sites = implantation.SiteInfos.Select((site, index) => Fingerprint(index.ToString(CultureInfo.InvariantCulture), site.Name, site.Patient?.ID, site.PatientIndex.ToString(CultureInfo.InvariantCulture), site.Index.ToString(CultureInfo.InvariantCulture), site.Electrode, site.SiteData?.ID, FloatText(site.NativePosition.x), FloatText(site.NativePosition.y), FloatText(site.NativePosition.z)));
            return ContentReference("implantation", implantation.Name, Fingerprint(sites));
        }

        private static string StaticLabelFingerprint(Column3DStatic column, string label)
        {
            if (!column.ColumnStaticData.Data.ValueByChannelIDByLabel.TryGetValue(label, out Dictionary<string, float> values))
                throw new InvalidDataException("Prepared static label has no values.");
            return Fingerprint(values.OrderBy(entry => entry.Key, StringComparer.Ordinal).Select(entry => Fingerprint(entry.Key, FloatText(entry.Value))));
        }

        private static string IbcLabelsFingerprint() => Fingerprint(Object3DManager.IBC.Information.AllLabels.Select(label => Fingerprint(label.Index.ToString(CultureInfo.InvariantCulture), label.Task, label.Contrast, label.PrettyName, label.ControlCondition, label.TargetCondition)));

        private static string DifumoLabelsFingerprint(string atlas) => Fingerprint(Object3DManager.DiFuMo.Information[atlas].AllLabels.Select(label => Fingerprint(label.Component.ToString(CultureInfo.InvariantCulture), label.Name, label.YeoNetworks7, label.YeoNetworks17, FloatText(label.GM), FloatText(label.WM), FloatText(label.CSF))));

        private string ContentReference(string kind, params string[] parts) => $"{kind}:{Fingerprint(new[] { m_ManifestHash, kind }.Concat(parts))}:1";

        private static string FloatText(float value) => value.ToString("R", CultureInfo.InvariantCulture);

        private static string Fingerprint(params string[] parts) => Fingerprint((IEnumerable<string>)parts);

        private static string Fingerprint(IEnumerable<string> parts)
        {
            using SHA256 sha = SHA256.Create();
            string encoded = string.Concat(parts.Select(part => part == null ? "-1:" : part.Length.ToString(CultureInfo.InvariantCulture) + ":" + part));
            return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(encoded))).Replace("-", "").ToLowerInvariant();
        }

        private static string Reference<T>(IReadOnlyList<T> objects, IReadOnlyList<string> references, T item) where T : class
        {
            if (item == null) return "";
            for (int i = 0; i < objects.Count; i++)
                if (ReferenceEquals(objects[i], item))
                    return references[i];
            throw new InvalidDataException("Selected resource is outside the prepared delivery.");
        }

        private static T Resolve<T>(IReadOnlyList<T> objects, IReadOnlyList<string> references, string reference, Func<T, bool> isLoaded) where T : class
        {
            if (reference.Length == 0) return null;
            for (int i = 0; i < references.Count; i++)
                if (references[i] == reference)
                {
                    if (!isLoaded(objects[i])) throw new InvalidDataException("Referenced prepared resource is not loaded.");
                    return objects[i];
                }

            throw new InvalidDataException("Referenced resource is absent from the prepared delivery.");
        }

        private static string[] MakeReferences(string kind, string manifestHash, int count)
        {
            var references = new string[count];
            using SHA256 sha = SHA256.Create();
            for (int i = 0; i < count; i++)
            {
                byte[] hash = sha.ComputeHash(Encoding.ASCII.GetBytes($"hbp-sync-v1:{manifestHash}:{kind}:{i}"));
                references[i] = $"{kind}:{BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant()}:1";
            }

            return references;
        }
    }
}
