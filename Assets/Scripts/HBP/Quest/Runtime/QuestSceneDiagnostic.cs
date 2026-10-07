#if DEVELOPMENT_BUILD && UNITY_ANDROID
using System;
using System.IO;
using System.Collections;
using System.Reflection;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using HBP.Transfer.Scene;
using UnityEngine;

namespace HBP.Quest
{
    /// <summary>One-shot requests placed by the agent through ADB; only operates on published content.</summary>
    public static class QuestSceneDiagnostic
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize() => WatchAsync(Application.exitCancellationToken).Forget(Debug.LogException);

        private static async UniTask WatchAsync(CancellationToken stop)
        {
            string request = Path.Combine(Application.persistentDataPath, "scene008-qualify");
            string retentionRequest = Path.Combine(Application.persistentDataPath, "retention-status");
            string pointerRequest = Path.Combine(Application.persistentDataPath, "pointer-status");
            while (!stop.IsCancellationRequested)
            {
                await UniTask.Delay(1000, cancellationToken: stop);
                if (File.Exists(pointerRequest))
                {
                    var pointer = UnityEngine.Object.FindAnyObjectByType<QuestPointerInput>();
                    if (pointer != null)
                    {
                        File.Delete(pointerRequest);
                        pointer.RequestPointerDiagnostic(Path.Combine(Application.persistentDataPath, "pointer-status.json"));
                    }
                }
                if (File.Exists(retentionRequest))
                {
                    File.Delete(retentionRequest);
                    File.WriteAllText(Path.Combine(Application.persistentDataPath, "retention-status.txt"), CaptureRetention());
                }
                if (!File.Exists(request)) continue;
                var view = UnityEngine.Object.FindAnyObjectByType<QuestAnatomyView>();
                if (view == null || view.Scene == null || view.IsPreparing) continue;
                File.Delete(request);
                string directory = Path.Combine(Application.persistentDataPath, "scene-008", DateTime.UtcNow.ToString("yyyyMMdd-HHmmss"));
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stop);
                timeout.CancelAfter(TimeSpan.FromMinutes(3));
                try { await SceneQualification.RunAsync(view.Scene, directory, timeout.Token); }
                catch (Exception exception) { Debug.LogException(exception); }
                Debug.Log("SCENE008_EVIDENCE " + directory);
            }
        }

        private static string CaptureRetention()
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var session = UnityEngine.Object.FindAnyObjectByType<QuestAnatomySession>();
            object replica = session == null ? null : typeof(QuestAnatomySession).GetField("v2Replica", flags).GetValue(session);
            if (replica == null) return "No published replica.";
            var result = new StringBuilder();
            object Read(object target, string name) => target.GetType().GetField(name, flags).GetValue(target);
            void Describe(string name, object value)
            {
                result.Append(name).Append('=');
                if (value is IDictionary dictionary)
                {
                    foreach (DictionaryEntry entry in dictionary) result.Append(entry.Key).Append(':').Append(entry.Value).Append(',');
                }
                else if (value is IEnumerable sequence && !(value is string))
                {
                    foreach (object entry in sequence) result.Append(entry).Append(',');
                }
                else result.Append(value);
                result.AppendLine();
            }
            object completion = Read(replica, "m_ApplicationCompletion");
            Describe("incomingFailure", Read(replica, "m_LastIncomingFailure"));
            foreach (string slot in new[] { "m_ProcessingRecord", "m_ReadAheadRecord", "m_CompletedSceneOperation" })
            {
                object record = Read(replica, slot);
                if (record == null) { Describe(slot, null); continue; }
                foreach (string property in new[] { "Kind", "Lane", "BodySchema", "MessageId", "OriginSequence", "CanonicalSequence", "PayloadLength" })
                    Describe(slot + "." + property, record.GetType().GetProperty(property).GetValue(record));
            }
            Describe("completedThrough", completion.GetType().GetProperty("CompletedThrough").GetValue(completion));
            Describe("completedGaps", Read(completion, "m_Completed"));
            foreach (string name in new[] { "m_SceneBulkOriginSequence", "m_CheckpointOriginSequence", "m_DeferredBytes", "m_DeferredDrainPending", "m_DesktopRetiredCanonicalThrough" }) Describe(name, Read(replica, name));
            object bulk = Read(replica, "m_SceneOperationBulkReceiver");
            Describe("bulkActive", bulk.GetType().GetProperty("IsActive").GetValue(bulk));
            Describe("bulkOperation", bulk.GetType().GetProperty("ActiveOperationId").GetValue(bulk));
            object driver = Read(replica, "m_Driver");
            foreach (string name in new[] { "m_ReceivedOrigins", "m_ReceivedSequences", "m_PendingObservations" }) Describe(name, Read(driver, name));
            foreach (string name in new[] { "ReceivedOperationCount", "PendingProposalCount", "LastObservedCanonicalSequence", "MinimumPendingObservedSequence", "ConnectionState" }) Describe(name, driver.GetType().GetProperty(name).GetValue(driver));
            return result.ToString();
        }
    }
}
#endif
