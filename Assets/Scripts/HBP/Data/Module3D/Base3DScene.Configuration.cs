using System;
using System.Linq;
using System.Collections.Generic;
using HBP.Core.Enums;
using HBP.Core.Tools;
using HBP.Core.Preferences;
using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Object3D;

namespace HBP.Data.Module3D
{
    /// <summary>A complete persisted site configuration prepared for an atomic application.</summary>
    public readonly struct SiteConfigurationChange
    {
        public Column3D Column { get; }
        public string SiteId { get; }
        public Core.Object3D.SiteState State { get; }
        public Core.Data.SiteConfiguration Configuration { get; }

        public SiteConfigurationChange(Column3D column, string siteId, Core.Object3D.SiteState state, Core.Data.SiteConfiguration configuration)
        {
            Column = column ? column : throw new ArgumentNullException(nameof(column));
            SiteId = string.IsNullOrWhiteSpace(siteId) ? throw new ArgumentException("A site configuration requires a stable site ID.", nameof(siteId)) : siteId;
            State = state ?? throw new ArgumentNullException(nameof(state));
            if (configuration == null) throw new ArgumentNullException(nameof(configuration));
            if (configuration.Labels == null) throw new ArgumentException("A site configuration requires an ordered label list.", nameof(configuration));
            Configuration = (Core.Data.SiteConfiguration)configuration.Clone();
        }
    }

    public partial class Base3DScene
    {
        private bool m_ConfiguredGeometryPending;
        private int m_ConfigurationMutationDepth;

        /// <summary>Raised once before the outermost synchronous configuration load or reset.</summary>
        public event Action ConfigurationMutationStarted;

        /// <summary>Raised once when the outermost configuration load or reset finishes.</summary>
        public event Action<Exception> ConfigurationMutationCompleted;

        /// <summary>Feature-neutral hook used by an attached synchronization boundary to validate and publish one site batch.</summary>
        public Func<IReadOnlyList<SiteConfigurationChange>, Action, bool> SiteConfigurationBatchRouter { get; set; }

        public bool IsConfigurationMutationActive => m_ConfigurationMutationDepth > 0;

        /// <summary>Runs nested scene/column configuration actions as one observable scope.</summary>
        public void RunConfigurationMutation(Action mutation)
        {
            if (mutation == null) throw new ArgumentNullException(nameof(mutation));
            bool isOutermost = m_ConfigurationMutationDepth++ == 0;
            Exception actionError = null;
            try
            {
                if (isOutermost) ConfigurationMutationStarted?.Invoke();
                mutation();
            }
            catch (Exception exception)
            {
                actionError = exception;
                throw;
            }
            finally
            {
                m_ConfigurationMutationDepth--;
                if (isOutermost)
                {
                    try
                    {
                        ConfigurationMutationCompleted?.Invoke(actionError);
                    }
                    catch (Exception completionError)
                    {
                        if (actionError != null) throw new AggregateException("Configuration mutation and completion both failed.", actionError, completionError);
                        throw;
                    }
                }
            }
        }

        /// <summary>Validates the complete persisted assignments before applying them with one scene invalidation.</summary>
        public bool ApplySiteConfigurationBatch(IReadOnlyList<SiteConfigurationChange> changes)
        {
            if (changes == null) throw new ArgumentNullException(nameof(changes));
            SiteConfigurationChange[] prepared = changes.ToArray();
            var targets = new HashSet<(string ColumnId, string SiteId)>();
            foreach (SiteConfigurationChange change in prepared)
            {
                if (!change.Column || change.State == null || change.Configuration == null || change.Configuration.Labels == null)
                    throw new ArgumentException("Site configuration assignments must be complete.", nameof(changes));
                if (!targets.Add((change.Column.ColumnData.ID, change.SiteId)))
                    throw new ArgumentException("A site configuration batch cannot assign a site more than once.", nameof(changes));
            }

            if (prepared.Length == 0) return false;
            Action apply = () => ApplySiteStateBatch(() =>
            {
                foreach (SiteConfigurationChange change in prepared)
                {
                    change.State.ApplyState(change.Configuration.IsBlacklisted, change.Configuration.IsHighlighted, change.Configuration.Color, change.Configuration.Labels);
                    change.Column.SiteStateBySiteID[change.SiteId] = change.State;
                }
            });
            if (SiteConfigurationBatchRouter != null)
                return SiteConfigurationBatchRouter(Array.AsReadOnly(prepared), apply);

            apply();
            return true;
        }

