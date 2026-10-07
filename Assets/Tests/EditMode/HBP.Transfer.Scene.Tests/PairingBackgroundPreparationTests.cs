using System;
using System.Threading;
using System.Threading.Tasks;
using HBP.Core.Object3D;
using HBP.Core.Preferences;
using HBP.Sync.Scene;
using HBP.Transfer.Transport;
using Newtonsoft.Json;
using NUnit.Framework;
using System.Text;

namespace HBP.Tests.Transfer
{
    public sealed class PairingBackgroundPreparationTests
    {
        [Test]
        public async Task SessionOpensAndReportsPendingPreloadsWithoutWaitingForInstallation()
        {
            var installed = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var preferences = new UserPreferences();
            preferences.Data.Atlases.PreloadIBC = true;
            int loads = 0;
            var preparation = new SessionAtlasPreparation(preferences.Data.Atlases, () => installed.Task,
                (id, token) => { loads++; return Task.FromResult(new AtlasLoadResult(id, AtlasLoadState.Loaded, new string('a', 64))); },
                id => new AtlasLoadResult(id, AtlasLoadState.Unloaded));
            Guid context = Guid.NewGuid();
            var receiver = new SessionPreferencesReceiver(() => context, () => preferences, () => false, preparation.Snapshot);
            preparation.Start();
            try
            {
                var open = await receiver.HandleAsync(new(context, 1, Guid.NewGuid(), SessionControlKind.Open), CancellationToken.None);
                Assert.That(open.Applied, Is.True);
                Assert.That(open.Message, Is.EqualTo(SessionControlCodec.AtlasInventoryCapability));
                var reply = await receiver.HandleAsync(new(context, 1, Guid.NewGuid(), SessionControlKind.AtlasInventory), CancellationToken.None);
                var inventory = JsonConvert.DeserializeObject<SessionAtlasInventory>(Encoding.UTF8.GetString(reply.GetBody()));
                Assert.That(inventory.Preparing, Is.True);
                Assert.That(inventory.Atlases["ibc"].State, Is.EqualTo(AtlasLoadState.Loading));
                Assert.That(loads, Is.Zero);
            }
            finally
            {
                installed.SetResult(true);
                await preparation.CloseAsync();
                await receiver.CloseAsync();
            }
        }

        [Test]
        public async Task PreloadFailureIsTerminalAndDoesNotInvalidateTheOpenSession()
        {
            var preferences = new UserPreferences();
            preferences.Data.Atlases.PreloadIBC = true;
            var preparation = new SessionAtlasPreparation(preferences.Data.Atlases, () => Task.FromException(new InvalidOperationException("Missing scientific files")),
                (id, token) => throw new Exception("Must not load"), id => new AtlasLoadResult(id, AtlasLoadState.Unloaded));
            preparation.Start();
            await preparation.Completion;
            var inventory = preparation.Snapshot();
            Assert.That(inventory.Preparing, Is.False);
            Assert.That(inventory.Atlases["ibc"].State, Is.EqualTo(AtlasLoadState.Failed));
            Guid context = Guid.NewGuid();
            var receiver = new SessionPreferencesReceiver(() => context, () => preferences, () => false, preparation.Snapshot);
            try
            {
                Assert.That((await receiver.HandleAsync(new(context, 1, Guid.NewGuid(), SessionControlKind.Open), CancellationToken.None)).Applied, Is.True);
                Assert.That((await receiver.HandleAsync(new(context, 1, Guid.NewGuid(), SessionControlKind.Inspect), CancellationToken.None)).Applied, Is.True);
            }
            finally { await preparation.CloseAsync(); await receiver.CloseAsync(); }
        }

        [Test]
        public async Task ClosingContextWaitsForNativeCompletionAndCancelsRemainingPreloads()
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
            var preferences = new AtlasesPreferences { PreloadMarsAtlas = true, PreloadIBC = true };
            var entered = new TaskCompletionSource<bool>();
            var complete = new TaskCompletionSource<AtlasLoadResult>();
            using var cancelled = timeout.Token.Register(() => { entered.TrySetCanceled(); complete.TrySetCanceled(); });
            int loads = 0;
            var preparation = new SessionAtlasPreparation(preferences, () => Task.CompletedTask,
                (id, token) => { loads++; entered.TrySetResult(true); return complete.Task; }, id => new AtlasLoadResult(id, AtlasLoadState.Unloaded));
            preparation.Start();
            await entered.Task;
            var closing = preparation.CloseAsync();
            Assert.That(closing.IsCompleted, Is.False);
            complete.SetResult(new("mars", AtlasLoadState.Cancelled));
            await closing;
            Assert.That(loads, Is.EqualTo(1));
            Assert.That(preparation.Snapshot().Preparing, Is.False);
            Assert.That(preparation.Snapshot().Atlases["ibc"].State, Is.EqualTo(AtlasLoadState.Cancelled));
        }

        [Test]
        public async Task ClosingWhileWaitingForInstallationDoesNotWaitForApplicationWork()
        {
            var installed = new TaskCompletionSource<bool>();
            var preparation = new SessionAtlasPreparation(new AtlasesPreferences(), () => installed.Task,
                (id, token) => throw new Exception("Cancelled context must not load"), id => new AtlasLoadResult(id, AtlasLoadState.Unloaded));
            preparation.Start();
            try
            {
                Task closing = preparation.CloseAsync();
                Task winner = await Task.WhenAny(closing, Task.Delay(3000));
                Assert.That(winner, Is.SameAs(closing));
                await closing;
                Assert.That(installed.Task.IsCompleted, Is.False);
                Assert.That(preparation.Snapshot().Preparing, Is.False);
            }
            finally { installed.TrySetResult(true); await preparation.CloseAsync(); }
        }

        [Test]
        public async Task SuccessfulPreloadDoesNotResurrectAnUnloadedNativeResource()
        {
            bool loaded = true;
            var preparation = new SessionAtlasPreparation(new AtlasesPreferences(), () => Task.CompletedTask,
                (id, token) => Task.FromResult(new AtlasLoadResult(id, AtlasLoadState.Loaded, new string('a', 64))),
                id => new AtlasLoadResult(id, loaded ? AtlasLoadState.Loaded : AtlasLoadState.Unloaded, loaded ? new string('a', 64) : null));
            preparation.Start();
            await preparation.Completion;
            Assert.That(preparation.Snapshot().Preparing, Is.False);
            loaded = false;
            Assert.That(preparation.Snapshot().Atlases["mars"].State, Is.EqualTo(AtlasLoadState.Unloaded));
            Assert.That(preparation.Snapshot().Atlases["mars"].Fingerprint, Is.Null);
            await preparation.CloseAsync();
        }
    }
}
