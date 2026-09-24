using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using HBP.Core.Enums;
using HBP.Data.Module3D;
using HBP.Sync;
using UnityEngine;

namespace HBP.Sync.Scene
{
    /// <summary>In-place adapter for the scene, site and geometry fields currently supported here.</summary>
    public sealed class LiveGeometryStateAdapter
    {
        private readonly Base3DScene m_Scene;
        private readonly Guid m_EpochId;
        private readonly string m_ManifestHash;
        private readonly PreparedSceneResourceCatalog m_Resources;
        private readonly HashSet<string> m_RemovedCuts = new();
        private readonly HashSet<string> m_RemovedRois = new();
        private readonly Dictionary<string, HashSet<string>> m_RemovedSpheres = new();
        private readonly HashSet<string> m_KnownCuts = new();
        private readonly HashSet<string> m_KnownRois = new();
        private readonly Dictionary<string, HashSet<string>> m_KnownSpheres = new();
        private readonly Dictionary<string, HashSet<string>> m_KnownSites = new();
        private readonly Dictionary<string, (byte[] Bytes, CorrelationResultResource Result)> m_CorrelationResources = new(StringComparer.Ordinal);
        private bool m_Bound;

        public LiveGeometryStateAdapter(Base3DScene scene, Guid epochId, PreparedSceneDeliveryBinding delivery) : this(scene, epochId, delivery?.ManifestHash)
        {
            if (delivery == null || m_Scene.Visualization.ID != delivery.VisualizationId)
                throw new InvalidDataException("Prepared scene differs from the published delivery.");
            m_Resources.AssertDeliveryManifest(delivery.Manifest);
        }

        internal LiveGeometryStateAdapter(Base3DScene scene, Guid epochId, string manifestHash)
        {
            m_Scene = scene ? scene : throw new ArgumentNullException(nameof(scene));
            if (epochId == Guid.Empty || manifestHash == null || manifestHash.Length != 64 || manifestHash.Any(character => character < '0' || character > '9' && (character < 'a' || character > 'f')))
                throw new ArgumentException("Invalid synchronization epoch or manifest hash.");
            m_EpochId = epochId;
            m_ManifestHash = manifestHash;
            m_Resources = new PreparedSceneResourceCatalog(scene, manifestHash);
        }

