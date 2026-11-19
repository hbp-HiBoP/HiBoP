using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Database;
using HBP.Core.Preferences;
using HBP.Transfer.Scene;
using HBP.Transfer.Transport;

namespace HBP.Quest
{
    public sealed partial class QuestAnatomySession
    {
        // Earlier scenes and retryable deliveries still own definitions and illustrations from these archives.
        private readonly List<SceneArchive> retiredGlobalArchives = new();
        private string definitionsHash;

        private void ValidateDefinitionsUpdate(SessionControlRequest request)
        {
            if (destroyed || globals == null || request.Kind != SessionControlKind.Definitions || request.ContextId != Guid.ParseExact(globals.Id, "N") || request.Generation != controlGeneration)
                throw new InvalidOperationException("The Quest connection changed during the definitions update.");
            if (request.Revision < globals.Data.DefinitionsRevision) throw new InvalidOperationException("A newer definitions revision is already installed.");
        }

        public async Task<DeliveryReceipt> ReceiveDefinitionsAsync(SessionControlRequest request, Stream stream, CancellationToken stop)
        {
            await loadingGate.WaitAsync(stop).ConfigureAwait(false);
            try
            {
                Task<DeliveryReceipt> work = await OnUnityThreadAsync(() =>
                {
                    ValidateDefinitionsUpdate(request);
                    return ReceiveCoreAsync(async (token, publish, progress) =>
                    {
                        using var close = token.Register(stream.Dispose);
                        return await ReceiveFileAsync(stream, token, publish, progress).ConfigureAwait(false);
                    }, stop,
                        (file, hash, token) => InstallDefinitionsAsync(request, file, hash, token));
                }, stop).ConfigureAwait(false);
                return await work.ConfigureAwait(false);
            }
            finally
            {
                loadingGate.Release();
            }
        }

        private async Task<DeliveryStatus> InstallDefinitionsAsync(SessionControlRequest request, string file, string hash, CancellationToken stop)
        {
            string directory = await OnUnityThreadAsync(() => Path.Combine(UnityEngine.Application.temporaryCachePath, "PairingData", Guid.NewGuid().ToString("N")), stop).ConfigureAwait(false);
            var archive = new SceneArchive(directory, true);
            bool consumed = false;
            try
            {
                var candidate = await Task.Run(() =>
                {
                    var context = new PairingContext(archive.ReadGlobalData(file, stop, inertPreferences: true));
                    context.RestoreFilterPresets(archive);
                    return context;
                }, stop).ConfigureAwait(false);
                return await OnUnityThreadAsync(() =>
                {
                    ValidateDefinitionsUpdate(request);
                    stop.ThrowIfCancellationRequested();
                    if (candidate.Id != globals.Id || candidate.Data.DefinitionsRevision != request.Revision) throw new InvalidDataException("Definitions snapshot identity mismatch.");
                    if (request.Revision == globals.Data.DefinitionsRevision)
                    {
                        if (definitionsHash != hash) throw new InvalidDataException("Definitions revision reused with different content.");
                        return DeliveryStatus.AlreadyPublished;
                    }

                    candidate.RetainPreviousDefinitions(globals);
                    // Preferences, filter presets, atlases and live replicas retain their current owners.
                    candidate.Data.Preferences = PersistentDataManager.UserPreferences;
                    candidate.Data.Grid = globals.Data.Grid;
                    candidate.Data.Interpolation = globals.Data.Interpolation;
                    PersistentDataManager.ApplySessionData(PersistentDataManager.UserPreferences, candidate.Data.Tags, candidate.Data.Aliases, PersistentDataManager.FilterConditionsPresets);
                    DatabaseManager.Database.SetProtocols(candidate.Data.Protocols, new ValidationRequest(ValidationAspect.None));
                    retiredGlobalArchives.Add(globalArchive);
                    globalArchive = archive;
                    globals = candidate;
                    definitionsHash = hash;
                    consumed = true;
                    return DeliveryStatus.Published;
                }, stop).ConfigureAwait(false);
            }
            finally
            {
                if (!consumed) archive.Dispose();
            }
        }

        private void ReleaseRetiredGlobalArchives()
        {
            foreach (var archive in retiredGlobalArchives) archive?.Dispose();
            retiredGlobalArchives.Clear();
        }
    }
}
