using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.Data.BIDS;
using NUnit.Framework;
using System;
using System.IO;

namespace HBP.Tests.Workflow
{
    public class SerializationTypeRegistryPlayModeTests
    {
        [Test]
        public void DataRegistry_IsInitializedBeforePlayModeTests()
        {
            Assert.That(SerializationTypeRegistry.IsRegistered(typeof(BIDSExportConfiguration)), Is.True);
        }

        [Test]
        public void LegacyAssemblyType_RoundTripsThroughGeneratedRegistry()
        {
            const string legacyJson = "{\"$type\":\"HBP.Core.Data.BoolTag, Assembly-CSharp\"," + "\"Name\":\"legacy-bool\",\"ID\":\"legacy-bool-id\"}";

            BoolTag legacyTag = ClassLoaderSaver.LoadFromJsonString<BoolTag>(legacyJson);
            Assert.That(legacyTag, Is.Not.Null);
            Assert.That(legacyTag.ID, Is.EqualTo("legacy-bool-id"));

            string path = Path.Combine(UnityEngine.Application.temporaryCachePath, $"hibop-registry-{Guid.NewGuid():N}.json");
            try
            {
                Assert.That(ClassLoaderSaver.SaveToJSon(legacyTag, path, true), Is.True);
                BoolTag loaded = ClassLoaderSaver.LoadFromJson<BoolTag>(path);
                Assert.That(loaded, Is.Not.Null);
                Assert.That(loaded.ID, Is.EqualTo(legacyTag.ID));
                Assert.That(loaded.GetType(), Is.EqualTo(typeof(BoolTag)));
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }
    }
}
