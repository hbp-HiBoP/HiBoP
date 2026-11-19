using System;
using System.Linq;
using System.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Tools;
using Newtonsoft.Json;
using NUnit.Framework;

namespace HBP.Tests.Serialization
{
    public class SerializationPropertyAliasTests
    {
        private static readonly JsonSerializerSettings Settings = new() { ContractResolver = new SerializationAliasContractResolver() };

        [TestCase(false)]
        [TestCase(true)]
        public void BothNamesAreRejectedRegardlessOfOrder(bool legacyFirst)
        {
            string legacy = "\"DynamicConfiguration\":{\"ID\":\"old\"}";
            string current = "\"CCEPConfiguration\":{\"ID\":\"new\"}";
            string json = "{" + (legacyFirst ? legacy + "," + current : current + "," + legacy) + "}";
            Assert.Throws<JsonSerializationException>(() => ClassLoaderSaver.LoadFromJsonString<CCEPColumn>(json));
        }

        [Test]
        public void NullLegacyValueKeepsConstructorDefaults()
        {
            var column = ClassLoaderSaver.LoadFromJsonString<CCEPColumn>("{\"DynamicConfiguration\":null}");
            Assert.That(column.CCEPConfiguration, Is.Not.Null);
            Assert.That(column.CCEPConfiguration.MarsAtlasLabel, Is.EqualTo(-1));
        }

        [Test]
        public void AliasDoesNotChangeIEEGConfigurationType()
        {
            var column = ClassLoaderSaver.LoadFromJsonString<IEEGColumn>("{\"ID\":\"column\",\"DynamicConfiguration\":{\"$type\":\"HBP.Core.Data.DynamicConfiguration\",\"ID\":\"ieeg\"}}");
            Assert.That(column.DynamicConfiguration, Is.TypeOf<DynamicConfiguration>());
            Assert.That(column.DynamicConfiguration.ID, Is.EqualTo("ieeg"));
        }

        [Test]
        public void PlainAliasUsesJsonMemberNamesAndWritesOnlyCurrentName()
        {
            SerializationTypeRegistry.RegisterGeneratedPropertyAlias(typeof(AliasFixture), "Old Name", "Current Name");
            var value = JsonConvert.DeserializeObject<AliasFixture>("{\"Old Name\":42}", Settings);
            Assert.That(value.Value, Is.EqualTo(42));
            string saved = JsonConvert.SerializeObject(value, Settings);
            Assert.That(saved, Does.Contain("\"Current Name\":42"));
            Assert.That(saved, Does.Not.Contain("Old Name"));
        }

        [Test]
        public void RepeatedPopulationDoesNotReusePresenceState()
        {
            var column = new CCEPColumn();
            JsonConvert.PopulateObject("{\"DynamicConfiguration\":{\"ID\":\"first\"}}", column, Settings);
            JsonConvert.PopulateObject("{\"CCEPConfiguration\":{\"ID\":\"second\"}}", column, Settings);
            Assert.That(column.CCEPConfiguration.ID, Is.EqualTo("second"));
        }

        [Test]
        public async Task SharedResolverKeepsConcurrentReadsIndependent()
        {
            var columns = await Task.WhenAll(Enumerable.Range(0, 32).Select(index => Task.Run(() => ClassLoaderSaver.LoadFromJsonString<CCEPColumn>("{\"ID\":\"column\",\"" + (index % 2 == 0 ? "DynamicConfiguration" : "CCEPConfiguration") + "\":{\"ID\":\"" + index + "\"}}"))));
            Assert.That(columns.Select(column => column.CCEPConfiguration.ID), Is.EqualTo(Enumerable.Range(0, 32).Select(index => index.ToString())));
        }

        [TestCase("Missing", "Current Name", null)]
        [TestCase("Old Name", "Missing", null)]
        [TestCase("Old Name", "Current Name", "Unknown")]
        public void InvalidAliasDeclarationsFailValidation(string oldName, string currentName, string migration)
        {
            // 'Missing' is itself a current member: historical names must never shadow it.
            Assert.Throws<InvalidOperationException>(() => SerializationAliasContractResolver.ValidateAliases(typeof(AliasFixture), new[] { new SerializationPropertyAlias(oldName, currentName, migration) }));
        }

        public class AliasFixture
        {
            [JsonProperty("Current Name")] public int Value { get; set; }
            [JsonProperty] public int Missing { get; }
        }
    }
}
