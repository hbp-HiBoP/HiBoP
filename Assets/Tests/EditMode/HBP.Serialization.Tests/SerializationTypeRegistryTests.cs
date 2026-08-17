using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.Data.BIDS;
using HBP.Data.Tools;
using HBP.Dev;
using Newtonsoft.Json;
using NUnit.Framework;
using System;
using System.Linq;

namespace HBP.Tests.Serialization
{
    public class SerializationTypeRegistryTests
    {
        [SetUp]
        public void RegisterDataTypes()
        {
            GeneratedDataSerializationTypes.Register();
        }

        [Test]
        public void GeneratedSources_AreUpToDate()
        {
            Assert.That(SerializationTypeRegistryGenerator.TryValidate(out string error), Is.True, error);
        }

        [Test]
        public void EveryCurrentConcreteJsonType_IsRegistered()
        {
            Type[] missingTypes = new[]
            {
                typeof(BaseData).Assembly,
                typeof(BIDSExportConfiguration).Assembly
            }.Distinct().SelectMany(assembly => assembly.GetTypes()).Where(type => type.IsClass && !type.IsAbstract).Where(type => type.GetCustomAttributes(typeof(JsonObjectAttribute), true).Length > 0).Where(type => !SerializationTypeRegistry.IsRegistered(type)).OrderBy(type => type.FullName).ToArray();

            Assert.That(missingTypes, Is.Empty, $"Missing generated registry entries: {string.Join(", ", missingTypes.Select(type => type.FullName))}");
        }

        [TestCase("HBP.Data.Preferences.UserPreferences", "HBP.Core.Preferences.UserPreferences")]
        [TestCase("HBP.Data.Database.GlobalDatabaseSettings", "HBP.Core.Database.GlobalDatabaseSettings")]
        [TestCase("HBP.Core.Data.BoolTag", "HBP.Core.Data.BoolTag")]
        public void HistoricalAndCurrentNames_ResolveFromGeneratedRegistry(string serializedName, string expectedTypeName)
        {
            Type expectedType = new[]
            {
                typeof(BaseData).Assembly,
                typeof(BIDSExportConfiguration).Assembly
            }.Distinct().SelectMany(assembly => assembly.GetTypes()).Single(type => type.FullName == expectedTypeName);

            Assert.That(SerializationTypeRegistry.TryResolve(serializedName, out Type resolvedType), Is.True);
            Assert.That(resolvedType, Is.EqualTo(expectedType));
        }
    }
}
