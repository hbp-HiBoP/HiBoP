using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Object3D;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using UnityEngine;
using FMRI = HBP.Core.Object3D.FMRI;

namespace HBP.Transfer.Scene
{
    public static class DesktopSceneCapture
    {
        public static string GetSelectionError() => !Module3DMain.IsInitialized || Module3DMain.SelectedScene == null ? "Open a visualization to send it to Quest." : Module3DMain.SelectedScene.IsClosing ? "The visualization is closing." : null;

        public static Task<SceneDelivery> CaptureDeliverySelectedAsync(string transferId, string sessionId, ulong revision, PairingContext globals, CancellationToken token = default)
        {
            if (!PlayerLoopHelper.IsMainThread) throw new InvalidOperationException("Capture must start on Unity's thread.");
            string error = GetSelectionError();
            if (error != null) throw new InvalidOperationException(error);
            return CaptureDeliveryAsync(Module3DMain.SelectedScene, transferId, sessionId, revision, globals, token);
        }

        public static async Task<SceneDelivery> CaptureDeliveryAsync(Base3DScene scene, string transferId, string sessionId, ulong revision, PairingContext globals, CancellationToken token = default)
        {
            if (!PlayerLoopHelper.IsMainThread) throw new InvalidOperationException("Capture must start on Unity's thread.");
            if (scene == null || scene.IsClosing) throw new InvalidOperationException("The visualization is unavailable or closing.");
            string folder = Path.Combine(Application.temporaryCachePath, "SceneCapture", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            string output = Path.Combine(folder, "visualization.hbscene");
            try
            {
                using var archive = new SceneArchive(Path.Combine(folder, "resources"), globals: globals);
                string summary = await scene.CapturePreparedAsync(() =>
                {
                    var payload = Capture(scene, archive, transferId, sessionId, revision);
                    // No await between state capture and serialization: Unity mutations cannot mix revisions.
                    archive.Write(payload, output);
                    return $"{payload.Visualization.Name} | {payload.Columns.Count} columns";
                }, token);
                token.ThrowIfCancellationRequested();
                return new SceneDelivery(output, transferId, sessionId, summary);
            }
            catch
            {
                if (Directory.Exists(folder)) Directory.Delete(folder, true);
                throw;
            }
        }

        private static ScenePayload Capture(Base3DScene scene, SceneArchive archive, string transferId, string sessionId, ulong revision)
        {
            // Preload caches must already be ready. Check before accessing resource getters,
            // which can otherwise trigger an implicit load during capture.
            foreach (var group in scene.MeshManager.PreloadedMeshes)
            foreach (var mesh in group.Value)
                if (!mesh.IsLoaded)
                    throw new InvalidOperationException($"Preloaded mesh '{mesh.Name}' for patient '{group.Key.Name}' is not loaded. Cannot export the visualization.");
            foreach (var group in scene.MRIManager.PreloadedMRIs)
            foreach (var mri in group.Value)
                if (!mri.IsLoaded)
                    throw new InvalidOperationException($"Preloaded MRI '{mri.Name}' for patient '{group.Key.Name}' is not loaded. Cannot export the visualization.");

            var model = (Visualization)scene.Visualization.Clone();
            model.Configuration = scene.CaptureConfiguration();
            model.Configuration.FirstColumnToSelect = -1;
            var payload = new ScenePayload { TransferId = transferId, SessionId = sessionId, Revision = revision, GlobalContextId = archive.Globals.Id, Visualization = model };
            if (Object3DManager.MNI.ResourceHashes == null) throw new InvalidOperationException("Standard resource provenance is unavailable. Reopen the visualization.");
            payload.StandardFiles = new System.Collections.Generic.Dictionary<string, string>(Object3DManager.MNI.ResourceHashes);
            foreach (var mesh in scene.MeshManager.Meshes) payload.Meshes.Add(CaptureMesh(mesh, null, archive));
            foreach (var group in scene.MeshManager.PreloadedMeshes)
            foreach (var mesh in group.Value)
                payload.Meshes.Add(CaptureMesh(mesh, group.Key.ID, archive));
            foreach (var mri in scene.MRIManager.MRIs) payload.MRIs.Add(CaptureMRI(mri, null, archive));
            foreach (var group in scene.MRIManager.PreloadedMRIs)
            foreach (var mri in group.Value)
                payload.MRIs.Add(CaptureMRI(mri, group.Key.ID, archive));
            for (int i = 0; i < scene.Columns.Count; i++)
            {
                Column3D column = scene.Columns[i];
                Column target = model.Columns[i];
                column.CaptureConfiguration(target);
                var state = new ColumnState { Id = target.ID, SelectedSite = column.SelectedSite?.Information.FullID };
                foreach (var site in column.SiteStateBySiteID) state.Sites[site.Key] = new SiteDisplayState { Masked = site.Value.IsMasked, Filtered = site.Value.IsFiltered };
                foreach (var site in column.Sites)
                {
                    var position = site.transform.localPosition;
                    state.Sites[site.Information.FullID] = new SiteDisplayState { Position = new[] { position.x, position.y, position.z }, Masked = site.State.IsMasked, Filtered = site.State.IsFiltered };
                }

                if (column.NavigationTimeline != null)
                {
                    var timeline = column.NavigationTimeline;
                    state.TimeIndex = timeline.CurrentIndex;
                    state.TimeStep = timeline.Step;
                    state.Playing = timeline.IsPlaying;
                    state.Looping = timeline.IsLooping;
                }

                switch (column)
                {
                    case Column3DAnatomy anatomy: state.AnatomyInfluence = anatomy.AnatomyParameters.InfluenceDistance; break;
                    case Column3DIEEG ieeg:
                        ((IEEGColumn)target).Data = ieeg.ColumnIEEGData.Data;
                        state.Correlations = ieeg.CorrelationBySitePair.ToDictionary(p => p.Key.Information.FullID, p => p.Value.ToDictionary(q => q.Key.Information.FullID, q => q.Value));
                        state.CorrelationMeans = ieeg.CorrelationMeanBySitePair.ToDictionary(p => p.Key.Information.FullID, p => p.Value.ToDictionary(q => q.Key.Information.FullID, q => q.Value));
                        break;
                    case Column3DCCEP ccep:
                        ((CCEPColumn)target).Data = ccep.ColumnCCEPData.Data;
                        state.SourceMode = (int)ccep.Mode;
                        state.SourceSite = ccep.SelectedSourceSite?.Information.FullID;
                        state.SourceLabel = ccep.SelectedSourceMarsAtlasLabel;
                        break;
                    case Column3DStatic staticColumn:
                        ((StaticColumn)target).Data = staticColumn.ColumnStaticData.Data;
                        state.ResourceIndex = staticColumn.SelectedLabelIndex;
                        break;
                    case Column3DFMRI fmri:
                        state.ResourceIndex = fmri.SelectedFMRIIndex;
                        foreach (var item in fmri.ColumnFMRIData.Data.FMRIs) state.Functional.Add(CaptureFunctional(item.Item1, item.Item2?.ID, archive));
                        break;
                    case Column3DMEG meg:
                        state.ResourceIndex = meg.SelectedMEGIndex;
                        foreach (var item in meg.ColumnMEGData.Data.MEGItems)
                        {
                            var functional = CaptureFunctional(item.FMRI, item.Patient?.ID, archive);
                            functional.Name = item.Label;
                            functional.Values = item.ValuesByChannel;
                            functional.Units = item.UnitByChannel;
                            functional.Frequency = item.Frequency.RawValue;
                            state.Functional.Add(functional);
                        }

                        break;
                    default: throw new InvalidOperationException("Unknown visualization column: " + column.GetType().Name);
                }

                payload.Columns.Add(state);
            }

            var atlas = scene.AtlasManager;
            var fmriManager = scene.FMRIManager;
            payload.State = new SceneState
            {
                SelectedColumn = scene.Columns.IndexOf(scene.SelectedColumn), SelectedROI = scene.ROIManager.SelectedROIID, ROICreationMode = scene.ROIManager.ROICreationMode, DisplayCorrelations = scene.DisplayCorrelations, SelectedSphere = scene.ROIManager.SelectedROI?.SelectedSphereID ?? -1,
                MarsAtlas = atlas.DisplayMarsAtlas, JuBrain = atlas.DisplayJuBrainAtlas, AtlasAlpha = atlas.AtlasAlpha,
                IBC = fmriManager.DisplayIBCContrasts, IBCIndex = fmriManager.SelectedIBCContrastID,
                DiFuMo = fmriManager.DisplayDiFuMo, DiFuMoAtlas = fmriManager.SelectedDiFuMoAtlas, DiFuMoArea = fmriManager.SelectedDiFuMoArea,
                Localizers = fmriManager.DisplayLocalizers, LocalizerProtocol = fmriManager.SelectedLocalizersProtocol, LocalizerData = fmriManager.SelectedLocalizersData, LocalizerBloc = fmriManager.SelectedLocalizersBloc, LocalizerTime = fmriManager.SelectedLocalizersTimelineIndex,
                FMRIAlpha = fmriManager.FMRIAlpha, NegativeMin = fmriManager.FMRINegativeCalMinFactor, NegativeMax = fmriManager.FMRINegativeCalMaxFactor, PositiveMin = fmriManager.FMRIPositiveCalMinFactor, PositiveMax = fmriManager.FMRIPositiveCalMaxFactor,
                LocalizerMin = fmriManager.LocalizersMin, LocalizerMiddle = fmriManager.LocalizersMiddle, LocalizerMax = fmriManager.LocalizersMax,
                ErasedTriangles = scene.TriangleEraser.CurrentMasks[0], ErasedSimplifiedTriangles = scene.TriangleEraser.CurrentMasks[1],
            };
            return payload;
        }

        private static MeshResource CaptureMesh(Mesh3D mesh, string patient, SceneArchive archive)
        {
            if (!mesh.IsLoaded || mesh.IsInflationInProgress) throw new InvalidOperationException($"Mesh '{mesh.Name}' is not prepared.");
            string standard = ReferenceEquals(mesh.Both, Object3DManager.MNI.GreyMatter.Both) ? "grey" : ReferenceEquals(mesh.Both, Object3DManager.MNI.WhiteMatter.Both) ? "white" : null;
            var resource = new MeshResource { Name = mesh.Name, PatientId = patient, Type = mesh.Type, Standard = standard, Representation = mesh.Representation };
            if (mesh is RuntimeSingleMesh3D preview)
            {
                resource.SourceMRI = preview.SourceMRIName;
                resource.GenerationReport = preview.GenerationReport;
            }

            if (standard != null)
            {
                var standardMesh = (LeftRightMesh3D)mesh;
                resource.StandardBothMask = mesh.Both.VisibilityMask;
                resource.StandardLeftMask = standardMesh.Left.VisibilityMask;
                resource.StandardRightMask = standardMesh.Right.VisibilityMask;
            }

            resource.SimplifiedBoth = archive.AddSurface(mesh.SimplifiedBoth);
            if (mesh is LeftRightMesh3D simplifiedHemispheres)
            {
                resource.SimplifiedLeft = archive.AddSurface(simplifiedHemispheres.SimplifiedLeft);
                resource.SimplifiedRight = archive.AddSurface(simplifiedHemispheres.SimplifiedRight);
            }

            if (standard == null)
            {
                resource.Both = archive.AddSurface(mesh.Both);
                if (mesh is LeftRightMesh3D hemispheres)
                {
                    resource.Left = archive.AddSurface(hemispheres.Left);
                    resource.Right = archive.AddSurface(hemispheres.Right);
                }
            }

            if (mesh.HasInflatedRepresentation)
            {
                var inflated = mesh.ActiveInflatedRepresentation;
                var key = inflated.CacheKey;
                resource.InflationPreset = key.Preset;
                resource.InflatedCoordinates = inflated.CoordinateSpace;
                resource.InflationOptions = new Core.DLL.SurfaceInflationOptions
                {
                    Method = key.Method, Rescale = key.Rescale, IterationCount = key.IterationCount,
                    SmoothingStrength = key.SmoothingStrength, MetricStrength = key.MetricStrength,
                    MaximumStepFraction = key.MaximumStepFraction, ConvergenceTolerance = key.ConvergenceTolerance,
                    MaximumBacktrackingSteps = key.MaximumBacktrackingSteps, FixBoundaryVertices = key.FixBoundaryVertices
                };
                resource.InflatedBoth = archive.AddSurface(inflated.Both);
                resource.InflatedSimplifiedBoth = archive.AddSurface(inflated.SimplifiedBoth);
                if (inflated.Left != null)
                {
                    resource.InflatedSimplifiedLeft = archive.AddSurface(inflated.SimplifiedLeft);
                    resource.InflatedSimplifiedRight = archive.AddSurface(inflated.SimplifiedRight);
                }

                if (inflated.Left != null)
                {
                    resource.InflatedLeft = archive.AddSurface(inflated.Left);
                    resource.InflatedRight = archive.AddSurface(inflated.Right);
                }
            }

            return resource;
        }

        private static VolumeResource CaptureMRI(MRI3D mri, string patient, SceneArchive archive)
        {
            if (!mri.IsLoaded) throw new InvalidOperationException($"MRI '{mri.Name}' is not prepared.");
            bool standard = ReferenceEquals(mri.Volume, Object3DManager.MNI.MRI.Volume);
            return new VolumeResource { Name = mri.Name, PatientId = patient, Standard = standard ? "MNI" : null, File = standard ? null : archive.AddFile(mri.Volume.SourceFilePath, mri.Volume.SourceFileSha256, mri.Volume.SourceCompanionSha256) };
        }

        private static FunctionalResource CaptureFunctional(FMRI fmri, string patient, SceneArchive archive)
        {
            if (string.IsNullOrEmpty(fmri.SourceFile)) return new FunctionalResource { Name = fmri.Name, PatientId = patient };
            if (!fmri.Loaded) throw new InvalidOperationException($"Functional MRI '{fmri.Name}' is not prepared.");
            return new FunctionalResource { Name = fmri.Name, PatientId = patient, File = archive.AddFile(fmri.SourceFile, fmri.SourceHash, fmri.SourceCompanionHash), Mask = string.IsNullOrEmpty(fmri.MaskFile) ? null : archive.AddFile(fmri.MaskFile, fmri.MaskHash, fmri.MaskCompanionHash) };
        }
    }
}
