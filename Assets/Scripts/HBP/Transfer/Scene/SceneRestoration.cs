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
                string path = StandardData.Resolve(ApplicationState.DataPath, entry.Key);
                if (!File.Exists(path) || StandardData.HashFile(path) != entry.Value) throw new InvalidDataException("Installed scientific reference missing or incompatible: " + entry.Key);
            }

            await UniTask.SwitchToMainThread();
            token.ThrowIfCancellationRequested();
            await Base3DScene.PrepareStandardResourcesAsync();
            await UniTask.SwitchToMainThread();
            token.ThrowIfCancellationRequested();
            if (prefab == null || prefab.DesktopPresentation != null) throw new InvalidOperationException("A common content prefab without Desktop presentation is required.");
            Base3DScene scene = Object.Instantiate(prefab, parent, false);
            var result = new RestoredScene(scene, payload, archive);
            IDisposable preparation = scene.RetainForPreparation();
            try
            {
                payload.Visualization.Configuration.FirstColumnToSelect = -1;
                scene.Initialize(payload.Visualization);
                foreach (var resource in payload.MRIs)
                {
                    token.ThrowIfCancellationRequested();
                    MRI3D mri;
                    if (resource.Standard == "MNI") mri = Object3DManager.MNI.MRI;
                    else
                    {
                        var volume = new Core.DLL.Volume();
                        try
                        {
                            if (!volume.LoadNIFTIFile(archive.ResolveNativeFile(resource.File))) throw new InvalidDataException("Cannot restore MRI: " + resource.Name);
                        }
                        catch
                        {
                            volume.Dispose();
                            throw;
                        }

                        mri = new MRI3D(resource.Name, volume, false);
                    }

                    if (resource.PatientId == null) scene.MRIManager.MRIs.Add(mri);
                    else
                    {
                        Patient patient = payload.Visualization.Patients.Single(p => p.ID == resource.PatientId);
                        if (!scene.MRIManager.PreloadedMRIs.TryGetValue(patient, out var mris)) scene.MRIManager.PreloadedMRIs.Add(patient, mris = new List<MRI3D>());
                        mris.Add(mri);
                    }
                }

                foreach (var resource in payload.Meshes)
                {
                    token.ThrowIfCancellationRequested();
                    Mesh3D mesh = RestoreMesh(resource, archive, scene);
                    if (resource.PatientId == null) scene.MeshManager.Meshes.Add(mesh);
                    else
                    {
                        Patient patient = payload.Visualization.Patients.Single(p => p.ID == resource.PatientId);
                        if (!scene.MeshManager.PreloadedMeshes.TryGetValue(patient, out var meshes)) scene.MeshManager.PreloadedMeshes.Add(patient, meshes = new List<Mesh3D>());
                        meshes.Add(mesh);
                    }
                }

                for (int i = 0; i < payload.Columns.Count; i++) await RestoreFunctionalAsync(payload.Visualization.Columns[i], payload.Columns[i], payload, archive, token);
                await LoadStandardFeaturesAsync(payload.State);
                await scene.InitializePreparedAsync(null, token);
                ApplyState(scene, payload);
                await scene.PrepareRenderingAsync(token);
                token.ThrowIfCancellationRequested();
                // Preparing the generators stops navigation, including its loop flag.
                // Restore the requested mode after the last invalidation; playback starts at publication.
                for (int i = 0; i < scene.Columns.Count; i++)
                    if (scene.Columns[i].NavigationTimeline != null)
                        scene.Columns[i].NavigationTimeline.IsLooping = payload.Columns[i].Looping;
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

        private static Mesh3D RestoreMesh(MeshResource resource, SceneArchive archive, Base3DScene scene)
        {
            var owned = new List<Core.DLL.Surface>();

            Core.DLL.Surface Read(string name)
            {
                if (name == null) return null;
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
                var standard = resource.Standard == null ? null : resource.Standard == "grey" ? Object3DManager.MNI.GreyMatter : Object3DManager.MNI.WhiteMatter;
                MRI3D sourceMRI = null;
                if (resource.SourceMRI != null)
                {
                    var candidates = resource.PatientId == null ? scene.MRIManager.MRIs : scene.MRIManager.PreloadedMRIs.Single(p => p.Key.ID == resource.PatientId).Value;
                    sourceMRI = candidates.Single(mri => mri.Name == resource.SourceMRI);
                }

                var mesh = Mesh3D.FromPrepared(resource.Name, resource.Type, standard != null ? Clone(standard.Both, resource.StandardBothMask) : Read(resource.Both), Read(resource.SimplifiedBoth), standard != null ? Clone(standard.Left, resource.StandardLeftMask) : Read(resource.Left), standard != null ? Clone(standard.Right, resource.StandardRightMask) : Read(resource.Right), Read(resource.SimplifiedLeft), Read(resource.SimplifiedRight), Read(resource.InflatedBoth), Read(resource.InflatedSimplifiedBoth), Read(resource.InflatedLeft), Read(resource.InflatedRight), Read(resource.InflatedSimplifiedLeft), Read(resource.InflatedSimplifiedRight), sourceMRI, resource.GenerationReport, new Mesh3DInflationSettings(resource.InflationPreset, resource.InflationOptions), resource.InflatedCoordinates);
                mesh.SelectRepresentation(resource.Representation);
                owned.Clear();
                return mesh;
            }
            finally
            {
                foreach (var surface in owned) surface.Dispose();
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

            if (column is IEEGColumn ieeg) ieeg.Data.IconicScenario = new IconicScenario(ieeg.Bloc, ieeg.Data.Timeline.Frequency, ieeg.Data.Timeline);
            if (column is CCEPColumn ccep) ccep.Data.IconicScenario = new IconicScenario(ccep.Bloc, ccep.Data.Timeline.Frequency, ccep.Data.Timeline);
            await UniTask.SwitchToMainThread();
        }

        private static async UniTask LoadStandardFeaturesAsync(SceneState state)
        {
            await UniTask.SwitchToThreadPool();
            if (!Object3DManager.MarsAtlas.Loaded) Object3DManager.MarsAtlas.Load();
            if (state.JuBrain && !Object3DManager.JuBrain.Loaded) Object3DManager.JuBrain.Load();
            if (state.IBC)
            {
                if (Object3DManager.IBC.FMRI == null) Object3DManager.IBC.Load();
                await Object3DManager.IBC.FMRI.LoadAsync();
                await Object3DManager.IBC.Information.LoadCompletion;
            }

            if (state.DiFuMo)
            {
                if (!Object3DManager.DiFuMo.FMRIs.ContainsKey(state.DiFuMoAtlas)) Object3DManager.DiFuMo.Load(state.DiFuMoAtlas);
                await Object3DManager.DiFuMo.FMRIs[state.DiFuMoAtlas].LoadAsync();
                await Object3DManager.DiFuMo.Information[state.DiFuMoAtlas].LoadCompletion;
            }

            if (state.Localizers)
            {
                if (!Object3DManager.Localizers.Protocols.Any(p => p.Name == state.LocalizerProtocol) && !Object3DManager.Localizers.TryLoad(state.LocalizerProtocol)) throw new InvalidDataException("Localizer protocol is unavailable.");
                var fmri = Object3DManager.Localizers.GetCurrentFMRI(state.LocalizerProtocol, state.LocalizerData, state.LocalizerBloc);
                if (fmri == null) throw new InvalidDataException("Localizer resource is unavailable.");
                await fmri.LoadAsync();
            }

            await UniTask.SwitchToMainThread();
        }

        private static void ApplyState(Base3DScene scene, ScenePayload payload)
        {
            var state = payload.State;
            scene.ROIManager.SelectedROIID = state.SelectedROI;
            scene.DisplayCorrelations = state.DisplayCorrelations;
            scene.ROIManager.ROICreationMode = state.ROICreationMode;
            scene.ROIManager.SelectedROI?.SelectSphere(state.SelectedSphere);
            for (int i = 0; i < scene.Columns.Count; i++)
            {
                var column = scene.Columns[i];
                var current = payload.Columns[i];
                if (column is Column3DAnatomy anatomy) anatomy.AnatomyParameters.InfluenceDistance = current.AnatomyInfluence;
                if (column is Column3DIEEG ieeg)
                {
                    var sites = column.Sites.ToDictionary(site => site.Information.FullID);
                    ieeg.CorrelationBySitePair = current.Correlations.ToDictionary(p => sites[p.Key], p => p.Value.ToDictionary(q => sites[q.Key], q => q.Value));
                    ieeg.CorrelationMeanBySitePair = current.CorrelationMeans.ToDictionary(p => sites[p.Key], p => p.Value.ToDictionary(q => sites[q.Key], q => q.Value));
                }

                if (column is Column3DFMRI fmri) fmri.SelectedFMRIIndex = current.ResourceIndex;
                if (column is Column3DMEG meg) meg.SelectedMEGIndex = current.ResourceIndex;
                if (column is Column3DStatic stat) stat.SelectedLabelIndex = current.ResourceIndex;
                if (column is Column3DCCEP ccep)
                {
                    ccep.Mode = (Column3DCCEP.CCEPMode)current.SourceMode;
                    if (current.SourceSite != null) ccep.SelectedSourceSite = ccep.Sources.Single(s => s.Information.FullID == current.SourceSite);
                    ccep.SelectedSourceMarsAtlasLabel = current.SourceLabel;
                }

                foreach (var saved in current.Sites)
                {
                    if (!column.SiteStateBySiteID.TryGetValue(saved.Key, out var siteState))
                    {
                        siteState = new SiteState();
                        if (column.ColumnData.BaseConfiguration.ConfigurationBySite.TryGetValue(saved.Key, out var configuration))
                        {
                            siteState.IsBlackListed = configuration.IsBlacklisted;
                            siteState.IsHighlighted = configuration.IsHighlighted;
                            siteState.Color = configuration.Color;
                            siteState.Labels = configuration.Labels.ToList();
                        }

                        column.SiteStateBySiteID.Add(saved.Key, siteState);
                    }

                    siteState.IsMasked = saved.Value.Masked;
                    siteState.IsFiltered = saved.Value.Filtered;
                }

                foreach (var site in column.Sites)
                {
                    if (!current.Sites.TryGetValue(site.Information.FullID, out var saved) || saved.Position == null) throw new InvalidDataException("Missing prepared implantation position.");
                    site.transform.localPosition = new Vector3(saved.Position[0], saved.Position[1], saved.Position[2]);
                    site.State.IsMasked = saved.Masked;
                    site.State.IsFiltered = saved.Filtered;
                    site.IsSelected = site.Information.FullID == current.SelectedSite;
                }

                if (column.NavigationTimeline != null)
                {
                    var timeline = column.NavigationTimeline;
                    if (current.TimeIndex < 0 || current.TimeIndex >= timeline.Length) throw new InvalidDataException("Navigation index outside the restored timeline.");
                    timeline.IsPlaying = false;
                    timeline.IsLooping = current.Looping;
                    timeline.Step = current.TimeStep;
                    timeline.CurrentIndex = current.TimeIndex;
                }

                column.IsSelected = i == state.SelectedColumn;
            }

            scene.ROIManager.UpdateROIMasks();
            if (state.ErasedTriangles.Length != scene.MeshManager.BrainSurface.NumberOfTriangles || state.ErasedSimplifiedTriangles.Length != scene.MeshManager.SimplifiedMeshToUse.NumberOfTriangles) throw new InvalidDataException("Erasure masks do not match the prepared topology.");
            scene.TriangleEraser.CurrentMasks = new List<int[]> { state.ErasedTriangles, state.ErasedSimplifiedTriangles };
            scene.AtlasManager.AtlasAlpha = state.AtlasAlpha;
            scene.AtlasManager.DisplayMarsAtlas = state.MarsAtlas;
            scene.AtlasManager.DisplayJuBrainAtlas = state.JuBrain;
            var manager = scene.FMRIManager;
            if (state.IBC)
            {
                manager.SelectedIBCContrastID = state.IBCIndex;
                manager.DisplayIBCContrasts = true;
            }

            if (state.DiFuMo)
            {
                manager.SelectedDiFuMoAtlas = state.DiFuMoAtlas;
                manager.SelectedDiFuMoArea = state.DiFuMoArea;
                manager.DisplayDiFuMo = true;
            }

            if (state.Localizers)
            {
                manager.SelectedLocalizersProtocol = state.LocalizerProtocol;
                manager.SelectedLocalizersData = state.LocalizerData;
                manager.SelectedLocalizersBloc = state.LocalizerBloc;
                manager.SelectedLocalizersTimelineIndex = state.LocalizerTime;
                manager.DisplayLocalizers = true;
            }

            manager.FMRIAlpha = state.FMRIAlpha;
            manager.FMRINegativeCalMinFactor = state.NegativeMin;
            manager.FMRINegativeCalMaxFactor = state.NegativeMax;
            manager.FMRIPositiveCalMinFactor = state.PositiveMin;
            manager.FMRIPositiveCalMaxFactor = state.PositiveMax;
            manager.LocalizersMin = state.LocalizerMin;
            manager.LocalizersMiddle = state.LocalizerMiddle;
            manager.LocalizersMax = state.LocalizerMax;
            scene.InvalidateActivityField();
        }
    }
}
