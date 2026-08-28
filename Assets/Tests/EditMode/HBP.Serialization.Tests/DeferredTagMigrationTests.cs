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

            plan.Commit();

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
        public void CompatibleDetachedValue_IsNotReportedAsMigrationAndLoadingContextRebindsIt()
        {
            StringTag canonical = new("status", "deferred-value-rebind-tag");
            StringTag detached = new("status", canonical.ID);
            StringTagValue value = new(detached, "ready", "deferred-value-rebind-value");
            Patient patient = CreatePatient(new BaseTagValue[] { value });
            TagCollection tags = new(Array.Empty<BaseTag>(), new BaseTag[] { canonical }, Array.Empty<BaseTag>());

            DeferredTagMigrationPlan plan = new DeferredTagMigrationService().Plan(DeferredTagMigrationScope.Project, tags, new[] { patient }, null, TagParsingPolicy.Default);

            Assert.That(plan.RequiresConfirmation, Is.False);
            Assert.That(plan.PatientValueCount, Is.Zero);
            Assert.That(value.Tag, Is.SameAs(detached));
            new LoadingContext(tags.AllTags, Array.Empty<Protocol>(), new[] { patient }).ResolveDatabase(new[] { patient }, Array.Empty<DataInfo>());
            Assert.That(patient.Tags.Single(), Is.SameAs(value));
            Assert.That(value.Tag, Is.SameAs(canonical));
        }

        [Test]
        public void MissingTag_IsRemovedAndReportedAndRollbackRestoresIt()
        {
            StringTag valid = new("valid", "deferred-valid-tag");
            TagCollection tags = new(Array.Empty<BaseTag>(), new BaseTag[] { valid }, Array.Empty<BaseTag>());
            StringTagValue validValue = new(valid, "kept", "deferred-valid-value");
            StringTagValue missingValue = new(new StringTag("missing", "deferred-missing-tag"), "removed", "deferred-missing-value");
            Patient patient = CreatePatient(new BaseTagValue[] { validValue, missingValue });

            DeferredTagMigrationPlan plan = new DeferredTagMigrationService().Plan(DeferredTagMigrationScope.Workspace, tags, new[] { patient }, null, TagParsingPolicy.Default);

            Assert.That(plan.Issues, Is.Empty);
            Assert.That(plan.RemovedValues, Has.Count.EqualTo(1));
            Assert.That(plan.RemovedValues[0].TagID, Is.EqualTo("deferred-missing-tag"));
            Assert.That(plan.RemovedValues[0].ValueID, Is.EqualTo(missingValue.ID));
            Assert.That(plan.RemovedValues[0].SerializedType, Is.EqualTo(nameof(StringTagValue)));
            Assert.That(plan.RemovedValues[0].PatientID, Is.EqualTo(patient.ID));
            Assert.That(plan.RemovedValues[0].PatientName, Is.EqualTo(patient.Name));
            Assert.That(patient.Tags, Is.EqualTo(new BaseTagValue[] { validValue, missingValue }));

            plan.Commit();

            Assert.That(patient.Tags, Is.EqualTo(new BaseTagValue[] { validValue }));
            Assert.That(patient.Tags.Single().Tag, Is.SameAs(valid));

            plan.Rollback();

            Assert.That(patient.Tags, Is.EqualTo(new BaseTagValue[] { validValue, missingValue }));
        }

        [Test]
        public void MissingSiteTag_ReportsItsParentPatient()
        {
            StringTagValue missingValue = new(new StringTag("missing", "deferred-missing-site-tag"), "removed", "deferred-missing-site-value");
            Site site = new("site", Array.Empty<Coordinate>(), new BaseTagValue[] { missingValue }, "deferred-missing-site");
            Patient patient = CreatePatient(Array.Empty<BaseTagValue>(), new[] { site });

            DeferredTagMigrationPlan plan = new DeferredTagMigrationService().Plan(DeferredTagMigrationScope.Workspace, new TagCollection(), new[] { patient }, null, TagParsingPolicy.Default);

            Assert.That(plan.RemovedValues, Has.Count.EqualTo(1));
            Assert.That(plan.RemovedValues[0].Scope, Is.EqualTo(TagMigrationIssueScope.SiteValue));
            Assert.That(plan.RemovedValues[0].OwnerID, Is.EqualTo(site.ID));
            Assert.That(plan.RemovedValues[0].PatientID, Is.EqualTo(patient.ID));
            Assert.That(plan.RemovedValues[0].PatientName, Is.EqualTo(patient.Name));

            plan.Commit();
            Assert.That(site.Tags, Is.Empty);
        }

        [Test]
        public void FilterRepair_MissingCurrentTagKeepsPresetAndRemovesCondition()
        {
            TagCollection tags = new();
            StringTag missing = new("missing", "deferred-current-filter-missing-tag");
            SiteTagFilterCondition condition = new(SiteTagFilterCondition.TargetType.Site, missing, new StringTagFilterValue { Value = "x" }, false);
            FilterConditionsPreset current = new("", new BaseFilterCondition[] { condition }, "deferred-current-filter-preset");
            FilterConditionsPresetCollection filters = new();
            filters.SetCurrentPreset(current, typeof(Site), false);

            FilterPresetRepairReport report = FilterPresetRepairService.Repair(tags, filters);

            Assert.That(report.RemovedConditionCount, Is.EqualTo(1));
            Assert.That(filters.GetCurrentPreset(typeof(Site)).ID, Is.EqualTo(current.ID));
            Assert.That(filters.GetCurrentPreset(typeof(Site)).Conditions, Is.Empty);
        }

        [Test]
        public void FilterRepair_NestedGroupWithOneValidChildIsCollapsedAndPresetIsPreserved()
        {
            StringTag canonical = new("valid", "deferred-valid-filter-tag");
            TagCollection tags = new(Array.Empty<BaseTag>(), new BaseTag[] { canonical }, Array.Empty<BaseTag>());
            PatientTagFilterCondition valid = new(PatientTagFilterCondition.TargetType.Patient, canonical, new StringTagFilterValue { Value = "ok" }, false, "deferred-valid-condition");
            StringTag missing = new("missing", "deferred-named-filter-missing-tag");
            PatientTagFilterCondition invalid = new(PatientTagFilterCondition.TargetType.Patient, missing, new StringTagFilterValue { Value = "x" }, false);
            AllFilterCondition nested = new(new BaseFilterCondition[] { valid, invalid }, true, "deferred-repaired-nested");
            FilterConditionsPreset preset = new("named", new BaseFilterCondition[] { nested }, "deferred-repaired-preset");
            FilterConditionsPresetCollection filters = new();
            filters.AddPreset(preset, typeof(Patient), false);

            FilterPresetRepairReport report = FilterPresetRepairService.Repair(tags, filters);

            FilterConditionsPreset repairedPreset = filters.GetPresets(typeof(Patient)).Single();
            PatientTagFilterCondition repairedCondition = (PatientTagFilterCondition)repairedPreset.Conditions.Single();
            Assert.That(report.RemovedConditionCount, Is.EqualTo(1));
            Assert.That(repairedPreset.ID, Is.EqualTo(preset.ID));
            Assert.That(repairedCondition.ID, Is.EqualTo(valid.ID));
            Assert.That(repairedCondition.IsNot, Is.True);
            Assert.That(repairedCondition.Tag, Is.SameAs(canonical));
        }

        [Test]
        public void FilterRepair_MultipleSiteTagsRemovesOnlyInvalidSingleFilter()
        {
            StringTag canonical = new("valid", "deferred-multiple-valid-tag");
            TagCollection tags = new(Array.Empty<BaseTag>(), Array.Empty<BaseTag>(), new BaseTag[] { canonical });
            SingleTagFilter valid = new(canonical, new StringTagFilterValue { Value = "ok" }, "deferred-multiple-valid");
            SingleTagFilter invalid = new(new StringTag("missing", "deferred-multiple-missing-tag"), new StringTagFilterValue { Value = "bad" }, "deferred-multiple-invalid");
            MultipleSiteTagsFilterCondition multiple = new(new[] { valid, invalid }, false, "deferred-multiple-condition");
            FilterConditionsPresetCollection filters = new();
            filters.AddPreset(new FilterConditionsPreset("multiple", new BaseFilterCondition[] { multiple }), typeof(Patient), false);

            FilterPresetRepairReport report = FilterPresetRepairService.Repair(tags, filters);

            MultipleSiteTagsFilterCondition repaired = (MultipleSiteTagsFilterCondition)filters.GetPresets(typeof(Patient)).Single().Conditions.Single();
            Assert.That(report.RemovedConditionCount, Is.EqualTo(1));
            Assert.That(repaired.TagFilters.Select(filter => filter.ID), Is.EqualTo(new[] { valid.ID }));
            Assert.That(repaired.TagFilters.Single().Tag, Is.SameAs(canonical));
        }

        [Test]
        public void FilterRepair_CompatibleDeserializedFilterIsBoundToCanonicalTag()
        {
            StringTag canonical = new("status", "filter-repair-bind-tag");
            TagCollection tags = new(Array.Empty<BaseTag>(), new BaseTag[] { canonical }, Array.Empty<BaseTag>());
            PatientTagFilterCondition source = new(PatientTagFilterCondition.TargetType.Patient, canonical, new StringTagFilterValue { Value = "ready" }, false);
            FilterConditionsPresetCollection filters = new();
            filters.AddPreset(new FilterConditionsPreset("valid", new BaseFilterCondition[] { source }), typeof(Patient), false);
            typeof(PatientTagFilterCondition).GetField("m_TagID", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(source, canonical.ID);
            source.Tag = null;

            FilterPresetRepairReport report = FilterPresetRepairService.Repair(tags, filters);

            Assert.That(report.HasChanges, Is.False);
            PatientTagFilterCondition unresolved = (PatientTagFilterCondition)filters.GetPresets(typeof(Patient)).Single().Conditions.Single();
            Assert.That(unresolved.Tag, Is.Null);
            new LoadingContext(tags.AllTags, Array.Empty<Protocol>()).ResolveFilterConditions(filters);
            PatientTagFilterCondition rebound = (PatientTagFilterCondition)filters.GetPresets(typeof(Patient)).Single().Conditions.Single();
            Assert.That(rebound.Tag, Is.SameAs(canonical));
        }

        [Test]
        public void FilterRepair_PreservesCurrentAndNamedPresetsWithDistinctIds()
        {
            TagCollection tags = new();
            FilterConditionsPreset current = new("current", new BaseFilterCondition[] { new PatientTagFilterCondition(PatientTagFilterCondition.TargetType.Patient, new StringTag("first", "filter-repair-first"), new StringTagFilterValue { Value = "a" }, false) }, "filter-repair-current-id");
            FilterConditionsPreset named = new("named", new BaseFilterCondition[] { new PatientTagFilterCondition(PatientTagFilterCondition.TargetType.Patient, new StringTag("second", "filter-repair-second"), new StringTagFilterValue { Value = "b" }, false) }, "filter-repair-named-id");
            FilterConditionsPresetCollection filters = new();
            filters.SetCurrentPreset(current, typeof(Patient), false);
            filters.AddPreset(named, typeof(Patient), false);

            FilterPresetRepairReport report = FilterPresetRepairService.Repair(tags, filters);

            Assert.That(report.RemovedConditionCount, Is.EqualTo(2));
            Assert.That(filters.GetCurrentPreset(typeof(Patient)).Name, Is.EqualTo("current"));
            Assert.That(filters.GetCurrentPreset(typeof(Patient)).Conditions, Is.Empty);
            Assert.That(filters.GetPresets(typeof(Patient)).Single().Name, Is.EqualTo("named"));
            Assert.That(filters.GetPresets(typeof(Patient)).Single().Conditions, Is.Empty);
        }

        [Test]
        public void FilterRepair_EnumExtensionIsPersistedAndDoesNotRepeatAfterRestart()
        {
            using TempDirectoryScope temp = new();
            using PersistentDataTestScope persistentData = new(temp.Path);
            EnumTag canonical = new("status", new[] { "old" }, "filter-repair-persist-enum");
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
            FilterPresetRepairReport first = FilterPresetRepairService.Repair(loadedTags, loadedFilters);
            Assert.That(first.MigratedPresetCount, Is.EqualTo(1));
            Assert.That(((EnumTag)loadedTags.AllTags.Single()).Values, Is.EqualTo(new[] { "old", "new" }));
            Assert.That(loadedTags.HasUnsavedTagMigration, Is.True);
            loadedTags.Save();
            loadedFilters.Save();

            TagCollection restartedTags = TagCollection.Initialize();
            FilterConditionsPresetCollection restartedFilters = FilterConditionsPresetCollection.Initialize();
            FilterPresetRepairReport second = FilterPresetRepairService.Repair(restartedTags, restartedFilters);
            Assert.That(second.HasChanges, Is.False);
            Assert.That(((EnumTag)restartedTags.AllTags.Single()).Values, Is.EqualTo(new[] { "old", "new" }));
            PatientTagFilterCondition restartedCondition = (PatientTagFilterCondition)restartedFilters.GetPresets(typeof(Patient)).Single().Conditions.Single();
            Assert.That(restartedCondition.Tag, Is.Null);
            new LoadingContext(restartedTags.AllTags, Array.Empty<Protocol>()).ResolveFilterConditions(restartedFilters);
            Assert.That(restartedCondition.Tag, Is.SameAs(restartedTags.AllTags.Single()));
        }

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
            plan.Commit();
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
            plan.Commit();
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
            plan.Commit();

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

            plan.Commit();
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

            Assert.Throws<InvalidOperationException>(() => plan.Commit());
            Assert.That(patient.Tags.Single(), Is.TypeOf<StringTagValue>());
        }

        private static Patient CreatePatient(BaseTagValue[] values, Site[] sites = null)
        {
            return new Patient("patient", Array.Empty<BaseMesh>(), Array.Empty<MRI>(), sites ?? Array.Empty<Site>(), values, "database", "deferred-patient");
        }
    }
}
