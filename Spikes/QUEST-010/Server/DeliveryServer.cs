using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using HBP.Transfer.Anatomy;
using HBP.Transfer.Anatomy.Delivery;
using HBP.Transfer.Transport;
using UnityEngine;

public sealed class DeliveryServer : MonoBehaviour
{
    [Serializable]
    private sealed class Config
    {
        public string fixture;
        public string secret;
        public string ready;
        public string runId;
    }

    [Serializable]
    private sealed class Ready
    {
        public string runId;
        public int port;
        public string pin;
    }

    private void Start() => _ = RunAsync();

    private static async Task RunAsync()
    {
        byte[] secret = null;
        TcpListener listener = null;
        int exitCode = 1;
        try
        {
            string[] args = Environment.GetCommandLineArgs();
            string path = args[Array.IndexOf(args, "-questDeliveryConfig") + 1];
            var config = JsonUtility.FromJson<Config>(File.ReadAllText(path));
            File.Delete(path);
            byte[] payload = File.ReadAllBytes(config.fixture);
            if (new DeliveryReceipt(TransportIdentity.Hash(payload), DeliveryStatus.Published).ContentHash != "065309ca8cc4babe9f8c2cd9f08bb7f6ae97b1e11374e1299e0f4a416e5ada12")
                throw new InvalidDataException("Only the audited MNI capture is allowed in this harness.");
            var offer = new AnatomyDelivery(AnatomySnapshotCodec.Decode(payload));
            secret = Convert.FromBase64String(config.secret);
            config.secret = null;
            using var identity = await Task.Run(TransportIdentity.Create);
            using var stop = CancellationTokenSource.CreateLinkedTokenSource(Application.exitCancellationToken);
            stop.CancelAfter(TimeSpan.FromSeconds(150));
            listener = new TcpListener(IPAddress.Any, 0);
            listener.Start();
            var received = new List<DeliveryStatus>();
            Task serving = offer.ServeAsync(listener, identity, secret, stop.Token, message => Debug.Log("QUEST010_SERVER " + message), receipt =>
            {
                received.Add(receipt.Status);
                Debug.Log($"QUEST010_ACK {receipt.Status} {receipt.ContentHash}");
                if (received.Count == 3) stop.Cancel();
            });
            try
            {
                var ready = new Ready { runId = config.runId, port = ((IPEndPoint)listener.LocalEndpoint).Port, pin = Convert.ToBase64String(TransportIdentity.Hash(identity.RawData)) };
                File.WriteAllText(config.ready, JsonUtility.ToJson(ready));
                await serving;
            }
            finally
            {
                stop.Cancel();
                await serving;
            }

            if (!received.SequenceEqual(new[] { DeliveryStatus.Published, DeliveryStatus.AlreadyPublished, DeliveryStatus.Closed }))
                throw new InvalidDataException("Unexpected publication receipts.");
            Debug.Log("QUEST010_SERVER_PASS " + config.runId);
            exitCode = 0;
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
        }
        finally
        {
            listener?.Stop();
            if (secret != null) Array.Clear(secret, 0, secret.Length);
            Application.Quit(exitCode);
        }
    }
}
