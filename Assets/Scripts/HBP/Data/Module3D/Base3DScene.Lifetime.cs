using System;
using System.Linq;
using Cysharp.Threading.Tasks;
using HBP.Core.Object3D;
using HBP.Core.Preferences;
using UnityEngine;

namespace HBP.Data.Module3D
{
    public partial class Base3DScene
    {
        #region Properties

        // Tracked operations are backed by AsyncLazy/UniTaskCompletionSource so callers
        // and cleanup can await the same work concurrently without consuming it twice.
        private UniTask m_InitializationWork = UniTask.CompletedTask;
        private UniTask m_ColliderWork = UniTask.CompletedTask;
        private UniTask m_AnatomyWork = UniTask.CompletedTask;
        private UniTask m_CorrelationWork = UniTask.CompletedTask;
        private AsyncLazy m_CloseWork;
        private static bool s_ResetStandardResourcesWhenUnused;
        private static UniTask s_StandardPreparation = UniTask.CompletedTask;
        private UserPreferences m_ObservedPreferences;

        public bool IsClosing => m_DestroyRequested;
        public UniTask AnatomyCompletion => m_AnatomyWork;

        #endregion

        #region Internal Methods

        internal static void ResetStandardResourcesWhenUnused()
        {
            s_ResetStandardResourcesWhenUnused = true;
            FinishStandardResourceResetAsync().Forget();
        }

        internal static void TrackStandardPreparation(UniTask work)
        {
            s_ResetStandardResourcesWhenUnused = false;
            s_StandardPreparation = work.ToAsyncLazy().Task;
            s_StandardPreparation.Forget();
        }

        #endregion

        #region Private Methods

        private static async UniTaskVoid FinishStandardResourceResetAsync()
        {
            try
            {
                await s_StandardPreparation;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }

            await UniTask.SwitchToMainThread();
            TryResetStandardResources();
        }

        private static void TryResetStandardResources()
        {
            if (!s_ResetStandardResourcesWhenUnused || s_LiveScenes.Count != 0 || !s_StandardPreparation.Status.IsCompleted()) return;
            s_ResetStandardResourcesWhenUnused = false;
            Object3DManager.Reset();
        }

        private void PreferencesChanged()
        {
            if (m_DestroyRequested) return;
            UpdateCutNumber(m_DisplayedObjects.BrainCutMeshes.Count);
            SceneInformation.CutsNeedUpdate = true;
            SceneInformation.FunctionalSurfaceNeedsUpdate = true;
            foreach (var column in Columns) column.SurfaceNeedsUpdate = true;
        }

        private UniTask BeginClose()
        {
            if (m_CloseWork != null) return m_CloseWork.Task;
            m_DestroyRequested = true;
            foreach (var column in Columns)
                if (column.NavigationTimeline != null)
                    column.NavigationTimeline.IsPlaying = false;
            SceneInformation.GeneratorNeedsUpdate = true;
            m_SurfaceRepresentationLifetime.Cancel();
            m_ObservedPreferences?.OnSavePreferences.RemoveListener(PreferencesChanged);
            Core.DLL.ActivityProjectionSettings.OnChanged -= InvalidateProjectionGrid;
            m_CloseWork = UniTask.Lazy(ReleaseSceneResourcesAsync);
            return m_CloseWork.Task;
        }

        private async UniTask ReleaseSceneResourcesAsync()
        {
            // Cancellation stops publication; it does not stop a native call already running.
            // Observe all workers even when one failed, before releasing any shared input.
            // These operations have already started. Await each completion even if
            // another failed: UniTask.WhenAll can report a failure before all finish.
            foreach (var work in new[] { m_ExternalPreparationWork, m_InitializationWork, m_AnatomyWork, m_ColliderWork, m_GeneratorWork, m_CorrelationWork })
            {
                try
                {
                    await work;
                }
                catch (Exception exception)
                {
                    if (exception is not OperationCanceledException) Debug.LogException(exception);
                }
            }

            await m_SurfaceRepresentationGate.WaitAsync();
            m_SurfaceRepresentationGate.Release();
            await UniTask.SwitchToMainThread();

            foreach (var column in Columns) await column.ReleaseResources();
            foreach (var generator in CutGeometryGenerators) generator.Dispose();
            foreach (var cut in Cuts) cut.Dispose();
            m_ActivityProjectionGrid?.Dispose();
            if (!ReferenceEquals(m_ImplantationManager, null))
            {
                foreach (var implantation in m_ImplantationManager.Implantations)
                    implantation.Clean();
            }

            if (!ReferenceEquals(m_MeshManager, null))
            {
                foreach (var mesh in m_MeshManager.Meshes.Concat(m_MeshManager.PreloadedMeshes.Values.SelectMany(meshes => meshes)).Distinct())
                {
                    if (s_LiveScenes.Any(scene => !ReferenceEquals(scene, this) && (scene.MeshManager.Meshes.Contains(mesh) || scene.MeshManager.PreloadedMeshes.Values.Any(meshes => meshes.Contains(mesh))))) continue;
                    mesh.ClearInflatedRepresentations();
                    if (!mesh.HasBeenLoadedOutside) mesh.Clean();
                }
            }

            if (!ReferenceEquals(m_MRIManager, null))
            {
                foreach (var mri in m_MRIManager.MRIs.Concat(m_MRIManager.PreloadedMRIs.Values.SelectMany(mris => mris)).Distinct())
                {
                    if (mri.HasBeenLoadedOutside) continue;
                    if (s_LiveScenes.Any(scene => !ReferenceEquals(scene, this) && (scene.MRIManager.MRIs.Contains(mri) || scene.MRIManager.PreloadedMRIs.Values.Any(mris => mris.Contains(mri))))) continue;
                    mri.Clean();
                }
            }

            if (Visualization != null && !s_LiveScenes.Any(scene => !ReferenceEquals(scene, this) && ReferenceEquals(scene.Visualization, Visualization)))
                Visualization.Unload();
            s_LiveScenes.Remove(this);
            m_SurfaceRepresentationLifetime.Dispose();
            TryResetStandardResources();
        }

        #endregion
    }
}
