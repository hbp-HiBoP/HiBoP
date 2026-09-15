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

        public static Task<SceneDelivery> CaptureDeliverySelectedAsync(string transferId, string sessionId, ulong revision, PairingContext globals, CancellationToken token = default, IProgress<string> progress = null)
        {
            if (!PlayerLoopHelper.IsMainThread) throw new InvalidOperationException("Capture must start on Unity's thread.");
            string error = GetSelectionError();
            if (error != null) throw new InvalidOperationException(error);
            return CaptureDeliveryAsync(Module3DMain.SelectedScene, transferId, sessionId, revision, globals, token, progress);
        }

        public static async Task<SceneDelivery> CaptureDeliveryAsync(Base3DScene scene, string transferId, string sessionId, ulong revision, PairingContext globals, CancellationToken token = default, IProgress<string> progress = null)
        {
            if (!PlayerLoopHelper.IsMainThread) throw new InvalidOperationException("Capture must start on Unity's thread.");
            if (scene == null || scene.IsClosing) throw new InvalidOperationException("The visualization is unavailable or closing.");
            string folder = Path.Combine(Application.temporaryCachePath, "SceneCapture", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            string output = Path.Combine(folder, "visualization.hbscene");
            var archive = new SceneArchive(Path.Combine(folder, "resources"), globals: globals, deferResourceWrites: true, cancellationToken: token);
            var clock = System.Diagnostics.Stopwatch.StartNew();
            bool logPhases = Debug.isDebugBuild;

            void Report(string phase, string message)
            {
                progress?.Report(message);
                if (logPhases) Debug.Log($"QUEST_CAPTURE phase={phase}; elapsedMs={clock.Elapsed.TotalMilliseconds:F1}");
            }

            try
            {
                Report("prepare", "Preparing visualization resources...");
                await UniTask.NextFrame(cancellationToken: token);
                var snapshot = await scene.CapturePreparedAsync(() =>
                {
                    Report("snapshot", "Capturing visualization...");
                    var payload = Capture(scene, archive, transferId, sessionId, revision);
                    // No await across the live graph: metadata and numeric bytes are now owned.
                    return (Metadata: archive.CaptureMetadata(payload), Summary: $"{payload.Visualization.Name} | {payload.Columns.Count} columns");
                }, token);
                token.ThrowIfCancellationRequested();
                Report("encode", "Preparing visualization for transfer...");
                var delivery = await Task.Run(() =>
                {
                    SceneDelivery result = null;
                    try
                    {
                        archive.WriteCaptured(snapshot.Metadata, output);
                        token.ThrowIfCancellationRequested();
                        Report("verify", "Verifying prepared visualization...");
                        result = new SceneDelivery(output, transferId, sessionId, snapshot.Summary);
                        token.ThrowIfCancellationRequested();
                        return result;
                    }
                    catch
                    {
                        result?.Dispose();
                        throw;
                    }
                    finally
                    {
                        archive.Dispose();
                    }
                });
                Report("complete", "Visualization prepared.");
                return delivery;
            }
            catch
            {
                // Await worker termination before cleanup; large archive cleanup must not block UI.
                await Task.Run(() =>
                {
                    archive.Dispose();
                    if (Directory.Exists(folder)) Directory.Delete(folder, true);
                });
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
                var state = new ColumnState { Id = target.ID };
                switch (column)
                {
                    case Column3DAnatomy: break;
                    case Column3DIEEG ieeg:
                        ((IEEGColumn)target).Data = ieeg.ColumnIEEGData.Data;
                        break;
                    case Column3DCCEP ccep:
                        ((CCEPColumn)target).Data = ccep.ColumnCCEPData.Data;
                        break;
                    case Column3DStatic staticColumn:
                        ((StaticColumn)target).Data = staticColumn.ColumnStaticData.Data;
                        break;
                    case Column3DFMRI fmri:
                        foreach (var item in fmri.ColumnFMRIData.Data.FMRIs) state.Functional.Add(CaptureFunctional(item.Item1, item.Item2?.ID, archive));
                        break;
                    case Column3DMEG meg:
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
