using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Data.Module3D;
using HBP.Sync;
using HBP.Sync.Scene;
using HBP.Transfer.Scene;
using HBP.Transfer.Transport;
using HBP.UI.Informations;

namespace HBP.Quest.Desktop
{
    /// <summary>One demand-driven publisher per authenticated scene connection.</summary>
    internal sealed class SignalPresentationPublisher : IDisposable
    {
        private readonly Base3DScene scene;
        private readonly PreparedSceneIdentity identity;
        private readonly PersistentTransport transport;
        private readonly Func<bool> ready;
        private readonly CancellationTokenSource lifetime = new();
        private readonly ConcurrentDictionary<Guid, TaskCompletionSource<bool>> acknowledgements = new();
        private readonly HashSet<string> known = new(StringComparer.Ordinal);
        private readonly Task runner;
        private SignalPresentationCapture capture = new();
        private Guid generation;
        private long sent = -1, samples = -1;
        private OperationId inFlight;
        private bool advertised;

        internal SignalPresentationPublisher(Base3DScene scene, PreparedSceneIdentity identity, PersistentTransport transport, Func<bool> ready)
        {
            this.scene = scene;
            this.identity = identity;
            this.transport = transport;
            this.ready = ready;
            runner = RunAsync(lifetime.Token);
        }

        internal void Receive(SignalMessage message)
        {
            if (message.Kind == SignalMessageKind.Acknowledge)
            {
                if (Guid.TryParse(message.Key, out Guid id) && acknowledgements.TryRemove(id, out var completion))
                {
                    if (message.Total == 0) completion.TrySetResult(true);
                    else completion.TrySetException(new IOException("Quest rejected the signal presentation. Retry from the window."));
                }

                return;
            }

            if (message.Kind == SignalMessageKind.Subscribe)
            {
                if (inFlight != null) transport.CancelBulk(inFlight);
                foreach (var completion in acknowledgements.Values) completion.TrySetCanceled();
                generation = message.Generation;
                sent = samples = -1;
                known.Clear();
                capture = new();
            }
            else if (message.Kind == SignalMessageKind.Unsubscribe && message.Generation == generation)
            {
                generation = Guid.Empty;
                known.Clear();
                capture = new();
                if (inFlight != null) transport.CancelBulk(inFlight);
                foreach (var completion in acknowledgements.Values) completion.TrySetCanceled();
            }
        }

