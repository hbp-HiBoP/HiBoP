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
    public static class QuestDensityDiagnostic
    {
        [Serializable] private sealed class Config { public string host, pin, secret; public int port; }
        [Serializable] private sealed class Result
        {
            public bool passed, networkUnavailable, published, exactRecalculation, buffersUploaded;
            public string failure, contentHash;
            public int cycles, vertices;
            public long[] memory;
            public double[] computeMs, uploadMs;
        }
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            string config = Path.Combine(Application.persistentDataPath, "quest019-delivery.json");
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
                var receipt = await session.ReceiveAsync(config.host, config.port, Convert.FromBase64String(config.pin), secret, Application.exitCancellationToken);
                report.published = receipt.Status == DeliveryStatus.Published;
                report.contentHash = session.ContentHash;
                await view.DensityCompletion;
                Require(view.Density != null && view.DensityError == null, "Initial density failed");
                var activity = view.Density.ActivityUV;
                var alpha = view.Density.AlphaUV;
                float maximum = view.Density.MaxDensity;
                report.vertices = activity.Length;
                report.memory[1] = Profiler.GetTotalAllocatedMemoryLong();
                File.WriteAllText(Path.Combine(Application.persistentDataPath, "quest019-received"), report.contentHash);
                // The host starts its on-device Wi-Fi restoration script after this marker.
                await UniTask.Delay(TimeSpan.FromSeconds(20), cancellationToken: Application.exitCancellationToken);
                report.networkUnavailable = Application.internetReachability == NetworkReachability.NotReachable;
                Require(report.networkUnavailable, "Expected the network to be unavailable before recalculation");
                session.Disconnect();
                report.exactRecalculation = true;
                report.buffersUploaded = true;
                for (int cycle = 0; cycle < 3; cycle++)
                {
                    view.RecalculateDensity();
                    await view.DensityCompletion;
                    Require(view.Density != null && view.DensityError == null, "Offline density failed");
                    report.exactRecalculation &= view.Density.ActivityUV.SequenceEqual(activity) && view.Density.AlphaUV.SequenceEqual(alpha) && view.Density.MaxDensity == maximum;
                    report.buffersUploaded &= view.SharedMesh.uv3.SequenceEqual(activity) && view.SharedMesh.uv2.SequenceEqual(alpha);
                    report.computeMs[cycle] = view.Density.ComputeMs;
                    report.uploadMs[cycle] = view.DensityUploadMs;
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
                File.WriteAllText(Path.Combine(Application.persistentDataPath, "quest019-delivery-result.json"), JsonUtility.ToJson(report, true));
                Debug.Log(report.passed ? "QUEST019_OFFLINE_PASS" : "QUEST019_OFFLINE_FAIL " + report.failure);
            }
        }
        private static void Require(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
}
#endif