        public StateSnapshot Capture(ulong revision)
        {
            if (m_Scene.SceneInformation.GeometryNeedsUpdate || m_Scene.IsSurfaceRepresentationTransitioning || m_Scene.MeshManager.BrainSurface == null || m_Scene.MeshManager.SimplifiedMeshToUse == null)
                throw new InvalidOperationException("Prepared geometry must stabilize before capture.");
            var fields = new SortedDictionary<StateKey, byte[]>();
            void Add(EntityKind kind, string parent, string id, ushort field, byte[] value) => fields.Add(new StateKey(kind, parent, id, field), value);
            void Scene(ushort field, byte[] value) => Add(EntityKind.Scene, "", "", field, value);

            Scene(1, StateValue.Text(m_Scene.SelectedColumn?.ColumnData.ID ?? ""));
            Scene(2, StateValue.Text(m_Scene.ROIManager.SelectedROI?.ID ?? ""));
            Scene(3, StateValue.Bool(m_Scene.StrongCuts));
            Scene(4, StateValue.Bool(m_Scene.AutomaticCutAroundSelectedSite));
            Scene(5, StateValue.Bool(m_Scene.HideBlacklistedSites));
            Scene(6, StateValue.Bool(m_Scene.ShowAllSites));
            Scene(7, StateValue.Float(m_Scene.SiteGain));
            var comparisonSite = m_Scene.ImplantationManager.SiteToCompare;
            var comparisonColumn = comparisonSite ? m_Scene.Columns.FirstOrDefault(column => column.Sites.Contains(comparisonSite)) : null;
            if (comparisonSite && comparisonColumn == null) throw new InvalidDataException("Comparison site is outside the prepared scene.");
            Scene(8, StateValue.Text(comparisonColumn == null ? "" : ComparisonSiteId(comparisonColumn.ColumnData.ID, comparisonSite.Information.FullID)));
            CorrelationResultResource correlation = CorrelationResultResource.Capture(m_Scene);
            string correlationReference = "";
            if (correlation != null)
            {
                byte[] bytes = correlation.Encode();
                correlationReference = CorrelationResultResource.Reference(bytes);
                m_CorrelationResources[correlationReference] = (bytes, correlation);
            }

            Scene(9, StateValue.Text(correlationReference));
            Scene(10, StateValue.Bool(m_Scene.DisplayCorrelations));
            var selectedMesh = m_Scene.MeshManager.Meshes.Count == 0 ? null : m_Scene.MeshManager.SelectedMesh;
            var selectedMri = m_Scene.MRIManager.MRIs.Count == 0 ? null : m_Scene.MRIManager.SelectedMRI;
            Scene(11, StateValue.Text(m_Resources.MeshReference(selectedMesh)));
            Scene(12, StateValue.Text(m_Resources.MriReference((selectedMesh as HBP.Core.Object3D.RuntimeSingleMesh3D)?.SourceMRI)));
            Scene(13, StateValue.Int((int)m_Scene.MeshManager.MeshPartToDisplay));
            Scene(14, StateValue.Int((int)(selectedMesh?.Representation ?? HBP.Core.Object3D.SurfaceRepresentation.Anatomical)));
            Scene(15, StateValue.Mask(PackMask(m_Scene.MeshManager.BrainSurface.VisibilityMask)));
            Scene(16, StateValue.Mask(PackMask(m_Scene.MeshManager.SimplifiedMeshToUse.VisibilityMask)));
            Scene(17, StateValue.Text(m_Resources.MriReference(selectedMri)));
            Scene(18, StateValue.Float(m_Scene.MRIManager.MRICalMinFactor));
            Scene(19, StateValue.Float(m_Scene.MRIManager.MRICalMaxFactor));
            Scene(20, StateValue.Text(m_Resources.ImplantationReference(m_Scene.ImplantationManager.SelectedImplantation)));
            Scene(21, StateValue.Int((int)m_Scene.BrainColor));
            Scene(22, StateValue.Int((int)m_Scene.CutColor));
            Scene(23, StateValue.Int((int)m_Scene.Colormap));
            Scene(24, StateValue.Bool(m_Scene.EdgeMode));
            Scene(25, StateValue.Bool(m_Scene.IsBrainTransparent));
            Scene(26, StateValue.Float(m_Scene.BrainMaterials.Alpha));
            Scene(27, StateValue.Bool(m_Scene.ProjectionEnabled));
            Scene(28, StateValue.Bool(m_Scene.AtlasManager.DisplayMarsAtlas));
            Scene(29, StateValue.Bool(m_Scene.AtlasManager.DisplayJuBrainAtlas));
            Scene(30, StateValue.Float(m_Scene.AtlasManager.AtlasAlpha));
            Scene(31, StateValue.Bool(m_Scene.FMRIManager.DisplayIBCContrasts));
            Scene(32, StateValue.Text(m_Resources.IbcContrastReference(m_Scene.FMRIManager.SelectedIBCContrastID)));
            Scene(33, StateValue.Bool(m_Scene.FMRIManager.DisplayDiFuMo));
            Scene(34, StateValue.Text(m_Resources.DifumoReference(m_Scene.FMRIManager.SelectedDiFuMoAtlas)));
            Scene(35, StateValue.Text(m_Scene.FMRIManager.SelectedDiFuMoArea.ToString(System.Globalization.CultureInfo.InvariantCulture)));
            var localizers = m_Scene.FMRIManager;
            string protocol = localizers.SelectedLocalizersProtocol ?? "";
            string data = localizers.SelectedLocalizersData ?? "";
            Scene(36, StateValue.Bool(localizers.DisplayLocalizers));
            Scene(37, StateValue.Text(m_Resources.LocalizerProtocolReference(protocol)));
            Scene(38, StateValue.Text(m_Resources.LocalizerDataReference(protocol, data)));
            Scene(39, StateValue.Text(m_Resources.LocalizerBlocReference(protocol, data, localizers.SelectedLocalizersBloc ?? "")));
            Scene(40, StateValue.Int(localizers.SelectedLocalizersTimelineIndex));
            Scene(41, StateValue.Float(localizers.LocalizersMin));
            Scene(42, StateValue.Float(localizers.LocalizersMiddle));
            Scene(43, StateValue.Float(localizers.LocalizersMax));
            Scene(44, StateValue.Float(m_Scene.FMRIManager.FMRIAlpha));
            Scene(45, StateValue.Float(m_Scene.FMRIManager.FMRINegativeCalMinFactor));
            Scene(46, StateValue.Float(m_Scene.FMRIManager.FMRINegativeCalMaxFactor));
            Scene(47, StateValue.Float(m_Scene.FMRIManager.FMRIPositiveCalMinFactor));
            Scene(48, StateValue.Float(m_Scene.FMRIManager.FMRIPositiveCalMaxFactor));

            for (int i = 0; i < m_Scene.Columns.Count; i++)
            {
                Column3D column = m_Scene.Columns[i];
                string id = column.ColumnData.ID;
                Add(EntityKind.Column, "", id, 1, StateValue.Bool(true));
                Add(EntityKind.Column, "", id, 2, StateValue.Int(i));
                Add(EntityKind.Column, "", id, 3, StateValue.Int(Modality(column)));
                Add(EntityKind.Column, "", id, 4, StateValue.Text(column.SelectedSite?.Information.FullID ?? ""));
                Add(EntityKind.Column, "", id, 5, StateValue.Float(column.ActivityAlpha));
                if (column is Column3DAnatomy anatomy)
                    Add(EntityKind.Column, "", id, 6, StateValue.Float(anatomy.AnatomyParameters.InfluenceDistance));
                if (column is Column3DStatic staticColumn)
                {
                    Add(EntityKind.Column, "", id, 7, StateValue.Text(m_Resources.ColumnReference(column)));
                    Add(EntityKind.Column, "", id, 8, StateValue.Float(staticColumn.StaticParameters.SpanMin));
                    Add(EntityKind.Column, "", id, 9, StateValue.Float(staticColumn.StaticParameters.Middle));
                    Add(EntityKind.Column, "", id, 10, StateValue.Float(staticColumn.StaticParameters.SpanMax));
                    Add(EntityKind.Column, "", id, 11, StateValue.Float(staticColumn.StaticParameters.InfluenceDistance));
                }

                if (column is Column3DDynamic dynamicColumn)
                {
                    Add(EntityKind.Column, "", id, 12, StateValue.Float(dynamicColumn.DynamicParameters.SpanMin));
                    Add(EntityKind.Column, "", id, 13, StateValue.Float(dynamicColumn.DynamicParameters.Middle));
                    Add(EntityKind.Column, "", id, 14, StateValue.Float(dynamicColumn.DynamicParameters.SpanMax));
                    Add(EntityKind.Column, "", id, 15, StateValue.Float(dynamicColumn.DynamicParameters.InfluenceDistance));
                }

                if (column is Column3DCCEP ccep)
                {
                    Add(EntityKind.Column, "", id, 16, StateValue.Int((int)ccep.Mode));
                    Add(EntityKind.Column, "", id, 17, StateValue.Text(ccep.SelectedSourceSite?.Information.FullID ?? ""));
                    Add(EntityKind.Column, "", id, 18, StateValue.Text(ccep.SelectedSourceMarsAtlasLabel < 0 ? "" : ccep.SelectedSourceMarsAtlasLabel.ToString(System.Globalization.CultureInfo.InvariantCulture)));
                }

                if (column is Column3DFMRI fmri)
                {
                    var parameters = fmri.FMRIParameters;
                    Add(EntityKind.Column, "", id, 19, StateValue.Text(m_Resources.ColumnReference(column)));
                    Add(EntityKind.Column, "", id, 20, StateValue.Float(parameters.FMRINegativeCalMinFactor));
                    Add(EntityKind.Column, "", id, 21, StateValue.Float(parameters.FMRINegativeCalMaxFactor));
                    Add(EntityKind.Column, "", id, 22, StateValue.Float(parameters.FMRIPositiveCalMinFactor));
                    Add(EntityKind.Column, "", id, 23, StateValue.Float(parameters.FMRIPositiveCalMaxFactor));
                    Add(EntityKind.Column, "", id, 24, StateValue.Bool(parameters.HideLowerValues));
                    Add(EntityKind.Column, "", id, 25, StateValue.Bool(parameters.HideMiddleValues));
                    Add(EntityKind.Column, "", id, 26, StateValue.Bool(parameters.HideHigherValues));
                }

                if (column is Column3DMEG meg)
                {
                    var parameters = meg.MEGParameters;
                    Add(EntityKind.Column, "", id, 19, StateValue.Text(m_Resources.ColumnReference(column)));
                    Add(EntityKind.Column, "", id, 20, StateValue.Float(parameters.FMRINegativeCalMinFactor));
                    Add(EntityKind.Column, "", id, 21, StateValue.Float(parameters.FMRINegativeCalMaxFactor));
                    Add(EntityKind.Column, "", id, 22, StateValue.Float(parameters.FMRIPositiveCalMinFactor));
                    Add(EntityKind.Column, "", id, 23, StateValue.Float(parameters.FMRIPositiveCalMaxFactor));
                    Add(EntityKind.Column, "", id, 24, StateValue.Bool(parameters.HideLowerValues));
                    Add(EntityKind.Column, "", id, 25, StateValue.Bool(parameters.HideMiddleValues));
                    Add(EntityKind.Column, "", id, 26, StateValue.Bool(parameters.HideHigherValues));
                }

                var timeline = column.NavigationTimeline;
                if (timeline != null && timeline.Length > 0)
                {
                    Add(EntityKind.Column, "", id, 27, StateValue.Int(timeline.CurrentIndex));
                    Add(EntityKind.Column, "", id, 28, StateValue.Bool(timeline.IsPlaying));
                    Add(EntityKind.Column, "", id, 29, StateValue.Bool(timeline.IsLooping));
                    Add(EntityKind.Column, "", id, 30, StateValue.Int(timeline.Step));
                    Add(EntityKind.Column, "", id, 31, StateValue.Float(timeline.Frequency.RawValue));
                    Add(EntityKind.Column, "", id, 32, StateValue.Float(timeline.CurrentIndexAnchorTime));
                }

                if (!m_KnownSites.TryGetValue(id, out var knownSites)) m_KnownSites.Add(id, knownSites = new HashSet<string>());
                foreach (string removedSite in knownSites.Except(column.Sites.Select(site => site.Information.FullID)))
                    Add(EntityKind.Site, id, removedSite, 1, StateValue.Bool(false));
                foreach (var site in column.Sites)
                {
                    string siteId = site.Information.FullID;
                    knownSites.Add(siteId);
                    Add(EntityKind.Site, id, siteId, 1, StateValue.Bool(true));
                    Add(EntityKind.Site, id, siteId, 2, StateValue.Bool(site.State.IsFiltered));
                    Add(EntityKind.Site, id, siteId, 3, StateValue.Bool(site.State.IsBlackListed));
                    Add(EntityKind.Site, id, siteId, 4, StateValue.Bool(site.State.IsHighlighted));
                    Color color = site.State.Color;
                    Add(EntityKind.Site, id, siteId, 5, StateValue.Color(color.r, color.g, color.b, color.a));
                    Add(EntityKind.Site, id, siteId, 6, StateValue.TextList(site.State.Labels));
                    Vector3 position = site.transform.localPosition;
                    Add(EntityKind.Site, id, siteId, 7, StateValue.Vector3(position.x, position.y, position.z));
                }
            }

            foreach (var id in m_KnownCuts.Except(m_Scene.Cuts.Select(cut => cut.ID))) m_RemovedCuts.Add(id);
            foreach (var id in m_RemovedCuts) Add(EntityKind.Cut, "", id, 1, StateValue.Bool(false));
            for (int i = 0; i < m_Scene.Cuts.Count; i++)
            {
                var cut = m_Scene.Cuts[i];
                m_KnownCuts.Add(cut.ID);
                Add(EntityKind.Cut, "", cut.ID, 1, StateValue.Bool(true));
                Add(EntityKind.Cut, "", cut.ID, 2, StateValue.Int(i));
                Add(EntityKind.Cut, "", cut.ID, 3, StateValue.Int((int)cut.Orientation));
                Add(EntityKind.Cut, "", cut.ID, 4, StateValue.Vector3(cut.Normal.x, cut.Normal.y, cut.Normal.z));
                Add(EntityKind.Cut, "", cut.ID, 5, StateValue.Bool(cut.Flip));
                Add(EntityKind.Cut, "", cut.ID, 6, StateValue.Float(cut.Position));
            }

            foreach (var id in m_KnownRois.Except(m_Scene.ROIManager.ROIs.Select(roi => roi.ID))) m_RemovedRois.Add(id);
            foreach (var id in m_RemovedRois) Add(EntityKind.Roi, "", id, 1, StateValue.Bool(false));
            foreach (var roiId in m_RemovedRois)
            {
                if (m_KnownSpheres.TryGetValue(roiId, out var known))
                {
                    if (!m_RemovedSpheres.TryGetValue(roiId, out var removed)) m_RemovedSpheres.Add(roiId, removed = new HashSet<string>());
                    removed.UnionWith(known);
                }

                if (m_RemovedSpheres.TryGetValue(roiId, out var deletedSpheres))
                    foreach (var sphereId in deletedSpheres)
                        Add(EntityKind.Sphere, roiId, sphereId, 1, StateValue.Bool(false));
            }

            for (int i = 0; i < m_Scene.ROIManager.ROIs.Count; i++)
            {
                ROI roi = m_Scene.ROIManager.ROIs[i];
                m_KnownRois.Add(roi.ID);
                Add(EntityKind.Roi, "", roi.ID, 1, StateValue.Bool(true));
                Add(EntityKind.Roi, "", roi.ID, 2, StateValue.Int(i));
                Add(EntityKind.Roi, "", roi.ID, 3, StateValue.Text(roi.Name));
                if (!m_KnownSpheres.TryGetValue(roi.ID, out var known)) m_KnownSpheres.Add(roi.ID, known = new HashSet<string>());
                if (!m_RemovedSpheres.TryGetValue(roi.ID, out var removed)) m_RemovedSpheres.Add(roi.ID, removed = new HashSet<string>());
                foreach (var id in known.Except(roi.Spheres.Select(sphere => sphere.ID))) removed.Add(id);
                foreach (var id in removed) Add(EntityKind.Sphere, roi.ID, id, 1, StateValue.Bool(false));
                for (int j = 0; j < roi.Spheres.Count; j++)
                {
                    Sphere sphere = roi.Spheres[j];
                    known.Add(sphere.ID);
                    Add(EntityKind.Sphere, roi.ID, sphere.ID, 1, StateValue.Bool(true));
                    Add(EntityKind.Sphere, roi.ID, sphere.ID, 2, StateValue.Int(j));
                    Add(EntityKind.Sphere, roi.ID, sphere.ID, 3, StateValue.Vector3(sphere.Position.x, sphere.Position.y, sphere.Position.z));
                    Add(EntityKind.Sphere, roi.ID, sphere.ID, 4, StateValue.Float(sphere.InfluenceRadius));
                }
            }

            var snapshot = new StateSnapshot(m_EpochId, m_Scene.Visualization.ID, m_ManifestHash, revision, fields);
            StateValidator.Validate(snapshot);
            return snapshot;
        }

