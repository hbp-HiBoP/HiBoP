using HBP.Core.Data;
using HBP.Core.Database;
using HBP.Core.Preferences;
using NUnit.Framework;
using System;
using System.IO;
using System.Linq;

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
            Type expected = TagInferenceService.Infer("tag", values, TagParsingPolicy.Default).GetType();

            Assert.That(TagInferenceService.Infer("tag", values.Reverse(), TagParsingPolicy.Default).GetType(), Is.EqualTo(expected));
        }

        [Test]
        public void RawFactory_UsesTheSameCustomBooleanPolicyAsInference()
        {
            TagParsingPolicy policy = new(new[] { "oui" }, new[] { "non" }, new[] { "inconnu" });
            BaseTag tag = TagInferenceService.Infer("consent", new[] { "oui", "non", "inconnu" }, policy);

            RawTagValueResult trueResult = RawTagValueFactory.TryCreate(tag, "oui", policy);
            RawTagValueResult falseResult = RawTagValueFactory.TryCreate(tag, "non", policy);
            RawTagValueResult ignoredResult = RawTagValueFactory.TryCreate(tag, "inconnu", policy);

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
            RawTagValueResult result = RawTagValueFactory.TryCreate(new StringTag("label"), "  original text  ", TagParsingPolicy.Default);

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
            BaseTag existing = tags.SitesTags.Single();
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
            TagParsingPolicy policy = preferences.CreatePolicy();

            Assert.That(policy.TrueValues, Is.EqualTo(new[] { "yes" }));
            Assert.Throws<ArgumentException>(() => new TagImportPreferences(new[] { "yes" }, new[] { "no" }, new[] { "YES" }));
        }

        [Test]
        public void Preferences_CloneDoesNotShareTokenLists()
        {
            TagImportPreferences source = new();
            TagImportPreferences clone = (TagImportPreferences)source.Clone();

            clone.TrueValues.Add("custom");

            Assert.That(source.TrueValues, Does.Not.Contain("custom"));
        }

        [Test]
        public void Scanner_AggregatesAllReferencesBeforeCreatingTags()
        {
            string root = Path.Combine(Path.GetTempPath(), "hibop-tag-inference-" + Guid.NewGuid().ToString("N"));
            string firstPath = Path.Combine(root, "first");
            string secondPath = Path.Combine(root, "second");
            try
            {
                Directory.CreateDirectory(firstPath);
                Directory.CreateDirectory(secondPath);
                File.WriteAllText(Path.Combine(firstPath, "patients.csv"), "patient,score\np1,1\np2,2\n");
                File.WriteAllText(Path.Combine(secondPath, "patients.csv"), "patient,score\np3,unknown\n");
                DatabaseReference first = new("first", DatabaseType.Tags, firstPath, new TagsDatabaseParameters(), DateTime.MinValue, "first-reference");
                DatabaseReference second = new("second", DatabaseType.Tags, secondPath, new TagsDatabaseParameters(), DateTime.MinValue, "second-reference");

                TagImportObservations observations = TagImportScanner.Scan(new[] { second, first });
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
        public void MaterializationOnlyMode_NeverCreatesMissingTags()
        {
            string path = Path.Combine(Path.GetTempPath(), "hibop-tag-materialization-" + Guid.NewGuid().ToString("N") + ".csv");
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

            TagImportDraft draft = TagImportDraft.Create(canonical, observations, TagParsingPolicy.Default);

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
            TagImportDraft draft = TagImportDraft.Create(canonical, observations, TagParsingPolicy.Default);
            EnumTag preparedEnum = (EnumTag)draft.PreparedTags.PatientsTags.Single();
            BaseTagValue preparedValue = draft.Context.TryCreate(TagCategory.Patient, preparedEnum, "new-value", "source.tsv", "patient-1").Value;
            Patient patient = new("patient-1", Array.Empty<BaseMesh>(), Array.Empty<MRI>(), Array.Empty<Site>(), new[] { preparedValue }, string.Empty);

            TagImportCommit commit = draft.Commit(canonical);
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
            TagImportDraft draft = TagImportDraft.Create(canonical, new TagImportObservations(), TagParsingPolicy.Default);

            RawTagValueResult incompatible = draft.Context.TryCreate(TagCategory.Patient, bounded, "42", "participants.tsv", "sub-01");
            RawTagValueResult ignored = draft.Context.TryCreate(TagCategory.Patient, bounded, "n/a", "participants.tsv", "sub-01");

            Assert.That(incompatible.Status, Is.EqualTo(RawTagValueStatus.Incompatible));
            Assert.That(ignored.Status, Is.EqualTo(RawTagValueStatus.Ignored));
            Assert.That(draft.Diagnostics.IncompatibleValues.Single().TagID, Is.EqualTo("score-id"));
            Assert.That(draft.Diagnostics.IncompatibleValues.Single().Source, Is.EqualTo("participants.tsv"));
            Assert.That(draft.Diagnostics.IncompatibleValues.Single().Owner, Is.EqualTo("sub-01"));
            Assert.That(draft.Diagnostics.IgnoredValues.Single().RawValue, Is.EqualTo("n/a"));
        }

        [Test]
        public void Draft_RejectsCommitWhenDefinitionChangedInPlace()
        {
            IntTag canonicalTag = new("score", true, 0, 10, "score-id");
            TagCollection canonical = new(Array.Empty<BaseTag>(), new BaseTag[] { canonicalTag }, Array.Empty<BaseTag>());
            TagImportDraft draft = TagImportDraft.Create(canonical, new TagImportObservations(), TagParsingPolicy.Default);

            canonicalTag.Max = 20;

            Assert.Throws<InvalidOperationException>(() => draft.Commit(canonical));
        }

        [Test]
        public void Observations_ChooseTagNameDeterministicallyAcrossCaseVariants()
        {
            TagImportObservations observations = new();
            observations.AddPatientValue("score", "1");
            observations.AddPatientValue("Score", "2");

            TagImportDraft draft = TagImportDraft.Create(new TagCollection(), observations, TagParsingPolicy.Default);

            Assert.That(draft.PreparedTags.PatientsTags.Single().Name, Is.EqualTo("Score"));
        }
    }
}
