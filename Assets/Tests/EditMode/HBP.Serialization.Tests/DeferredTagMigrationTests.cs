using System;
using System.Linq;
using System.Reflection;
using HBP.Core.Data;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;

namespace HBP.Tests.Serialization
{
    public class DeferredTagMigrationTests
    {
        [Test]
        public void PlanCommitRollback_MigratesDetachedPatientAndSiteValuesWithoutEarlyMutation()
        {
            IntTag canonical = new("age", "deferred-age-tag");
            TagCollection tags = new(Array.Empty<BaseTag>(), new BaseTag[] { canonical }, Array.Empty<BaseTag>());
            StringTag serializedDefinition = new("age", canonical.ID);
            StringTagValue patientValue = new(serializedDefinition, "42", "deferred-patient-value");
            StringTagValue siteValue = new(serializedDefinition, "7", "deferred-site-value");
            Site site = new("site", Array.Empty<Coordinate>(), new BaseTagValue[] { siteValue }, "deferred-site");
            Patient patient = CreatePatient(new BaseTagValue[] { patientValue }, new[] { site });

            DeferredTagMigrationPlan plan = new DeferredTagMigrationService().Plan(DeferredTagMigrationScope.Project, tags, new[] { patient }, new FilterConditionsPresetCollection(), TagParsingPolicy.Default);

            Assert.That(plan.RequiresConfirmation, Is.True);
            Assert.That(plan.PatientValueCount, Is.EqualTo(1));
            Assert.That(plan.SiteValueCount, Is.EqualTo(1));
            Assert.That(patient.Tags.Single(), Is.SameAs(patientValue));
            Assert.That(site.Tags.Single(), Is.SameAs(siteValue));

            plan.Commit(DeferredTagMigrationDecision.Apply);

            Assert.That(patient.Tags.Single(), Is.TypeOf<IntTagValue>());
            Assert.That(((IntTagValue)patient.Tags.Single()).Value, Is.EqualTo(42));
            Assert.That(patient.Tags.Single().ID, Is.EqualTo(patientValue.ID));
            Assert.That(patient.Tags.Single().Tag, Is.SameAs(canonical));
            Assert.That(((IntTagValue)site.Tags.Single()).Value, Is.EqualTo(7));
            Assert.That(site.Tags.Single().Tag, Is.SameAs(canonical));

            plan.Rollback();

            Assert.That(patient.Tags.Single(), Is.SameAs(patientValue));
            Assert.That(site.Tags.Single(), Is.SameAs(siteValue));
        }

        [Test]
        public void MissingTag_RequiresExplicitDestructiveDecisionAndRemovesOnlyIncompatibleValue()
        {
            StringTag valid = new("valid", "deferred-valid-tag");
            TagCollection tags = new(Array.Empty<BaseTag>(), new BaseTag[] { valid }, Array.Empty<BaseTag>());
            StringTagValue validValue = new(valid, "kept", "deferred-valid-value");
            StringTagValue missingValue = new(new StringTag("missing", "deferred-missing-tag"), "removed", "deferred-missing-value");
            Patient patient = CreatePatient(new BaseTagValue[] { validValue, missingValue });

            DeferredTagMigrationPlan plan = new DeferredTagMigrationService().Plan(DeferredTagMigrationScope.Workspace, tags, new[] { patient }, null, TagParsingPolicy.Default);

            Assert.That(plan.Issues.Count, Is.EqualTo(1));
            Assert.That(plan.CanRemoveIncompatibleValues, Is.True);
            Assert.That(plan.DestructiveRemovalCount, Is.EqualTo(1));
            Assert.Throws<OperationCanceledException>(() => plan.Commit(DeferredTagMigrationDecision.Cancel));
            Assert.Throws<InvalidOperationException>(() => plan.Commit(DeferredTagMigrationDecision.Apply));
            Assert.That(patient.Tags, Is.EqualTo(new BaseTagValue[] { validValue, missingValue }));

            plan.Commit(DeferredTagMigrationDecision.ApplyAndRemoveIncompatibleValues);

            Assert.That(patient.Tags, Is.EqualTo(new BaseTagValue[] { validValue }));
            Assert.That(patient.Tags.Single().Tag, Is.SameAs(valid));
        }

