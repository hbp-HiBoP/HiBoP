using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Tools;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;

namespace HBP.Tests.Serialization
{
    public class BaseDataLazyIdTests
    {
        private static readonly FieldInfo s_IDField = typeof(BaseData).GetField("m_ID", BindingFlags.Instance | BindingFlags.NonPublic);

        [Test]
        public void NewObject_DoesNotGenerateIdUntilFirstAccess()
        {
            StringTag tag = new("lazy-id");

            Assert.That(GetStoredID(tag), Is.Null);

            string ID = tag.ID;

            Assert.That(ID, Is.Not.Null.And.Not.Empty);
            Assert.That(GetStoredID(tag), Is.EqualTo(ID));
            Assert.That(tag.ID, Is.EqualTo(ID));
        }

        [Test]
        public void Serialization_GeneratesAndPersistsId()
        {
            StringTag tag = new("serialized-lazy-id");

            string json = JsonConvert.SerializeObject(tag);
            JObject serialized = JObject.Parse(json);

            Assert.That(serialized.Value<string>("ID"), Is.EqualTo(tag.ID));
            Assert.That(serialized.Value<string>("ID"), Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void Deserialization_WithId_PreservesStoredValue()
        {
            const string expectedID = "existing-id-from-json";

            StringTag tag = ClassLoaderSaver.LoadFromJsonString<StringTag>(
                $"{{\"ID\":\"{expectedID}\",\"Name\":\"existing\"}}");

            Assert.That(GetStoredID(tag), Is.EqualTo(expectedID));
            Assert.That(tag.ID, Is.EqualTo(expectedID));
        }

        [Test]
        public void Deserialization_WithoutId_GeneratesBackwardCompatibleValue()
        {
            StringTag missingID = ClassLoaderSaver.LoadFromJsonString<StringTag>("{\"Name\":\"legacy-without-id\"}");
            StringTag emptyID = ClassLoaderSaver.LoadFromJsonString<StringTag>("{\"ID\":\"\",\"Name\":\"legacy-empty-id\"}");

            Assert.That(GetStoredID(missingID), Is.Not.Null.And.Not.Empty);
            Assert.That(missingID.ID, Is.EqualTo(GetStoredID(missingID)));
            Assert.That(GetStoredID(emptyID), Is.Not.Null.And.Not.Empty);
            Assert.That(emptyID.ID, Is.EqualTo(GetStoredID(emptyID)));
        }

        [Test]
        public void ExplicitEmptyId_RemainsAvailableForValidation()
        {
            StringTag tag = new("invalid");

            tag.ID = string.Empty;

            Assert.That(tag.ID, Is.Empty);
            Assert.That(GetStoredID(tag), Is.Empty);
        }

        [Test]
        public void DictionaryKey_GeneratesStableIdBeforeHashing()
        {
            StringTag tag = new("dictionary-key");
            Dictionary<BaseData, string> values = new();

            values.Add(tag, "value");

            Assert.That(GetStoredID(tag), Is.Not.Null.And.Not.Empty);
            Assert.That(values[tag], Is.EqualTo("value"));
        }

        [Test]
        public void Equality_GeneratesIdsForNewObjects()
        {
            StringTag first = new("first");
            StringTag second = new("second");

            Assert.That(first.Equals(second), Is.False);
            Assert.That(GetStoredID(first), Is.Not.Null.And.Not.Empty);
            Assert.That(GetStoredID(second), Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public void Clone_PreservesIdAndGenerateIdCreatesANewOne()
        {
            StringTag source = new("source");

            StringTag clone = (StringTag)source.Clone();
            string clonedID = clone.ID;

            Assert.That(clonedID, Is.EqualTo(source.ID));

            clone.GenerateID();

            Assert.That(clone.ID, Is.Not.EqualTo(source.ID));
            Assert.That(clone.ID, Is.Not.Null.And.Not.Empty);
        }

        [Test]
        public async Task ConcurrentFirstAccess_ReturnsOneStableId()
        {
            StringTag tag = new("concurrent");
            Task<string>[] readers = Enumerable.Range(0, 64)
                .Select(_ => Task.Run(() => tag.ID))
                .ToArray();

            string[] IDs = await Task.WhenAll(readers);

            Assert.That(IDs.Distinct().Count(), Is.EqualTo(1));
            Assert.That(IDs[0], Is.EqualTo(GetStoredID(tag)));
            Assert.That(IDs[0], Is.Not.Null.And.Not.Empty);
        }

        private static string GetStoredID(BaseData data)
        {
            Assert.That(s_IDField, Is.Not.Null);
            return (string)s_IDField.GetValue(data);
        }
    }
}