        /// <summary>
        /// Load the visualization configuration from the loaded visualization
        /// </summary>
        /// <param name="firstCall">Has this method not been called by another load method ?</param>
        public void LoadConfiguration(bool firstCall = true)
        {
            RunConfigurationMutation(() => LoadConfigurationCore(firstCall));
        }

        private void LoadConfigurationCore(bool firstCall)
        {
            NormalizeRegionOfInterestIDs();
            SurfaceRepresentation configuredRepresentation = Visualization.Configuration.SurfaceRepresentation;
            if (firstCall) ResetConfiguration();
            BrainColor = Visualization.Configuration.BrainColor;
            CutColor = Visualization.Configuration.BrainCutColor;
            Colormap = Visualization.Configuration.Colormap;
            EdgeMode = Visualization.Configuration.ShowEdges;
            IsBrainTransparent = Visualization.Configuration.TransparentBrain;
            BrainMaterials.SetAlpha(Visualization.Configuration.BrainAlpha);
            StrongCuts = Visualization.Configuration.StrongCuts;
            HideBlacklistedSites = Visualization.Configuration.HideBlacklistedSites;
            ShowAllSites = Visualization.Configuration.ShowAllSites;
            AutomaticCutAroundSelectedSite = Visualization.Configuration.AutomaticCutAroundSelectedSite;
            SiteGain = Visualization.Configuration.SiteGain;
            m_MRIManager.SetCalValues(Visualization.Configuration.MRICalMinFactor, Visualization.Configuration.MRICalMaxFactor);
            CameraType = Visualization.Configuration.CameraType;

            if (Type == SceneType.SinglePatient)
            {
                m_MeshManager.SelectInitialMeshForScene(Visualization.Configuration.MeshName, PersistentDataManager.UserPreferences.Visualization._3D.DefaultSelectedMeshInSinglePatientVisualization, !string.IsNullOrEmpty(Visualization.Configuration.MRIName) ? Visualization.Configuration.MRIName : PersistentDataManager.UserPreferences.Visualization._3D.DefaultSelectedMRIInSinglePatientVisualization);
            }
            else if (!string.IsNullOrEmpty(Visualization.Configuration.MeshName))
            {
                m_MeshManager.Select(Visualization.Configuration.MeshName);
            }

            if (Visualization.Configuration.PreviewMRIName != null)
            {
                var preview = m_MeshManager.Meshes.OfType<RuntimeSingleMesh3D>().FirstOrDefault(mesh => mesh.SourceMRIName == Visualization.Configuration.PreviewMRIName);
                if (preview != null) m_MeshManager.Select(preview.Name);
            }

            if (!string.IsNullOrEmpty(Visualization.Configuration.MRIName)) m_MRIManager.Select(Visualization.Configuration.MRIName);
            if (!string.IsNullOrEmpty(Visualization.Configuration.ImplantationName)) m_ImplantationManager.Select(Visualization.Configuration.ImplantationName);

            m_MeshManager.SelectMeshPart(Visualization.Configuration.MeshPart);
            if (configuredRepresentation == SurfaceRepresentation.Inflated && m_MeshManager.SelectedMesh.HasInflatedRepresentation)
            {
                m_MeshManager.SelectRepresentation(configuredRepresentation);
            }

            foreach (Core.Data.Cut cut in Visualization.Configuration.Cuts)
            {
                Core.Object3D.Cut newCut = AddCutPlane(cut.ID);
                newCut.Normal = cut.Normal.ToVector3();
                newCut.Orientation = cut.Orientation;
                newCut.Flip = cut.Flip;
                newCut.Position = cut.Position;
                UpdateCutPlane(newCut);
            }

            if (m_DesktopPresentation) m_DesktopPresentation.LoadConfiguration();

            m_ROIManager.LoadROIsFromConfiguration(Visualization.Configuration.RegionsOfInterest);

            foreach (Column3D column in Columns)
            {
                column.LoadConfiguration(false);
            }

            m_ConfiguredGeometryPending = true;
            SceneInformation.GeometryNeedsUpdate = true;
            LogRuntimePreviewSiteDistanceDiagnostic();

            SceneInformation.SitesNeedUpdate = true;

            Module3DMain.OnRequestUpdateInToolbar.Invoke();
        }

