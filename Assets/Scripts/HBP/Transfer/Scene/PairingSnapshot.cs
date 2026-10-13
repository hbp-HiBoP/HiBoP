using System;
using System.IO;
using System.Linq;
using HBP.Core.Database;
using HBP.Core.Preferences;
using HBP.Core.DLL;
using UnityEngine;
using System.Threading;
using Cysharp.Threading.Tasks;
using HBP.Transfer.Transport;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Unity.Profiling;

namespace HBP.Transfer.Scene
{
    /// <summary>Owns the immutable global archive sent during pairing, including protocol images.</summary>
    public sealed class PairingSnapshot : IDisposable
    {
        public PairingContext Context { get; }
        public SceneDelivery Delivery { get; }
        private readonly SceneArchive archive;
        private static readonly ProfilerMarker CaptureMarker = new("Quest.Pairing.CaptureManagedMetadata");

        private PairingSnapshot(SceneArchive archive, PairingContext context, SceneDelivery delivery)
        {
            this.archive = archive;
            Context = context;
            Delivery = delivery;
        }

        public static PairingSnapshot Capture()
        {
            if (!PersistentDataManager.IsInitialized || !DatabaseManager.IsInitialized || !DatabaseManager.Database.IsLoaded)
                throw new InvalidOperationException("Wait for the Desktop database to finish loading before pairing.");
            string folder = Path.Combine(Application.temporaryCachePath, "PairingCapture", Guid.NewGuid().ToString("N"));
            var archive = new SceneArchive(Path.Combine(folder, "resources"));
            string file = Path.Combine(folder, "globals.hbglobal");
            try
            {
                var data = new GlobalDataPayload
                {
                    Preferences = PersistentDataManager.UserPreferences,
                    Tags = PersistentDataManager.Tags,
                    Protocols = DatabaseManager.Database.Protocols.ToList(),
                    Aliases = PersistentDataManager.Aliases,
                    Grid = ActivityProjectionSettings.VolumeGridDimension,
                    Interpolation = ActivityProjectionSettings.VolumeInterpolation
                };
                var source = new PairingContext(data);
                source.CaptureFilterPresets(PersistentDataManager.FilterConditionsPresets, archive);
                archive.WriteGlobalData(data, file);
                // Deserialize once to detach the pairing snapshot from subsequent Desktop edits.
                var context = new PairingContext(archive.LoadGlobalData());
                context.RestoreFilterPresets(archive);
                return new PairingSnapshot(archive, context, new SceneDelivery(file, context.Id, context.Id, null));
            }
            catch
            {
                archive.Dispose();
                if (File.Exists(file)) File.Delete(file);
                if (Directory.Exists(folder)) Directory.Delete(folder);
                throw;
            }
        }

        public static async UniTask<PairingSnapshot> CaptureAsync(CancellationToken token)
        {
            using var timing = PairingTiming.Measure("desktop.globals-capture");
            await UniTask.SwitchToMainThread();
            if (!PersistentDataManager.IsInitialized || !DatabaseManager.IsInitialized || !DatabaseManager.Database.IsLoaded)
                throw new InvalidOperationException("Wait for the Desktop database to finish loading before pairing.");
            string folder = Path.Combine(Application.temporaryCachePath, "PairingCapture", Guid.NewGuid().ToString("N"));
            var archive = new SceneArchive(Path.Combine(folder, "resources"), deferResourceWrites: true, cancellationToken: token);
            string file = Path.Combine(folder, "globals.hbglobal");
            try
            {
                var data = new GlobalDataPayload
                {
                    Preferences = PersistentDataManager.UserPreferences,
                    Tags = PersistentDataManager.Tags,
                    Protocols = DatabaseManager.Database.Protocols.ToList(),
                    Aliases = PersistentDataManager.Aliases,
                    Grid = ActivityProjectionSettings.VolumeGridDimension,
                    Interpolation = ActivityProjectionSettings.VolumeInterpolation
                };
                PairingContext.DeferredReferences references;
                JObject metadata;
                using (CaptureMarker.Auto())
                using (PairingTiming.Measure("desktop.globals-managed-capture"))
                {
                    var source = new PairingContext(data, deferFingerprints: true);
                    source.CaptureFilterPresets(PersistentDataManager.FilterConditionsPresets, archive);
                    references = source.Deferred;
                    metadata = archive.CaptureDetachedGlobalMetadata(data);
                }

                Debug.Log($"[QUEST-PAIRING] globals: {data.Tags.AllTags.Count()} tags, {data.Protocols.Count} protocols");
                return await UniTask.RunOnThreadPool(() =>
                {
                    token.ThrowIfCancellationRequested();
                    using (PairingTiming.Measure("desktop.globals-illustrations")) archive.CompleteIllustrations(metadata);
                    PairingContext context;
                    using (PairingTiming.Measure("desktop.globals-fingerprints"))
                    {
                        context = new PairingContext(metadata.ToObject<GlobalDataPayload>(PreparedDataJson.Create(archive, inertPreferences: true)));
                        PairingTiming.Count("desktop.global-definitions", context.DefinitionCount);
                        references.Complete(context, context.Data.FilterPresets);
                        metadata["FilterPresets"] = context.Data.FilterPresets.DeepClone();
                    }

                    using (PairingTiming.Measure("desktop.globals-encode-and-compress"))
                        archive.WriteCapturedGlobals(Encoding.UTF8.GetBytes(metadata.ToString(Formatting.None)), file);
                    PairingTiming.Count("desktop.global-files", Directory.EnumerateFiles(archive.DirectoryPath).LongCount());
                    context.RestoreFilterPresets(archive);
                    token.ThrowIfCancellationRequested();
                    return new PairingSnapshot(archive, context, new SceneDelivery(file, context.Id, context.Id, null));
                });
            }
            catch
            {
                archive.Dispose();
                if (File.Exists(file)) File.Delete(file);
                if (Directory.Exists(folder)) Directory.Delete(folder);
                throw;
            }
        }

        public void Dispose()
        {
            archive.Dispose();
            Delivery.Dispose();
        }
    }
}
