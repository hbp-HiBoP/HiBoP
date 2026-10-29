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
    public sealed partial class QuestAnatomyView : MonoBehaviour
    {
        [SerializeField] private Base3DScene contentPrefab;
        [SerializeField] private QuestColumnPresentation columnPrefab;
        [SerializeField] private GameObject servicesPrefab;

        public sealed partial class Presentation
        {
            public RestoredScene Restored { get; internal set; }
            internal Base3DScene LocalScene;
            public Base3DScene Scene => !ReferenceEquals(LocalScene, null) ? LocalScene : Restored.Scene;
            public string TransferId => Restored != null ? Restored.Payload.TransferId : LocalScene.Visualization.ID;
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
        public GenericEvent<Base3DScene> OnPresentationChanging { get; } = new();

        private void OnEnable()
        {
            Camera.onPreCull += BeforeCamera;
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering += BeforePipelineCamera;
            Module3DMain.OnSelectColumn.AddListener(OnColumnSelected);
        }

        private void OnDisable()
        {
            Camera.onPreCull -= BeforeCamera;
            UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering -= BeforePipelineCamera;
            Module3DMain.OnSelectColumn.RemoveListener(OnColumnSelected);
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
            if (column != null && !column.IsSelected) column.IsSelected = true;
            SynchronizeSelection(target);
            if (intentional) OnContextSelected.Invoke(scene, column ?? scene.SelectedColumn);
        }

        /// <summary>Open an embedded visualization through the common scientific pipeline, without a transport session.</summary>
        public async UniTask<Presentation> OpenLocalAsync(HBP.Core.Data.Visualization visualization, CancellationToken token, bool projectActivity = true)
        {
            if (preparation != null) throw new InvalidOperationException("A visualization is already being prepared.");
            using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(token, this.GetCancellationTokenOnDestroy());
            preparation = lifetime;
            var completed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            preparationCompletion = completed.Task;
            var value = new Presentation();
            IDisposable lease = null;
            try
            {
                await StandardData.EnsureInstalledAsync();
                await Base3DScene.PrepareStandardResourcesAsync();
                await UniTask.SwitchToMainThread();
                lifetime.Token.ThrowIfCancellationRequested();
                value.LocalScene = Instantiate(contentPrefab, transform, false);
                preparingScene = value.Scene;
                lease = value.Scene.RetainForPreparation();
                value.Scene.Initialize(visualization);
                if (!projectActivity)
                {
                    value.Scene.SetCoordinatedAutomaticRecomputePolicy(false);
                    value.Scene.SetProjectionEnabled(false);
                }

                await value.Scene.InitializeAsync(visualization, (_, _, _) => { }, lifetime.Token);
                await value.Scene.CompleteInitializationAsync(null, null, lifetime.Token);
                await value.Scene.PrepareRenderingAsync(lifetime.Token);
                lifetime.Token.ThrowIfCancellationRequested();
                foreach (var column in value.Scene.Columns)
                {
                    var item = Instantiate(columnPrefab, transform, false);
                    value.columns.Add(item);
                    item.Bind(value.Scene, column);
                }

                RestorePresentation(value, null);
                value.VisibilityListener = visible => ApplyVisibility(value, visible);
                value.Scene.OnChangeVisibleState.AddListener(value.VisibilityListener);
                scenes.Add(value);
                current = value;
                ApplyVisibility(value, value.Scene.IsVisible);
                SelectContext(value.Scene, value.Scene.SelectedColumn, false);
                return value;
            }
            catch
            {
                await UniTask.SwitchToMainThread();
                lease?.Dispose();
                lease = null;
                await ReleaseAsync(value);
                throw;
            }
            finally
            {
                lease?.Dispose();
                preparingScene = null;
                preparation = null;
                completed.TrySetResult(true);
            }
        }

        public async UniTask ApplyAsync(ScenePayload payload, SceneArchive archive, CancellationToken stop, Action<float, float, HBP.Core.Tools.LoadingText> progress = null)
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
                candidate = await SceneRestoration.PrepareAsync(payload, archive, contentPrefab, transform, lifetime.Token, scene => preparingScene = scene, progress);
                progress?.Invoke(.96f, 0, new HBP.Core.Tools.LoadingText("Setting up scene"));
                if (candidate.Scene.SelectedColumn == null && candidate.Scene.Columns.Count > 0) candidate.Scene.Columns[0].IsSelected = true;
                var previous = scenes.FirstOrDefault(value => value.Scene.Visualization.ID == candidate.Scene.Visualization.ID);
                if (previous != null) FinishTransition(previous);
                foreach (var column in candidate.Scene.Columns)
                {
                    var item = Instantiate(columnPrefab, transform, false);
                    item.gameObject.SetActive(false);
                    presentation.columns.Add(item);
                    item.Bind(candidate.Scene, column);
                }

                lifetime.Token.ThrowIfCancellationRequested();
                presentation.Restored = candidate;
                candidate = null;
                presentation.SurfaceHidden = previous?.SurfaceHidden == true;
                presentation.IsPlaced = previous?.IsPlaced == true;
                RestorePresentation(presentation, previous);
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
            if (!visible) FinishTransition(presentation);
            foreach (var column in presentation.columns)
            {
                if (column == null) continue;
                // Hidden columns must still advance their scientific timelines.
                column.gameObject.SetActive(true);
                if (!visible) column.Manipulator.CancelGrab();
            }

            if (!visible) OnPresentationChanging.Invoke(presentation.Scene);
            UpdateRenderingVisibility();
        }

        public void CompletePlacement(Presentation presentation)
        {
            if (presentation == null || !scenes.Contains(presentation)) return;
            presentation.IsPlaced = true;
            presentation.displayed = SelectedPresentation(presentation);
            if (presentation.displayed != null && presentation.DisplayMode == QuestColumnDisplayMode.Single) presentation.commonPose = ColumnPose.Read(presentation.displayed.transform);
            ApplyVisibility(presentation, presentation.Scene.IsVisible);
        }

        private static void HidePresentation(Presentation presentation)
        {
            presentation.transition = null;
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
                if (presentation.Restored != null) await presentation.Restored.CloseAsync();
                else if (!ReferenceEquals(presentation.LocalScene, null)) await presentation.LocalScene.CleanAsync();
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
            foreach (var presentation in scenes)
            {
                SynchronizeSelection(presentation);
                AdvanceTransition(presentation, Time.unscaledDeltaTime);
            }

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
                    {
                        bool visible = IsColumnPresented(column);
                        foreach (var renderer in column.GetComponentsInChildren<Renderer>(true))
                            renderer.forceRenderingOff = !visible || presentation.SurfaceHidden && column.Column != null && renderer.gameObject == column.Column.BrainMesh;
                        column.SetSelectionFeedbackVisible(visible);
                    }
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
