using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Data.Processed;
using HBP.Core.Enums;
using HBP.Core.Object3D;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using UnityEngine;
using Object = UnityEngine.Object;
using FMRI = HBP.Core.Object3D.FMRI;

namespace HBP.Transfer.Scene
{
    /// <summary>One prepared scene and its file lifetime. Closing waits for all native users.</summary>
    public sealed class RestoredScene
    {
        public Base3DScene Scene { get; }
        public ScenePayload Payload { get; }
        public string VerifiedContentHash => archive.VerifiedContentHash;
        public PreparedSceneManifest PreparedManifest => archive.PreparedManifest;
        private readonly SceneArchive archive;
        private AsyncLazy close;

        internal RestoredScene(Base3DScene scene, ScenePayload payload, SceneArchive archive)
        {
            Scene = scene;
            Payload = payload;
            this.archive = archive;
        }

        public UniTask CloseAsync()
        {
            close ??= UniTask.Lazy(CloseCoreAsync);
            return close.Task;
        }

        private async UniTask CloseCoreAsync()
        {
            // A destroyed Unity object still owns managed/native completion work.
            try
            {
                if (!ReferenceEquals(Scene, null)) await Scene.CleanAsync();
            }
            finally
            {
                archive.Dispose();
            }
        }
    }

    public static class SceneRestoration
    {
        public static async UniTask<RestoredScene> PrepareAsync(ScenePayload payload, SceneArchive archive, Base3DScene prefab, Transform parent, CancellationToken token)
        {
            SceneValidation.Validate(payload, archive);
            await StandardData.EnsureInstalledAsync();
            await UniTask.SwitchToThreadPool();
            foreach (var entry in payload.StandardFiles)
            {
                token.ThrowIfCancellationRequested();
                StandardData.ValidateExpectedFile(entry.Key, entry.Value);
            }

            await UniTask.SwitchToMainThread();
            token.ThrowIfCancellationRequested();
            await Base3DScene.PrepareStandardResourcesAsync();
            await UniTask.SwitchToMainThread();
            token.ThrowIfCancellationRequested();
            if (prefab == null)
                throw new InvalidOperationException("A scene content prefab is required.");
            Base3DScene scene;
            scene = Object.Instantiate(prefab, parent, false);
            var result = new RestoredScene(scene, payload, archive);
            IDisposable preparation = scene.RetainForPreparation();
            try
            {
                scene.Initialize(payload.Visualization);
                await scene.InitializePreparedAsync(async resourceToken =>
                {
                    await UniTask.SwitchToMainThread();
                    foreach (var resource in payload.MRIs)
                    {
                        resourceToken.ThrowIfCancellationRequested();
                        MRI3D mri;
                        if (resource.Standard == "MNI")
                            mri = Object3DManager.MNI.MRI;
                        else
                        {
                            mri = await NativePreparation.RunAsync(() =>
                            {
                                var volume = new Core.DLL.Volume();
                                try
                                {
                                    using var verified = archive.AcquireNativeResource(resource.File);
                                    if (!volume.LoadVerifiedNIFTIFile(verified))
                                        throw new InvalidDataException("Cannot restore MRI: " + resource.Name);
                                    return new MRI3D(resource.Name, volume, false);
                                }
                                catch
                                {
                                    volume.Dispose();
                                    throw;
                                }
                            }, item => item.Clean(), resourceToken);
                        }

                        if (resource.PatientId == null)
                            scene.MRIManager.MRIs.Add(mri);
                        else
                        {
                            Patient patient = payload.Visualization.Patients.Single(p => p.ID == resource.PatientId);
                            if (!scene.MRIManager.PreloadedMRIs.TryGetValue(patient, out var mris))
                                scene.MRIManager.PreloadedMRIs.Add(patient, mris = new List<MRI3D>());
                            mris.Add(mri);
                        }
                    }

                    foreach (var resource in payload.Meshes)
                    {
                        resourceToken.ThrowIfCancellationRequested();
                        // Resolve live scene references on Unity before giving private work to the worker.
                        var standard = resource.Standard == null ? null : resource.Standard == "grey" ? Object3DManager.MNI.GreyMatter : Object3DManager.MNI.WhiteMatter;
                        MRI3D sourceMRI = null;
                        if (resource.SourceMRI != null)
                        {
                            var candidates = resource.PatientId == null ? scene.MRIManager.MRIs : scene.MRIManager.PreloadedMRIs.Single(p => p.Key.ID == resource.PatientId).Value;
                            sourceMRI = candidates.Single(mri => mri.Name == resource.SourceMRI);
                        }

                        Mesh3D mesh = await NativePreparation.RunAsync(() => RestoreMesh(resource, archive, standard, sourceMRI), item => item.Clean(), resourceToken);
                        if (resource.PatientId == null)
                            scene.MeshManager.Meshes.Add(mesh);
                        else
                        {
                            Patient patient = payload.Visualization.Patients.Single(p => p.ID == resource.PatientId);
                            if (!scene.MeshManager.PreloadedMeshes.TryGetValue(patient, out var meshes))
                                scene.MeshManager.PreloadedMeshes.Add(patient, meshes = new List<Mesh3D>());
                            meshes.Add(mesh);
                        }
                    }

                    for (int i = 0; i < payload.Columns.Count; i++)
                        await RestoreFunctionalAsync(payload.Visualization.Columns[i], payload.Columns[i], payload, archive, resourceToken);
                }, null, token);
                await scene.CompleteInitializationAsync(null, null, token);
                await scene.PrepareRenderingAsync(token);
                token.ThrowIfCancellationRequested();
                return result;
            }
            catch
            {
                preparation.Dispose();
                await result.CloseAsync();
                throw;
            }
            finally
            {
                preparation.Dispose();
            }
        }

