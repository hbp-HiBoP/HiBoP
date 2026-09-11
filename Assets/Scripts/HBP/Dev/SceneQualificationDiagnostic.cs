#if UNITY_EDITOR || DEVELOPMENT_BUILD
using System;
using System.IO;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using HBP.Data.Module3D;
using HBP.Transfer.Scene;
using UnityEngine;

namespace HBP.Dev
{
    public static class SceneQualificationDiagnostic
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            string[] args = Environment.GetCommandLineArgs();
            int option = Array.IndexOf(args, "-sceneEvidence");
            if (option >= 0 && option + 1 < args.Length) RunAsync(Path.GetFullPath(args[option + 1]), args.Contains("-sceneEvidenceOnce")).Forget(Debug.LogException);
        }

        private static async UniTask RunAsync(string directory, bool quit)
        {
            int code = 0;
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(Application.exitCancellationToken);
            timeout.CancelAfter(TimeSpan.FromMinutes(5));
            try
            {
                await UniTask.WaitUntil(() => Module3DMain.IsInitialized && Module3DMain.SelectedScene != null && Module3DMain.SelectedScene.SceneInformation.CompletelyLoaded, cancellationToken: timeout.Token);
                if (Environment.GetCommandLineArgs().Contains("-scenePairingEvidence"))
                {
                    using var pairing = PairingSnapshot.Capture();
                    Directory.CreateDirectory(directory);
                    File.WriteAllText(Path.Combine(directory, "pairing-capture.txt"), "Global pairing snapshot captured successfully. No network connection attempted.");
                }

                await SceneQualification.RunAsync(Module3DMain.SelectedScene, directory, timeout.Token);
            }
            catch (Exception exception)
            {
                code = 1;
                Directory.CreateDirectory(directory);
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
