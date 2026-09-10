using System;
using HBP.Transfer.Anatomy;
using HBP.Transfer.Transport;
using UnityEngine;
using UnityEngine.Profiling;

namespace HBP.Quest.Legacy
{
    /// <summary>Opt-in observation only; never injects content, changes presentation or forces collection.</summary>
    internal sealed class QuestAnatomyMeasurements
    {
        private double started = Time.realtimeSinceStartupAsDouble;
        private int frames;
        private double sumMs, maxMs;

        public void Sample(QuestAnatomySession session, QuestAnatomyView view)
        {
            double interval = Time.unscaledDeltaTime * 1000.0;
            frames++;
            sumMs += interval;
            maxMs = Math.Max(maxMs, interval);
            double now = Time.realtimeSinceStartupAsDouble;
            if (now - started < 5) return;
            Debug.Log("QUEST012_SAMPLE " + JsonUtility.ToJson(new SampleRecord
            {
                utc = DateTime.UtcNow.ToString("O"), uptimeSeconds = now, windowSeconds = now - started,
                frames = frames, meanFrameIntervalMs = sumMs / frames, maxFrameIntervalMs = maxMs,
                ready = session.IsReady, hash = session.ContentHash, uploads = view.UploadCount,
                vertices = view.SharedMesh != null ? view.SharedMesh.vertexCount : 0,
                bufferBytesEstimate = view.BufferBytes, unityAllocatedBytes = Profiler.GetTotalAllocatedMemoryLong(),
                unityReservedBytes = Profiler.GetTotalReservedMemoryLong(), managedBytes = GC.GetTotalMemory(false),
                gen0Collections = GC.CollectionCount(0), position = view.transform.position,
                contacts = view.Contacts.Sites.Count, surfaceVisible = view.SurfaceVisible, surfaceHidden = view.SurfaceHidden,
                contactBufferBytes = view.GetComponentInChildren<QuestContactRenderer>().BufferBytes,
                rotation = view.transform.rotation, scale = view.transform.localScale
            }));
            started = now;
            frames = 0;
            sumMs = maxMs = 0;
        }

        public void Published(QuestAnatomySession session, QuestAnatomyView view, AnatomySnapshot snapshot, byte[] bytes, DeliveryStatus status, double decodeMs, double publicationMs, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            // HBNA ends in [surface SHA-256][surface buffers][content SHA-256].
            // Decode already checked both hashes; reading the stored digest makes no extra buffer copy.
            string surfaceHash = BitConverter.ToString(bytes, AnatomySnapshotCodec.GetSurfaceHashOffset(snapshot), 32).Replace("-", "").ToLowerInvariant();
            Debug.Log("QUEST012_PUBLISH " + JsonUtility.ToJson(new PublicationRecord
            {
                utc = DateTime.UtcNow.ToString("O"), transfer = snapshot.TransferId, hash = session.ContentHash,
                surfaceHash = surfaceHash, status = status.ToString(), bytes = bytes.Length,
                vertices = snapshot.VertexCount, indices = snapshot.Indices.Count, uploads = view.UploadCount,
                decodeAndHashMs = decodeMs, meshPrepareAndUploadSubmissionMs = publicationMs,
                positionBefore = position, rotationBefore = rotation, scaleBefore = scale,
                positionAfter = view.transform.position, rotationAfter = view.transform.rotation, scaleAfter = view.transform.localScale
            }));
        }

        [Serializable]
        private sealed class SampleRecord
        {
            public string utc, hash;
            public double uptimeSeconds, windowSeconds, meanFrameIntervalMs, maxFrameIntervalMs;
            public int frames, uploads, vertices, gen0Collections, contacts;
            public bool ready, surfaceVisible, surfaceHidden;
            public long bufferBytesEstimate, unityAllocatedBytes, unityReservedBytes, managedBytes, contactBufferBytes;
            public Vector3 position, scale;
            public Quaternion rotation;
        }

        [Serializable]
        private sealed class PublicationRecord
        {
            public string utc, transfer, hash, surfaceHash, status;
            public int bytes, vertices, indices, uploads;
            public double decodeAndHashMs, meshPrepareAndUploadSubmissionMs;
            public Vector3 positionBefore, scaleBefore, positionAfter, scaleAfter;
            public Quaternion rotationBefore, rotationAfter;
        }
    }
}
