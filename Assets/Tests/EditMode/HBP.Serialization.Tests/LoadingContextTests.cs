using System;
using System.Collections.Generic;
using HBP.Core.Data;
using HBP.Core.Errors;
using HBP.Core.Tools;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;

namespace HBP.Tests.Serialization
{
    public class LoadingContextTests
    {
        [Test]
        public void ResolveProject_BindsEveryReferenceToTheCanonicalScopedInstance()
        {
            using TempDirectoryScope temp = new();
            using PersistentDataTestScope persistentData = new(temp.Path);
            StringTag canonicalTag = new("canonical", "loading-context-tag");
            StringTag placeholderTag = new("placeholder", canonicalTag.ID);
            Patient canonicalPatient = CreatePatient("project patient", "loading-context-patient", new StringTagValue(placeholderTag, "value", "loading-context-value"));
            Patient placeholderPatient = CreatePatient("placeholder", canonicalPatient.ID);
            Bloc canonicalBloc = new("bloc", 0, string.Empty, string.Empty, Array.Empty<SubBloc>(), "loading-context-bloc");
            Protocol canonicalProtocol = new("protocol", new[] { canonicalBloc }, "loading-context-protocol");
            Protocol placeholderProtocol = new("placeholder", new[]
            {
                new Bloc("placeholder", 0, string.Empty, string.Empty, Array.Empty<SubBloc>(), canonicalBloc.ID)
            }, canonicalProtocol.ID);
            PatientDataInfo dataInfo = new("data", placeholderProtocol, new HBP.Core.Data.Container.Elan(), Array.Empty<Error>(), Array.Empty<Warning>(), placeholderPatient, string.Empty, "loading-context-data");
            Dataset dataset = new("dataset", placeholderProtocol, new DataInfo[] { dataInfo }, "loading-context-dataset");
            Dataset placeholderDataset = new("placeholder", placeholderProtocol, Array.Empty<DataInfo>(), dataset.ID);
            Group group = new("group", new[] { placeholderPatient }, "loading-context-group");
            IEEGColumn column = new("column", new BaseConfiguration(), placeholderDataset, "data", placeholderProtocol.Blocs[0], new DynamicConfiguration(), "loading-context-column");
            Visualization visualization = new("visualization", new[] { placeholderPatient }, new Column[] { column }, new VisualizationConfiguration(), "loading-context-visualization");

            LoadingContext context = new(new BaseTag[] { canonicalTag }, new[] { canonicalProtocol }, new[] { canonicalPatient }, new[] { dataset });

            context.ResolveProject(new[] { canonicalPatient }, new[] { group }, new[] { dataset }, new[] { visualization });

            Assert.That(canonicalPatient.Tags[0].Tag, Is.SameAs(canonicalTag));
            Assert.That(dataset.Protocol, Is.SameAs(canonicalProtocol));
            Assert.That(dataInfo.Protocol, Is.SameAs(canonicalProtocol));
            Assert.That(dataInfo.Patient, Is.SameAs(canonicalPatient));
            Assert.That(group.Patients, Is.EqualTo(new[] { canonicalPatient }));
            Assert.That(visualization.Patients, Is.EqualTo(new[] { canonicalPatient }));
            Assert.That(column.Dataset, Is.SameAs(dataset));
            Assert.That(column.Bloc, Is.SameAs(canonicalBloc));
        }

        [Test]
        public void Constructor_RejectsDuplicateCanonicalIds()
        {
            Patient first = CreatePatient("first", "loading-context-duplicate");
            Patient second = CreatePatient("second", "loading-context-duplicate");

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => new LoadingContext(Array.Empty<BaseTag>(), Array.Empty<Protocol>(), new[] { first, second }));

            Assert.That(exception.Message, Does.Contain("patient"));
            Assert.That(exception.Message, Does.Contain(first.ID));
        }

