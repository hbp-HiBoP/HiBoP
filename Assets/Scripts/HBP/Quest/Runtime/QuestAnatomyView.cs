using HBP.Core.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.ExceptionServices;
using Cysharp.Threading.Tasks;
using HBP.Data.Module3D;
using HBP.Transfer.Scene;
using UnityEngine;

namespace HBP.Quest
{
    /// <summary>Per-scene presentation and publication; science stays in the common scenes.</summary>
    public sealed class QuestAnatomyView : MonoBehaviour
    {
        [SerializeField] private Base3DScene contentPrefab;
        [SerializeField] private QuestColumnPresentation columnPrefab;
        [SerializeField] private GameObject servicesPrefab;

        public sealed class Presentation
        {
            public RestoredScene Restored { get; internal set; }
            public Base3DScene Scene => Restored.Scene;
            public string TransferId => Restored.Payload.TransferId;
            public IReadOnlyList<QuestColumnPresentation> Columns => columns;
            internal readonly List<QuestColumnPresentation> columns = new();
            internal bool SurfaceHidden;
            public bool IsPlaced { get; internal set; }
            internal UnityEngine.Events.UnityAction<bool> VisibilityListener;
        }

        private readonly List<Presentation> scenes = new();
        private readonly List<UniTask> releases = new();
        private Presentation current;
        private Base3DScene preparingScene;
        private CancellationTokenSource preparation;
        private Task preparationCompletion = Task.CompletedTask;
        public IReadOnlyList<Presentation> Scenes => scenes;
        public Base3DScene Scene => current?.Scene;
        public RestoredScene PublishedScene => current?.Restored;
        public IReadOnlyList<QuestColumnPresentation> Columns => current?.Columns ?? Array.Empty<QuestColumnPresentation>();
        public IReadOnlyList<QuestColumnPresentation> AllColumns => scenes.SelectMany(value => value.columns).ToArray();
        public bool SurfaceHidden => current?.SurfaceHidden == true;
        public bool IsPreparing => preparation != null;
        public string Summary => scenes.Count == 0 ? "No visualization received" : $"{scenes.Count} visualizations | Available offline";
        public GenericEvent<Base3DScene, Column3D> OnContextSelected { get; } = new();

        private void OnEnable()
        {
            Camera.onPreCull += BeforeCamera;
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering += BeforePipelineCamera;
        }

        private void OnDisable()
        {
            Camera.onPreCull -= BeforeCamera;
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering -= BeforePipelineCamera;
        }

        private void BeforeCamera(Camera camera) => UpdateRenderingVisibility();
        private void BeforePipelineCamera(UnityEngine.Rendering.ScriptableRenderContext context, Camera camera) => UpdateRenderingVisibility();

        private void Awake()
        {
            if (!HBP.Core.Preferences.PersistentDataManager.IsInitialized || !HBP.Core.Database.DatabaseManager.IsInitialized)
            {
                if (servicesPrefab == null) throw new InvalidOperationException("Missing serialized Quest common services prefab.");
                Instantiate(servicesPrefab);
            }
        }

        public Presentation Find(string transferId) => scenes.FirstOrDefault(value => value.TransferId == transferId);
        public Presentation Find(Base3DScene scene) => scenes.FirstOrDefault(value => value.Scene == scene);

        public void SelectContext(Base3DScene scene, Column3D column, bool intentional = true)
        {
            var target = Find(scene);
            if (target == null || scene.IsClosing || !scene.IsVisible || column != null && !scene.Columns.Contains(column)) return;
            current = target;
            foreach (var other in scenes)
                if (other != target && other.Scene.IsSelected)
                    other.Scene.IsSelected = false;
            if (!scene.IsSelected) scene.IsSelected = true;
            if (column != null) column.IsSelected = true;
            if (intentional) OnContextSelected.Invoke(scene, column ?? scene.SelectedColumn);
        }

        public async UniTask ApplyAsync(ScenePayload payload, SceneArchive archive, CancellationToken stop)
        {
            if (preparation != null) throw new InvalidOperationException("A visualization is already being prepared.");
            using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(stop, this.GetCancellationTokenOnDestroy());
            preparation = lifetime;
            var completed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            preparationCompletion = completed.Task;
            RestoredScene candidate = null;
            var presentation = new Presentation();
            try
            {
                candidate = await SceneRestoration.PrepareAsync(payload, archive, contentPrefab, transform, lifetime.Token, scene => preparingScene = scene);
                var previous = scenes.FirstOrDefault(value => value.Scene.Visualization.ID == candidate.Scene.Visualization.ID);
                var poses = previous?.columns.Where(value => value != null && value.Column != null).ToDictionary(value => value.Column.ColumnData.ID, value => (Position: value.transform.localPosition, Rotation: value.transform.localRotation, Scale: value.transform.localScale));
                foreach (var column in candidate.Scene.Columns)
                {
                    var item = Instantiate(columnPrefab, transform, false);
                    item.gameObject.SetActive(false);
                    presentation.columns.Add(item);
                    item.Bind(candidate.Scene, column);
                    if (poses != null && poses.TryGetValue(column.ColumnData.ID, out var pose))
                    {
                        item.transform.localPosition = pose.Position;
                        item.transform.localRotation = pose.Rotation;
                        item.transform.localScale = pose.Scale;
                    }
                }

                lifetime.Token.ThrowIfCancellationRequested();
                presentation.Restored = candidate;
                candidate = null;
                presentation.SurfaceHidden = previous?.SurfaceHidden == true;
                presentation.IsPlaced = previous?.IsPlaced == true;
                presentation.VisibilityListener = visible => ApplyVisibility(presentation, visible);
                presentation.Scene.OnChangeVisibleState.AddListener(presentation.VisibilityListener);
                if (previous != null)
                {
                    scenes.Remove(previous);
                    previous.Scene.OnChangeVisibleState.RemoveListener(previous.VisibilityListener);
                    HidePresentation(previous);
                    TrackRelease(previous).Forget();
                }

                scenes.Add(presentation);
                current = presentation;
                ApplyVisibility(presentation, presentation.Scene.IsVisible);
                SelectContext(presentation.Scene, presentation.Scene.SelectedColumn, false);
                Debug.Log($"QUEST_TRANSFER_PUBLISHED transfer={payload.TransferId}");
            }
            finally
            {
                // Preparation can fail on a worker before returning to Unity.
                await UniTask.SwitchToMainThread();
                try
                {
                    if (candidate != null) await candidate.CloseAsync();
                    if (presentation.Restored == null)
                        foreach (var item in presentation.columns)
                            if (item != null)
                                Destroy(item.gameObject);
                }
                finally
                {
                    preparingScene = null;
                    preparation = null;
                    completed.TrySetResult(true);
                }
            }
        }