        [Test]
        public void MissingTag_RecoveryDecisionPreservesRawValueOutsideActiveTagsAndRollbackRestoresIt()
        {
            StringTag valid = new("valid", "deferred-recovery-valid-tag");
            TagCollection tags = new(Array.Empty<BaseTag>(), new BaseTag[] { valid }, Array.Empty<BaseTag>());
            StringTagValue validValue = new(valid, "kept", "deferred-recovery-valid-value");
            StringTagValue missingValue = new(new StringTag("missing", "deferred-recovery-missing-tag"), "preserved", "deferred-recovery-missing-value");
            Patient patient = CreatePatient(new BaseTagValue[] { validValue, missingValue });

            DeferredTagMigrationPlan plan = new DeferredTagMigrationService().Plan(DeferredTagMigrationScope.Workspace, tags, new[] { patient }, null, TagParsingPolicy.Default);
            plan.Commit(DeferredTagMigrationDecision.ApplyWithRecovery);

            Assert.That(patient.Tags, Is.EqualTo(new BaseTagValue[] { validValue }));
            Assert.That(patient.QuarantinedTagValues, Has.Count.EqualTo(1));
            Assert.That(patient.QuarantinedTagValues[0].TagID, Is.EqualTo("deferred-recovery-missing-tag"));
            Assert.That(patient.QuarantinedTagValues[0].ValueID, Is.EqualTo(missingValue.ID));
            Assert.That(patient.QuarantinedTagValues[0].SerializedValue, Does.Contain("preserved"));

            plan.Rollback();

            Assert.That(patient.Tags, Is.EqualTo(new BaseTagValue[] { validValue, missingValue }));
            Assert.That(patient.QuarantinedTagValues, Is.Empty);
        }

        [Test]
        public void FilterRecovery_MissingCurrentTagResetsCurrentPresetWithoutBlockingOtherData()
        {
            TagCollection tags = new();
            StringTag missing = new("missing", "deferred-current-filter-missing-tag");
            SiteTagFilterCondition condition = new(SiteTagFilterCondition.TargetType.Site, missing, new StringTagFilterValue { Value = "x" }, false);
            FilterConditionsPreset current = new("", new BaseFilterCondition[] { condition }, "deferred-current-filter-preset");
            FilterConditionsPresetCollection filters = new();
            filters.SetCurrentPreset(current, typeof(Site), false);

            FilterPresetRecoveryReport report = FilterPresetRecoveryService.Recover(tags, filters);

            Assert.That(report.ResetCurrentPresetCount, Is.EqualTo(1));
            Assert.That(filters.GetCurrentPreset(typeof(Site)).Conditions, Is.Empty);
            Assert.That(filters.DisabledPresetCount, Is.EqualTo(1));
        }

        [Test]
        public void FilterRecovery_MissingNamedTagDisablesWholePresetAndPreservesNestedConditions()
        {
            TagCollection tags = new();
            StringTag missing = new("missing", "deferred-named-filter-missing-tag");
            PatientTagFilterCondition condition = new(PatientTagFilterCondition.TargetType.Patient, missing, new StringTagFilterValue { Value = "x" }, false);
            AllFilterCondition nested = new(new BaseFilterCondition[] { condition }, false, "deferred-disabled-nested");
            FilterConditionsPreset preset = new("named", new BaseFilterCondition[] { nested }, "deferred-disabled-preset");
            FilterConditionsPresetCollection filters = new();
            filters.AddPreset(preset, typeof(Patient), false);

            FilterPresetRecoveryReport report = FilterPresetRecoveryService.Recover(tags, filters);

            Assert.That(report.DisabledNamedPresetCount, Is.EqualTo(1));
            Assert.That(filters.GetPresets(typeof(Patient)), Is.Empty);
            Assert.That(filters.DisabledPresetCount, Is.EqualTo(1));
        }

