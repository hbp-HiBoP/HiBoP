using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using HBP.Transfer.Transport;
using UnityEngine;

namespace HBP.Quest.Legacy
{
    /// <summary>Development-only network harness. No fixture, endpoint or pairing secret is embedded in the APK.</summary>
    public static class QuestDeliveryDiagnostic
    {
        [Serializable]
        private sealed class Config
        {
            public string runId;
            public string host;
            public int port;
            public string pin;
            public string secret;
        }

        [Serializable]
        private sealed class Result
        {
            public string runId;
            public string unity;
            public bool passed;
            public string error;
            public string contentHash;
            public int vertices;
            public long bufferBytes;
            public int uploads;
            public bool offlineRetained;
            public bool replacementNotDuplicated;
            public bool closedReleased;
            public string receipts;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
#if DEVELOPMENT_BUILD && UNITY_ANDROID
            string path = Path.Combine(Application.persistentDataPath, "quest010-config.json");
            if (!File.Exists(path)) return;
            Config config = JsonUtility.FromJson<Config>(File.ReadAllText(path));
            File.Delete(path);
            _ = RunAsync(config); // RunAsync observes all failures and writes a result.
#endif
        }

        private static async Task RunAsync(Config config)
        {
            var result = new Result { runId = config.runId, unity = Application.unityVersion };
            byte[] secret = null;
            try
            {
                Debug.Log("QUEST010_RUN " + config.runId);
                var session = UnityEngine.Object.FindFirstObjectByType<QuestAnatomySession>();
                if (session == null) throw new InvalidOperationException("QuestAnatomySession is missing from the bootstrap prefab.");
                var view = session.GetComponent<QuestAnatomyView>();
                byte[] pin = Convert.FromBase64String(config.pin);
                secret = Convert.FromBase64String(config.secret);
                config.secret = null;
                var token = Application.exitCancellationToken;
                DeliveryReceipt first = await session.ReceiveAsync(config.host, config.port, pin, secret, token);
                Mesh mesh = view.SharedMesh;
                Vector3 position = view.transform.localPosition;
                Vector3 scale = view.transform.localScale;
                result.contentHash = session.ContentHash;
                result.vertices = mesh.vertexCount;
                result.bufferBytes = view.BufferBytes;
                session.Disconnect();
                // Exercise background lifecycle without purging the publication.
                session.enabled = false;
                await Task.Delay(500, token);
                result.offlineRetained = session.IsReady && view.SharedMesh == mesh && view.transform.localPosition == position && view.transform.localScale == scale;
                session.enabled = true;
                DeliveryReceipt retry = await session.ReceiveAsync(config.host, config.port, pin, secret, token);
                result.uploads = view.UploadCount;
                result.replacementNotDuplicated = view.SharedMesh == mesh && result.uploads == 1;
                session.CloseSession();
                await Task.Delay(100, token); // Unity must finish deferred mesh destruction.
                result.closedReleased = mesh == null && !session.IsReady && view.BufferBytes == 0 && !Resources.FindObjectsOfTypeAll<Mesh>().Any(value => value.name == "Quest Anatomy");
                DeliveryReceipt closed = await session.ReceiveAsync(config.host, config.port, pin, secret, token);
                result.receipts = $"{first.Status},{retry.Status},{closed.Status}";
                result.passed = result.offlineRetained && result.replacementNotDuplicated && result.closedReleased && result.vertices == 69104 && result.bufferBytes == 3869920 && result.contentHash == "065309ca8cc4babe9f8c2cd9f08bb7f6ae97b1e11374e1299e0f4a416e5ada12" && result.receipts == "Published,AlreadyPublished,Closed" && !session.IsReady;
                if (!result.passed) result.error = "A delivery/lifetime invariant failed.";
            }
            catch (Exception exception)
            {
                result.error = exception.GetType().Name + ": " + exception.Message;
            }
            finally
            {
                if (secret != null) Array.Clear(secret, 0, secret.Length);
                string json = JsonUtility.ToJson(result, true);
                File.WriteAllText(Path.Combine(Application.persistentDataPath, "quest010-result.json"), json);
                Debug.Log("QUEST010_RESULT " + JsonUtility.ToJson(result));
                Debug.Log((result.passed ? "QUEST010_PASS " : "QUEST010_FAIL ") + config.runId);
            }
        }
    }
}
