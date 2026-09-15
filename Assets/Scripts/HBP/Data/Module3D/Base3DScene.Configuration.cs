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
    public partial class Base3DScene
    {
        private bool m_ConfiguredGeometryPending;

        /// <summary>
        /// Load the visualization configuration from the loaded visualization
        /// </summary>
        /// <param name="firstCall">Has this method not been called by another load method ?</param>
        public void LoadConfiguration(bool firstCall = true)
        {
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
                Core.Object3D.Cut newCut = AddCutPlane();
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
                cuts.Add(new Core.Data.Cut(cut.Normal, cut.Orientation, cut.Flip, cut.Position));
            }

            Visualization.Configuration.Cuts = cuts;

            if (m_DesktopPresentation) m_DesktopPresentation.SaveConfiguration();

            List<RegionOfInterest> rois = new();
            foreach (ROI roi in ROIManager.ROIs)
            {
                rois.Add(new RegionOfInterest(roi.Name, roi.Spheres.Select(s => new Core.Data.Sphere(s.Position, s.InfluenceRadius)).ToList()));
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
                cuts.Add(new Core.Data.Cut(cut.Normal, cut.Orientation, cut.Flip, cut.Position));
            }

            configuration.Cuts = cuts;

            List<RegionOfInterest> rois = new();
            foreach (ROI roi in ROIManager.ROIs)
            {
                rois.Add(new RegionOfInterest(roi.Name, roi.Spheres.Select(s => new Core.Data.Sphere(s.Position, s.InfluenceRadius)).ToList()));
            }

            configuration.RegionsOfInterest = rois;
            CaptureAdditionalConfiguration(configuration);

            return configuration;
        }

        /// <summary>
        /// Reset the settings of the loaded scene
        /// </summary>
        public void ResetConfiguration()
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
            var state = Visualization.Configuration.AtlasConfiguration;
            token.ThrowIfCancellationRequested();
            await UniTask.SwitchToThreadPool();
            if (!Object3DManager.MarsAtlas.Loaded) Object3DManager.MarsAtlas.Load();
            if (state == null)
            {
                await UniTask.SwitchToMainThread();
                return;
            }

            if (state.JuBrain && !Object3DManager.JuBrain.Loaded) Object3DManager.JuBrain.Load();
            if (state.IBC)
            {
                if (Object3DManager.IBC.FMRI == null) Object3DManager.IBC.Load();
                await Object3DManager.IBC.FMRI.LoadAsync();
                await Object3DManager.IBC.Information.LoadCompletion;
            }

            if (!string.IsNullOrEmpty(state.DiFuMoAtlas))
            {
                if (!Object3DManager.DiFuMo.FMRIs.ContainsKey(state.DiFuMoAtlas)) Object3DManager.DiFuMo.Load(state.DiFuMoAtlas);
                await Object3DManager.DiFuMo.FMRIs[state.DiFuMoAtlas].LoadAsync();
                await Object3DManager.DiFuMo.Information[state.DiFuMoAtlas].LoadCompletion;
            }

            if (!string.IsNullOrEmpty(state.LocalizerProtocol))
            {
                if (!Object3DManager.Localizers.Protocols.Any(p => p.Name == state.LocalizerProtocol) && !Object3DManager.Localizers.TryLoad(state.LocalizerProtocol)) throw new System.IO.InvalidDataException("Localizer protocol is unavailable.");
                var fmri = Object3DManager.Localizers.GetCurrentFMRI(state.LocalizerProtocol, state.LocalizerData, state.LocalizerBloc);
                if (fmri == null) throw new System.IO.InvalidDataException("Localizer resource is unavailable.");
                await fmri.LoadAsync();
            }

            await UniTask.SwitchToMainThread();
            token.ThrowIfCancellationRequested();
        }
    }
}
