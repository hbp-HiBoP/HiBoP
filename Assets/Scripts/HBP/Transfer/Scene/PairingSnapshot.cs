using System;
using System.IO;
using System.Linq;
using HBP.Core.Database;
using HBP.Core.Preferences;
using HBP.Core.DLL;
using UnityEngine;

namespace HBP.Transfer.Scene
{
    /// <summary>Owns the immutable global archive sent during pairing, including protocol images.</summary>
    public sealed class PairingSnapshot : IDisposable
    {
        public PairingContext Context { get; }
        public SceneDelivery Delivery { get; }
        private readonly SceneArchive archive;

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

        public void Dispose()
        {
            archive.Dispose();
            Delivery.Dispose();
        }
    }
}