        [Test]
        public void FilterRecovery_CompatibleDeserializedFilterIsBoundToCanonicalTag()
        {
            StringTag canonical = new("status", "filter-recovery-bind-tag");
            TagCollection tags = new(Array.Empty<BaseTag>(), new BaseTag[] { canonical }, Array.Empty<BaseTag>());
            PatientTagFilterCondition source = new(PatientTagFilterCondition.TargetType.Patient, canonical, new StringTagFilterValue { Value = "ready" }, false);
            FilterConditionsPresetCollection filters = new();
            filters.AddPreset(new FilterConditionsPreset("valid", new BaseFilterCondition[] { source }), typeof(Patient), false);
            typeof(PatientTagFilterCondition).GetField("m_TagID", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(source, canonical.ID);
            source.Tag = null;

            FilterPresetRecoveryReport report = FilterPresetRecoveryService.Recover(tags, filters);

            Assert.That(report.Issues, Is.Empty);
            PatientTagFilterCondition rebound = (PatientTagFilterCondition)filters.GetPresets(typeof(Patient)).Single().Conditions.Single();
            Assert.That(rebound.Tag, Is.SameAs(canonical));
        }

        [Test]
        public void FilterRecovery_PreservesCurrentAndNamedPayloadsThatShareAnId()
        {
            TagCollection tags = new();
            StringTag firstMissing = new("first", "filter-recovery-same-id-first-tag");
            StringTag secondMissing = new("second", "filter-recovery-same-id-second-tag");
            FilterConditionsPreset current = new("current", new BaseFilterCondition[] { new PatientTagFilterCondition(PatientTagFilterCondition.TargetType.Patient, firstMissing, new StringTagFilterValue { Value = "a" }, false) }, "filter-recovery-shared-preset-id");
            FilterConditionsPreset named = new("named", new BaseFilterCondition[] { new PatientTagFilterCondition(PatientTagFilterCondition.TargetType.Patient, secondMissing, new StringTagFilterValue { Value = "b" }, false) }, current.ID);
            FilterConditionsPresetCollection filters = new();
            filters.SetCurrentPreset(current, typeof(Patient), false);
            filters.AddPreset(named, typeof(Patient), false);

            FilterPresetRecoveryReport report = FilterPresetRecoveryService.Recover(tags, filters);

            Assert.That(report.Issues, Has.Count.EqualTo(2));
            Assert.That(filters.DisabledPresetCount, Is.EqualTo(2));
            Assert.That(filters.GetDisabledPresetEntries().Select(entry => entry.Preset.Name), Is.EquivalentTo(new[] { "current", "named" }));
        }

        [Test]
        public void FilterRecovery_EnumExtensionIsPersistedAndDoesNotRepeatAfterRestart()
        {
            using TempDirectoryScope temp = new();
            using PersistentDataTestScope persistentData = new(temp.Path);
            EnumTag canonical = new("status", new[] { "old" }, "filter-recovery-persist-enum");
            TagCollection tags = new(Array.Empty<BaseTag>(), new BaseTag[] { canonical }, Array.Empty<BaseTag>());
            EnumTag serializedDefinition = new("status", new[] { "new" }, canonical.ID);
            EnumTagFilterValue value = new();
            value.SetValue(serializedDefinition, 0);
            FilterConditionsPresetCollection filters = new();
            filters.AddPreset(new FilterConditionsPreset("enum", new BaseFilterCondition[] { new PatientTagFilterCondition(PatientTagFilterCondition.TargetType.Patient, serializedDefinition, value, false) }), typeof(Patient), false);
            tags.Save();
            filters.Save();

            TagCollection loadedTags = TagCollection.Initialize();
            FilterConditionsPresetCollection loadedFilters = FilterConditionsPresetCollection.Initialize();
            FilterPresetRecoveryReport first = FilterPresetRecoveryService.Recover(loadedTags, loadedFilters);
            Assert.That(first.MigratedPresetCount, Is.EqualTo(1));
            Assert.That(((EnumTag)loadedTags.AllTags.Single()).Values, Is.EqualTo(new[] { "old", "new" }));
            Assert.That(loadedTags.HasUnsavedTagMigration, Is.True);
            loadedTags.SaveRecovered();
            loadedFilters.SaveRecovered();

            TagCollection restartedTags = TagCollection.Initialize();
            FilterConditionsPresetCollection restartedFilters = FilterConditionsPresetCollection.Initialize();
            FilterPresetRecoveryReport second = FilterPresetRecoveryService.Recover(restartedTags, restartedFilters);
            Assert.That(second.HasChanges, Is.False);
            Assert.That(((EnumTag)restartedTags.AllTags.Single()).Values, Is.EqualTo(new[] { "old", "new" }));
            Assert.That(((PatientTagFilterCondition)restartedFilters.GetPresets(typeof(Patient)).Single().Conditions.Single()).Tag, Is.SameAs(restartedTags.AllTags.Single()));
        }

        [Test]
        public void LegacyEnum_ReconstructsCurrentLabelAndWarns()
        {
            EnumTag canonical = new("status", new[] { "ready" }, "deferred-legacy-enum-tag");
            TagCollection tags = new(Array.Empty<BaseTag>(), new BaseTag[] { canonical }, Array.Empty<BaseTag>());
            EnumTagValue legacy = new(new EnumTag("status", new[] { "ready" }, canonical.ID), 0, "deferred-legacy-enum-value");
            typeof(EnumTagValue).GetField("m_StringValue", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(legacy, null);
            Patient patient = CreatePatient(new BaseTagValue[] { legacy });

            DeferredTagMigrationPlan plan = new DeferredTagMigrationService().Plan(DeferredTagMigrationScope.Project, tags, new[] { patient }, null, TagParsingPolicy.Default);

            Assert.That(plan.Warnings.Any(warning => warning.Contains("current index")), Is.True);
            Assert.That(legacy.StringValue, Is.Null);
            plan.Commit(DeferredTagMigrationDecision.Apply);
            Assert.That(((EnumTagValue)patient.Tags.Single()).StringValue, Is.EqualTo("ready"));
            Assert.That(patient.Tags.Single().Tag, Is.SameAs(canonical));
        }

        [Test]
        public void ModernEnum_StagesUnknownLabelUntilCommit()
        {
            EnumTag canonical = new("status", new[] { "old" }, "deferred-modern-enum-tag");
            TagCollection tags = new(Array.Empty<BaseTag>(), new BaseTag[] { canonical }, Array.Empty<BaseTag>());
            EnumTag serializedDefinition = new("status", new[] { "new" }, canonical.ID);
            EnumTagValue serializedValue = new(serializedDefinition, 0, "deferred-modern-enum-value");
            Patient patient = CreatePatient(new BaseTagValue[] { serializedValue });

            DeferredTagMigrationPlan plan = new DeferredTagMigrationService().Plan(DeferredTagMigrationScope.Project, tags, new[] { patient }, null, TagParsingPolicy.Default);

            Assert.That(plan.EnumAdditionCount, Is.EqualTo(1));
            Assert.That(canonical.Values, Is.EqualTo(new[] { "old" }));
            plan.Commit(DeferredTagMigrationDecision.Apply);
            Assert.That(canonical.Values, Is.EqualTo(new[] { "old", "new" }));
            Assert.That(((EnumTagValue)patient.Tags.Single()).StringValue, Is.EqualTo("new"));
            Assert.That(((EnumTagValue)patient.Tags.Single()).Value, Is.EqualTo(1));

            plan.MarkPersistenceRequired();
            Assert.That(tags.HasUnsavedTagMigration, Is.True);
        }

        [Test]
        public void NestedFilterMigration_PreservesIdsAndBindsCanonicalTag()
        {
            BoolTag canonical = new("accepted", "deferred-filter-tag");
            TagCollection tags = new(new BaseTag[] { canonical }, Array.Empty<BaseTag>(), Array.Empty<BaseTag>());
            StringTag serializedDefinition = new("accepted", canonical.ID);
            StringTagFilterValue patientValue = new() { ID = "deferred-filter-patient-value", Value = "yes", ExactMatch = true };
            PatientTagFilterCondition patientCondition = new(PatientTagFilterCondition.TargetType.Patient, serializedDefinition, patientValue, false, "deferred-filter-patient");
            StringTagFilterValue siteValue = new() { ID = "deferred-filter-site-value", Value = "no", ExactMatch = true };
            SingleTagFilter single = new(serializedDefinition, siteValue, "deferred-filter-single");
            MultipleSiteTagsFilterCondition multiple = new(new[] { single }, false, "deferred-filter-multiple");
            AllFilterCondition nested = new(new BaseFilterCondition[] { patientCondition, multiple }, false, "deferred-filter-all");
            FilterConditionsPresetCollection filters = new();
            filters.AddPreset(new FilterConditionsPreset("preset", new BaseFilterCondition[] { nested }), typeof(Patient), false);

            DeferredTagMigrationPlan plan = new DeferredTagMigrationService().Plan(DeferredTagMigrationScope.Workspace, tags, Array.Empty<Patient>(), filters, TagParsingPolicy.Default);

            Assert.That(plan.FilterCount, Is.EqualTo(2));
            Assert.That(patientCondition.Value, Is.SameAs(patientValue));
            plan.Commit(DeferredTagMigrationDecision.Apply);

            AllFilterCondition migrated = (AllFilterCondition)filters.GetPresets(typeof(Patient)).Single().Conditions.Single();
            PatientTagFilterCondition migratedPatient = (PatientTagFilterCondition)migrated.Conditions[0];
            SingleTagFilter migratedSingle = ((MultipleSiteTagsFilterCondition)migrated.Conditions[1]).TagFilters.Single();
            Assert.That(migratedPatient.ID, Is.EqualTo(patientCondition.ID));
            Assert.That(migratedPatient.Value.ID, Is.EqualTo(patientValue.ID));
            Assert.That(migratedPatient.Value, Is.TypeOf<BoolTagFilterValue>());
            Assert.That(migratedPatient.Tag, Is.SameAs(canonical));
            Assert.That(migratedSingle.ID, Is.EqualTo(single.ID));
            Assert.That(migratedSingle.Value.ID, Is.EqualTo(siteValue.ID));
            Assert.That(migratedSingle.Tag, Is.SameAs(canonical));

            plan.Rollback();
            AllFilterCondition restored = (AllFilterCondition)filters.GetPresets(typeof(Patient)).Single().Conditions.Single();
            Assert.That(restored, Is.SameAs(nested));
            Assert.That(restored.Conditions[0], Is.SameAs(patientCondition));
            Assert.That(((MultipleSiteTagsFilterCondition)restored.Conditions[1]).TagFilters.Single(), Is.SameAs(single));

            plan.Commit(DeferredTagMigrationDecision.Apply);
            plan.MarkPersistenceRequired();
            Assert.That(filters.HasUnsavedTagMigration, Is.True);
        }

        [Test]
        public void Commit_RejectsCanonicalDefinitionChangedDuringReview()
        {
            IntTag canonical = new("age", "deferred-stale-tag");
            TagCollection tags = new(Array.Empty<BaseTag>(), new BaseTag[] { canonical }, Array.Empty<BaseTag>());
            Patient patient = CreatePatient(new BaseTagValue[] { new StringTagValue(new StringTag("age", canonical.ID), "1") });
            DeferredTagMigrationPlan plan = new DeferredTagMigrationService().Plan(DeferredTagMigrationScope.Project, tags, new[] { patient }, null, TagParsingPolicy.Default);
            canonical.Name = "changed";

            Assert.Throws<InvalidOperationException>(() => plan.Commit(DeferredTagMigrationDecision.Apply));
            Assert.That(patient.Tags.Single(), Is.TypeOf<StringTagValue>());
        }

        private static Patient CreatePatient(BaseTagValue[] values, Site[] sites = null)
        {
            return new Patient("patient", Array.Empty<BaseMesh>(), Array.Empty<MRI>(), sites ?? Array.Empty<Site>(), values, "database", "deferred-patient");
        }
    }
}
