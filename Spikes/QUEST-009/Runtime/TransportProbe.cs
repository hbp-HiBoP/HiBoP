using System;
using System.Collections;
using System.IO;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Unity.Profiling;

namespace HBP.Transfer.Transport.Probe
{
    public sealed class TransportProbe : MonoBehaviour
    {
        private CancellationTokenSource lifetime;
        private Task work;
        private ProfilerRecorder allocations;

        private IEnumerator Start()
        {
            Application.runInBackground = true;
            lifetime = new CancellationTokenSource(TimeSpan.FromMinutes(3));
            allocations = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame");
            Debug.Log("QUEST009_ALLOCATIONS counterValid=" + allocations.Valid + "; scope=whole-player-frames");
            work = RunAsync(lifetime.Token);
            while (!work.IsCompleted) yield return null;
            if (work.IsFaulted) Debug.LogError("QUEST009_FAIL " + work.Exception);
            else if (work.IsCanceled) Debug.LogError("QUEST009_FAIL cancelled");
            else Debug.Log("QUEST009_PASS capability");
            lifetime.Dispose();
            allocations.Dispose();
            lifetime = null;
            Application.Quit(work.IsCompletedSuccessfully ? 0 : 1);
        }

        private static async Task RunAsync(CancellationToken cancellation)
        {
            if (Array.IndexOf(Environment.GetCommandLineArgs(), "-builtinIdentity") < 0)
            {
                await TransportQualification.RunAsync(cancellation);
                return;
            }

            Debug.Log("QUEST009_STAGE identity; unity=" + Application.unityVersion + "; platform=" + Application.platform);
            using var identity = Array.IndexOf(Environment.GetCommandLineArgs(), "-builtinIdentity") >= 0 ? TransportIdentity.CreateWithRuntimeApi() : TransportIdentity.Create();
            Debug.Log("QUEST009_STAGE identity-created; hasPrivateKey=" + identity.HasPrivateKey);
            byte[] pin = TransportIdentity.Hash(identity.RawData);
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start(1);
            using var stopListener = cancellation.Register(listener.Stop);
            using var client = new TcpClient();
            using var stopClient = cancellation.Register(client.Close);
            Task<TcpClient> accept = listener.AcceptTcpClientAsync();
            try
            {
                await client.ConnectAsync(IPAddress.Loopback, ((IPEndPoint)listener.LocalEndpoint).Port);
                using var server = await accept;
                using var stopServer = cancellation.Register(server.Close);
                using var serverTls = new SslStream(server.GetStream(), false);
                using var clientTls = new SslStream(client.GetStream(), false, (_, certificate, __, ___) => TransportIdentity.Matches(certificate, pin));
                Debug.Log("QUEST009_STAGE handshake");
                await Task.WhenAll(serverTls.AuthenticateAsServerAsync(identity, false, SslProtocols.Tls12, false), clientTls.AuthenticateAsClientAsync("HiBoP-Transport-Probe", null, SslProtocols.Tls12, false));
                Debug.Log("QUEST009_STAGE encrypted; server=" + serverTls.IsEncrypted + "; client=" + clientTls.IsEncrypted + "; protocol=" + clientTls.SslProtocol);
                await serverTls.WriteAsync(new byte[] { 9 }, 0, 1, cancellation);
                var received = new byte[1];
                if (await clientTls.ReadAsync(received, 0, 1, cancellation) != 1 || received[0] != 9) throw new InvalidDataException("TLS byte mismatch.");
            }
            finally
            {
                listener.Stop();
                if (!accept.IsCompletedSuccessfully)
                {
                    try
                    {
                        using var abandoned = await accept;
                    }
                    catch (Exception) when (cancellation.IsCancellationRequested)
                    {
                    }
                }
            }
        }

        private void OnApplicationQuit() => lifetime?.Cancel();
        private void OnDestroy() => lifetime?.Cancel();

        private void Update()
        {
            if (allocations.Valid) TransportQualification.AllocatedFrameBytes += allocations.LastValue;
        }
    }
}
