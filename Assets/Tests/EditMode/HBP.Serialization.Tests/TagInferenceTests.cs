using System;
using System.IO;
using System.Linq;
using HBP.Core.Data;
using HBP.Core.Database;
using HBP.Core.Preferences;
using NUnit.Framework;

namespace HBP.Tests.Serialization
{
    public class TagInferenceTests
    {
        [TestCase(new[] { "true", "false", "yes", "no" }, typeof(BoolTag))]
        [TestCase(new[] { "0", "0" }, typeof(IntTag))]
        [TestCase(new[] { "1", "-2" }, typeof(IntTag))]
        [TestCase(new[] { "1", "1.5" }, typeof(FloatTag))]
        [TestCase(new[] { "1", "unknown" }, typeof(StringTag))]
        [TestCase(new[] { "n/a", "2", "3" }, typeof(IntTag))]
        [TestCase(new[] { "n/a", "not found" }, typeof(StringTag))]
        public void Infer_UsesUniformPolicy(string[] values, Type expectedType)
        {
            Assert.That(TagInferenceService.Infer("tag", values, TagParsingPolicy.Default), Is.TypeOf(expectedType));
        }

        [Test]
        public void Infer_IsIndependentOfObservationOrder()
        {
            string[] values = { "2", "1.5", "n/a", "3" };
            var expected = TagInferenceService.Infer("tag", values, TagParsingPolicy.Default).GetType();

            Assert.That(TagInferenceService.Infer("tag", values.Reverse(), TagParsingPolicy.Default).GetType(), Is.EqualTo(expected));
        }

        [Test]
        public void RawFactory_UsesTheSameCustomBooleanPolicyAsInference()
        {
            TagParsingPolicy policy = new(new[] { "oui" }, new[] { "non" }, new[] { "inconnu" });
            var tag = TagInferenceService.Infer("consent", new[] { "oui", "non", "inconnu" }, policy);

            var trueResult = RawTagValueFactory.TryCreate(tag, "oui", policy);
            var falseResult = RawTagValueFactory.TryCreate(tag, "non", policy);
            var ignoredResult = RawTagValueFactory.TryCreate(tag, "inconnu", policy);

            Assert.That(tag, Is.TypeOf<BoolTag>());
            Assert.That(trueResult.Status, Is.EqualTo(RawTagValueStatus.Success));
            Assert.That(trueResult.Value, Is.TypeOf<BoolTagValue>());
            Assert.That(trueResult.Value.Value, Is.True);
            Assert.That(falseResult.Value.Value, Is.False);
            Assert.That(ignoredResult.Status, Is.EqualTo(RawTagValueStatus.Ignored));
        }

        [Test]
        public void RawFactory_PreservesNonIgnoredStringText()
        {
            var result = RawTagValueFactory.TryCreate(new StringTag("label"), "  original text  ", TagParsingPolicy.Default);

            Assert.That(result.Status, Is.EqualTo(RawTagValueStatus.Success));
            Assert.That(result.Value.Value, Is.EqualTo("  original text  "));
        }

        [Test]
        public void Observations_AggregateBeforeCreation_AndNeverRetypeExistingTags()
        {
            TagImportObservations observations = new();
            observations.AddPatientValue("score", "1");
            observations.AddPatientValue("score", "unknown");
            observations.AddSiteValue("score", "1");
            TagCollection tags = new(Array.Empty<BaseTag>(), Array.Empty<BaseTag>(), new BaseTag[] { new IntTag("existing", "existing-tag") });
            var existing = tags.SitesTags.Single();
            observations.AddSiteValue("existing", "not-an-int");

            observations.CreateMissingTags(tags, TagParsingPolicy.Default);

            Assert.That(tags.PatientsTags.Single(tag => tag.Name == "score"), Is.TypeOf<StringTag>());
            Assert.That(tags.SitesTags.Single(tag => tag.Name == "score"), Is.TypeOf<IntTag>());
            Assert.That(tags.SitesTags.Single(tag => tag.Name == "existing"), Is.SameAs(existing));
        }

        [Test]
        public void Preferences_NormalizeAndRejectOverlappingTokens()
        {
            TagImportPreferences preferences = new(new[] { " yes ", "YES" }, new[] { " no " }, new[] { " n/a " });
            var policy = preferences.CreatePolicy();

            Assert.That(policy.TrueValues, Is.EqualTo(new[] { "yes" }));
            Assert.Throws<ArgumentException>(() => new TagImportPreferences(new[] { "yes" }, new[] { "no" }, new[] { "YES" }));
        }

        [Test]
        public void Preferences_CloneDoesNotShareTokenLists()
        {
            TagImportPreferences source = new();
            var clone = (TagImportPreferences)source.Clone();

            clone.TrueValues.Add("custom");

            Assert.That(source.TrueValues, Does.Not.Contain("custom"));
        }

