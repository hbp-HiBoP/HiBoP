#if DEVELOPMENT_BUILD && UNITY_ANDROID
using System;
using System.IO;
using System.Linq;
using Cysharp.Threading.Tasks;
using HBP.Transfer.Transport;
using UnityEngine;
using UnityEngine.Profiling;

namespace HBP.Quest.Legacy
{
    /// <summary>Receive through the real session, then calculate after an externally observed radio outage.</summary>
    public static class QuestIEEGDiagnostic
    {
        [Serializable] private sealed class Config { public string host, pin, secret; public int port; }
        [Serializable] private sealed class Result
        {
            public bool passed, networkUnavailable, published, exactRecalculation, buffersUploaded;
            public string failure, contentHash;
            public int cycles, vertices;
            public long[] memory;
            public long peakAllocatedBytes;
            public string provenance;
            public bool contactsUploaded;
            public double[] computeMs, uploadMs;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            string config = Path.Combine(Application.persistentDataPath, "quest023-delivery.json");
            if (File.Exists(config)) RunAsync(config).Forget(Debug.LogException);
        }
        private static async UniTask RunAsync(string path)
        {
            var report = new Result { memory = new long[3], computeMs = new double[3], uploadMs = new double[3] };
            var config = JsonUtility.FromJson<Config>(File.ReadAllText(path));
            File.Delete(path);
            byte[] secret = Convert.FromBase64String(config.secret);
            config.secret = null;
            try
            {
                var session = UnityEngine.Object.FindAnyObjectByType<QuestAnatomySession>();
                var view = UnityEngine.Object.FindAnyObjectByType<QuestAnatomyView>();
                report.memory[0] = Profiler.GetTotalAllocatedMemoryLong();
                bool sampling = true;
                async UniTask SampleMemory()
                {
                    while (sampling)
                    {
                        report.peakAllocatedBytes = Math.Max(report.peakAllocatedBytes, Profiler.GetTotalAllocatedMemoryLong());
                        await UniTask.NextFrame();
                    }
                }
                var sampler = SampleMemory();
                DeliveryReceipt receipt;
                try { receipt = await session.ReceiveAsync(config.host, config.port, Convert.FromBase64String(config.pin), secret, Application.exitCancellationToken); }
                finally { sampling = false; await sampler; }
                report.published = receipt.Status == DeliveryStatus.Published;
                report.contentHash = session.ContentHash;
                await view.IEEGCompletion;
                Require(view.IEEGProjection != null && view.IEEGError == null, "Initial iEEG failed");
                var activity = view.IEEGProjection.ActivityUV;
                var alpha = view.IEEGProjection.AlphaUV;
                var contacts = view.Contacts;
                string provenance = view.IEEG.Summary;
                report.provenance = provenance;
                report.contactsUploaded = VerifyContacts(view);
                Require(report.contactsUploaded, "Contact GPU buffers differ");
                report.vertices = activity.Length;
                report.memory[1] = Profiler.GetTotalAllocatedMemoryLong();
                File.WriteAllText(Path.Combine(Application.persistentDataPath, "quest023-received"), report.contentHash);
                // The host starts its on-device Wi-Fi restoration script after this marker.
                await UniTask.Delay(TimeSpan.FromSeconds(20), cancellationToken: Application.exitCancellationToken);
                report.networkUnavailable = Application.internetReachability == NetworkReachability.NotReachable;
                Require(report.networkUnavailable, "Expected the network to be unavailable before recalculation");
                session.Disconnect();
                report.exactRecalculation = true;
                report.buffersUploaded = true;
                for (int cycle = 0; cycle < 3; cycle++)
                {
                    view.RecalculateProjection();
                    await view.IEEGCompletion;
                    Require(view.IEEGProjection != null && view.IEEGError == null, "Offline iEEG failed");
                    report.exactRecalculation &= view.IEEGProjection.ActivityUV.SequenceEqual(activity) && view.IEEGProjection.AlphaUV.SequenceEqual(alpha) && view.Contacts == contacts && view.IEEG.Summary == provenance;
                    report.buffersUploaded &= view.SharedMesh.uv3.SequenceEqual(activity) && view.SharedMesh.uv2.SequenceEqual(alpha);
                    report.computeMs[cycle] = view.IEEGProjection.ComputeMs;
                    report.uploadMs[cycle] = view.IEEGUploadMs;
                    report.cycles++;
                }
                report.memory[2] = Profiler.GetTotalAllocatedMemoryLong();
                report.passed = report.published && report.exactRecalculation && report.buffersUploaded && report.networkUnavailable;
                // Leave the final projection visible for the owner's controller validation (D23).
            }
            catch (Exception exception) { report.failure = exception.ToString(); }
            finally
            {
                Array.Clear(secret, 0, secret.Length);
                File.WriteAllText(Path.Combine(Application.persistentDataPath, "quest023-delivery-result.json"), JsonUtility.ToJson(report, true));
                Debug.Log(report.passed ? "QUEST023_OFFLINE_PASS" : "QUEST023_OFFLINE_FAIL " + report.failure);
            }
        }
        private static bool VerifyContacts(QuestAnatomyView view)
        {
            var data = view.GetComponentInChildren<QuestContactRenderer>().ReadDiagnosticSites();
            for (int i = 0; i < data.Length; i++)
            {
                var s = view.Contacts.Sites[i];
                if (!data[i].PositionRadius.Equals(new Vector4(s.Position[0], s.Position[1], s.Position[2], s.Visible ? s.Diameter * .5f : 0)) || !data[i].Color.Equals(new Vector4(s.Color[0], s.Color[1], s.Color[2], s.Color[3]))) return false;
            }
            return data.Length == view.Contacts.Sites.Count;
        }
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
}
#endif
