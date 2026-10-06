using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DataManager = HBP.Core.Data.DataManager;
using Site = HBP.Core.Data.Site;
using HBP.Core.Enums;
using HBP.Core.Object3D;
using HBP.Core.Preferences;
using HBP.Core.Tools;
using HBP.Sync.Scene;
using HBP.Transfer.Scene;
using HBP.Transfer.Transport;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace HBP.Tests.Transfer
{
    [Category("SceneFocused")]
    public sealed class SessionPreferencesTests
    {
        private string m_Folder, m_Path;
        private UserPreferences m_Preferences;
        private NormalizationType m_Normalization;
        private AveragingType m_Averaging, m_Position;
        private bool m_SiteCorrection;

        [SetUp]
        public void SetUp()
        {
            m_Normalization = DataManager.DefaultNormalization;
            m_Averaging = DataManager.DefaultAveraging;
            m_Position = DataManager.DefaultPositionAveraging;
            m_SiteCorrection = Site.SiteNameCorrection;
            m_Path = UserPreferences.PATH;
            m_Folder = Path.Combine(Path.GetTempPath(), "hibop-session-preferences-" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(m_Folder);
            UserPreferences.PATH = Path.Combine(m_Folder, "Preferences.txt");
            var general = new GeneralPreferences(new ProjectPreferences("Session test", m_Folder, m_Folder), new ThemePreferences(), new LocalizationPreferences(), new SystemPreferences(), new MiscPreferences());
            m_Preferences = new UserPreferences(general, new DataPreferences(), new VisualizationPreferences());
        }

        [TearDown]
        public void TearDown()
        {
            SessionAtlasCatalog.End();
            DataManager.DefaultNormalization = m_Normalization;
            DataManager.DefaultAveraging = m_Averaging;
            DataManager.DefaultPositionAveraging = m_Position;
            Site.SiteNameCorrection = m_SiteCorrection;
            UserPreferences.PATH = m_Path;
            Directory.Delete(m_Folder, true);
        }

        private byte[] Snapshot(string name = "Received", NormalizationType? normalization = null)
        {
            var value = JObject.Parse(Encoding.UTF8.GetString(PreferencesSnapshot.Capture(m_Preferences)));
            value["General"]["Project"]["DefaultName"] = name;
            value["Data"]["Atlases"]["PreloadIBC"] = true;
            if (normalization.HasValue) value["Data"]["EEG"]["Normalization"] = (int)normalization.Value;
            return Encoding.UTF8.GetBytes(value.ToString());
        }

        [Test]
        public async Task DecodeIsDetachedAndReloadRefusalDoesNotMutateGlobals()
        {
            byte[] bytes = Snapshot(normalization: NormalizationType.Protocol);
            var snapshot = await Task.Run(() => PreferencesSnapshot.Read(bytes));
            Assert.That(DataManager.DefaultNormalization, Is.EqualTo(NormalizationType.None));
            Assert.Throws<InvalidOperationException>(() => snapshot.Apply(m_Preferences, true));
            Assert.That(DataManager.DefaultNormalization, Is.EqualTo(NormalizationType.None));
            Assert.That(m_Preferences.General.Project.DefaultName, Is.EqualTo("Session test"));
            Assert.That(File.Exists(UserPreferences.PATH), Is.False);
        }

        [Test]
        public void ApplyKeepsIdentityListenersAndQuestFileAndDoesNotPreload()
        {
            File.WriteAllText(UserPreferences.PATH, "quest-local-preferences");
            var identity = m_Preferences;
            var listeners = m_Preferences.OnSavePreferences;
            int notifications = 0, loads = 0;
            listeners.AddListener(() => notifications++);

            void Loading(AtlasLoadResult result)
            {
                if (result.State == AtlasLoadState.Loading) loads++;
            }

            AtlasResources.Changed += Loading;
            try
            {
                PreferencesSnapshot.Read(Snapshot()).Apply(m_Preferences, false);
            }
            finally
            {
                AtlasResources.Changed -= Loading;
            }

            Assert.That(m_Preferences, Is.SameAs(identity));
            Assert.That(m_Preferences.OnSavePreferences, Is.SameAs(listeners));
            Assert.That(notifications, Is.EqualTo(1));
            Assert.That(loads, Is.Zero);
            Assert.That(m_Preferences.Data.Atlases.PreloadIBC, Is.True);
            Assert.That(File.ReadAllText(UserPreferences.PATH), Is.EqualTo("quest-local-preferences"));
            m_Preferences.NotifyChanged();
            Assert.That(notifications, Is.EqualTo(2));
        }

        [TestCase(false)]
        [TestCase(true)]
        public async Task ReceiverOrdersRevisionsAndRejectsOldContextsAndConnections(bool sceneOpen)
        {
            Guid context = Guid.NewGuid();
            var receiver = new SessionPreferencesReceiver(() => context, () => m_Preferences, () => sceneOpen);
            SessionControlRequest Request(SessionControlKind kind, long revision = 0, string name = "Latest", long generation = 2) => new(context, generation, Guid.NewGuid(), kind, revision, body: kind == SessionControlKind.Preferences ? Snapshot(name) : null);
            int applied = 0;
            m_Preferences.OnSavePreferences.AddListener(() => applied++);
            try
            {
                Assert.That((await receiver.HandleAsync(Request(SessionControlKind.Open), CancellationToken.None)).Applied, Is.True);
                var current = Request(SessionControlKind.Preferences, 2);
                Assert.That((await receiver.HandleAsync(current, CancellationToken.None)).Applied, Is.True);
                Assert.That((await receiver.HandleAsync(current, CancellationToken.None)).Applied, Is.True);
                Assert.That(applied, Is.EqualTo(1), "Duplicate operation must return its prior applied ACK without notifying again.");
                Assert.That((await receiver.HandleAsync(Request(SessionControlKind.Preferences, 1, "Old"), CancellationToken.None)).Status, Is.EqualTo(SessionControlStatus.Stale));
                Assert.That((await receiver.HandleAsync(Request(SessionControlKind.Preferences, 3, "Old connection", 1), CancellationToken.None)).Status, Is.EqualTo(SessionControlStatus.Stale));
                var oldContext = Request(SessionControlKind.Preferences, 4, "Old context");
                context = Guid.NewGuid();
                Assert.That((await receiver.HandleAsync(oldContext, CancellationToken.None)).Status, Is.EqualTo(SessionControlStatus.Stale));
                Assert.That(m_Preferences.General.Project.DefaultName, Is.EqualTo("Latest"));
                Assert.That(applied, Is.EqualTo(1));
            }
            finally
            {
                await receiver.CloseAsync();
            }
        }

        [Test]
        public void InvalidSnapshotAndWireVersionAreRejected()
        {
            var value = JObject.Parse(Encoding.UTF8.GetString(Snapshot()));
            value["Data"]["EEG"]["Normalization"] = 999;
            Assert.Throws<InvalidDataException>(() => PreferencesSnapshot.Read(Encoding.UTF8.GetBytes(value.ToString())));
            var request = new SessionControlRequest(Guid.NewGuid(), 1, Guid.NewGuid(), SessionControlKind.Preferences, 4, body: Snapshot());
            byte[] frame = SessionControlCodec.Encode(request);
            var copy = SessionControlCodec.DecodeRequest(frame);
            Assert.That(copy.OperationId, Is.EqualTo(request.OperationId));
            Assert.That(copy.Revision, Is.EqualTo(4));
            frame[0] = 99;
            Assert.Throws<InvalidDataException>(() => SessionControlCodec.DecodeRequest(frame));
            Assert.Throws<InvalidDataException>(() => SessionControlCodec.DecodeRequest(SessionControlCodec.Encode(request).Concat(new byte[] { 0 }).ToArray()));
        }

        [Test]
        public void ResourceUsageAndReservationExcludeEachOther()
        {
            string id = "test:" + Guid.NewGuid();
            using (ResourceRetention.Retain(id)) Assert.Throws<InvalidOperationException>(() => ResourceRetention.ReserveRelease(id));
            bool released = false;
            using (var reservation = ResourceRetention.ReserveRelease(id))
            {
                Assert.Throws<InvalidOperationException>(() => ResourceRetention.Retain(id));
                Assert.Throws<InvalidOperationException>(() => ResourceRetention.EnsureCanUse(id));
                reservation.Release(() =>
                {
                    ResourceRetention.EnsureCanRelease(id);
                    released = true;
                });
            }

            Assert.That(released, Is.True);
            using (ResourceRetention.Retain(id))
            {
            }
        }

        [Test]
        public void InstalledLocalizerFingerprintUsesSameCatalogAndDetectsContentChanges()
        {
            string directory = Path.Combine(m_Folder, "Atlases", "Localizers", "AUDI", "Data");
            Directory.CreateDirectory(directory);
            string volume = Path.Combine(directory, "bloc.nii");
            File.WriteAllText(volume, "first test content");
            string first = AtlasResources.Fingerprint("localizer:AUDI", m_Folder);
            File.WriteAllText(volume, "second test content");
            Assert.That(AtlasResources.Fingerprint("localizer:AUDI", m_Folder), Is.Not.EqualTo(first));
            File.Delete(volume);
            Assert.Throws<FileNotFoundException>(() => AtlasResources.Fingerprint("localizer:AUDI", m_Folder));
        }
    }
}