        /// <summary>Capture a cut operation without visiting masks, sites or scientific results.</summary>
        public StateSnapshot CaptureCuts(StateSnapshot baseline)
        {
            var fields = baseline.Fields;
            foreach (StateKey key in fields.Keys.Where(key => key.Entity == EntityKind.Cut).ToArray()) fields.Remove(key);
            foreach (var id in m_KnownCuts.Except(m_Scene.Cuts.Select(cut => cut.ID))) m_RemovedCuts.Add(id);
            foreach (string id in m_RemovedCuts)
                fields[new StateKey(EntityKind.Cut, "", id, 1)] = StateValue.Bool(false);
            for (int i = 0; i < m_Scene.Cuts.Count; i++)
            {
                var cut = m_Scene.Cuts[i];
                m_KnownCuts.Add(cut.ID);
                fields[new StateKey(EntityKind.Cut, "", cut.ID, 1)] = StateValue.Bool(true);
                fields[new StateKey(EntityKind.Cut, "", cut.ID, 2)] = StateValue.Int(i);
                fields[new StateKey(EntityKind.Cut, "", cut.ID, 3)] = StateValue.Int((int)cut.Orientation);
                fields[new StateKey(EntityKind.Cut, "", cut.ID, 4)] = StateValue.Vector3(cut.Normal.x, cut.Normal.y, cut.Normal.z);
                fields[new StateKey(EntityKind.Cut, "", cut.ID, 5)] = StateValue.Bool(cut.Flip);
                fields[new StateKey(EntityKind.Cut, "", cut.ID, 6)] = StateValue.Float(cut.Position);
            }

            return baseline.WithFields(fields, baseline.CommonRevision);
        }

        /// <summary>Capture timeline ticks without sampling geometry, masks or sites.</summary>
        public StateSnapshot CaptureTimelines(StateSnapshot baseline)
        {
            var fields = baseline.Fields;
            foreach (var column in m_Scene.Columns)
            {
                var timeline = column.NavigationTimeline;
                if (timeline is not { Length: > 0 }) continue;
                string id = column.ColumnData.ID;
                fields[new StateKey(EntityKind.Column, "", id, 27)] = StateValue.Int(timeline.CurrentIndex);
                fields[new StateKey(EntityKind.Column, "", id, 28)] = StateValue.Bool(timeline.IsPlaying);
                fields[new StateKey(EntityKind.Column, "", id, 29)] = StateValue.Bool(timeline.IsLooping);
                fields[new StateKey(EntityKind.Column, "", id, 30)] = StateValue.Int(timeline.Step);
                fields[new StateKey(EntityKind.Column, "", id, 31)] = StateValue.Float(timeline.Frequency.RawValue);
                fields[new StateKey(EntityKind.Column, "", id, 32)] = StateValue.Float(timeline.CurrentIndexAnchorTime);
            }

            return baseline.WithFields(fields, baseline.CommonRevision);
        }

        /// <summary>Bind the prepared scene to the exact state shipped with its initial delivery.</summary>
        public void BindInitialState(StateSnapshot initial)
        {
            if (m_Bound) throw new InvalidOperationException("The initial state is already bound.");
            var fields = ValidateAndPrepare(initial);
            if (Members(fields, EntityKind.Cut, "").Count != m_Scene.Cuts.Count || Members(fields, EntityKind.Roi, "").Count != m_Scene.ROIManager.ROIs.Count)
                throw new InvalidDataException("Initial delivery does not match prepared geometry.");
            for (int i = 0; i < m_Scene.ROIManager.ROIs.Count; i++)
                if (Members(fields, EntityKind.Sphere, Members(fields, EntityKind.Roi, "")[i].Id).Count != m_Scene.ROIManager.ROIs[i].Spheres.Count)
                    throw new InvalidDataException("Initial delivery does not match prepared ROI spheres.");
            BindInitialIds(fields);
            RememberMembership(fields);
            m_Bound = true;
        }

        /// <summary>Return the immutable bytes of a captured scientific correlation resource.</summary>
        public byte[] ExportCorrelationResource(string reference)
        {
            if (!m_CorrelationResources.TryGetValue(reference, out var entry)) throw new InvalidDataException("Correlation resource was not captured in this epoch.");
            return (byte[])entry.Bytes.Clone();
        }

        /// <summary>Verify and stage a scientific result before accepting a dependent snapshot.</summary>
        public void PrepareCorrelationResource(string reference, byte[] bytes)
        {
            if (bytes == null || reference != CorrelationResultResource.Reference(bytes)) throw new InvalidDataException("Correlation resource hash mismatch.");
            CorrelationResultResource result = CorrelationResultResource.Decode(bytes);
            m_CorrelationResources[reference] = ((byte[])bytes.Clone(), result);
        }

        public void Apply(StateSnapshot state, ReplicaDelta delta = null)
        {
            if (!m_Bound) throw new InvalidOperationException("Bind the initial delivered state before applying revisions.");
            bool cutsOnly = delta != null && delta.Removals.All(key => key.Entity == EntityKind.Cut) && delta.Assignments.Keys.All(key => key.Entity == EntityKind.Cut);
            bool timelinesOnly = delta != null && delta.Removals.Count == 0 && delta.Assignments.Keys.All(key => key.Entity == EntityKind.Column && key.FieldId is >= 27 and <= 32);
            var fields = cutsOnly || timelinesOnly ? state.Fields : ValidateAndPrepare(state);
            if (cutsOnly || timelinesOnly)
            {
                if (state.EpochId != m_EpochId || state.ManifestHash != m_ManifestHash || state.VisualizationId != m_Scene.Visualization.ID)
                    throw new InvalidDataException("Operation does not belong to the prepared scene.");
                StateValidator.Validate(state);
            }

            if (cutsOnly)
            {
                foreach (var member in Members(fields, EntityKind.Cut, ""))
                {
                    for (ushort field = 1; field <= 6; field++) Require(fields, EntityKind.Cut, "", member.Id, field);
                    if (!Enum.IsDefined(typeof(CutOrientation), Int(fields, EntityKind.Cut, "", member.Id, 3)) || Float(fields, EntityKind.Cut, "", member.Id, 6) is < 0 or > 1)
                        throw new InvalidDataException("Invalid cut orientation or position.");
                }
            }

            ValidateCutOrder(fields);
            if (!m_Scene.CanApplyLegacyStateSnapshot) throw new InvalidOperationException("Prepared scene is busy with native or geometry work.");

            m_Scene.BeginSynchronizedStateApplication();
            if (cutsOnly)
            {
                m_Scene.InvalidateSynchronizedGeometryColliderWork();
                ApplyCuts(fields, delta.Assignments.Keys.Concat(delta.Removals).Select(key => key.Id).ToHashSet());
                RememberMembership(fields);
                m_Scene.SceneInformation.CutsNeedUpdate = true;
                return;
            }

            if (timelinesOnly)
            {
                ApplyTimelines(fields, delta.Assignments.Keys.Select(key => key.Id).ToHashSet());
                m_Scene.PreserveSynchronizedTimelinesOnNextGenerator();
                return;
            }

            var changed = delta == null ? null : delta.Assignments.Keys.Concat(delta.Removals).ToArray();
            bool Has(EntityKind kind) => changed == null || changed.Any(key => key.Entity == kind);
            bool SceneField(params ushort[] ids) => changed == null || changed.Any(key => key.Entity == EntityKind.Scene && ids.Contains(key.FieldId));
            bool geometry = SceneField(11, 12, 13, 14, 17, 18, 19, 20);
            if (geometry || Has(EntityKind.Cut)) m_Scene.InvalidateSynchronizedGeometryColliderWork();
            if (SceneField(27)) m_Scene.SetProjectionEnabled(Bool(fields, EntityKind.Scene, "", "", 27));
            if (geometry)
            {
                ApplyResources(fields);
                m_Scene.RebuildPreparedGeometryForSynchronization();
            }

            if (geometry || SceneField(15, 16)) ApplyMasks(fields);
            if (geometry || Has(EntityKind.Cut)) ApplyCuts(fields);
            if (geometry || Has(EntityKind.Roi) || Has(EntityKind.Sphere)) ApplyRois(fields);
            if (geometry || Has(EntityKind.Column) || Has(EntityKind.Site))
            {
                ApplySites(fields, geometry ? null : changed.ToHashSet());
                m_Scene.PreserveSynchronizedTimelinesOnNextGenerator();
                m_Scene.SceneInformation.SitesNeedUpdate = true;
            }

            if (Has(EntityKind.Scene)) ApplyScene(fields, changed == null ? null : changed.Where(key => key.Entity == EntityKind.Scene).Select(key => key.FieldId).ToHashSet());
            RememberMembership(fields);
            if (geometry || Has(EntityKind.Cut)) m_Scene.SceneInformation.CutsNeedUpdate = true;
        }

