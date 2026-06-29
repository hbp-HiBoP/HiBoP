using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Database;
using HBP.Core.Tools;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;

namespace HBP.Tests.Serialization
{
    public class LegacyProjectCompatibilityTests
    {
        [TestCase("legacy_bool_tag_assembly_csharp.json", typeof(BoolTag))]
        [TestCase("legacy_user_preferences_namespace.json", typeof(HBP.Core.Preferences.UserPreferences))]
        [TestCase("legacy_database_settings_namespace.json", typeof(GlobalDatabaseSettings))]
        public void LegacyFixtures_LoadWithExpectedConcreteType(string fileName, Type expectedType)
        {
            string json = File.ReadAllText(TestPathUtility.FixturePath("Serialization", fileName));

            object loaded = ClassLoaderSaver.LoadFromJsonString<object>(json);

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.GetType(), Is.EqualTo(expectedType));
        }

        [Test]
        public void UnknownLegacyType_FailsExplicitly()
        {
            string json = "{\"$type\":\"HBP.Data.DoesNotExist.MissingPlotTag, Assembly-CSharp\",\"ID\":\"missing-alpha\"}";

            Exception exception = Assert.Catch(() => ClassLoaderSaver.LoadFromJsonString<object>(json));
            Assert.That(exception.GetType().FullName, Is.EqualTo("Newtonsoft.Json.JsonSerializationException"));
            Assert.That(exception.Message, Does.Contain("was not resolved"));
        }

        [Test]
        public async Task PlotTagRegression_ProjectRoundTrip_DoesNotBreakLoad()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Project source = SyntheticProjectFactory.CreateCompleteProject();
            string saveDirectory = temp.GetPath("plot-regression");
            Directory.CreateDirectory(saveDirectory);

            await source.SaveAsync(saveDirectory, NoProgress, CancellationToken.None);

            string archivePath = Path.Combine(saveDirectory, source.FileName);
            ProjectInfo info = new(archivePath);
            Project loaded = new(info.Name, new ProjectPreferences("load-placeholder"));
            ApplicationState.LoadedProject = loaded;
            DatabaseManager.Database.SetProtocols(new[] { SyntheticProjectFactory.CreateProtocol() });

            await loaded.LoadAsync(info, NoProgress, CancellationToken.None);
            await loaded.SaveAsync(saveDirectory, NoProgress, CancellationToken.None);

            Assert.That(loaded.Patients[0].Sites[0].Tags[0].Tag.ID, Is.EqualTo(SyntheticProjectFactory.PlotTagId));
        }

        private static void NoProgress(float progress, float duration, LoadingText text)
        {
        }
    }
}
