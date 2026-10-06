using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using HBP.Transfer.Transport;
using NUnit.Framework;

namespace HBP.Tests.Transfer
{
    public sealed class SessionControlTransportTests
    {
        [TestCase(false)]
        [TestCase(true)]
        public async Task AuthenticatedControlWorksWithoutSceneAndRevokesDisconnectedRequest(bool disconnect)
        {
            using var stop = new CancellationTokenSource(TimeSpan.FromSeconds(20));
            using var clientStop = CancellationTokenSource.CreateLinkedTokenSource(stop.Token);
            using var pairing = new QuestPairing();
            var listener = new TcpListener(IPAddress.Loopback, QuestPairing.Port);
            listener.Start();
            var entered = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var revoked = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var complete = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var watchdog = stop.Token.Register(() =>
            {
                entered.TrySetCanceled();
                revoked.TrySetCanceled();
                complete.TrySetCanceled();
            });
            Task serving = pairing.ServeAsync(listener, stop.Token, (_, __) => throw new InvalidOperationException("No scene should be needed."), _ => { }, receiveSessionControl: async (request, token) =>
            {
                entered.TrySetResult(true);
                using var cancellation = token.Register(() =>
                {
                    revoked.TrySetResult(true);
                    complete.TrySetCanceled();
                });
                await complete.Task;
                token.ThrowIfCancellationRequested();
                return new SessionControlResponse(request.OperationId, SessionControlStatus.Applied);
            });
            Task<SessionControlResponse> operation = null;
            try
            {
                byte[] pin = await QuestPairing.InspectAsync("127.0.0.1", stop.Token);
                byte[] credential = await QuestPairing.PairAsync("127.0.0.1", pin, pairing.Code, stop.Token);
                var request = new SessionControlRequest(Guid.NewGuid(), 1, Guid.NewGuid(), SessionControlKind.Inspect);
                operation = QuestPairing.SendSessionControlAsync("127.0.0.1", pin, credential, request, clientStop.Token);
                await entered.Task;
                if (disconnect)
                {
                    clientStop.Cancel();
                    await revoked.Task;
                    Exception failure = null;
                    try
                    {
                        await operation;
                    }
                    catch (Exception exception)
                    {
                        failure = exception;
                    }

                    Assert.That(failure, Is.Not.Null);
                }
                else
                {
                    complete.TrySetResult(true);
                    var result = await operation;
                    Assert.That(result.OperationId, Is.EqualTo(request.OperationId));
                    Assert.That(result.Applied, Is.True);
                }
            }
            finally
            {
                stop.Cancel();
                if (operation != null)
                    try
                    {
                        await operation;
                    }
                    catch (Exception)
                    {
                    }

                await serving;
            }
        }
    }
}
