using System;
using System.IO;
using System.Text.RegularExpressions;
using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.Tests.Serialization.Helpers;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace HBP.Tests.Serialization
{
    public class EnumTagReferenceTests
    {
        [Test]
        public void ModernValue_ReorderedDefinitionRepairsIndexFromStringValue()
        {
            using TempDirectoryScope temp = new();
            EnumTag sourceTag = new("enum", new[] { "one", "two" }, "enum-reference-value-tag");
            EnumTagValue source = new(sourceTag, 1, "enum-reference-value");
            string path = temp.GetPath("modern-enum-value.json");

            Assert.That(ClassLoaderSaver.SaveToJSon(source, path, true), Is.True);
            Assert.That(File.ReadAllText(path), Does.Contain("\"StringValue\": \"two\""));
            EnumTagValue loaded = ClassLoaderSaver.LoadFromJson<EnumTagValue>(path);
            EnumTag canonicalTag = new("enum", new[] { "two", "one" }, sourceTag.ID);
            Patient patient = CreatePatient(loaded);
            LoadingContext context = new(new BaseTag[] { canonicalTag }, Array.Empty<Protocol>(), new[] { patient });

            context.ResolveDatabase(new[] { patient }, Array.Empty<DataInfo>());

            Assert.That(loaded.Tag, Is.SameAs(canonicalTag));
            Assert.That(loaded.Value, Is.Zero);
            Assert.That(loaded.StringValue, Is.EqualTo("two"));
            Assert.That(loaded.Reference.Index, Is.Zero);
            Assert.That(loaded.Reference.Value, Is.EqualTo("two"));
            Assert.That(context.LegacyEnumWarnings, Is.Empty);
        }

        [Test]
        public void LegacyValue_UsesCurrentIndexAndProducesGroupedWarning()
        {
            using TempDirectoryScope temp = new();
            EnumTag tag = new("enum", new[] { "one", "two" }, "enum-reference-legacy-value-tag");
            string path = temp.GetPath("legacy-enum-value.json");
            Assert.That(ClassLoaderSaver.SaveToJSon(new EnumTagValue(tag, 1, "enum-reference-legacy-value"), path, true), Is.True);
            RemoveProperty(path, "StringValue");
            EnumTagValue loaded = ClassLoaderSaver.LoadFromJson<EnumTagValue>(path);
            EnumTagValue secondLoaded = (EnumTagValue)loaded.Clone();
            secondLoaded.ID = "enum-reference-second-legacy-value";
            Patient patient = CreatePatient(loaded, secondLoaded);
            LoadingContext context = new(new BaseTag[] { tag }, Array.Empty<Protocol>(), new[] { patient });
            LogAssert.Expect(LogType.Warning, new Regex("Legacy enum references were resolved from their current indices.*2 value\\(s\\), 0 filter\\(s\\)"));

            context.ResolveDatabase(new[] { patient }, Array.Empty<DataInfo>());

            Assert.That(loaded.Value, Is.EqualTo(1));
            Assert.That(loaded.StringValue, Is.EqualTo("two"));
            Assert.That(context.LegacyEnumWarnings, Has.Count.EqualTo(1));
            Assert.That(context.LegacyEnumWarnings[0].ValueCount, Is.EqualTo(2));
            Assert.That(context.LegacyEnumWarnings[0].FilterCount, Is.Zero);
        }

        [Test]
        public void ModernFilter_ReorderedDefinitionRepairsIndexFromStringValue()
        {
            using TempDirectoryScope temp = new();
            EnumTag sourceTag = new("enum", new[] { "one", "two" }, "enum-reference-filter-tag");
            EnumTagFilterValue sourceValue = new() { Value = 1 };
            FilterConditionsPresetCollection source = CreatePresets(new PatientTagFilterCondition(PatientTagFilterCondition.TargetType.Patient, sourceTag, sourceValue, false));
            string path = temp.GetPath("modern-enum-filter.json");

            Assert.That(ClassLoaderSaver.SaveToJSon(source, path, true), Is.True);
            FilterConditionsPresetCollection loaded = ClassLoaderSaver.LoadFromJson<FilterConditionsPresetCollection>(path);
            PatientTagFilterCondition loadedCondition = GetCondition(loaded);
            EnumTag canonicalTag = new("enum", new[] { "two", "one" }, sourceTag.ID);
            LoadingContext context = new(new BaseTag[] { canonicalTag }, Array.Empty<Protocol>());

            context.ResolveFilterConditions(loaded);

            EnumTagFilterValue loadedValue = (EnumTagFilterValue)loadedCondition.Value;
            Assert.That(loadedCondition.Tag, Is.SameAs(canonicalTag));
            Assert.That(loadedValue.Value, Is.Zero);
            Assert.That(loadedValue.StringValue, Is.EqualTo("two"));
            Assert.That(context.LegacyEnumWarnings, Is.Empty);
        }

        [Test]
        public void LegacyFilter_UsesCurrentIndexAndProducesGroupedWarning()
        {
            using TempDirectoryScope temp = new();
            EnumTag tag = new("enum", new[] { "one", "two" }, "enum-reference-legacy-filter-tag");
            FilterConditionsPresetCollection source = CreatePresets(new PatientTagFilterCondition(PatientTagFilterCondition.TargetType.Patient, tag, new EnumTagFilterValue { Value = 1 }, false));
            string path = temp.GetPath("legacy-enum-filter.json");
            Assert.That(ClassLoaderSaver.SaveToJSon(source, path, true), Is.True);
            JObject json = JObject.Parse(File.ReadAllText(path));
            JObject serializedFilterValue = (JObject)json.SelectToken("m_PresetsByType.*[0].Conditions[0].Value");
            Assert.That(serializedFilterValue, Is.Not.Null);
            serializedFilterValue.Remove("StringValue");
            File.WriteAllText(path, json.ToString());
            FilterConditionsPresetCollection loaded = ClassLoaderSaver.LoadFromJson<FilterConditionsPresetCollection>(path);
            LoadingContext context = new(new BaseTag[] { tag }, Array.Empty<Protocol>());
            LogAssert.Expect(LogType.Warning, new Regex("Legacy enum references were resolved from their current indices.*0 value\\(s\\), 1 filter\\(s\\)"));

            context.ResolveFilterConditions(loaded);

            EnumTagFilterValue loadedValue = (EnumTagFilterValue)GetCondition(loaded).Value;
            Assert.That(loadedValue.Value, Is.EqualTo(1));
            Assert.That(loadedValue.StringValue, Is.EqualTo("two"));
            Assert.That(context.LegacyEnumWarnings, Has.Count.EqualTo(1));
            Assert.That(context.LegacyEnumWarnings[0].ValueCount, Is.Zero);
            Assert.That(context.LegacyEnumWarnings[0].FilterCount, Is.EqualTo(1));
        }

        [Test]
        public void ModernValue_MissingStringValueInCanonicalDefinitionIsRejected()
        {
            using TempDirectoryScope temp = new();
            EnumTag sourceTag = new("enum", new[] { "one", "two" }, "enum-reference-missing-tag");
            string path = temp.GetPath("missing-modern-enum-value.json");
            Assert.That(ClassLoaderSaver.SaveToJSon(new EnumTagValue(sourceTag, 1), path, true), Is.True);
            EnumTagValue loaded = ClassLoaderSaver.LoadFromJson<EnumTagValue>(path);
            EnumTag canonicalTag = new("enum", new[] { "one" }, sourceTag.ID);
            Patient patient = CreatePatient(loaded);
            LoadingContext context = new(new BaseTag[] { canonicalTag }, Array.Empty<Protocol>(), new[] { patient });

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => context.ResolveDatabase(new[] { patient }, Array.Empty<DataInfo>()));

            Assert.That(exception.Message, Does.Contain("two"));
            Assert.That(loaded.Tag, Is.Null);
        }

        [Test]
        public void CreateValue_AddsMissingOptionsOnlyAtTheEnd()
        {
            EnumTag tag = new("enum", new[] { "one" }, "enum-reference-append-tag");

            EnumTagValue added = (EnumTagValue)tag.CreateValue("two");
            EnumTagValue existing = (EnumTagValue)tag.CreateValue("one");

            Assert.That(tag.Values, Is.EqualTo(new[] { "one", "two" }));
            Assert.That(added.Reference.Index, Is.EqualTo(1));
            Assert.That(added.Reference.Value, Is.EqualTo("two"));
            Assert.That(existing.Reference.Index, Is.Zero);
            Assert.That(tag.Values, Has.Length.EqualTo(2));
            Assert.That(tag.CreateValue(string.Empty), Is.Null);
            Assert.That(tag.Values, Has.Length.EqualTo(2));
        }

        [Test]
        public void Values_RejectDuplicateLabelsWithOrdinalComparison()
        {
            Assert.Throws<ArgumentException>(() => new EnumTag("enum", new[] { "same", "same" }));
            Assert.DoesNotThrow(() => new EnumTag("enum", new[] { "same", "Same" }));
        }

        [Test]
        public void PresetCollectionClone_PreservesCurrentEnumFiltersForValidation()
        {
            EnumTag tag = new("enum", new[] { "one", "two" }, "enum-reference-current-filter-tag");
            PatientTagFilterCondition condition = new(PatientTagFilterCondition.TargetType.Patient, tag, new EnumTagFilterValue { Value = 1 }, false);
            FilterConditionsPreset currentPreset = new("current", new BaseFilterCondition[] { condition });
            FilterConditionsPresetCollection source = new();
            source.SetCurrentPreset(currentPreset, typeof(Patient), false);

            FilterConditionsPresetCollection clone = (FilterConditionsPresetCollection)source.Clone();
            PatientTagFilterCondition clonedCondition = (PatientTagFilterCondition)clone.GetCurrentPreset(typeof(Patient)).Conditions[0];

            Assert.That(clonedCondition, Is.Not.SameAs(condition));
            Assert.That(((EnumTagFilterValue)clonedCondition.Value).StringValue, Is.EqualTo("two"));
        }

        private static Patient CreatePatient(params BaseTagValue[] values)
        {
            return new Patient("patient", Array.Empty<BaseMesh>(), Array.Empty<MRI>(), Array.Empty<Site>(), values, string.Empty, "enum-reference-patient");
        }

        private static FilterConditionsPresetCollection CreatePresets(PatientTagFilterCondition condition)
        {
            FilterConditionsPresetCollection presets = new();
            presets.AddPreset(new FilterConditionsPreset("preset", new BaseFilterCondition[] { condition }), typeof(Patient), false);
            return presets;
        }

        private static PatientTagFilterCondition GetCondition(FilterConditionsPresetCollection presets)
        {
            return (PatientTagFilterCondition)presets.GetPresets(typeof(Patient))[0].Conditions[0];
        }

        private static void RemoveProperty(string path, string propertyName)
        {
            JObject json = JObject.Parse(File.ReadAllText(path));
            Assert.That(json.Remove(propertyName), Is.True);
            File.WriteAllText(path, json.ToString());
        }
    }
}
