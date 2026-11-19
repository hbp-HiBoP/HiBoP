#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using HBP.Core.Database;
using HBP.Core.Preferences;
using HBP.Core.Tools;
using HBP.Data.Module3D;
using HBP.Transfer.Scene;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace HBP.Dev
{
    /// <summary>Opt-in capture measurements against a normally opened Desktop visualization.</summary>
    public static class SceneCaptureDiagnostic
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            string[] args = Environment.GetCommandLineArgs();
            int option = Array.IndexOf(args, "-sceneCaptureEvidence");
            if (option >= 0 && option + 1 < args.Length)
                RunAsync(Path.GetFullPath(args[option + 1]), args.Contains("-sceneCaptureOnce")).Forget(Debug.LogException);
        }

        public static async UniTask RunAsync(string directory, bool quit = false)
        {
            int code = 0;
            Directory.CreateDirectory(directory);
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(Application.exitCancellationToken);
            timeout.CancelAfter(TimeSpan.FromMinutes(10));
            try
            {
                await UniTask.WaitUntil(() => Module3DMain.IsInitialized && Module3DMain.SelectedScene != null && Module3DMain.SelectedScene.SceneInformation.CompletelyLoaded && DatabaseManager.IsInitialized && DatabaseManager.Database.IsLoaded, cancellationToken: timeout.Token);
                var scene = Module3DMain.SelectedScene;
                using var pairing = PairingSnapshot.Capture();
                var results = new JArray();
                for (int pass = 1; pass <= 2; pass++)
                {
                    var clock = System.Diagnostics.Stopwatch.StartNew();
                    double previousFrame = 0, maximumFrameGap = 0;
                    int frames = 0;
                    bool sampling = true;

                    async UniTask SampleFrames()
                    {
                        while (sampling)
                        {
                            await UniTask.NextFrame();
                            double now = clock.Elapsed.TotalMilliseconds;
                            maximumFrameGap = Math.Max(maximumFrameGap, now - previousFrame);
                            previousFrame = now;
                            ++frames;
                        }
                    }

                    var sampler = SampleFrames();
                    try
                    {
                        using var delivery = await DesktopSceneCapture.CaptureDeliveryAsync(scene, Guid.NewGuid().ToString("N"), Guid.NewGuid().ToString("N"), 1, pairing.Context, timeout.Token);
                        await UniTask.NextFrame();
                        results.Add(new JObject { ["pass"] = pass, ["captureMs"] = clock.Elapsed.TotalMilliseconds, ["maximumFrameGapMs"] = maximumFrameGap, ["frames"] = frames, ["bytes"] = delivery.EncodedBytes, ["sha256"] = delivery.ContentHash });
                    }
                    finally
                    {
                        sampling = false;
                        await sampler;
                    }

                    await UniTask.NextFrame();
                }

                File.WriteAllText(Path.Combine(directory, "result.json"), new JObject
                {
                    ["success"] = true, ["visualization"] = scene.Name,
                    ["automaticActivity"] = PersistentDataManager.UserPreferences.Visualization._3D.AutomaticEEGUpdate,
                    ["captures"] = results
                }.ToString());
            }
            catch (Exception exception)
            {
                code = 1;
                File.WriteAllText(Path.Combine(directory, "failure.txt"), exception.ToString());
                Debug.LogException(exception);
            }
            finally
            {
                if (quit) Application.Quit(code);
            }
        }
    }
}
#endif