        /// <summary>
        /// Logs a non-blocking warning when the runtime preview is farther from many sites than
        /// the smallest active projection distance. The configured distance is never modified.
        /// </summary>
        private void LogRuntimePreviewSiteDistanceDiagnostic()
        {
            if (m_MeshManager.SelectedMesh is not RuntimeSingleMesh3D preview) return;
            Implantation3D implantation = m_ImplantationManager.SelectedImplantation;
            if (implantation?.SiteInfos == null || implantation.SiteInfos.Count == 0) return;

            float influenceDistance = Columns.Select(GetInfluenceDistance).Where(distance => !float.IsNaN(distance) && !float.IsInfinity(distance) && distance >= 0f).DefaultIfEmpty(15f).Min();

            Mesh vertexBuffer = new();
            RuntimePreviewDistanceReport report;
            try
            {
                preview.Both.UpdateMeshFromDLL(vertexBuffer, all: false, vertices: true, normals: false, uv: false, triangles: false, colors: false);
                report = RuntimePreviewDistanceDiagnostic.Evaluate(vertexBuffer.vertices, implantation.SiteInfos.Select(site => site.UnityPosition).ToArray(), influenceDistance);
            }
            finally
            {
                Destroy(vertexBuffer);
            }

            if (report.ShouldWarn)
            {
                Debug.LogWarning($"MRI preview site-distance diagnostic for '{preview.SourceMRIName}': " + $"P50={report.Percentile50:0.0} mm, P90={report.Percentile90:0.0} mm, P95={report.Percentile95:0.0} mm; " + $"{report.FractionBeyondInfluence:P0} of sites exceed the minimum active influence distance of {report.InfluenceDistance:0.0} mm. " + $"A non-persistent value of at least {report.SuggestedInfluenceDistance:0.0} mm may be more appropriate for this scene.");
            }
        }

        private static float GetInfluenceDistance(Column3D column)
        {
            return column switch
            {
                Column3DAnatomy anatomy => anatomy.AnatomyParameters.InfluenceDistance,
                Column3DDynamic dynamicColumn => dynamicColumn.DynamicParameters.InfluenceDistance,
                Column3DStatic staticColumn => staticColumn.StaticParameters.InfluenceDistance,
                _ => float.NaN
            };
        }

        /// <summary>
        /// Save the current settings of this scene to the configuration of the linked visualization
        /// </summary>
        public void SaveConfiguration()
        {
            Visualization.Configuration.BrainColor = BrainColor;
            Visualization.Configuration.BrainCutColor = CutColor;
            Visualization.Configuration.Colormap = Colormap;
            Visualization.Configuration.MeshPart = MeshManager.MeshPartToDisplay;
            Visualization.Configuration.SurfaceRepresentation = MeshManager.SelectedMesh.Representation;
            if (m_MeshManager.SelectedMesh is not RuntimeSingleMesh3D)
            {
                Visualization.Configuration.MeshName = m_MeshManager.SelectedMesh.Name;
            }

            Visualization.Configuration.MRIName = m_MRIManager.SelectedMRI.Name;
            Visualization.Configuration.ImplantationName = m_ImplantationManager.SelectedImplantation != null ? m_ImplantationManager.SelectedImplantation.Name : "";
            Visualization.Configuration.ShowEdges = EdgeMode;
            Visualization.Configuration.TransparentBrain = IsBrainTransparent;
            Visualization.Configuration.BrainAlpha = BrainMaterials.Alpha;
            Visualization.Configuration.StrongCuts = StrongCuts;
            Visualization.Configuration.HideBlacklistedSites = m_HideBlacklistedSites;
            Visualization.Configuration.ShowAllSites = ShowAllSites;
            Visualization.Configuration.AutomaticCutAroundSelectedSite = AutomaticCutAroundSelectedSite;
            Visualization.Configuration.SiteGain = SiteGain;
            Visualization.Configuration.MRICalMinFactor = m_MRIManager.MRICalMinFactor;
            Visualization.Configuration.MRICalMaxFactor = m_MRIManager.MRICalMaxFactor;
            Visualization.Configuration.CameraType = CameraType;

            List<Core.Data.Cut> cuts = new();
            foreach (Core.Object3D.Cut cut in Cuts)
            {
                cuts.Add(new Core.Data.Cut(cut.ID, cut.Normal, cut.Orientation, cut.Flip, cut.Position));
            }

            Visualization.Configuration.Cuts = cuts;

            if (m_DesktopPresentation) m_DesktopPresentation.SaveConfiguration();

            List<RegionOfInterest> rois = new();
            foreach (ROI roi in ROIManager.ROIs)
            {
                rois.Add(new RegionOfInterest(roi.Name, roi.Spheres.Select(s => new Core.Data.Sphere(s.Position, s.InfluenceRadius, s.ID)).ToList(), roi.ID));
            }

            Visualization.Configuration.RegionsOfInterest = rois;
            CaptureAdditionalConfiguration(Visualization.Configuration);

            foreach (Column3D column in Columns)
            {
                column.SaveConfiguration();
            }
        }

