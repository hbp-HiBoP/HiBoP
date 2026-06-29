using System.IO;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Core.Preferences;
using HBP.Core.Tools;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;

namespace HBP.Tests.Serialization
{
    public class CoreDataSerializationTests
    {
        [Test]
        public void TagsAndTagValues_RoundTrip()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            BoolTag boolTag = new("flag-alpha", "tag-bool-alpha");
            StringTag stringTag = new("text-alpha", "tag-string-alpha");
            IntTag intTag = new("int-alpha", false, 0, 0, "tag-int-alpha");
            FloatTag floatTag = new("float-alpha", false, 0, 0, "tag-float-alpha");
            EnumTag enumTag = new("enum-alpha", new[] { "one", "two" }, "tag-enum-alpha");
            PersistentDataManager.Tags.SetGeneralTags(new BaseTag[] { boolTag, stringTag, intTag, floatTag, enumTag }, false);

            Patient patient = new(
                "patient-alpha",
                new BaseMesh[0],
                new MRI[0],
                new Site[0],
                new BaseTagValue[]
                {
                    new BoolTagValue(boolTag, true, "value-bool-alpha"),
                    new StringTagValue(stringTag, "value-alpha", "value-string-alpha"),
                    new IntTagValue(intTag, 42, "value-int-alpha"),
                    new FloatTagValue(floatTag, 4.2f, "value-float-alpha"),
                    new EnumTagValue(enumTag, 1, "value-enum-alpha")
                },
                "db-alpha",
                "patient-alpha");

            Patient loaded = RoundTrip(temp, patient, "patient.json");

            Assert.That(loaded.Tags, Has.Count.EqualTo(5));
            Assert.That(loaded.Tags[0], Is.TypeOf<BoolTagValue>());
            Assert.That(((BoolTagValue)loaded.Tags[0]).Value, Is.True);
            Assert.That(((StringTagValue)loaded.Tags[1]).Value, Is.EqualTo("value-alpha"));
            Assert.That(((IntTagValue)loaded.Tags[2]).Value, Is.EqualTo(42));
            Assert.That(((FloatTagValue)loaded.Tags[3]).Value, Is.EqualTo(4.2f).Within(0.001f));
            Assert.That(((EnumTagValue)loaded.Tags[4]).Value, Is.EqualTo(1));
        }

        [Test]
        public void Filters_RoundTrip()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            BoolTag boolTag = new("filter-flag-alpha", "filter-tag-bool-alpha");
            StringTag stringTag = new("filter-text-alpha", "filter-tag-string-alpha");
            PersistentDataManager.Tags.SetGeneralTags(new BaseTag[] { boolTag, stringTag }, false);

            AllFilterCondition source = new(
                new BaseFilterCondition[]
                {
                    new NameFilterCondition("patient-alpha", true, false, false, "filter-name-alpha"),
                    new DataStateFilterCondition(DataInfo.DataState.Ok, false, "filter-state-alpha"),
                    new DataTypeFilterCondition(typeof(IEEGDataInfo), false, "filter-type-alpha"),
                    new PatientTagFilterCondition(PatientTagFilterCondition.TargetType.Patient, boolTag, new BoolTagFilterValue { Value = true }, false, "filter-patient-tag-alpha"),
                    new SiteTagFilterCondition(SiteTagFilterCondition.TargetType.Site, stringTag, new StringTagFilterValue { Value = "plot-value-alpha" }, false, "filter-site-tag-alpha"),
                    new AnyFilterCondition(new BaseFilterCondition[] { new PatientNameFilterCondition("patient", false, false, false, "filter-patient-name-alpha") }, false, "filter-any-alpha")
                },
                false,
                "filter-all-alpha");

            AllFilterCondition loaded = RoundTrip(temp, source, "filters.json");

            Assert.That(loaded.Conditions, Has.Count.EqualTo(6));
            Assert.That(loaded.Conditions[0], Is.TypeOf<NameFilterCondition>());
            Assert.That(loaded.Conditions[3], Is.TypeOf<PatientTagFilterCondition>());
            Assert.That(loaded.Conditions[4], Is.TypeOf<SiteTagFilterCondition>());
            Assert.That(loaded.Conditions[5], Is.TypeOf<AnyFilterCondition>());
        }

        [Test]
        public void ProtocolDatasetAndVisualization_RoundTrip()
        {
            using TempDirectoryScope temp = new();
            using ApplicationStateTestScope appState = new(temp.Path);
            using PersistentDataTestScope persistentData = new(temp.Path);

            Project sourceProject = SyntheticProjectFactory.CreateCompleteProject();
            Project loadedProject = RoundTrip(temp, sourceProject, "project-root.json");

            ProjectSnapshotAssert.AreFunctionallyEquivalent(sourceProject, loadedProject);
        }

        private static T RoundTrip<T>(TempDirectoryScope temp, T source, string fileName) where T : new()
        {
            string path = temp.GetPath(fileName);
            Assert.That(ClassLoaderSaver.SaveToJSon(source, path, true), Is.True);
            return ClassLoaderSaver.LoadFromJson<T>(path);
        }
    }
}
