#if DEVELOPMENT_BUILD && UNITY_ANDROID
using System;
using System.IO;
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
            while (!stop.IsCancellationRequested)
            {
                await UniTask.Delay(1000, cancellationToken: stop);
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
    }
}
#endif