        private SortedDictionary<StateKey, byte[]> ValidateAndPrepare(StateSnapshot state)
        {
            StateValidator.Validate(state);
            if (state.EpochId != m_EpochId || state.ManifestHash != m_ManifestHash || state.VisualizationId != m_Scene.Visualization.ID)
                throw new InvalidDataException("Snapshot does not belong to this prepared scene and epoch.");
            var fields = state.Fields;
            foreach (StateKey key in fields.Keys)
                if (!Supports(key))
                    throw new NotSupportedException($"Live geometry adapter cannot apply {key}");
            foreach (ushort field in new ushort[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, 36, 37, 38, 39, 40, 41, 42, 43, 44, 45, 46, 47, 48 })
                Require(fields, EntityKind.Scene, "", "", field);
            var targetMesh = m_Resources.ResolveMesh(Text(fields, EntityKind.Scene, "", "", 11));
            var previewMri = m_Resources.ResolveMri(Text(fields, EntityKind.Scene, "", "", 12));
            var targetMri = m_Resources.ResolveMri(Text(fields, EntityKind.Scene, "", "", 17));
            var targetImplantation = m_Resources.ResolveImplantation(Text(fields, EntityKind.Scene, "", "", 20));
            if (targetImplantation == null && m_Scene.ImplantationManager.Implantations.Count > 0)
                throw new InvalidDataException("A prepared implantation is required.");
            string[] preparedSiteIds = targetImplantation?.SiteInfos.Select(site => site.Patient.ID + "_" + site.Name).OrderBy(id => id, StringComparer.Ordinal).ToArray() ?? Array.Empty<string>();
            if (preparedSiteIds.Distinct(StringComparer.Ordinal).Count() != preparedSiteIds.Length)
                throw new InvalidDataException("Prepared implantation contains duplicate site IDs.");
            if (targetMesh is HBP.Core.Object3D.RuntimeSingleMesh3D runtimeMesh ? runtimeMesh.SourceMRI != previewMri : previewMri != null)
                throw new InvalidDataException("Preview MRI does not match the prepared mesh.");
            if (targetMesh == null || targetMri == null) throw new InvalidDataException("A prepared mesh and MRI are required.");
            var part = (MeshPart)Int(fields, EntityKind.Scene, "", "", 13);
            var representation = (HBP.Core.Object3D.SurfaceRepresentation)Int(fields, EntityKind.Scene, "", "", 14);
            if (!Enum.IsDefined(typeof(MeshPart), part) || !Enum.IsDefined(typeof(HBP.Core.Object3D.SurfaceRepresentation), representation) || !targetMesh.SupportsHemispheres && part != MeshPart.Both)
                throw new InvalidDataException("Invalid prepared mesh part or representation.");
            if (targetMesh.GetSurface(representation, part) == null || targetMesh.GetSurface(HBP.Core.Object3D.SurfaceRepresentation.Anatomical, part, simplified: true) == null)
                throw new InvalidDataException("Prepared surface variant is unavailable.");
            ReadMask(fields[new StateKey(EntityKind.Scene, "", "", 15)], targetMesh.GetSurface(representation, part).NumberOfTriangles);
            ReadMask(fields[new StateKey(EntityKind.Scene, "", "", 16)], targetMesh.GetSurface(HBP.Core.Object3D.SurfaceRepresentation.Anatomical, part, simplified: true).NumberOfTriangles);
            if (Float(fields, EntityKind.Scene, "", "", 18) > Float(fields, EntityKind.Scene, "", "", 19))
                throw new InvalidDataException("Invalid MRI calibration.");
            if (Bool(fields, EntityKind.Scene, "", "", 28) && (!HBP.Core.Object3D.Object3DManager.MarsAtlas.Loaded || !targetMesh.SupportsMarsAtlas) || Bool(fields, EntityKind.Scene, "", "", 29) && (!HBP.Core.Object3D.Object3DManager.JuBrain.Loaded || !targetMesh.SupportsMNIResources))
                throw new InvalidDataException("Requested atlas is not ready for the prepared mesh.");
            if (Float(fields, EntityKind.Scene, "", "", 30) is < 0 or > 1 || Float(fields, EntityKind.Scene, "", "", 44) is < 0 or > 1 || Enumerable.Range(45, 4).Any(field => Float(fields, EntityKind.Scene, "", "", (ushort)field) is < 0 or > 1) || Float(fields, EntityKind.Scene, "", "", 45) > Float(fields, EntityKind.Scene, "", "", 46) || Float(fields, EntityKind.Scene, "", "", 47) > Float(fields, EntityKind.Scene, "", "", 48))
                throw new InvalidDataException("Invalid atlas opacity or calibration.");
            int ibcContrast = m_Resources.ResolveIbcContrast(Text(fields, EntityKind.Scene, "", "", 32));
            string difumoAtlas = m_Resources.ResolveDifumo(Text(fields, EntityKind.Scene, "", "", 34));
            if ((Bool(fields, EntityKind.Scene, "", "", 31) && (ibcContrast < 0 || !targetMesh.SupportsMNIResources)) || (Bool(fields, EntityKind.Scene, "", "", 33) && (difumoAtlas.Length == 0 || !targetMesh.SupportsMNIResources)))
                throw new InvalidDataException("Requested functional atlas is not prepared for the selected mesh.");
            if (!int.TryParse(Text(fields, EntityKind.Scene, "", "", 35), System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out int difumoArea) || difumoArea < 0 || (difumoAtlas.Length > 0 && difumoArea >= m_Resources.DifumoVolumeCount(difumoAtlas)))
                throw new InvalidDataException("DiFuMo area is outside the prepared atlas.");
            var localizer = m_Resources.ResolveLocalizer(Text(fields, EntityKind.Scene, "", "", 37), Text(fields, EntityKind.Scene, "", "", 38), Text(fields, EntityKind.Scene, "", "", 39));
            int localizerIndex = Int(fields, EntityKind.Scene, "", "", 40);
            if (Bool(fields, EntityKind.Scene, "", "", 36) && (localizer == null || !targetMesh.SupportsMNIResources) || localizer != null && localizerIndex >= localizer.Volumes.Count || localizer == null && localizerIndex != 0)
                throw new InvalidDataException("Localizer selection is outside the prepared resources.");
            if (Float(fields, EntityKind.Scene, "", "", 41) > Float(fields, EntityKind.Scene, "", "", 42) || Float(fields, EntityKind.Scene, "", "", 42) > Float(fields, EntityKind.Scene, "", "", 43))
                throw new InvalidDataException("Invalid localizer thresholds.");
            if (!Enum.IsDefined(typeof(ColorType), Int(fields, EntityKind.Scene, "", "", 21)) || !Enum.IsDefined(typeof(ColorType), Int(fields, EntityKind.Scene, "", "", 22)) || !Enum.IsDefined(typeof(ColorType), Int(fields, EntityKind.Scene, "", "", 23)))
                throw new InvalidDataException("Invalid scene color or colormap.");
            var columns = Members(fields, EntityKind.Column, "");
            if (columns.Count != m_Scene.Columns.Count || !columns.Select(member => member.Id).SequenceEqual(m_Scene.Columns.Select(column => column.ColumnData.ID)))
                throw new InvalidDataException("Column topology or order changed.");
            string comparisonId = Text(fields, EntityKind.Scene, "", "", 8);
            if (comparisonId.Length > 0 && !columns.Any(column => preparedSiteIds.Any(siteId => ComparisonSiteId(column.Id, siteId) == comparisonId)))
                throw new InvalidDataException("Comparison site is outside the prepared implantation.");
            string correlationReference = Text(fields, EntityKind.Scene, "", "", 9);
            if (correlationReference.Length > 0)
            {
                if (!m_CorrelationResources.TryGetValue(correlationReference, out var resource)) throw new InvalidDataException("Correlation resource is not prepared.");
                resource.Result.ValidateFor(m_Scene, preparedSiteIds);
            }

            if (columns.Count(member => Text(fields, EntityKind.Column, "", member.Id, 4).Length > 0) > 1)
                throw new InvalidDataException("Only one site can be selected in the common scene.");
            string activeColumnId = Text(fields, EntityKind.Scene, "", "", 1);
            if (columns.Any(member => Text(fields, EntityKind.Column, "", member.Id, 4).Length > 0 && member.Id != activeColumnId))
                throw new InvalidDataException("Selected site must belong to the active column.");
            for (int i = 0; i < columns.Count; i++)
            {
                Column3D column = m_Scene.Columns[i];
                string id = columns[i].Id;
                foreach (ushort field in new ushort[] { 3, 4, 5 }) Require(fields, EntityKind.Column, "", id, field);
                if (Int(fields, EntityKind.Column, "", id, 3) != Modality(column)) throw new InvalidDataException("Column modality changed.");
                if (fields.Keys.Any(key => key.Entity == EntityKind.Column && key.Id == id && !SupportsColumnField(column, key.FieldId)))
                    throw new NotSupportedException($"Column {id} received a field outside its modality.");
                if (column is Column3DAnatomy) Require(fields, EntityKind.Column, "", id, 6);
                if (column is Column3DStatic)
                {
                    for (ushort field = 7; field <= 11; field++) Require(fields, EntityKind.Column, "", id, field);
                    m_Resources.ResolveColumnIndex(column, Text(fields, EntityKind.Column, "", id, 7));
                }

                if (column is Column3DDynamic)
                    for (ushort field = 12; field <= 15; field++)
                        Require(fields, EntityKind.Column, "", id, field);
                if (column is Column3DFMRI or Column3DMEG)
                {
                    for (ushort field = 19; field <= 26; field++) Require(fields, EntityKind.Column, "", id, field);
                    m_Resources.ResolveColumnIndex(column, Text(fields, EntityKind.Column, "", id, 19));
                    float negativeMin = Float(fields, EntityKind.Column, "", id, 20);
                    float negativeMax = Float(fields, EntityKind.Column, "", id, 21);
                    float positiveMin = Float(fields, EntityKind.Column, "", id, 22);
                    float positiveMax = Float(fields, EntityKind.Column, "", id, 23);
                    if (negativeMin < 0 || negativeMin > negativeMax || negativeMax > 1 || positiveMin < 0 || positiveMin > positiveMax || positiveMax > 1)
                        throw new InvalidDataException("Invalid functional calibration.");
                }

                if (column is Column3DStatic && (Float(fields, EntityKind.Column, "", id, 8) > Float(fields, EntityKind.Column, "", id, 9) || Float(fields, EntityKind.Column, "", id, 9) > Float(fields, EntityKind.Column, "", id, 10)))
                    throw new InvalidDataException("Invalid static span.");
                if (column is Column3DDynamic && (Float(fields, EntityKind.Column, "", id, 12) > Float(fields, EntityKind.Column, "", id, 13) || Float(fields, EntityKind.Column, "", id, 13) > Float(fields, EntityKind.Column, "", id, 14)))
                    throw new InvalidDataException("Invalid dynamic span.");
                if (column is Column3DCCEP ccep)
                {
                    for (ushort field = 16; field <= 18; field++) Require(fields, EntityKind.Column, "", id, field);
                    int mode = Int(fields, EntityKind.Column, "", id, 16);
                    if (!Enum.IsDefined(typeof(Column3DCCEP.CCEPMode), mode)) throw new InvalidDataException("Invalid CCEP source mode.");
                    string sourceId = Text(fields, EntityKind.Column, "", id, 17);
                    if (sourceId.Length > 0 && (!preparedSiteIds.Contains(sourceId) || !ccep.ColumnCCEPData.Data.ProcessedValuesByChannelIDByStimulatedChannelID.ContainsKey(sourceId))) throw new InvalidDataException("Unknown CCEP source site.");
                    string area = Text(fields, EntityKind.Column, "", id, 18);
                    if (area.Length > 0 && !int.TryParse(area, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out _)) throw new InvalidDataException("Invalid CCEP source area.");
                    if (mode == (int)Column3DCCEP.CCEPMode.Site && area.Length > 0 || mode == (int)Column3DCCEP.CCEPMode.MarsAtlas && sourceId.Length > 0)
                        throw new InvalidDataException("CCEP source does not match its mode.");
                    if (mode == (int)Column3DCCEP.CCEPMode.MarsAtlas && area.Length > 0 && (!targetMesh.SupportsMarsAtlas || !HBP.Core.Object3D.Object3DManager.MarsAtlas.Loaded || !HBP.Core.Object3D.Object3DManager.MarsAtlas.Labels().Contains(int.Parse(area, System.Globalization.CultureInfo.InvariantCulture)) || targetImplantation?.SiteInfos.Any(info => info.SiteData?.Tags?.Any(value => value.Tag is HBP.Core.Data.StringTag tag && tag.Name == "MarsAtlas") == true) != true))
                        throw new InvalidDataException("CCEP Mars atlas source is not prepared for this implantation.");
                }

                if (column.NavigationTimeline is { Length: > 0 } timeline)
                {
                    for (ushort field = 27; field <= 32; field++) Require(fields, EntityKind.Column, "", id, field);
                    int length = timeline.Length;
                    float sampling = timeline.Frequency.RawValue;
                    if (column is Column3DFMRI fmri)
                    {
                        int resourceIndex = m_Resources.ResolveColumnIndex(column, Text(fields, EntityKind.Column, "", id, 19));
                        if (resourceIndex < 0) throw new InvalidDataException("Functional timeline has no prepared resource.");
                        var resource = fmri.ColumnFMRIData.Data.FMRIs[resourceIndex].Item1;
                        length = resource.Volumes.Count;
                        sampling = HBP.Core.Data.TimelineTools.GetFrequencyFromUnit(resource.TimeUnit, resource.TimeStep).RawValue;
                    }
                    else if (column is Column3DMEG meg)
                    {
                        int resourceIndex = m_Resources.ResolveColumnIndex(column, Text(fields, EntityKind.Column, "", id, 19));
                        if (resourceIndex < 0) throw new InvalidDataException("Functional timeline has no prepared resource.");
                        var resource = meg.ColumnMEGData.Data.MEGItems[resourceIndex].FMRI;
                        length = resource.Volumes.Count;
                        sampling = HBP.Core.Data.TimelineTools.GetFrequencyFromUnit(resource.TimeUnit, resource.TimeStep).RawValue;
                    }

                    int index = Int(fields, EntityKind.Column, "", id, 27);
                    bool playing = Bool(fields, EntityKind.Column, "", id, 28);
                    float anchor = Float(fields, EntityKind.Column, "", id, 32);
                    if (index < 0 || index >= length || Float(fields, EntityKind.Column, "", id, 31) != sampling || (!playing && anchor != 0f) || anchor < 0f)
                        throw new InvalidDataException("Invalid navigation timeline or clock anchor.");
                }

                string[] incomingSites = fields.Keys.Where(key => key.Entity == EntityKind.Site && key.ParentId == id && key.FieldId == 1 && fields[key][0] == 1).Select(key => key.Id).OrderBy(value => value, StringComparer.Ordinal).ToArray();
                if (!incomingSites.SequenceEqual(preparedSiteIds))
                    throw new InvalidDataException("Site topology changed.");
                foreach (string siteId in incomingSites)
                    for (ushort field = 1; field <= 7; field++)
                        Require(fields, EntityKind.Site, id, siteId, field);
                string selectedSiteId = Text(fields, EntityKind.Column, "", id, 4);
                if (selectedSiteId.Length > 0 && !incomingSites.Contains(selectedSiteId)) throw new InvalidDataException("Selected site is outside the prepared column.");
            }

            foreach (var member in Members(fields, EntityKind.Cut, ""))
            {
                if (m_RemovedCuts.Contains(member.Id)) throw new InvalidDataException("A deleted cut ID cannot be reused.");
                for (ushort field = 1; field <= 6; field++) Require(fields, EntityKind.Cut, "", member.Id, field);
                if (!Enum.IsDefined(typeof(CutOrientation), Int(fields, EntityKind.Cut, "", member.Id, 3)) || Float(fields, EntityKind.Cut, "", member.Id, 6) is < 0 or > 1)
                    throw new InvalidDataException("Invalid cut orientation or position.");
            }

            foreach (var roi in Members(fields, EntityKind.Roi, ""))
            {
                if (m_RemovedRois.Contains(roi.Id)) throw new InvalidDataException("A deleted ROI ID cannot be reused.");
                for (ushort field = 1; field <= 3; field++) Require(fields, EntityKind.Roi, "", roi.Id, field);
                foreach (var sphere in Members(fields, EntityKind.Sphere, roi.Id))
                {
                    if (m_RemovedSpheres.TryGetValue(roi.Id, out var removed) && removed.Contains(sphere.Id)) throw new InvalidDataException("A deleted sphere ID cannot be reused.");
                    for (ushort field = 1; field <= 4; field++) Require(fields, EntityKind.Sphere, roi.Id, sphere.Id, field);
                }
            }

            return fields;
        }