        [Test]
        public void ResolveProject_GroupsMissingReferencesInOneExplicitException()
        {
            Patient missingPatient = CreatePatient("missing", "loading-context-missing-patient");
            Protocol missingProtocol = new("missing", Array.Empty<Bloc>(), "loading-context-missing-protocol");
            Dataset dataset = new("dataset", missingProtocol, Array.Empty<DataInfo>(), "loading-context-invalid-dataset");
            Dataset missingDataset = new("missing", missingProtocol, Array.Empty<DataInfo>(), "loading-context-missing-dataset");
            Group group = new("group", new[] { missingPatient }, "loading-context-invalid-group");
            StaticColumn column = new("column", new BaseConfiguration(), missingDataset, "data", new StaticConfiguration(), "loading-context-invalid-column");
            Visualization visualization = new("visualization", Array.Empty<Patient>(), new Column[] { column }, new VisualizationConfiguration(), "loading-context-invalid-visualization");
            LoadingContext context = new(Array.Empty<BaseTag>(), Array.Empty<Protocol>(), Array.Empty<Patient>(), new[] { dataset });

            ReferenceResolutionException exception = Assert.Throws<ReferenceResolutionException>(() => context.ResolveProject(Array.Empty<Patient>(), new[] { group }, new[] { dataset }, new[] { visualization }));

            Assert.That(exception.Issues, Has.Count.EqualTo(3));
            Assert.That(exception.Message, Does.Contain(missingProtocol.ID));
            Assert.That(exception.Message, Does.Contain(missingPatient.ID));
            Assert.That(exception.Message, Does.Contain(missingDataset.ID));
        }

        [Test]
        public void Deserialization_KeepsIdsRawUntilAnExplicitContextResolvesThem()
        {
            using TempDirectoryScope temp = new();
            StringTag tag = new("tag", "loading-context-raw-tag");
            StringTagValue source = new(tag, "value", "loading-context-raw-value");
            string path = temp.GetPath("tag-value.json");

            Assert.That(ClassLoaderSaver.SaveToJSon(source, path, true), Is.True);
            StringTagValue loaded = ClassLoaderSaver.LoadFromJson<StringTagValue>(path);

            Assert.That(loaded.Tag, Is.Null);

            Patient patient = CreatePatient("patient", "loading-context-raw-patient", loaded);
            LoadingContext context = new(new BaseTag[] { tag }, Array.Empty<Protocol>(), new[] { patient });
            context.ResolveDatabase(new[] { patient }, Array.Empty<DataInfo>());

            Assert.That(loaded.Tag, Is.SameAs(tag));
        }

        [Test]
        public void ResolveAuxiliaryReferences_BindsFilterTagsAndPatientConfigurations()
        {
            using TempDirectoryScope temp = new();
            StringTag canonicalTag = new("canonical", "loading-context-filter-tag");
            StringTag placeholderTag = new("placeholder", canonicalTag.ID);
            Patient canonicalPatient = CreatePatient("canonical", "loading-context-configuration-patient");
            Patient placeholderPatient = CreatePatient("placeholder", canonicalPatient.ID);
            PatientTagFilterCondition condition = new(PatientTagFilterCondition.TargetType.Patient, placeholderTag, new StringTagFilterValue { Value = "value" }, false, "loading-context-filter-condition");
            FilterConditionsPresetCollection presets = new();
            presets.AddPreset(new FilterConditionsPreset("preset", new BaseFilterCondition[] { condition }, "loading-context-filter-preset"), typeof(Patient), false);
            string presetPath = temp.GetPath("loading-context-filter-presets.json");
            Assert.That(ClassLoaderSaver.SaveToJSon(presets, presetPath, true), Is.True);
            FilterConditionsPresetCollection loadedPresets = ClassLoaderSaver.LoadFromJson<FilterConditionsPresetCollection>(presetPath);
            PatientTagFilterCondition loadedCondition = (PatientTagFilterCondition)loadedPresets.GetPresets(typeof(Patient))[0].Conditions[0];
            PatientConfiguration configuration = new(new Dictionary<string, ElectrodeConfiguration>(), UnityEngine.Color.white, placeholderPatient);
            LoadingContext context = new(new BaseTag[] { canonicalTag }, Array.Empty<Protocol>(), new[] { canonicalPatient });

            context.ResolveFilterConditions(loadedPresets);
            context.ResolvePatientConfiguration(configuration);

            Assert.That(loadedCondition.Tag, Is.SameAs(canonicalTag));
            Assert.That(configuration.Patient, Is.SameAs(canonicalPatient));
        }

        private static Patient CreatePatient(string name, string id, params BaseTagValue[] tags)
        {
            return new Patient(name, Array.Empty<BaseMesh>(), Array.Empty<MRI>(), Array.Empty<Site>(), tags, string.Empty, id);
        }
    }
}