        [Test]
        public void Scanner_AggregatesAllReferencesBeforeCreatingTags()
        {
            var root = Path.Combine(Path.GetTempPath(), "hibop-tag-inference-" + Guid.NewGuid().ToString("N"));
            var firstPath = Path.Combine(root, "first");
            var secondPath = Path.Combine(root, "second");
            try
            {
                Directory.CreateDirectory(firstPath);
                Directory.CreateDirectory(secondPath);
                File.WriteAllText(Path.Combine(firstPath, "patients.csv"), "patient,score\np1,1\np2,2\n");
                File.WriteAllText(Path.Combine(secondPath, "patients.csv"), "patient,score\np3,unknown\n");
                DatabaseReference first = new("first", DatabaseType.Tags, firstPath, new TagsDatabaseParameters(), DateTime.MinValue, "first-reference");
                DatabaseReference second = new("second", DatabaseType.Tags, secondPath, new TagsDatabaseParameters(), DateTime.MinValue, "second-reference");

                var observations = TagImportScanner.Scan(new[] { second, first });
                TagCollection tags = new();
                observations.CreateMissingTags(tags, TagParsingPolicy.Default);

                Assert.That(tags.PatientsTags.Single(tag => tag.Name == "score"), Is.TypeOf<StringTag>());
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [Test]
        public void TagsDatabaseScannerAndMaterialization_SupportIntranatSiteCsv()
        {
            var root = Path.Combine(Path.GetTempPath(), "hibop-intranat-tag-csv-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(root);
                var csvPath = Path.Combine(root, "patient-1.csv");
                File.WriteAllLines(csvPath, new[]
                {
                    "Contacts Positions",
                    "Use of MNI Template\tMarsAtlas\tFalse",
                    " contact \tregion\tquality",
                    "A01\ttemporal\tgood",
                    "B02\tfrontal\tn/a"
                });
                DatabaseReference reference = new("tags", DatabaseType.Tags, root, new TagsDatabaseParameters(), DateTime.MinValue, "intranat-tags-reference");

                var observations = TagImportScanner.Scan(new[] { reference });
                var draft = TagImportDraft.Create(new TagCollection(), observations, TagParsingPolicy.Default);
                var tagsBySite = draft.PreparedTags.GenerateSiteTagsFromCSV(csvPath, TagParsingPolicy.Default, false, draft.Context);

                Assert.That(draft.PreparedTags.SitesTags.Select(tag => tag.Name), Is.EquivalentTo(new[] { "region", "quality" }));
                Assert.That(draft.PreparedTags.SitesTags.Select(tag => tag.Name), Does.Not.Contain("Contacts Positions"));
                Assert.That(tagsBySite.Keys, Is.EquivalentTo(new[] { "A01", "B02" }));
                Assert.That(tagsBySite["A01"].Select(tag => tag.Tag.Name), Is.EquivalentTo(new[] { "region", "quality" }));
                Assert.That(tagsBySite["B02"].Select(tag => tag.Tag.Name), Is.EquivalentTo(new[] { "region" }));
            }
            finally
            {
                if (Directory.Exists(root)) Directory.Delete(root, true);
            }
        }

        [Test]
        public void MaterializationOnlyMode_NeverCreatesMissingTags()
        {
            var path = Path.Combine(Path.GetTempPath(), "hibop-tag-materialization-" + Guid.NewGuid().ToString("N") + ".csv");
            try
            {
                File.WriteAllText(path, "patient,new-tag\np1,42\n");
                TagCollection tags = new();

                tags.GeneratePatientTagsFromCSV(path, TagParsingPolicy.Default, false);

                Assert.That(tags.AllTags, Is.Empty);
            }
            finally
            {
                if (File.Exists(path)) File.Delete(path);
            }
        }

        [Test]
        public void Draft_StagesNewTagsAndDeterministicEnumExtensionsWithoutMutatingCanonicalTags()
        {
            EnumTag canonicalEnum = new("status", new[] { "existing" }, "status-id");
            TagCollection canonical = new(Array.Empty<BaseTag>(), new BaseTag[] { canonicalEnum }, Array.Empty<BaseTag>());
            TagImportObservations observations = new();
            observations.AddPatientValue("status", "zeta");
            observations.AddPatientValue("status", "alpha");
            observations.AddPatientValue("status", "zeta");
            observations.AddPatientValue("score", "42");

            var draft = TagImportDraft.Create(canonical, observations, TagParsingPolicy.Default);

            Assert.That(canonicalEnum.Values, Is.EqualTo(new[] { "existing" }));
            Assert.That(canonical.PatientsTags.Count, Is.EqualTo(1));
            Assert.That(((EnumTag)draft.PreparedTags.PatientsTags.Single(tag => tag.ID == canonicalEnum.ID)).Values, Is.EqualTo(new[] { "existing", "alpha", "zeta" }));
            Assert.That(draft.PreparedTags.PatientsTags.Single(tag => tag.Name == "score"), Is.TypeOf<IntTag>());
            Assert.That(draft.Diagnostics.CreatedTags.Single().TagType, Is.EqualTo(nameof(IntTag)));
            Assert.That(draft.Diagnostics.EnumExtensions.Single().Values, Is.EqualTo(new[] { "alpha", "zeta" }));
        }

        [Test]
        public void Draft_CommitAndRebindUseCanonicalEnumAndRollbackRestoresSchema()
        {
            EnumTag canonicalEnum = new("status", new[] { "existing" }, "status-id");
            TagCollection canonical = new(Array.Empty<BaseTag>(), new BaseTag[] { canonicalEnum }, Array.Empty<BaseTag>());
            TagImportObservations observations = new();
            observations.AddPatientValue("status", "new-value");
            var draft = TagImportDraft.Create(canonical, observations, TagParsingPolicy.Default);
            var preparedEnum = (EnumTag)draft.PreparedTags.PatientsTags.Single();
            var preparedValue = draft.Context.TryCreate(TagCategory.Patient, preparedEnum, "new-value", "source.tsv", "patient-1").Value;
            Patient patient = new("patient-1", Array.Empty<BaseMesh>(), Array.Empty<MRI>(), Array.Empty<Site>(), new[] { preparedValue }, string.Empty);

            var commit = draft.Commit(canonical);
            new LoadingContext(canonical.AllTags, Array.Empty<Protocol>(), new[] { patient }).ResolveDatabase(new[] { patient }, Array.Empty<DataInfo>());

            Assert.That(canonicalEnum.Values, Is.EqualTo(new[] { "existing", "new-value" }));
            Assert.That(patient.Tags.Single().Tag, Is.SameAs(canonicalEnum));
            Assert.That(((EnumTagValue)patient.Tags.Single()).StringValue, Is.EqualTo("new-value"));

            commit.Rollback();
            Assert.That(canonicalEnum.Values, Is.EqualTo(new[] { "existing" }));
        }

        [Test]
        public void Draft_ReportsIncompatibleAndIgnoredValuesWithTheirOrigin()
        {
            IntTag bounded = new("score", true, 0, 10, "score-id");
            TagCollection canonical = new(Array.Empty<BaseTag>(), new BaseTag[] { bounded }, Array.Empty<BaseTag>());
            var draft = TagImportDraft.Create(canonical, new TagImportObservations(), TagParsingPolicy.Default);

            var incompatible = draft.Context.TryCreate(TagCategory.Patient, bounded, "42", "participants.tsv", "sub-01");
            draft.Context.TryCreate(TagCategory.Patient, bounded, "43", "other.tsv", "sub-02");
            var ignored = draft.Context.TryCreate(TagCategory.Patient, bounded, "n/a", "participants.tsv", "sub-01");

            Assert.That(incompatible.Status, Is.EqualTo(RawTagValueStatus.Incompatible));
            Assert.That(ignored.Status, Is.EqualTo(RawTagValueStatus.Ignored));
            Assert.That(draft.Diagnostics.IncompatibleValues, Has.Count.EqualTo(2));
            Assert.That(draft.Diagnostics.IncompatibleValues.Single(value => value.RawValue == "42").Source, Is.EqualTo("participants.tsv"));
            Assert.That(draft.Diagnostics.IncompatibleValues.Single(value => value.RawValue == "42").Owner, Is.EqualTo("sub-01"));
            Assert.That(draft.Diagnostics.IncompatibleValueSummaries.Single().TagID, Is.EqualTo("score-id"));
            Assert.That(draft.Diagnostics.IncompatibleValueSummaries.Single().Count, Is.EqualTo(2));
            Assert.That(draft.Diagnostics.IgnoredValues.Single().RawValue, Is.EqualTo("n/a"));
            Assert.That(draft.Diagnostics.IgnoredValueSummaries.Single().Count, Is.EqualTo(1));
        }

        [Test]
        public void Draft_RejectsCommitWhenDefinitionChangedInPlace()
        {
            IntTag canonicalTag = new("score", true, 0, 10, "score-id");
            TagCollection canonical = new(Array.Empty<BaseTag>(), new BaseTag[] { canonicalTag }, Array.Empty<BaseTag>());
            var draft = TagImportDraft.Create(canonical, new TagImportObservations(), TagParsingPolicy.Default);

            canonicalTag.Max = 20;

            Assert.Throws<InvalidOperationException>(() => draft.Commit(canonical));
        }

        [Test]
        public void Observations_ChooseTagNameDeterministicallyAcrossCaseVariants()
        {
            TagImportObservations observations = new();
            observations.AddPatientValue("score", "1");
            observations.AddPatientValue("Score", "2");

            var draft = TagImportDraft.Create(new TagCollection(), observations, TagParsingPolicy.Default);

            Assert.That(draft.PreparedTags.PatientsTags.Single().Name, Is.EqualTo("Score"));
        }
    }
}