        private static void Require(SortedDictionary<StateKey, byte[]> fields, EntityKind kind, string parent, string id, ushort field)
        {
            if (!fields.ContainsKey(new StateKey(kind, parent, id, field))) throw new InvalidDataException($"Missing required field {kind}/{parent}/{id}/{field}.");
        }

        private void ValidateCutOrder(SortedDictionary<StateKey, byte[]> fields)
        {
            var wanted = Members(fields, EntityKind.Cut, "").Select(member => member.Id).ToArray();
            var retained = m_Scene.Cuts.Select(cut => cut.ID).Where(id => wanted.Contains(id)).ToArray();
            var appended = wanted.Where(id => !retained.Contains(id));
            if (!retained.Concat(appended).SequenceEqual(wanted)) throw new InvalidDataException("Cut reordering is not available in place.");
        }

        private void RememberMembership(SortedDictionary<StateKey, byte[]> fields)
        {
            foreach (StateKey key in fields.Keys.Where(key => key.Entity == EntityKind.Site && key.FieldId == 1))
            {
                if (!m_KnownSites.TryGetValue(key.ParentId, out var sites)) m_KnownSites.Add(key.ParentId, sites = new HashSet<string>());
                sites.Add(key.Id);
            }

            foreach (var entry in fields.Where(entry => entry.Key.FieldId == 1 && entry.Key.Entity is EntityKind.Cut or EntityKind.Roi or EntityKind.Sphere))
            {
                StateKey key = entry.Key;
                bool exists = entry.Value[0] == 1;
                if (key.Entity == EntityKind.Cut)
                {
                    m_KnownCuts.Add(key.Id);
                    if (!exists) m_RemovedCuts.Add(key.Id);
                }
                else if (key.Entity == EntityKind.Roi)
                {
                    m_KnownRois.Add(key.Id);
                    if (!exists) m_RemovedRois.Add(key.Id);
                }
                else
                {
                    if (!m_KnownSpheres.TryGetValue(key.ParentId, out var known)) m_KnownSpheres.Add(key.ParentId, known = new HashSet<string>());
                    if (!m_RemovedSpheres.TryGetValue(key.ParentId, out var removed)) m_RemovedSpheres.Add(key.ParentId, removed = new HashSet<string>());
                    known.Add(key.Id);
                    if (!exists) removed.Add(key.Id);
                }
            }
        }

