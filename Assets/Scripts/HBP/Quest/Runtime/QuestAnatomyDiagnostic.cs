using System;
using System.Collections;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBP.Transfer.Anatomy;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Profiling;

namespace HBP.Quest
{
    /// <summary>Opt-in local file injection, never evidence of a network transfer.</summary>
    public sealed class QuestAnatomyDiagnostic : MonoBehaviour
    {
        public const string FixtureFileName = "quest-anatomy.hbna";
        [SerializeField] private QuestAnatomyView view;
        [SerializeField] private QuestDevicePoseTracker head;
        [SerializeField] private GameObject landmarks;
        [SerializeField] private TextMesh statusText;
        private AnatomySnapshot snapshot;
        private InputAction reload;
        private InputAction clear;

        private void OnEnable() => StartCoroutine(Run());

        private IEnumerator Run()
        {
            string path = Path.Combine(Application.persistentDataPath, FixtureFileName);
            if (!File.Exists(path)) yield break;
            if (view == null || head == null || landmarks == null || statusText == null)
            {
                Debug.LogError("QUEST-007 missing serialized diagnostic references.", this);
                yield break;
            }

            statusText.text = "LOCAL HBNA DIAGNOSTIC\nLoading file (no network)";
            Task<AnatomySnapshot> decode = Task.Run(() =>
            {
                using var stream = File.OpenRead(path);
                if (stream.Length > AnatomySnapshotCodec.MaximumEncodedBytes) throw new InvalidDataException("Diagnostic fixture exceeds HBNA limit.");
                var bytes = new byte[checked((int)stream.Length)];
                int offset = 0;
                while (offset < bytes.Length)
                {
                    int read = stream.Read(bytes, offset, bytes.Length - offset);
                    if (read == 0) throw new EndOfStreamException();
                    offset += read;
                }

                return AnatomySnapshotCodec.Decode(bytes);
            });
            // Observe failure even if the diagnostic is destroyed while the pure worker runs.
            _ = decode.ContinueWith(task => { _ = task.Exception; }, TaskContinuationOptions.OnlyOnFaulted);
            while (!decode.IsCompleted) yield return null;
            if (decode.IsFaulted)
            {
                statusText.text = "LOCAL HBNA DIAGNOSTIC FAILED";
                Debug.LogException(decode.Exception, this);
                yield break;
            }

            snapshot = decode.Result; // Already completed; never blocks the PlayerLoop.
            statusText.text = "LOCAL HBNA DIAGNOSTIC\nWaiting for head tracking";
            while (!head.IsTracked) yield return null;
            Vector3 forward = Vector3.ProjectOnPlane(head.transform.forward, Vector3.up).normalized;
            if (forward.sqrMagnitude < 0.5f) forward = Vector3.forward;
            view.transform.position = head.transform.position + forward * 0.65f - Vector3.up * 0.12f;
            view.transform.rotation = Quaternion.LookRotation(forward) * Quaternion.Euler(-90, 0, 0);
            landmarks.SetActive(true);
            Record("baseline");
            for (int cycle = 0; cycle < 3; cycle++)
            {
                if (!Load()) yield break;
                yield return null;
                Record("load-" + cycle);
                if (!Load()) yield break;
                yield return null;
                Record("replace-" + cycle);
                view.Clear();
                yield return null;
                Record("clear-" + cycle);
            }

            if (!Load()) yield break;
            yield return null;
            Record("ready");
            reload = new InputAction("Diagnostic reload", binding: "<XRController>{RightHand}/primaryButton");
            clear = new InputAction("Diagnostic clear", binding: "<XRController>{RightHand}/secondaryButton");
            reload.Enable();
            clear.Enable();
            statusText.text = "LOCAL HBNA DIAGNOSTIC (no network)\nA: load/replace  B: release\nX/Y/Z: prepared frame, 100 mm marks";
        }

        private bool Load()
        {
            try
            {
                view.ApplySnapshot(snapshot);
                return true;
            }
            catch (Exception exception)
            {
                statusText.text = "LOCAL HBNA DIAGNOSTIC FAILED";
                Debug.LogException(exception, this);
                return false;
            }
        }

        private void Update()
        {
            if (reload?.WasPressedThisFrame() == true)
            {
                Load();
                Record("button-load");
            }

            if (clear?.WasPressedThisFrame() == true)
            {
                view.Clear();
                Record("button-clear-deferred");
            }
        }

        private void Record(string stage)
        {
            var meshes = Resources.FindObjectsOfTypeAll<Mesh>().Where(mesh => mesh.name == "Quest Anatomy").ToArray();
            Debug.Log($"QUEST-007 local-only stage={stage}; vertices={view.SharedMesh?.vertexCount ?? 0}; uploads={view.UploadCount}; liveMeshes={meshes.Length}; meshRuntimeBytes={meshes.Sum(mesh => Profiler.GetRuntimeMemorySizeLong(mesh))}; bufferBytesEstimate={view.BufferBytes}; totalAllocatedBytes={Profiler.GetTotalAllocatedMemoryLong()}; managedBytes={GC.GetTotalMemory(false)}; boundsMm={view.SharedMesh?.bounds}; scale={view.transform.lossyScale}; transfer={view.TransferId}", this);
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            reload?.Dispose();
            clear?.Dispose();
            reload = null;
            clear = null;
            snapshot = null;
            if (view != null) view.Clear();
            if (landmarks != null) landmarks.SetActive(false);
        }
    }
}