        private async Task RunAsync(CancellationToken stop)
        {
            try
            {
                while (!stop.IsCancellationRequested)
                {
                    await UniTask.Yield(PlayerLoopTiming.Update, stop);
                    if (transport.State != PersistentTransportState.Connected)
                    {
                        generation = Guid.Empty;
                        known.Clear();
                        capture = new();
                        advertised = false;
                        continue;
                    }

                    if (!ready()) continue;
                    if (!advertised)
                    {
                        var capability = new SceneAction(identity.SceneId, identity.IncarnationId, SceneActionKind.Result, column: SignalWire.Capability);
                        advertised = transport.EnqueueSessionControl(capability.Encode(), DeliveryReliability.Reliable).Accepted;
                    }

                    if (generation == Guid.Empty) continue;
                    var wrapper = InformationPresentation.Find(scene);
                    if (wrapper == null) continue;
                    wrapper.FlushPresentation();
                    if (wrapper.Presentation.Revision == sent) continue;
                    Guid current = generation;
                    long revision = wrapper.Presentation.Revision;
                    try
                    {
                        var snapshot = capture.Capture(wrapper);
                        await SendAsync(new SignalMessage { Kind = SignalMessageKind.Begin, Generation = current, Revision = revision, Key = samples == wrapper.Presentation.SamplesRevision ? "settings" : "content" }, stop);
                        CheckCurrent(current, revision, wrapper);
                        known.IntersectWith(SignalPresentationBuffer.Required(snapshot));
                        await SendAsync(new SignalMessage { Kind = SignalMessageKind.Manifest, Generation = current, Revision = revision, Body = SignalSnapshotCodec.Encode(snapshot) }, stop);
                        foreach (string key in SignalPresentationBuffer.Required(snapshot))
                        {
                            if (known.Contains(key)) continue;
                            byte[] resource = capture.Resources[key];
                            for (int offset = 0; offset < resource.Length; offset += SignalWire.PartBytes)
                            {
                                CheckCurrent(current, revision, wrapper);
                                int count = Math.Min(SignalWire.PartBytes, resource.Length - offset);
                                var part = new byte[count];
                                Buffer.BlockCopy(resource, offset, part, 0, count);
                                await SendAsync(new SignalMessage { Kind = SignalMessageKind.Resource, Generation = current, Revision = revision, Key = key, Offset = offset, Total = resource.Length, Body = part }, stop);
                            }

                            known.Add(key);
                        }

                        CheckCurrent(current, revision, wrapper);
                        await SendAsync(new SignalMessage { Kind = SignalMessageKind.Commit, Generation = current, Revision = revision }, stop);
                        sent = revision;
                        samples = wrapper.Presentation.SamplesRevision;
                        known.IntersectWith(SignalPresentationBuffer.Required(snapshot));
                    }
                    catch (OperationCanceledException) when (!stop.IsCancellationRequested)
                    {
                    }
                    catch (Exception error) when (error is not OperationCanceledException)
                    {
                        sent = revision;
                        known.Clear();
                        capture = new();
                        var message = error.Message.Length > 1000 ? error.Message.Substring(0, 1000) : error.Message;
                        transport.EnqueueSessionControl(SignalWire.Encode(new SignalMessage { Kind = SignalMessageKind.Error, Generation = current, Revision = revision, Key = message }), DeliveryReliability.Reliable);
                    }
                }
            }
            catch (OperationCanceledException) when (stop.IsCancellationRequested)
            {
            }
            catch (ObjectDisposedException) when (stop.IsCancellationRequested)
            {
            }
        }

        private void CheckCurrent(Guid current, long revision, InformationsWrapper wrapper)
        {
            wrapper.FlushPresentation();
            if (generation != current || wrapper.Presentation.Revision != revision || !ready()) throw new OperationCanceledException();
        }

        private async Task SendAsync(SignalMessage message, CancellationToken stop)
        {
            var operation = new OperationId(Guid.NewGuid());
            var completion = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            acknowledgements[operation.Value] = completion;
            inFlight = operation;
            try
            {
                byte[] body = SignalWire.Encode(message);
                while (true)
                {
                    stop.ThrowIfCancellationRequested();
                    if (generation != message.Generation) throw new OperationCanceledException();
                    var queued = transport.EnqueueSceneOperation(body, ScheduleDescriptor.ForPresentation(identity.SceneId, identity.IncarnationId), bodySchema: SignalWire.BodySchema, operationId: operation);
                    if (queued.Accepted) break;
                    if (queued.Disposition != EnqueueDisposition.Backpressured) throw new IOException("Signals transport unavailable.");
                    await Task.Delay(10, stop);
                }

                using var cancellation = stop.Register(() => completion.TrySetCanceled());
                var timeout = Task.Delay(TimeSpan.FromSeconds(10), stop);
                if (await Task.WhenAny(completion.Task, timeout) != completion.Task) throw new IOException("Signals response timed out. Retry from the window.");
                await completion.Task;
                await UniTask.SwitchToMainThread(stop);
            }
            finally
            {
                acknowledgements.TryRemove(operation.Value, out _);
                inFlight = null;
            }
        }

        public void Dispose()
        {
            lifetime.Cancel();
            foreach (var completion in acknowledgements.Values) completion.TrySetCanceled();
            acknowledgements.Clear();
            known.Clear();
            capture = new();
        }
    }
}