        private static Mesh3D RestoreMesh(MeshResource resource, SceneArchive archive, LeftRightMesh3D standard, MRI3D sourceMRI)
        {
            var owned = new List<Core.DLL.Surface>();

            Core.DLL.Surface Read(string name)
            {
                if (name == null)
                    return null;
                var surface = archive.ReadSurface(name);
                owned.Add(surface);
                return surface;
            }

            Core.DLL.Surface Clone(Core.DLL.Surface source, int[] mask)
            {
                var surface = (Core.DLL.Surface)source.Clone();
                owned.Add(surface);
                surface.UpdateVisibilityMask(mask).Dispose();
                return surface;
            }

            try
            {
                var mesh = Mesh3D.FromPrepared(resource.Name, resource.Type, standard != null ? Clone(standard.Both, resource.StandardBothMask) : Read(resource.Both), Read(resource.SimplifiedBoth), standard != null ? Clone(standard.Left, resource.StandardLeftMask) : Read(resource.Left), standard != null ? Clone(standard.Right, resource.StandardRightMask) : Read(resource.Right), Read(resource.SimplifiedLeft), Read(resource.SimplifiedRight), Read(resource.InflatedBoth), Read(resource.InflatedSimplifiedBoth), Read(resource.InflatedLeft), Read(resource.InflatedRight), Read(resource.InflatedSimplifiedLeft), Read(resource.InflatedSimplifiedRight), sourceMRI, resource.GenerationReport, new Mesh3DInflationSettings(resource.InflationPreset, resource.InflationOptions), resource.InflatedCoordinates);
                mesh.SelectRepresentation(resource.Representation);
                owned.Clear();
                return mesh;
            }
            finally
            {
                foreach (var surface in owned)
                    surface.Dispose();
            }
        }

        private static async UniTask RestoreFunctionalAsync(Column column, ColumnState state, ScenePayload payload, SceneArchive archive, CancellationToken token)
        {
            foreach (var resource in state.Functional)
            {
                token.ThrowIfCancellationRequested();
                Patient patient = resource.PatientId == null ? null : payload.Visualization.Patients.Single(p => p.ID == resource.PatientId);
                var fmri = resource.File == null ? new FMRI() : new FMRI(resource.Name, archive.ResolveNativeFile(resource.File), resource.Mask == null ? "" : archive.ResolveNativeFile(resource.Mask), false);
                if (column is FMRIColumn fmriColumn) fmriColumn.Data.FMRIs.Add(Tuple.Create(fmri, patient));
                else if (column is MEGColumn megColumn)
                {
                    var item = new MEGItem { Label = resource.Name, Patient = patient, ValuesByChannel = resource.Values ?? new(), UnitByChannel = resource.Units ?? new(), Frequency = new Frequency(resource.Frequency) };
                    item.FMRI.Clean();
                    item.FMRI = fmri;
                    megColumn.Data.MEGItems.Add(item);
                }
                else
                {
                    fmri.Clean();
                    throw new InvalidDataException("Functional resource assigned to a nonfunctional column.");
                }

                if (resource.File != null) await fmri.LoadAsync();
            }

            await UniTask.SwitchToMainThread();
        }
    }
}
