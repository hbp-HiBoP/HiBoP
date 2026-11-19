using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Database;
using HBP.Core.Preferences;
using HBP.Core.Tools;
using HBP.Quest.Desktop;
using NUnit.Framework;
using UnityEngine;

namespace HBP.Tests.Quest
{
    public sealed class SessionDefinitionsOwnerTests
    {
        [Test]
        public async Task AliasTagSavesAndProtocolChangesQueueUpdatesAndDisposalUnsubscribes()
        {
            string folder = Path.Combine(Path.GetTempPath(), "hibop-definitions-owner-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            var root = new GameObject("definitions owner test");
            root.SetActive(false);
            var persistent = root.AddComponent<PersistentDataManager>();
            var database = root.AddComponent<DatabaseManager>();
            var manager = root.AddComponent<QuestManager>();
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            var persistentField = typeof(Singleton<PersistentDataManager>).GetField("m_Instance", BindingFlags.Static | BindingFlags.NonPublic);
            var databaseField = typeof(Singleton<DatabaseManager>).GetField("m_Instance", BindingFlags.Static | BindingFlags.NonPublic);
            var priorPersistent = persistentField.GetValue(null);
            var priorDatabase = databaseField.GetValue(null);
            string aliasPath = AliasCollection.PATH, tagsPath = TagCollection.PATH, databasePath = ApplicationState.DatabasePath;
            var preferences = new UserPreferences();
            var aliases = new AliasCollection();
            var tags = new TagCollection();
            var protocols = new GlobalDatabase();
            long Revision() => (long)typeof(QuestManager).GetField("definitionsRevision", flags).GetValue(manager);
            try
            {
                persistentField.SetValue(null, persistent);
                databaseField.SetValue(null, database);
                typeof(PersistentDataManager).GetField("m_UserPreferences", flags).SetValue(persistent, preferences);
                typeof(PersistentDataManager).GetField("m_Aliases", flags).SetValue(persistent, aliases);
                typeof(PersistentDataManager).GetField("m_Tags", flags).SetValue(persistent, tags);
                typeof(DatabaseManager).GetField("m_Database", flags).SetValue(database, protocols);
                AliasCollection.PATH = Path.Combine(folder, "Aliases.json");
                TagCollection.PATH = Path.Combine(folder, "Tags.json");
                typeof(ApplicationState).GetProperty("DatabasePath").SetValue(null, folder);
                typeof(QuestManager).GetMethod("InitializeDefinitions", flags).Invoke(manager, null);
                preferences.NotifyChanged();
                Assert.That(Revision(), Is.Zero);
                aliases.Save();
                Assert.That(Revision(), Is.EqualTo(1));
                tags.Save();
                Assert.That(Revision(), Is.EqualTo(2));
                await protocols.SaveProtocolsAsync();
                Assert.That(Revision(), Is.EqualTo(3));
                Assert.That(Directory.Exists(Path.Combine(folder, "Protocols")), Is.True);
                protocols.SetProtocols(Array.Empty<Protocol>(), new ValidationRequest(ValidationAspect.None));
                Assert.That(Revision(), Is.EqualTo(4), "Quick Start and command-line protocol replacements also change the shared definitions.");

                string blocked = Path.Combine(folder, "blocked");
                File.WriteAllText(blocked, "cannot create directories here");
                typeof(ApplicationState).GetProperty("DatabasePath").SetValue(null, blocked);
                Exception failure = null;
                try { await protocols.SaveProtocolsAsync(); } catch (Exception error) { failure = error; }
                Assert.That(failure, Is.Not.Null);
                Assert.That(Revision(), Is.EqualTo(4), "A failed save must not advertise new definitions.");
                typeof(QuestManager).GetMethod("DisposeDefinitions", flags).Invoke(manager, null);
                aliases.Save();
                tags.Save();
                protocols.OnProtocolsChanged.Invoke();
                Assert.That(Revision(), Is.EqualTo(4));
                Assert.That(manager.IsPaired, Is.False, "Saving while disconnected must not attempt pairing.");
                using var pending = new System.Threading.CancellationTokenSource();
                typeof(QuestManager).GetField("definitionsAttempt", flags).SetValue(manager, pending);
                typeof(QuestManager).GetMethod("DisconnectTransport", flags).Invoke(manager, null);
                Assert.That(pending.IsCancellationRequested, Is.True, "Disconnecting must not leave an old transfer blocking the next connection.");
                typeof(QuestManager).GetField("definitionsAttempt", flags).SetValue(manager, null);
            }
            finally
            {
                typeof(QuestManager).GetMethod("DisposeDefinitions", flags).Invoke(manager, null);
                persistentField.SetValue(null, priorPersistent);
                databaseField.SetValue(null, priorDatabase);
                AliasCollection.PATH = aliasPath;
                TagCollection.PATH = tagsPath;
                typeof(ApplicationState).GetProperty("DatabasePath").SetValue(null, databasePath);
                UnityEngine.Object.DestroyImmediate(root);
                Directory.Delete(folder, true);
            }
        }
    }
}
