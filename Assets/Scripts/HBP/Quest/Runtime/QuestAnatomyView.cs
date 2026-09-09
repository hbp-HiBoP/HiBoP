using System;
using HBP.Transfer.Anatomy;
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
        private MaterialPropertyBlock properties;
        private Mesh ownedMesh;
        private int mainThread;

        public Mesh SharedMesh => ownedMesh;
        public AnatomyContacts Contacts { get; private set; } = AnatomyContacts.Empty;
        public string TransferId { get; private set; }
        public long BufferBytes { get; private set; }
        public int UploadCount { get; private set; }

        private void Awake() => mainThread = System.Threading.Thread.CurrentThread.ManagedThreadId;

        public void ApplySnapshot(AnatomySnapshot snapshot)
        {
            RequireMainThread();
            if (millimeterFrame == null || meshFilter == null || meshRenderer == null || opaqueMaterial == null)
                throw new InvalidOperationException("Anatomy references must be serialized in QuestAnatomy.prefab.");
            if (millimeterFrame.localScale != Vector3.one * 0.001f || millimeterFrame.localPosition != Vector3.zero || millimeterFrame.localRotation != Quaternion.identity || meshFilter.transform != millimeterFrame || meshRenderer.transform != millimeterFrame)
                throw new InvalidOperationException("The anatomical frame must apply exactly one uniform mm-to-m scale.");
            // Validate and prepare before replacing the visible surface. Invalid deliveries preserve it.
            Mesh next = AnatomyMeshUploader.CreateMesh(snapshot);
            MaterialPropertyBlock nextProperties;
            long nextBufferBytes;
            try
            {
                next.UploadMeshData(true); // Release the CPU mesh copy; Mesh owns its GPU buffers.
                nextProperties = new MaterialPropertyBlock();
                // HBNA RGB is already linear: SetVector avoids a second color-space conversion.
                nextProperties.SetVector(BaseColorId, new Vector4(snapshot.Color[0], snapshot.Color[1], snapshot.Color[2], 1));
                nextBufferBytes = 24L * snapshot.VertexCount + snapshot.Uvs.Count * 4L + snapshot.Indices.Count * (next.indexFormat == UnityEngine.Rendering.IndexFormat.UInt32 ? 4L : 2L);
            }
            catch
            {
                AnatomyMeshUploader.Release(next);
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
                meshRenderer.enabled = snapshot.Visible;
            }
            catch
            {
                meshFilter.sharedMesh = ownedMesh;
                meshRenderer.sharedMaterial = previousMaterial;
                meshRenderer.SetPropertyBlock(properties);
                meshRenderer.enabled = previousEnabled;
                AnatomyMeshUploader.Release(next);
                throw;
            }

            Mesh previous = ownedMesh;
            ownedMesh = next;
            properties = nextProperties;
            TransferId = snapshot.TransferId;
            Contacts = snapshot.Contacts;
            BufferBytes = nextBufferBytes;
            UploadCount++;
            AnatomyMeshUploader.Release(previous);
        }

        public void Clear()
        {
            RequireMainThread();
            if (meshFilter != null) meshFilter.sharedMesh = null;
            if (meshRenderer != null)
            {
                meshRenderer.enabled = false;
                meshRenderer.SetPropertyBlock(null);
            }

            properties = null;
            AnatomyMeshUploader.Release(ownedMesh);
            ownedMesh = null;
            TransferId = null;
            Contacts = AnatomyContacts.Empty;
            BufferBytes = 0;
        }

        private void RequireMainThread()
        {
            if (mainThread != System.Threading.Thread.CurrentThread.ManagedThreadId)
                throw new InvalidOperationException("Dispatch anatomy deliveries to Unity's main thread.");
        }

        private void OnDestroy() => Clear();
    }
}
