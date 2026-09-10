using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Data.Module3D;
using HBP.Transfer.Scene;
using UnityEngine;

namespace HBP.Quest
{
    /// <summary>Presentation and publication only; scientific operations belong to the common scene.</summary>
    public sealed class QuestAnatomyView : MonoBehaviour
    {
        [SerializeField] private Base3DScene contentPrefab;
        [SerializeField] private QuestColumnPresentation columnPrefab;
        [SerializeField] private GameObject servicesPrefab;
        private RestoredScene current;
        private readonly List<Task> releases = new();
        private readonly List<QuestColumnPresentation> columns = new();
        private CancellationTokenSource preparation;
        private Task preparationCompletion = Task.CompletedTask;
        public Base3DScene Scene => current?.Scene;
        public IReadOnlyList<QuestColumnPresentation> Columns => columns;
        public bool SurfaceHidden { get; private set; }
        public bool IsPreparing => preparation != null;
        public string Summary => Scene == null ? "No visualization received" : $"{Scene.Name} | {Scene.Columns.Count} columns | Available offline";

        private void Awake()
        {
            if (!HBP.Core.Preferences.PersistentDataManager.IsInitialized || !HBP.Core.Database.DatabaseManager.IsInitialized)
            {
                if (servicesPrefab == null) throw new InvalidOperationException("Missing serialized Quest common services prefab.");
                Instantiate(servicesPrefab);
            }
        }

        public async Task ApplyAsync(ScenePayload payload, SceneArchive archive, CancellationToken stop)
        {
            if (preparation != null) throw new InvalidOperationException("A visualization is already being prepared.");
            using var lifetime = CancellationTokenSource.CreateLinkedTokenSource(stop, this.GetCancellationTokenOnDestroy());
            preparation = lifetime;
            var completed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            preparationCompletion = completed.Task;
            RestoredScene candidate = null;
            var presentations = new List<QuestColumnPresentation>();
            try
            {
                candidate = await SceneRestoration.PrepareAsync(payload, archive, contentPrefab, transform, lifetime.Token);
                foreach (Column3D column in candidate.Scene.Columns)
                {
                    var presentation = Instantiate(columnPrefab, transform, false);
                    presentations.Add(presentation);
                    presentation.Bind(candidate.Scene, column);
                    presentation.transform.localPosition = new Vector3((presentations.Count - 1) * 0.35f, 0, 0);
                }

                lifetime.Token.ThrowIfCancellationRequested();
                RestoredScene previous = current;
                var previousColumns = columns.ToArray();
                current = candidate;
                candidate = null;
                columns.Clear();
                columns.AddRange(presentations);
                presentations.Clear();
                SurfaceHidden = false;
                foreach (var old in previousColumns) old.Hide();
                if (previous != null) TrackRelease(previous, previousColumns);
                foreach (var column in columns) column.Show();
                for (int i = 0; i < current.Scene.Columns.Count; i++)
                    if (current.Scene.Columns[i].NavigationTimeline != null)
                        current.Scene.Columns[i].NavigationTimeline.IsPlaying = payload.Columns[i].Playing;
            }
            finally
            {
                try
                {
                    if (candidate != null) await candidate.CloseAsync();
                    foreach (var item in presentations)
                        if (item != null)
                            Destroy(item.gameObject);
                }
                finally
                {
                    preparation = null;
                    completed.TrySetResult(true);
                }
            }
        }

        private void TrackRelease(RestoredScene scene, QuestColumnPresentation[] presentations)
        {
            releases.RemoveAll(task => task.Status == TaskStatus.RanToCompletion);
            Task release = ReleaseAsync(scene, presentations);
            releases.Add(release);
            release.AsUniTask().Forget();
        }

        public async Task ClearAsync()
        {
            Task preparing = preparationCompletion;
            Clear();
            await preparing;
            await Task.WhenAll(releases.ToArray());
            releases.Clear();
        }

        private static async Task ReleaseAsync(RestoredScene scene, QuestColumnPresentation[] presentations)
        {
            try
            {
                await scene.CloseAsync();
            }
            finally
            {
                foreach (var item in presentations)
                    if (item != null)
                        Destroy(item.gameObject);
            }
        }

        private void LateUpdate()
        {
            var camera = Camera.main;
            if (camera != null && camera.TryGetComponent<HBP.Rendering.HBPEdgeCameraSettings>(out var edges)) edges.EdgesEnabled = Scene != null && Scene.EdgeMode;
            foreach (var renderer in GetComponentsInChildren<Renderer>(true))
            {
                var owner = columns.FirstOrDefault(column => renderer.transform.IsChildOf(column.transform));
                renderer.forceRenderingOff = owner == null || (SurfaceHidden && owner.Column != null && renderer.gameObject == owner.Column.BrainMesh);
            }
        }

        public void ToggleSurface() => SurfaceHidden = !SurfaceHidden;

        public void RecalculateProjection()
        {
            if (Scene != null)
            {
                Scene.InvalidateActivityField();
                Scene.UpdateGenerator();
            }
        }

        public void Clear()
        {
            preparation?.Cancel();
            RestoredScene previous = current;
            current = null;
            var previousColumns = columns.ToArray();
            columns.Clear();
            foreach (var item in previousColumns) item.Hide();
            if (previous != null) TrackRelease(previous, previousColumns);
        }

        private void OnDestroy() => Clear();
    }
}
