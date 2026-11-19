using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.Database;
using HBP.Core.Preferences;
using HBP.Transfer.Scene;
using HBP.Transfer.Transport;
using UnityEngine;

namespace HBP.Quest.Desktop
{
    public sealed partial class QuestManager
    {
        private long definitionsRevision;
        private Task definitionsWork = Task.CompletedTask;
        private PairingSnapshot pendingDefinitions;
        private readonly List<PairingSnapshot> retiredDefinitionSnapshots = new();
        private float nextDefinitionsAttempt;
        private long definitionsEpoch;
        private CancellationTokenSource definitionsAttempt;

        private void InitializeDefinitions()
        {
            PersistentDataManager.Aliases.OnSaveAliases.AddListener(DefinitionsSaved);
            PersistentDataManager.Tags.OnSaveTags.AddListener(DefinitionsSaved);
            DatabaseManager.Database.OnProtocolsChanged.AddListener(DefinitionsSaved);
        }

        private void DefinitionsSaved() => Interlocked.Increment(ref definitionsRevision);

        private void TickDefinitions()
        {
            if (!IsPaired || globals == null || busy || restoringScenes || reloadingScenes || !definitionsWork.IsCompleted || Time.unscaledTime < nextDefinitionsAttempt || globals.Context.Data.DefinitionsRevision >= definitionsRevision) return;
            _ = SynchronizeDefinitionsSafelyAsync();
        }

        private async Task SynchronizeDefinitionsSafelyAsync()
        {
            try
            {
                await FlushDefinitionsAsync(lifetime.Token);
                if (IsPaired) SetStatus("Aliases, protocols and tags synchronized with Quest.");
            }
            catch (OperationCanceledException) { }
            catch (Exception error)
            {
                await UniTask.SwitchToMainThread();
                nextDefinitionsAttempt = Time.unscaledTime + 5;
                SetStatus("Shared definitions could not synchronize; retrying: " + TransferError.Describe(error));
            }
        }

        private async Task FlushDefinitionsAsync(CancellationToken token)
        {
            await UniTask.SwitchToMainThread(token);
            while (IsPaired && globals != null && globals.Context.Data.DefinitionsRevision < definitionsRevision)
            {
                if (definitionsWork.IsCompleted) definitionsWork = SynchronizeDefinitionsCoreAsync();
                // Cancelling a scene's capture must not cancel a session-wide update.
                await definitionsWork.AsUniTask().AttachExternalCancellation(token);
                await UniTask.SwitchToMainThread(token);
            }
        }

        private async Task SynchronizeDefinitionsCoreAsync()
        {
            using var attempt = CancellationTokenSource.CreateLinkedTokenSource(lifetime.Token);
            definitionsAttempt = attempt;
            try
            {
            var previous = globals;
            long generation = sessionGeneration;
            long epoch = definitionsEpoch;
            if (pendingDefinitions == null)
            {
                var captured = await PairingSnapshot.CaptureAsync(attempt.Token, previous.Context.Id, definitionsRevision);
                if (definitionsEpoch != epoch)
                {
                    captured.Dispose();
                    throw new OperationCanceledException("The pairing was cleared during definitions capture.");
                }
                pendingDefinitions = captured;
            }
            var candidate = pendingDefinitions;
            void RequireSession()
            {
                if (!IsPaired || !ReferenceEquals(globals, previous) || sessionGeneration != generation) throw new InvalidOperationException("The Quest connection changed while synchronizing shared definitions.");
            }
            RequireSession();
            var request = new SessionControlRequest(Guid.ParseExact(previous.Context.Id, "N"), generation, Guid.NewGuid(), SessionControlKind.Definitions, candidate.Context.Data.DefinitionsRevision);
            var receipt = await QuestPairing.UpdateDefinitionsAsync(endpoint, pin, credential, request, attempt.Token,
                (stream, stop) => candidate.Delivery.SendAsync(stream, stop));
            await UniTask.SwitchToMainThread(attempt.Token);
            RequireSession();
            if (receipt.Status is not DeliveryStatus.Published and not DeliveryStatus.AlreadyPublished) throw new InvalidOperationException("Quest refused the shared definitions update.");
            candidate.Context.RetainPreviousDefinitions(previous.Context);
            candidate.Context.Data.Preferences = previous.Context.Data.Preferences;
            retiredDefinitionSnapshots.Add(previous);
            globals = candidate;
            pendingDefinitions = null;
            }
            finally
            {
                await UniTask.SwitchToMainThread();
                if (ReferenceEquals(definitionsAttempt, attempt)) definitionsAttempt = null;
            }
        }

        private async Task RestoreDefinitionsHistoryAsync(CancellationToken token)
        {
            // After a Quest restart, install the original archive then replay newer definitions.
            // Older retryable scene deliveries must remain readable on the fresh receiver too.
            var query = new SessionControlRequest(Guid.ParseExact(globals.Context.Id, "N"), sessionGeneration, Guid.NewGuid(), SessionControlKind.DefinitionsRevision);
            var response = await QuestPairing.SendSessionControlAsync(endpoint, pin, credential, query, token);
            if (!response.Applied || !long.TryParse(response.Message, System.Globalization.NumberStyles.None, System.Globalization.CultureInfo.InvariantCulture, out long remoteRevision))
                throw new InvalidOperationException("Could not inspect Quest definitions. Update Desktop and Quest together.");
            if (retiredDefinitionSnapshots.Count != 0)
            {
                var snapshots = new List<PairingSnapshot>(retiredDefinitionSnapshots);
                snapshots.Add(globals);
                foreach (var snapshot in snapshots.GetRange(1, snapshots.Count - 1))
                {
                    if (snapshot.Context.Data.DefinitionsRevision <= remoteRevision) continue;
                    var request = new SessionControlRequest(Guid.ParseExact(globals.Context.Id, "N"), sessionGeneration, Guid.NewGuid(), SessionControlKind.Definitions, snapshot.Context.Data.DefinitionsRevision);
                    var receipt = await QuestPairing.UpdateDefinitionsAsync(endpoint, pin, credential, request, token, (stream, stop) => snapshot.Delivery.SendAsync(stream, stop));
                    if (receipt.Status is not DeliveryStatus.Published and not DeliveryStatus.AlreadyPublished) throw new InvalidOperationException("Quest refused the definitions history.");
                }
            }
        }

        private void ClearDefinitions()
        {
            definitionsAttempt?.Cancel();
            definitionsEpoch++;
            pendingDefinitions?.Dispose();
            pendingDefinitions = null;
            foreach (var snapshot in retiredDefinitionSnapshots) snapshot.Dispose();
            retiredDefinitionSnapshots.Clear();
        }

        private void DisposeDefinitions()
        {
            if (PersistentDataManager.IsInitialized)
            {
                PersistentDataManager.Aliases.OnSaveAliases.RemoveListener(DefinitionsSaved);
                PersistentDataManager.Tags.OnSaveTags.RemoveListener(DefinitionsSaved);
            }
            if (DatabaseManager.IsInitialized) DatabaseManager.Database.OnProtocolsChanged.RemoveListener(DefinitionsSaved);
        }
    }
}