        private static bool Supports(StateKey key) =>
            key.Entity switch
            {
                EntityKind.Scene => key.FieldId is 1 or 2 or 3 or 4 or 5 or 6 or 7 or 8 or 9 or 10 or 11 or 12 or 13 or 14 or 15 or 16 or 17 or 18 or 19 or 20 or 21 or 22 or 23 or 24 or 25 or 26 or 27 or 28 or 29 or 30 or 31 or 32 or 33 or 34 or 35 or 36 or 37 or 38 or 39 or 40 or 41 or 42 or 43 or 44 or 45 or 46 or 47 or 48,
                EntityKind.Column => key.FieldId <= 32,
                EntityKind.Site => key.FieldId <= 7,
                EntityKind.Cut => key.FieldId <= 6,
                EntityKind.Roi => key.FieldId <= 3,
                EntityKind.Sphere => key.FieldId <= 4,
                _ => false
            };

        private void BindInitialIds(SortedDictionary<StateKey, byte[]> fields)
        {
            var cuts = Members(fields, EntityKind.Cut, "");
            for (int i = 0; i < Math.Min(cuts.Count, m_Scene.Cuts.Count); i++) m_Scene.Cuts[i].ID = cuts[i].Id;
            var rois = Members(fields, EntityKind.Roi, "");
            for (int i = 0; i < Math.Min(rois.Count, m_Scene.ROIManager.ROIs.Count); i++)
            {
                ROI roi = m_Scene.ROIManager.ROIs[i];
                roi.ID = rois[i].Id;
                var spheres = Members(fields, EntityKind.Sphere, roi.ID);
                for (int j = 0; j < Math.Min(spheres.Count, roi.Spheres.Count); j++) roi.Spheres[j].ID = spheres[j].Id;
            }
        }

        private void ApplyResources(SortedDictionary<StateKey, byte[]> fields)
        {
            var implantation = m_Resources.ResolveImplantation(Text(fields, EntityKind.Scene, "", "", 20));
            if (implantation != m_Scene.ImplantationManager.SelectedImplantation) m_Scene.ImplantationManager.SelectPrepared(implantation);
            var mesh = m_Resources.ResolveMesh(Text(fields, EntityKind.Scene, "", "", 11));
            var mri = m_Resources.ResolveMri(Text(fields, EntityKind.Scene, "", "", 17));
            m_Scene.MeshManager.SelectPrepared(mesh);
            var part = (MeshPart)Int(fields, EntityKind.Scene, "", "", 13);
            if (m_Scene.MeshManager.MeshPartToDisplay != part) m_Scene.MeshManager.SelectMeshPart(part);
            var representation = (HBP.Core.Object3D.SurfaceRepresentation)Int(fields, EntityKind.Scene, "", "", 14);
            if (mesh.Representation != representation) m_Scene.MeshManager.SelectRepresentation(representation);
            m_Scene.MRIManager.SelectPrepared(mri);
            m_Scene.MRIManager.SetCalValues(Float(fields, EntityKind.Scene, "", "", 18), Float(fields, EntityKind.Scene, "", "", 19));
        }

        private void ApplyMasks(SortedDictionary<StateKey, byte[]> fields)
        {
            int[] full = ReadMask(fields[new StateKey(EntityKind.Scene, "", "", 15)], m_Scene.MeshManager.BrainSurface.NumberOfTriangles);
            int[] simplified = ReadMask(fields[new StateKey(EntityKind.Scene, "", "", 16)], m_Scene.MeshManager.SimplifiedMeshToUse.NumberOfTriangles);
            if (full.SequenceEqual(m_Scene.MeshManager.BrainSurface.VisibilityMask) && simplified.SequenceEqual(m_Scene.MeshManager.SimplifiedMeshToUse.VisibilityMask)) return;
            m_Scene.TriangleEraser.CurrentMasks = new List<int[]> { full, simplified };
        }

        private void ApplyCuts(SortedDictionary<StateKey, byte[]> fields, HashSet<string> changed = null)
        {
            var wanted = Members(fields, EntityKind.Cut, "");
            foreach (var cut in m_Scene.Cuts.ToArray())
                if (wanted.All(member => member.Id != cut.ID))
                    m_Scene.RemoveCutPlane(cut);
            foreach (var member in wanted)
            {
                if (changed != null && !changed.Contains(member.Id)) continue;
                var cut = m_Scene.Cuts.FirstOrDefault(item => item.ID == member.Id);
                if (cut == null) (cut = m_Scene.AddCutPlane()).ID = member.Id;
                cut.Orientation = (CutOrientation)Int(fields, EntityKind.Cut, "", member.Id, 3);
                cut.Normal = Vector(fields, EntityKind.Cut, "", member.Id, 4);
                cut.Flip = Bool(fields, EntityKind.Cut, "", member.Id, 5);
                cut.Position = Float(fields, EntityKind.Cut, "", member.Id, 6);
                m_Scene.UpdateCutPlane(cut);
            }

            if (!m_Scene.Cuts.Select(cut => cut.ID).SequenceEqual(wanted.Select(member => member.Id)))
                throw new InvalidDataException("Cut order cannot be applied in place");
        }

        private void ApplyTimelines(SortedDictionary<StateKey, byte[]> fields, HashSet<string> changed)
        {
            foreach (string id in changed)
            {
                var column = m_Scene.Columns.SingleOrDefault(item => item.ColumnData.ID == id);
                var timeline = column?.NavigationTimeline;
                if (timeline is not { Length: > 0 }) throw new InvalidDataException("Unknown prepared timeline.");
                for (ushort field = 27; field <= 32; field++) Require(fields, EntityKind.Column, "", id, field);
                int index = Int(fields, EntityKind.Column, "", id, 27);
                bool playing = Bool(fields, EntityKind.Column, "", id, 28);
                int step = Int(fields, EntityKind.Column, "", id, 30);
                float anchor = Float(fields, EntityKind.Column, "", id, 32);
                if (index < 0 || index >= timeline.Length || step < 1 || Float(fields, EntityKind.Column, "", id, 31) != timeline.Frequency.RawValue || (!playing && anchor != 0f) || anchor < 0f)
                    throw new InvalidDataException("Invalid prepared timeline state.");
                timeline.IsLooping = Bool(fields, EntityKind.Column, "", id, 29);
                timeline.Step = step;
                if (timeline.CurrentIndex != index) timeline.CurrentIndex = index;
                if (timeline.IsPlaying != playing) timeline.IsPlaying = playing;
                timeline.ApplySynchronizedClockAnchor(anchor);
            }
        }

        private void ApplyRois(SortedDictionary<StateKey, byte[]> fields)
        {
            var wanted = Members(fields, EntityKind.Roi, "");
            foreach (var roi in m_Scene.ROIManager.ROIs.ToArray())
                if (wanted.All(member => member.Id != roi.ID))
                    m_Scene.ROIManager.RemoveROI(roi);
            foreach (var member in wanted)
            {
                ROI roi = m_Scene.ROIManager.ROIs.FirstOrDefault(item => item.ID == member.Id);
                if (!roi) (roi = m_Scene.ROIManager.AddROI()).ID = member.Id;
                roi.Name = Text(fields, EntityKind.Roi, "", member.Id, 3);
                var spheres = Members(fields, EntityKind.Sphere, member.Id);
                for (int i = roi.Spheres.Count - 1; i >= 0; i--)
                    if (spheres.All(sphere => sphere.Id != roi.Spheres[i].ID))
                        roi.RemoveSphere(i);
                foreach (var sphereMember in spheres)
                {
                    Sphere sphere = roi.Spheres.FirstOrDefault(item => item.ID == sphereMember.Id);
                    Vector3 position = Vector(fields, EntityKind.Sphere, member.Id, sphereMember.Id, 3);
                    float radius = Float(fields, EntityKind.Sphere, member.Id, sphereMember.Id, 4);
                    if (!sphere)
                    {
                        roi.AddSphere(Module3DMain.DEFAULT_MESHES_LAYER, "Sphere", position, radius);
                        (sphere = roi.Spheres.Last()).ID = sphereMember.Id;
                    }

                    sphere.Position = position;
                    sphere.SetInfluenceRadius(radius);
                }

                Sphere selectedSphere = roi.SelectedSphereID >= 0 && roi.SelectedSphereID < roi.Spheres.Count ? roi.Spheres[roi.SelectedSphereID] : null;
                var sphereOrders = spheres.Select((sphere, index) => (sphere.Id, index)).ToDictionary(item => item.Id, item => item.index);
                roi.Spheres.Sort((left, right) => sphereOrders[left.ID].CompareTo(sphereOrders[right.ID]));
                roi.SelectedSphereID = selectedSphere ? roi.Spheres.IndexOf(selectedSphere) : -1;
            }

            var roiOrders = wanted.Select((roi, index) => (roi.Id, index)).ToDictionary(item => item.Id, item => item.index);
            m_Scene.ROIManager.ROIs.Sort((left, right) => roiOrders[left.ID].CompareTo(roiOrders[right.ID]));
            m_Scene.ROIManager.UpdateROIMasks();
        }