        private void ApplyVisibility(Presentation presentation, bool visible)
        {
            visible &= presentation.IsPlaced;
            foreach (var column in presentation.columns)
            {
                if (column == null) continue;
                if (!visible) column.Manipulator.CancelGrab();
                column.gameObject.SetActive(visible);
                if (visible) column.Show();
                else column.Hide();
            }
        }

        public void CompletePlacement(Presentation presentation)
        {
            if (presentation == null || !scenes.Contains(presentation)) return;
            presentation.IsPlaced = true;
            ApplyVisibility(presentation, presentation.Scene.IsVisible);
        }

        private static void HidePresentation(Presentation presentation)
        {
            foreach (var column in presentation.columns)
                if (column != null)
                {
                    column.Hide();
                    column.gameObject.SetActive(false);
                }
        }

        private UniTask TrackRelease(Presentation presentation)
        {
            releases.RemoveAll(task => task.Status == UniTaskStatus.Succeeded);
            UniTask release = ReleaseAsync(presentation).ToAsyncLazy().Task;
            releases.Add(release);
            return release;
        }

        private static async UniTask ReleaseAsync(Presentation presentation)
        {
            try
            {
                await presentation.Restored.CloseAsync();
            }
            finally
            {
                await UniTask.SwitchToMainThread();
                foreach (var column in presentation.columns)
                    if (column != null)
                        Destroy(column.gameObject);
            }
        }

        public async UniTask CloseAsync(string transferId)
        {
            var presentation = Find(transferId);
            if (presentation == null) return;
            scenes.Remove(presentation);
            presentation.Scene.OnChangeVisibleState.RemoveListener(presentation.VisibilityListener);
            HBP.Sync.Scene.SceneReconciliationRecord.Get(presentation.Scene)?.Dispose();
            if (current == presentation) current = null;
            HidePresentation(presentation);
            await TrackRelease(presentation);
        }

        public async UniTask ClearAsync()
        {
            Task preparing = preparationCompletion;
            Clear();
            await preparing;
            // All releases are already running. Drain every one before propagating a failure,
            // so replacing globals cannot dispose inputs still borrowed by another scene.
            ExceptionDispatchInfo failure = null;
            foreach (UniTask release in releases.ToArray())
            {
                try
                {
                    await release;
                }
                catch (Exception exception)
                {
                    if (failure == null || failure.SourceException is OperationCanceledException && exception is not OperationCanceledException)
                        failure = ExceptionDispatchInfo.Capture(exception);
                }
            }

            failure?.Throw();
            releases.Clear();
        }

        private void LateUpdate()
        {
            var camera = Camera.main;
            if (camera != null && camera.TryGetComponent<HBP.Rendering.HBPEdgeCameraSettings>(out var edges)) edges.EdgesEnabled = Scene != null && Scene.EdgeMode;
            UpdateRenderingVisibility();
        }

        private void UpdateRenderingVisibility()
        {
            // Preparation advances on the PlayerLoop. Keep its scientific renderers
            // hidden even if it completes after LateUpdate, before either render pipeline draws.
            if (preparingScene != null)
                foreach (var renderer in preparingScene.GetComponentsInChildren<Renderer>(true))
                    renderer.forceRenderingOff = true;
            foreach (var presentation in scenes)
            {
                // Scientific source objects (including newly added ROI spheres) are
                // copied into the column frames; their millimetre-sized originals stay hidden.
                foreach (var renderer in presentation.Scene.GetComponentsInChildren<Renderer>(true)) renderer.forceRenderingOff = true;
                foreach (var column in presentation.columns)
                    if (column != null)
                        foreach (var renderer in column.GetComponentsInChildren<Renderer>(true))
                            renderer.forceRenderingOff = !presentation.IsPlaced || !presentation.Scene.IsVisible || presentation.SurfaceHidden && column.Column != null && renderer.gameObject == column.Column.BrainMesh;
            }
        }

        public void ToggleSurface()
        {
            if (current != null) current.SurfaceHidden = !current.SurfaceHidden;
        }

        public void RecalculateProjection()
        {
            if (Scene != null && Scene.IsVisible)
            {
                Scene.InvalidateActivityField();
                Scene.UpdateGenerator();
            }
        }

        public void Clear()
        {
            preparation?.Cancel();
            foreach (var presentation in scenes.ToArray())
            {
                scenes.Remove(presentation);
                presentation.Scene.OnChangeVisibleState.RemoveListener(presentation.VisibilityListener);
                HBP.Sync.Scene.SceneReconciliationRecord.Get(presentation.Scene)?.Dispose();
                HidePresentation(presentation);
                TrackRelease(presentation).Forget();
            }

            current = null;
        }

        private void OnDestroy() => Clear();
    }
}
