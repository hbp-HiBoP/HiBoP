#if (DEVELOPMENT_BUILD || UNITY_EDITOR) && (UNITY_STANDALONE || UNITY_EDITOR)
using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Cysharp.Threading.Tasks;
using HBP.Data.Module3D;
using HBP.Transfer.Anatomy.Delivery;
using HBP.Transfer.Transport;
using UnityEngine;

namespace HBP.Transfer.Anatomy.Desktop
{
    /// <summary>Opt-in real Desktop capture/transport proof; credential files are local and consumed once.</summary>
    public static class DensityDeliveryDiagnostic
    {
        [Serializable]
        private sealed class Config
        {
            public string secret, output;
        }

        [Serializable]
        private sealed class Ready
        {
            public string pin, hash;
            public int port;
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Initialize()
        {
            var args = Environment.GetCommandLineArgs();
            int index = Array.IndexOf(args, "-densityDelivery");
            if (index >= 0 && index + 1 < args.Length) RunAsync(args[index + 1]).Forget(Debug.LogException);
        }

        private static async UniTask RunAsync(string path)
        {
            var config = JsonUtility.FromJson<Config>(File.ReadAllText(path));
            File.Delete(path);
            byte[] secret = Convert.FromBase64String(config.secret);
            config.secret = null;
            using var stop = CancellationTokenSource.CreateLinkedTokenSource(Application.exitCancellationToken);
            stop.CancelAfter(TimeSpan.FromMinutes(5));
            TcpListener listener = null;
            try
            {
                Directory.CreateDirectory(config.output);
                while (!Module3DMain.IsInitialized || Module3DMain.SelectedScene == null || !Module3DMain.SelectedScene.SceneInformation.CompletelyLoaded || Module3DMain.SelectedScene.SceneInformation.FunctionalSurfaceNeedsUpdate || Module3DMain.SelectedScene.SceneInformation.CutsNeedUpdate || Module3DMain.SelectedScene.SceneInformation.SitesNeedUpdate)
                    await UniTask.NextFrame(cancellationToken: stop.Token);
                var snapshot = await DesktopAnatomyCapture.CaptureSelectedAsync(Guid.NewGuid().ToString(), "quest019", 1, stop.Token);
                File.WriteAllBytes(Path.Combine(config.output, "received-source.hbna"), AnatomySnapshotCodec.Encode(snapshot));
                var offer = new AnatomyDelivery(snapshot);
                using var identity = await System.Threading.Tasks.Task.Run(TransportIdentity.Create);
                listener = new TcpListener(IPAddress.Any, 0);
                listener.Start();
                var ready = new Ready { pin = Convert.ToBase64String(TransportIdentity.Hash(identity.RawData)), hash = offer.ContentHash, port = ((IPEndPoint)listener.LocalEndpoint).Port };
                File.WriteAllText(Path.Combine(config.output, "ready.json"), JsonUtility.ToJson(ready));
                await offer.ServeAsync(listener, identity, secret, stop.Token, _ => { }, receipt =>
                {
                    File.WriteAllText(Path.Combine(config.output, "receipt.json"), "{\"published\":" + (receipt.Status == DeliveryStatus.Published ? "true" : "false") + "}");
                    stop.Cancel();
                });
            }
            catch (OperationCanceledException) when (stop.IsCancellationRequested)
            {
            }
            catch (Exception exception)
            {
                File.WriteAllText(Path.Combine(config.output, "failure.txt"), exception.ToString());
                Debug.LogException(exception);
            }
            finally
            {
                listener?.Stop();
                Array.Clear(secret, 0, secret.Length);
            }
        }
    }
}
#endif
