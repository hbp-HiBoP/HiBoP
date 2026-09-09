using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Transfer.Anatomy;
using HBP.Transfer.Projection;
using UnityEngine;

namespace HBP.Quest
{
    /// <summary>Single main-thread entry point for decoded deliveries, including local diagnostics.</summary>
    public sealed class QuestAnatomyView : MonoBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        [SerializeField] private Transform millimeterFrame;
        [SerializeField] private MeshFilter meshFilter;
        [SerializeField] private MeshRenderer meshRenderer;
        [SerializeField] private Material opaqueMaterial;
        [SerializeField] private QuestContactRenderer contactRenderer;
        private MaterialPropertyBlock properties;
        private Mesh ownedMesh;
        private int mainThread;
        private Texture2D densityColors;
        private int generation;
        public bool DensityComputing { get; private set; }
        public string DensityError { get; private set; }
        public NativeProjectionInputs.DensityResult Density { get; private set; }
        public Task DensityCompletion { get; private set; } = Task.CompletedTask;
        public double DensityUploadMs { get; private set; }

        public Mesh SharedMesh => ownedMesh;
        public NativeProjectionInputs ProjectionInputs { get; private set; }
        public AnatomyContacts Contacts { get; private set; } = AnatomyContacts.Empty;
        public string TransferId { get; private set; }
        public long BufferBytes { get; private set; }
        public int UploadCount { get; private set; }
        public bool SurfaceHidden { get; private set; }
        public bool SurfaceVisible => meshRenderer != null && meshRenderer.enabled;
        private bool preparedSurfaceVisible;

        public void ToggleSurface()
        {
            RequireMainThread();
            if (ownedMesh == null) return;
            SurfaceHidden = !SurfaceHidden;
            meshRenderer.enabled = preparedSurfaceVisible && !SurfaceHidden;
        }

        private void Awake() => mainThread = System.Threading.Thread.CurrentThread.ManagedThreadId;

        public void ApplySnapshot(AnatomySnapshot snapshot)
        {
            RequireMainThread();
            if (millimeterFrame == null || meshFilter == null || meshRenderer == null || opaqueMaterial == null || contactRenderer == null)
                throw new InvalidOperationException("Anatomy references must be serialized in QuestAnatomy.prefab.");
            if (millimeterFrame.localScale != Vector3.one * 0.001f || millimeterFrame.localPosition != Vector3.zero || millimeterFrame.localRotation != Quaternion.identity || meshFilter.transform != millimeterFrame || meshRenderer.transform != millimeterFrame || contactRenderer.transform != millimeterFrame)
                throw new InvalidOperationException("The anatomical frame must apply exactly one uniform mm-to-m scale.");
            // Validate and prepare before replacing the visible surface. Invalid deliveries preserve it.
            Mesh next = AnatomyMeshUploader.CreateMesh(snapshot);
            MaterialPropertyBlock nextProperties;
            long nextBufferBytes;
            QuestContactRenderer.Frame nextContacts = null;
            NativeProjectionInputs nextProjection = null;
            try
            {
                next.UploadMeshData(snapshot.Projection == null); // Keep projection UVs writable for offline recalculation.
                nextProperties = new MaterialPropertyBlock();
                // HBNA RGB is already linear: SetVector avoids a second color-space conversion.
                nextProperties.SetVector(BaseColorId, new Vector4(snapshot.Color[0], snapshot.Color[1], snapshot.Color[2], 1));
                nextBufferBytes = 24L * snapshot.VertexCount + snapshot.Uvs.Count * 4L + snapshot.Indices.Count * (next.indexFormat == UnityEngine.Rendering.IndexFormat.UInt32 ? 4L : 2L);
                nextContacts = contactRenderer.Prepare(snapshot.Contacts);
                if (snapshot.Projection != null) nextProjection = NativeProjectionInputs.Create(snapshot, PrivateProjectionRoot());
            }
            catch
            {
                AnatomyMeshUploader.Release(next);
                nextContacts?.Dispose();
                nextProjection?.Dispose();
                throw;
            }

            bool previousEnabled = meshRenderer.enabled;
            Material previousMaterial = meshRenderer.sharedMaterial;
            try
            {
                // Synchronous main-thread commit: no frame observes partially prepared resources.
                meshRenderer.SetPropertyBlock(nextProperties);
                meshRenderer.sharedMaterial = opaqueMaterial;
                meshFilter.sharedMesh = next;
                meshRenderer.enabled = snapshot.Visible && !SurfaceHidden;
            }
            catch
            {
                meshFilter.sharedMesh = ownedMesh;
                meshRenderer.sharedMaterial = previousMaterial;
                meshRenderer.SetPropertyBlock(properties);
                meshRenderer.enabled = previousEnabled;
                AnatomyMeshUploader.Release(next);
                nextContacts.Dispose();
                nextProjection?.Dispose();
                throw;
            }

            Mesh previous = ownedMesh;
            NativeProjectionInputs previousProjection = ProjectionInputs;
            ownedMesh = next;
            ProjectionInputs = nextProjection;
            generation++;
            DensityComputing = false;
            Density = null;
            DensityError = null;
            properties = nextProperties;
            TransferId = snapshot.TransferId;
            Contacts = snapshot.Contacts;
            preparedSurfaceVisible = snapshot.Visible;
            contactRenderer.Commit(nextContacts);
            BufferBytes = nextBufferBytes + contactRenderer.BufferBytes;
            UploadCount++;
            AnatomyMeshUploader.Release(previous);
            previousProjection?.Dispose();
            RecalculateDensity();
        }