        private void ApplySites(SortedDictionary<StateKey, byte[]> fields, HashSet<StateKey> changed = null)
        {
            Column3D selectedColumn = null;
            HBP.Core.Object3D.Site selectedSite = null;
            foreach (Column3D column in m_Scene.Columns)
            {
                string id = column.ColumnData.ID;
                bool columnChanged = changed == null || changed.Any(key => key.Entity == EntityKind.Column && key.Id == id);
                bool siteChanged = changed == null || changed.Any(key => key.Entity == EntityKind.Site && key.ParentId == id);
                if (!columnChanged && !siteChanged) continue;
                if (columnChanged)
                {
                    column.ActivityAlpha = Float(fields, EntityKind.Column, "", id, 5);
                    if (column is Column3DAnatomy anatomy)
                        anatomy.AnatomyParameters.InfluenceDistance = Float(fields, EntityKind.Column, "", id, 6);
                    if (column is Column3DStatic staticColumn)
                    {
                        int labelIndex = m_Resources.ResolveColumnIndex(column, Text(fields, EntityKind.Column, "", id, 7));
                        if (labelIndex >= 0 && staticColumn.SelectedLabelIndex != labelIndex) staticColumn.SelectedLabelIndex = labelIndex;
                        staticColumn.StaticParameters.InfluenceDistance = Float(fields, EntityKind.Column, "", id, 11);
                        staticColumn.StaticParameters.ApplySynchronizedSpanValues(Float(fields, EntityKind.Column, "", id, 8), Float(fields, EntityKind.Column, "", id, 9), Float(fields, EntityKind.Column, "", id, 10));
                    }

                    if (column is Column3DDynamic dynamicColumn)
                    {
                        dynamicColumn.DynamicParameters.InfluenceDistance = Float(fields, EntityKind.Column, "", id, 15);
                        dynamicColumn.DynamicParameters.ApplySynchronizedSpanValues(Float(fields, EntityKind.Column, "", id, 12), Float(fields, EntityKind.Column, "", id, 13), Float(fields, EntityKind.Column, "", id, 14));
                    }

                    if (column is Column3DCCEP ccep)
                    {
                        var mode = (Column3DCCEP.CCEPMode)Int(fields, EntityKind.Column, "", id, 16);
                        if (ccep.Mode != mode) ccep.Mode = mode;
                        string sourceId = Text(fields, EntityKind.Column, "", id, 17);
                        ccep.SelectedSourceSite = sourceId.Length == 0 ? null : ccep.Sources.Single(site => site.Information.FullID == sourceId);
                        string area = Text(fields, EntityKind.Column, "", id, 18);
                        ccep.SelectedSourceMarsAtlasLabel = area.Length == 0 ? -1 : int.Parse(area, System.Globalization.CultureInfo.InvariantCulture);
                    }

                    if (column is Column3DFMRI fmri)
                    {
                        int resourceIndex = m_Resources.ResolveColumnIndex(column, Text(fields, EntityKind.Column, "", id, 19));
                        if (resourceIndex >= 0 && fmri.SelectedFMRIIndex != resourceIndex) fmri.SelectedFMRIIndex = resourceIndex;
                        fmri.FMRIParameters.ApplySynchronizedCalibration(Float(fields, EntityKind.Column, "", id, 20), Float(fields, EntityKind.Column, "", id, 21), Float(fields, EntityKind.Column, "", id, 22), Float(fields, EntityKind.Column, "", id, 23));
                        bool lower = Bool(fields, EntityKind.Column, "", id, 24), middle = Bool(fields, EntityKind.Column, "", id, 25), higher = Bool(fields, EntityKind.Column, "", id, 26);
                        if (fmri.FMRIParameters.HideLowerValues != lower || fmri.FMRIParameters.HideMiddleValues != middle || fmri.FMRIParameters.HideHigherValues != higher)
                            fmri.FMRIParameters.SetHideValues(lower, middle, higher);
                    }

                    if (column is Column3DMEG meg)
                    {
                        int resourceIndex = m_Resources.ResolveColumnIndex(column, Text(fields, EntityKind.Column, "", id, 19));
                        if (resourceIndex >= 0 && meg.SelectedMEGIndex != resourceIndex) meg.SelectedMEGIndex = resourceIndex;
                        meg.MEGParameters.ApplySynchronizedCalibration(Float(fields, EntityKind.Column, "", id, 20), Float(fields, EntityKind.Column, "", id, 21), Float(fields, EntityKind.Column, "", id, 22), Float(fields, EntityKind.Column, "", id, 23));
                        bool lower = Bool(fields, EntityKind.Column, "", id, 24), middle = Bool(fields, EntityKind.Column, "", id, 25), higher = Bool(fields, EntityKind.Column, "", id, 26);
                        if (meg.MEGParameters.HideLowerValues != lower || meg.MEGParameters.HideMiddleValues != middle || meg.MEGParameters.HideHigherValues != higher)
                            meg.MEGParameters.SetHideValues(lower, middle, higher);
                    }

                    if (column.NavigationTimeline is { Length: > 0 } timeline)
                    {
                        timeline.IsLooping = Bool(fields, EntityKind.Column, "", id, 29);
                        timeline.Step = Int(fields, EntityKind.Column, "", id, 30);
                        int targetIndex = Int(fields, EntityKind.Column, "", id, 27);
                        if (timeline.CurrentIndex != targetIndex) timeline.CurrentIndex = targetIndex;
                        bool playing = Bool(fields, EntityKind.Column, "", id, 28);
                        if (timeline.IsPlaying != playing) timeline.IsPlaying = playing;
                        timeline.ApplySynchronizedClockAnchor(Float(fields, EntityKind.Column, "", id, 32));
                    }
                }

                foreach (var site in column.Sites)
                {
                    string siteId = site.Information.FullID;
                    if (changed != null && !changed.Any(key => key.Entity == EntityKind.Site && key.ParentId == id && key.Id == siteId)) continue;
                    if (!Bool(fields, EntityKind.Site, id, siteId, 1)) throw new InvalidDataException("Site topology changed");
                    Color color = ReadColor(fields[new StateKey(EntityKind.Site, id, siteId, 5)]);
                    site.State.ApplySynchronizedState(Bool(fields, EntityKind.Site, id, siteId, 2), Bool(fields, EntityKind.Site, id, siteId, 3), Bool(fields, EntityKind.Site, id, siteId, 4), color, ReadList(fields[new StateKey(EntityKind.Site, id, siteId, 6)]));
                    Vector3 position = Vector(fields, EntityKind.Site, id, siteId, 7);
                    if (site.transform.localPosition != position) site.transform.localPosition = position;
                }

                if (columnChanged)
                {
                    string selectedId = Text(fields, EntityKind.Column, "", id, 4);
                    var selected = selectedId.Length == 0 ? null : column.Sites.FirstOrDefault(site => site.Information.FullID == selectedId);
                    if (column.SelectedSite && column.SelectedSite != selected) column.UnselectSite();
                    if (selected)
                    {
                        selectedColumn = column;
                        selectedSite = selected;
                    }
                }
            }

            if (selectedSite && selectedColumn.SelectedSite != selectedSite) m_Scene.SelectSiteForSynchronization(selectedColumn, selectedSite);
        }

