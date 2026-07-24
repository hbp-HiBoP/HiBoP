using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using HBP.Core.Database;
using HBP.Core.Data;
using HBP.Core.Preferences;
using HBP.Core.Tools;
using HBP.Tests.Serialization.Helpers;
using Newtonsoft.Json;
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
            Assert.That(json, Does.Contain("\n"));
            Assert.That(json, Does.Contain("  \""));

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

        [Test]
        public async Task JsonMethods_DoNotRequireParameterlessConstructor()
        {
            using TempDirectoryScope temp = new();
            ConstructorOnlyPayload source = new("payload-alpha", 42);
            string path = temp.GetPath("constructor-only.json");

            Assert.That(await ClassLoaderSaver.SaveToJsonAsync(source, path, true), Is.True);

            ConstructorOnlyPayload loaded = await ClassLoaderSaver.LoadFromJsonAsync<ConstructorOnlyPayload>(path);
            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.Name, Is.EqualTo(source.Name));
            Assert.That(loaded.Value, Is.EqualTo(source.Value));
        }

        [Test]
        public void LoadFromJson_AcceptsIndentedAndCompactJson()
        {
            using TempDirectoryScope temp = new();
            string indentedPath = temp.GetPath("indented.json");
            string compactPath = temp.GetPath("compact.json");
            File.WriteAllText(indentedPath, "{\n  \"Name\": \"payload-alpha\",\n  \"Value\": 42\n}");
            File.WriteAllText(compactPath, "{\"Name\":\"payload-alpha\",\"Value\":42}");

            ConstructorOnlyPayload indented = ClassLoaderSaver.LoadFromJson<ConstructorOnlyPayload>(indentedPath);
            ConstructorOnlyPayload compact = ClassLoaderSaver.LoadFromJson<ConstructorOnlyPayload>(compactPath);

            Assert.That(indented.Name, Is.EqualTo(compact.Name));
            Assert.That(indented.Value, Is.EqualTo(compact.Value));
        }

        [Test]
        public void LoadFromJson_StreamDeserializesWithoutTakingOwnership()
        {
            byte[] json = Encoding.UTF8.GetBytes("{\"Name\":\"payload-alpha\",\"Value\":42}");
            using MemoryStream stream = new(json);

            ConstructorOnlyPayload loaded = ClassLoaderSaver.LoadFromJson<ConstructorOnlyPayload>(
                stream,
                json.Length,
                LoadingDiagnostics.Phase.None,
                LoadingDiagnostics.Phase.None);

            Assert.That(loaded.Name, Is.EqualTo("payload-alpha"));
            Assert.That(loaded.Value, Is.EqualTo(42));
            Assert.That(stream.CanRead, Is.True);
        }

        [Test]
        public void LoadFromJson_EmptyFilePreservesNullResult()
        {
            using TempDirectoryScope temp = new();
            string path = temp.GetPath("empty.json");
            File.WriteAllText(path, string.Empty);

            ConstructorOnlyPayload loaded = ClassLoaderSaver.LoadFromJson<ConstructorOnlyPayload>(path);

            Assert.That(loaded, Is.Null);
        }

        [TestCase("{\"Name\":\"payload-alpha\"")]
        [TestCase("{not-json}")]
        public void LoadFromJson_InvalidJsonThrows(string json)
        {
            using TempDirectoryScope temp = new();
            string path = temp.GetPath("invalid.json");
            File.WriteAllText(path, json);

            Assert.Catch<JsonException>(() => ClassLoaderSaver.LoadFromJson<ConstructorOnlyPayload>(path));
        }

        [Test]
        public void LargeJsonFile_RoundTripPreservesPayloadAndIndentation()
        {
            using TempDirectoryScope temp = new();
            ConstructorOnlyPayload source = new(new string('x', 100_000), 42);
            string path = temp.GetPath("large.json");

            Assert.That(ClassLoaderSaver.SaveToJSon(source, path, true), Is.True);
            Assert.That(new FileInfo(path).Length, Is.GreaterThan(85_000));
            Assert.That(File.ReadAllText(path), Does.Contain("\n"));

            ConstructorOnlyPayload loaded = ClassLoaderSaver.LoadFromJson<ConstructorOnlyPayload>(path);
            Assert.That(loaded.Name, Is.EqualTo(source.Name));
            Assert.That(loaded.Value, Is.EqualTo(source.Value));
        }

        [JsonObject(MemberSerialization.OptIn)]
        private sealed class ConstructorOnlyPayload
        {
            [JsonProperty]
            public string Name { get; }

            [JsonProperty]
            public int Value { get; }

            [JsonConstructor]
            public ConstructorOnlyPayload(string name, int value)
            {
                Name = name;
                Value = value;
            }
        }
    }
}