        public VisualizationConfiguration CaptureConfiguration()
        {
            var configuration = (VisualizationConfiguration)Visualization.Configuration.Clone();
            configuration.BrainColor = BrainColor;
            configuration.BrainCutColor = CutColor;
            configuration.Colormap = Colormap;
            configuration.MeshPart = MeshManager.MeshPartToDisplay;
            configuration.SurfaceRepresentation = MeshManager.SelectedMesh.Representation;
            if (m_MeshManager.SelectedMesh is not RuntimeSingleMesh3D)
            {
                configuration.MeshName = m_MeshManager.SelectedMesh.Name;
            }

            configuration.MRIName = m_MRIManager.SelectedMRI.Name;
            configuration.ImplantationName = m_ImplantationManager.SelectedImplantation != null ? m_ImplantationManager.SelectedImplantation.Name : "";
            configuration.ShowEdges = EdgeMode;
            configuration.TransparentBrain = IsBrainTransparent;
            configuration.BrainAlpha = BrainMaterials.Alpha;
            configuration.StrongCuts = StrongCuts;
            configuration.HideBlacklistedSites = m_HideBlacklistedSites;
            configuration.ShowAllSites = ShowAllSites;
            configuration.AutomaticCutAroundSelectedSite = AutomaticCutAroundSelectedSite;
            configuration.SiteGain = SiteGain;
            configuration.MRICalMinFactor = m_MRIManager.MRICalMinFactor;
            configuration.MRICalMaxFactor = m_MRIManager.MRICalMaxFactor;
            configuration.CameraType = CameraType;

            List<Core.Data.Cut> cuts = new();
            foreach (Core.Object3D.Cut cut in Cuts)
            {
                cuts.Add(new Core.Data.Cut(cut.ID, cut.Normal, cut.Orientation, cut.Flip, cut.Position));
            }

            configuration.Cuts = cuts;

            List<RegionOfInterest> rois = new();
            foreach (ROI roi in ROIManager.ROIs)
            {
                rois.Add(new RegionOfInterest(roi.Name, roi.Spheres.Select(s => new Core.Data.Sphere(s.Position, s.InfluenceRadius, s.ID)).ToList(), roi.ID));
            }

            configuration.RegionsOfInterest = rois;
            CaptureAdditionalConfiguration(configuration);

            return configuration;
        }

        private void NormalizeRegionOfInterestIDs()
        {
            List<RegionOfInterest> rois = Visualization.Configuration.RegionsOfInterest ?? new List<RegionOfInterest>();
            for (int i = 0; i < rois.Count; i++)
            {
                RegionOfInterest roi = rois[i];
                if (string.IsNullOrEmpty(roi.ID)) roi.ID = Guid.NewGuid().ToString("D");
                roi.Spheres ??= new List<Core.Data.Sphere>();
                for (int j = 0; j < roi.Spheres.Count; j++)
                {
                    Core.Data.Sphere sphere = roi.Spheres[j];
                    if (string.IsNullOrEmpty(sphere.ID)) sphere.ID = Guid.NewGuid().ToString("D");
                    roi.Spheres[j] = sphere;
                }

                rois[i] = roi;
            }

            Visualization.Configuration.RegionsOfInterest = rois;
        }

        /// <summary>
        /// Reset the settings of the loaded scene
        /// </summary>
        public void ResetConfiguration()
        {
            RunConfigurationMutation(ResetConfigurationCore);
        }