        public void RecalculateDensity(bool captureGrid = false)
        {
            RequireMainThread();
            if (ProjectionInputs == null || DensityComputing) return;
            DensityCompletion = ComputeAndPublishAsync(ProjectionInputs, ownedMesh, generation, captureGrid).AsTask();
        }

        private async UniTask ComputeAndPublishAsync(NativeProjectionInputs inputs, Mesh mesh, int version, bool captureGrid)
        {
            DensityComputing = true;
            DensityError = null;
            try
            {
                var result = await inputs.ComputeDensityAsync(captureGrid);
                // Replacement/close retires publication, never the worker's input lease.
                if (!this || generation != version || ProjectionInputs != inputs) return;
                var watch = System.Diagnostics.Stopwatch.StartNew();
                mesh.uv3 = result.ActivityUV;
                mesh.uv2 = result.AlphaUV;
                mesh.UploadMeshData(false);
                if (densityColors == null) densityColors = Core.Tools.UnityTextureFactory.Generate1DColorTexture(Core.Enums.ColorType.MatLab);
                properties.SetTexture("_ColorTex", densityColors);
                properties.SetFloat("_DensityEnabled", 1);
                meshRenderer.SetPropertyBlock(properties);
                DensityUploadMs = watch.Elapsed.TotalMilliseconds;
                Density = result;
            }
            catch (Exception exception)
            {
                if (!this || generation != version || ProjectionInputs != inputs) return;
                DensityError = exception.Message;
                Debug.LogWarning("Quest density failed: " + exception);
            }
            finally
            {
                if (this && generation == version && ProjectionInputs == inputs) DensityComputing = false;
            }
        }

        private static string PrivateProjectionRoot()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            using var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            using var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
            using var files = activity.Call<AndroidJavaObject>("getFilesDir");
            return System.IO.Path.Combine(files.Call<string>("getCanonicalPath"), "projection-sessions");
#else
            return System.IO.Path.Combine(Application.persistentDataPath, "projection-sessions");
#endif
        }

        public void Clear()
        {
            RequireMainThread();
            generation++;
            DensityComputing = false;
            Density = null;
            DensityError = null;
            if (meshFilter != null) meshFilter.sharedMesh = null;
            if (meshRenderer != null)
            {
                meshRenderer.enabled = false;
                meshRenderer.SetPropertyBlock(null);
            }

            properties = null;
            if (contactRenderer != null) contactRenderer.Clear();
            SurfaceHidden = false;
            preparedSurfaceVisible = false;
            AnatomyMeshUploader.Release(ownedMesh);
            ownedMesh = null;
            ProjectionInputs?.Dispose();
            ProjectionInputs = null;
            TransferId = null;
            Contacts = AnatomyContacts.Empty;
            BufferBytes = 0;
        }

        private void RequireMainThread()
        {
            if (mainThread != System.Threading.Thread.CurrentThread.ManagedThreadId)
                throw new InvalidOperationException("Dispatch anatomy deliveries to Unity's main thread.");
        }

        private void OnDestroy()
        {
            Clear();
            if (densityColors != null)
            {
                if (Application.isPlaying) Destroy(densityColors);
                else DestroyImmediate(densityColors);
            }
        }
    }
}