        private void ApplyScene(SortedDictionary<StateKey, byte[]> fields, HashSet<ushort> changed = null)
        {
            bool Has(params ushort[] ids) => changed == null || ids.Any(changed.Contains);
            if (Has(3)) m_Scene.StrongCuts = Bool(fields, EntityKind.Scene, "", "", 3);
            if (Has(4)) m_Scene.AutomaticCutAroundSelectedSite = Bool(fields, EntityKind.Scene, "", "", 4);
            if (Has(5)) m_Scene.HideBlacklistedSites = Bool(fields, EntityKind.Scene, "", "", 5);
            if (Has(6)) m_Scene.ShowAllSites = Bool(fields, EntityKind.Scene, "", "", 6);
            if (Has(7)) m_Scene.SiteGain = Float(fields, EntityKind.Scene, "", "", 7);
            if (Has(8))
            {
                string comparisonId = Text(fields, EntityKind.Scene, "", "", 8);
                var comparisonSite = comparisonId.Length == 0 ? null : m_Scene.Columns.SelectMany(column => column.Sites.Select(site => (column, site))).Single(pair => ComparisonSiteId(pair.column.ColumnData.ID, pair.site.Information.FullID) == comparisonId).site;
                m_Scene.ImplantationManager.SetComparisonSiteForSynchronization(comparisonSite);
            }

            if (Has(9))
            {
                string correlationReference = Text(fields, EntityKind.Scene, "", "", 9);
                if (correlationReference.Length == 0)
                {
                    if (m_Scene.ColumnsIEEG.Any(column => column.CorrelationBySitePair.Count > 0 || column.CorrelationMeanBySitePair.Count > 0)) m_Scene.ResetCorrelations();
                }
                else m_CorrelationResources[correlationReference].Result.Apply(m_Scene);
            }

            if (Has(10)) m_Scene.DisplayCorrelations = Bool(fields, EntityKind.Scene, "", "", 10);
            if (Has(21)) m_Scene.BrainColor = (ColorType)Int(fields, EntityKind.Scene, "", "", 21);
            if (Has(22)) m_Scene.CutColor = (ColorType)Int(fields, EntityKind.Scene, "", "", 22);
            if (Has(23)) m_Scene.Colormap = (ColorType)Int(fields, EntityKind.Scene, "", "", 23);
            if (Has(24)) m_Scene.EdgeMode = Bool(fields, EntityKind.Scene, "", "", 24);
            if (Has(25)) m_Scene.IsBrainTransparent = Bool(fields, EntityKind.Scene, "", "", 25);
            if (Has(26)) m_Scene.BrainMaterials.SetAlpha(Float(fields, EntityKind.Scene, "", "", 26));
            if (Has(28))
            {
                bool mars = Bool(fields, EntityKind.Scene, "", "", 28);
                if (m_Scene.AtlasManager.DisplayMarsAtlas != mars) m_Scene.AtlasManager.DisplayMarsAtlas = mars;
            }

            if (Has(29))
            {
                bool juBrain = Bool(fields, EntityKind.Scene, "", "", 29);
                if (m_Scene.AtlasManager.DisplayJuBrainAtlas != juBrain) m_Scene.AtlasManager.DisplayJuBrainAtlas = juBrain;
            }

            if (Has(30)) m_Scene.AtlasManager.AtlasAlpha = Float(fields, EntityKind.Scene, "", "", 30);
            var fmri = m_Scene.FMRIManager;
            if (Has(31, 32, 33, 34, 35))
                fmri.ApplySynchronizedAtlasSources(Bool(fields, EntityKind.Scene, "", "", 31), Math.Max(0, m_Resources.ResolveIbcContrast(Text(fields, EntityKind.Scene, "", "", 32))), Bool(fields, EntityKind.Scene, "", "", 33), m_Resources.ResolveDifumo(Text(fields, EntityKind.Scene, "", "", 34)), int.Parse(Text(fields, EntityKind.Scene, "", "", 35), System.Globalization.CultureInfo.InvariantCulture));
            if (Has(36, 37, 38, 39, 40, 41, 42, 43))
            {
                var localizerNames = m_Resources.ResolveLocalizerNames(Text(fields, EntityKind.Scene, "", "", 37), Text(fields, EntityKind.Scene, "", "", 38));
                fmri.ApplySynchronizedLocalizer(Bool(fields, EntityKind.Scene, "", "", 36), localizerNames.Protocol, localizerNames.Data, m_Resources.ResolveLocalizerBlocName(Text(fields, EntityKind.Scene, "", "", 37), Text(fields, EntityKind.Scene, "", "", 38), Text(fields, EntityKind.Scene, "", "", 39)), Int(fields, EntityKind.Scene, "", "", 40), Float(fields, EntityKind.Scene, "", "", 41), Float(fields, EntityKind.Scene, "", "", 42), Float(fields, EntityKind.Scene, "", "", 43));
            }

            if (Has(44, 45, 46, 47, 48)) fmri.ApplySynchronizedAtlasCalibration(Float(fields, EntityKind.Scene, "", "", 44), Float(fields, EntityKind.Scene, "", "", 45), Float(fields, EntityKind.Scene, "", "", 46), Float(fields, EntityKind.Scene, "", "", 47), Float(fields, EntityKind.Scene, "", "", 48));
            if (Has(1))
            {
                string selectedColumnId = Text(fields, EntityKind.Scene, "", "", 1);
                if (selectedColumnId.Length != 0) m_Scene.SelectColumn(m_Scene.Columns.Single(column => column.ColumnData.ID == selectedColumnId));
                else
                    foreach (var column in m_Scene.Columns)
                        column.IsSelected = false;
            }

            if (Has(2))
            {
                string activeRoiId = Text(fields, EntityKind.Scene, "", "", 2);
                m_Scene.ROIManager.SelectedROI = activeRoiId.Length == 0 ? null : m_Scene.ROIManager.ROIs.Single(roi => roi.ID == activeRoiId);
            }

            if (Has(4) && m_Scene.AutomaticCutAroundSelectedSite && m_Scene.Cuts.Count == 3) m_Scene.MarkAutomaticCutsAsCurrent();
        }

        private static List<(string Id, int Order)> Members(SortedDictionary<StateKey, byte[]> fields, EntityKind kind, string parent) => fields.Where(field => field.Key.Entity == kind && field.Key.ParentId == parent && field.Key.FieldId == 1 && field.Value[0] == 1).Select(field => (field.Key.Id, Int(fields, kind, parent, field.Key.Id, 2))).OrderBy(member => member.Item2).ToList();

        private static bool SupportsColumnField(Column3D column, ushort field) => field <= 5 || column is Column3DAnatomy && field == 6 || column is Column3DStatic && field is >= 7 and <= 11 || column is Column3DDynamic && field is >= 12 and <= 15 || column is Column3DCCEP && field is >= 16 and <= 18 || (column is Column3DFMRI or Column3DMEG) && field is >= 19 and <= 26 || column.NavigationTimeline is { Length: > 0 } && field is >= 27 and <= 32;

        private static int Modality(Column3D column) =>
            column switch
            {
                Column3DAnatomy => 0,
                Column3DStatic => 1,
                Column3DIEEG => 2,
                Column3DCCEP => 3,
                Column3DFMRI => 4,
                Column3DMEG => 5,
                _ => throw new NotSupportedException($"Unsupported column modality: {column.GetType().Name}")
            };

        private static bool Bool(SortedDictionary<StateKey, byte[]> fields, EntityKind kind, string parent, string id, ushort field) => fields[new StateKey(kind, parent, id, field)][0] == 1;
        private static int Int(SortedDictionary<StateKey, byte[]> fields, EntityKind kind, string parent, string id, ushort field) => BitConverter.ToInt32(fields[new StateKey(kind, parent, id, field)], 0);
        private static float Float(SortedDictionary<StateKey, byte[]> fields, EntityKind kind, string parent, string id, ushort field) => BitConverter.ToSingle(fields[new StateKey(kind, parent, id, field)], 0);
        private static string Text(SortedDictionary<StateKey, byte[]> fields, EntityKind kind, string parent, string id, ushort field) => Encoding.UTF8.GetString(fields[new StateKey(kind, parent, id, field)]);

        private static string ComparisonSiteId(string columnId, string siteId)
        {
            using SHA256 sha = SHA256.Create();
            byte[] columnBytes = Encoding.UTF8.GetBytes(columnId);
            byte[] siteBytes = Encoding.UTF8.GetBytes(siteId);
            byte[] key = new byte[4 + columnBytes.Length + siteBytes.Length];
            Buffer.BlockCopy(BitConverter.GetBytes(columnBytes.Length), 0, key, 0, 4);
            Buffer.BlockCopy(columnBytes, 0, key, 4, columnBytes.Length);
            Buffer.BlockCopy(siteBytes, 0, key, 4 + columnBytes.Length, siteBytes.Length);
            return "site:" + BitConverter.ToString(sha.ComputeHash(key)).Replace("-", "").ToLowerInvariant();
        }

        private static Vector3 Vector(SortedDictionary<StateKey, byte[]> fields, EntityKind kind, string parent, string id, ushort field)
        {
            byte[] value = fields[new StateKey(kind, parent, id, field)];
            return new Vector3(BitConverter.ToSingle(value, 0), BitConverter.ToSingle(value, 4), BitConverter.ToSingle(value, 8));
        }

        private static Color ReadColor(byte[] value) => new(BitConverter.ToSingle(value, 0), BitConverter.ToSingle(value, 4), BitConverter.ToSingle(value, 8), BitConverter.ToSingle(value, 12));

        private static byte[] PackMask(IReadOnlyList<int> triangles)
        {
            var bits = new byte[(triangles.Count + 7) / 8];
            for (int i = 0; i < triangles.Count; i++)
            {
                if (triangles[i] is not (0 or 1)) throw new InvalidDataException("Nonbinary triangle visibility mask.");
                if (triangles[i] == 1) bits[i / 8] |= (byte)(1 << (i % 8));
            }

            return bits;
        }

        private static int[] ReadMask(byte[] bits, int triangleCount)
        {
            if (triangleCount < 0 || bits.Length != (triangleCount + 7) / 8)
                throw new InvalidDataException("Triangle mask does not match prepared topology.");
            if (bits.Length > 0 && triangleCount % 8 != 0 && (bits[^1] >> (triangleCount % 8)) != 0)
                throw new InvalidDataException("Triangle mask has noncanonical trailing bits.");
            var triangles = new int[triangleCount];
            for (int i = 0; i < triangleCount; i++) triangles[i] = (bits[i / 8] >> (i % 8)) & 1;
            return triangles;
        }

        private static string[] ReadList(byte[] value)
        {
            using var stream = new MemoryStream(value);
            using var reader = new BinaryReader(stream, Encoding.UTF8);
            int count = reader.ReadInt32();
            var items = new string[count];
            for (int i = 0; i < count; i++) items[i] = Encoding.UTF8.GetString(reader.ReadBytes(reader.ReadInt32()));
            return items;
        }
    }
}