        private void ResetConfigurationCore()
        {
            BrainColor = ColorType.BrainColor;
            CutColor = ColorType.Default;
            Colormap = ColorType.MatLab;
            m_MeshManager.SelectMeshPart(MeshPart.Both);
            if (m_MeshManager.Meshes.Count > 0 && m_MeshManager.SelectedMesh.Representation != SurfaceRepresentation.Anatomical)
            {
                m_MeshManager.SelectRepresentation(SurfaceRepresentation.Anatomical);
            }

            EdgeMode = false;
            IsBrainTransparent = false;
            BrainMaterials.SetAlpha(0.2f);
            StrongCuts = false;
            HideBlacklistedSites = false;
            ShowAllSites = false;
            AutomaticCutAroundSelectedSite = false;
            SiteGain = 1.0f;
            m_MRIManager.SetCalValues(0, 1);
            CameraType = CameraControl.Trackball;

            switch (Type)
            {
                case SceneType.SinglePatient:
                    m_MeshManager.SelectInitialMeshForScene(null, PersistentDataManager.UserPreferences.Visualization._3D.DefaultSelectedMeshInSinglePatientVisualization, PersistentDataManager.UserPreferences.Visualization._3D.DefaultSelectedMRIInSinglePatientVisualization);
                    m_MRIManager.Select(PersistentDataManager.UserPreferences.Visualization._3D.DefaultSelectedMRIInSinglePatientVisualization, true);
                    m_ImplantationManager.Select(PersistentDataManager.UserPreferences.Visualization._3D.DefaultSelectedImplantationInSinglePatientVisualization);
                    break;
                case SceneType.MultiPatients:
                    m_MeshManager.Select(PersistentDataManager.UserPreferences.Visualization._3D.DefaultSelectedMeshInMultiPatientsVisualization);
                    m_MRIManager.Select(PersistentDataManager.UserPreferences.Visualization._3D.DefaultSelectedMRIInMultiPatientsVisualization);
                    m_ImplantationManager.Select(PersistentDataManager.UserPreferences.Visualization._3D.DefaultSelectedImplantationInMultiPatientsVisualization);
                    break;
                default:
                    break;
            }

            while (Cuts.Count > 0)
            {
                RemoveCutPlane(Cuts.Last());
            }

            if (m_DesktopPresentation) m_DesktopPresentation.ResetConfiguration();

            m_ROIManager.Clear();

            foreach (Column3D column in Columns)
            {
                column.ResetConfiguration();
            }

            // A reset can leave the selected mesh unchanged while its presentation
            // buffers still need the atlas/fMRI colors and masks rebuilt.
            SceneInformation.GeometryNeedsUpdate = true;
            Module3DMain.OnRequestUpdateInToolbar.Invoke();
        }

        private void CaptureAdditionalConfiguration(VisualizationConfiguration configuration)
        {
            configuration.AtlasConfiguration = m_FMRIManager.CaptureConfiguration();
            configuration.PreviewMRIName = (m_MeshManager.SelectedMesh as RuntimeSingleMesh3D)?.SourceMRIName;
            // Keep configured masks until their first application. A pending mesh change
            // must not associate masks from the previous topology with the new selection.
            if (m_ConfiguredGeometryPending) return;
            bool currentTopology = m_MeshManager.ReferenceSurface != null && ReferenceEquals(m_MeshManager.ReferenceSurface, m_MeshManager.SelectedMesh.GetSurface(SurfaceRepresentation.Anatomical, m_MeshManager.MeshPartToDisplay));
            configuration.ErasedTriangles = currentTopology ? m_MeshManager.BrainSurface.VisibilityMask.ToArray() : null;
            configuration.ErasedSimplifiedTriangles = currentTopology ? m_MeshManager.SimplifiedMeshToUse.VisibilityMask.ToArray() : null;
        }

        private void ApplyConfiguredGeometry()
        {
            if (!m_ConfiguredGeometryPending) return;
            m_ConfiguredGeometryPending = false;
            try
            {
                var configuration = Visualization.Configuration;
                if (configuration.ErasedTriangles != null && configuration.ErasedSimplifiedTriangles != null)
                {
                    if (configuration.ErasedTriangles.Length != m_MeshManager.BrainSurface.NumberOfTriangles || configuration.ErasedSimplifiedTriangles.Length != m_MeshManager.SimplifiedMeshToUse.NumberOfTriangles)
                        throw new System.IO.InvalidDataException("Configured erasure does not match the selected topology.");
                    TriangleEraser.CurrentMasks = new System.Collections.Generic.List<int[]> { configuration.ErasedTriangles.ToArray(), configuration.ErasedSimplifiedTriangles.ToArray() };
                }

                m_FMRIManager.LoadConfiguration(configuration.AtlasConfiguration);
            }
            catch (Exception exception)
            {
                // A configuration reload from Update must also fail pending preparation.
                m_PreparationError = exception;
                throw;
            }
        }

        private async UniTask LoadConfiguredResourcesAsync(CancellationToken token)
        {
            foreach (string id in RequiredAtlasIds())
            {
                var result = await AtlasResources.LoadAsync(id, token);
                if (!result.Succeeded) throw new System.IO.InvalidDataException(result.Error);
            }

            token.ThrowIfCancellationRequested();
        }
    }
}
