using System.IO;
using System.Linq;
using HBP.Core.Database;
using HBP.Core.Data;
using HBP.Core.Preferences;
using HBP.Core.Tools;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;

namespace HBP.Tests.Serialization
{
    public class ClassLoaderSaverTests
    {
        [Test]
        public void RoundTrip_PreservesConcreteTypeNames()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            TagCollection source = new(
                new BaseTag[] { new StringTag("general-alpha", "tag-general-alpha") },
                new BaseTag[] { new BoolTag("patient-alpha", "tag-patient-alpha") },
                new BaseTag[] { new EnumTag("site-alpha", new[] { "one", "two" }, "tag-site-alpha") },
                "tag-collection-alpha");

            string path = temp.GetPath("tags.json");
            Assert.That(ClassLoaderSaver.SaveToJSon(source, path, true), Is.True);

            string json = File.ReadAllText(path);
            Assert.That(json, Does.Contain("HBP.Core.Data.StringTag"));
            Assert.That(json, Does.Contain("HBP.Core.Data.BoolTag"));
            Assert.That(json, Does.Contain("HBP.Core.Data.EnumTag"));

            TagCollection loaded = ClassLoaderSaver.LoadFromJson<TagCollection>(path);
            Assert.That(loaded.AllTags, Has.Count.EqualTo(3));
            Assert.That(loaded.AllTags.Single(tag => tag.ID == "tag-general-alpha"), Is.TypeOf<StringTag>());
            Assert.That(loaded.AllTags.Single(tag => tag.ID == "tag-patient-alpha"), Is.TypeOf<BoolTag>());
            Assert.That(loaded.AllTags.Single(tag => tag.ID == "tag-site-alpha"), Is.TypeOf<EnumTag>());
        }

        [Test]
        public void LoadFromJsonString_ResolvesLegacyAssemblyCSharpTypes()
        {
            string json = File.ReadAllText(TestPathUtility.FixturePath("Serialization", "legacy_bool_tag_assembly_csharp.json"));

            BoolTag tag = ClassLoaderSaver.LoadFromJsonString<BoolTag>(json);

            Assert.That(tag, Is.Not.Null);
            Assert.That(tag.ID, Is.EqualTo("legacy-bool-tag-001"));
            Assert.That(tag.Name, Is.EqualTo("legacy-bool-alpha"));
        }

        [Test]
        public void LoadFromJsonString_MigratesLegacyPreferenceNamespace()
        {
            string json = File.ReadAllText(TestPathUtility.FixturePath("Serialization", "legacy_user_preferences_namespace.json"));

            UserPreferences preferences = ClassLoaderSaver.LoadFromJsonString<UserPreferences>(json);

            Assert.That(preferences, Is.Not.Null);
            Assert.That(preferences.ID, Is.EqualTo("legacy-user-preferences-001"));
        }

        [Test]
        public void LoadFromJsonString_MigratesLegacyDatabaseNamespace()
        {
            string json = File.ReadAllText(TestPathUtility.FixturePath("Serialization", "legacy_database_settings_namespace.json"));

            GlobalDatabaseSettings settings = ClassLoaderSaver.LoadFromJsonString<GlobalDatabaseSettings>(json);

            Assert.That(settings, Is.Not.Null);
            Assert.That(settings.ID, Is.EqualTo("legacy-database-settings-001"));
            Assert.That(settings.IsFirstUse, Is.True);
        }
    }
}
