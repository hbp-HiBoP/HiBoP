using HBP.Core.Data;
using HBP.Core.Database;
using HBP.Core.Enums;
using HBP.Core.Tools;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

namespace HBP.Tests.Serialization
{
    public class TagSchemaMigrationTests
    {
        [Test]
        public void ParsingPolicy_NormalizesTokensAndRejectsOverlaps()
        {
            TagParsingPolicy policy = new(new[] { " YES " }, new[] { "No" }, new[] { "N/A" });

            Assert.That(policy.TryParseBoolean("yes", out bool yes), Is.True);
            Assert.That(yes, Is.True);
            Assert.That(policy.TryParseBoolean(" NO ", out bool no), Is.True);
            Assert.That(no, Is.False);
            Assert.That(policy.IsIgnored(" n/a "), Is.True);
            Assert.Throws<ArgumentException>(() => new TagParsingPolicy(new[] { "yes" }, new[] { "YES" }, Array.Empty<string>()));
        }

        [Test]
        public void ValueConverter_PreservesIdAndSourceAndReportsClampLoss()
        {
            StringTag sourceTag = new("source", "migration-converter-source-tag");
            StringTagValue source = new(sourceTag, "15", "migration-converter-value");
            IntTag target = new("target", true, 0, 10, sourceTag.ID);

            TagValueConversionResult result = new TagValueConversionService().TryConvert(source, target, TagParsingPolicy.Default);

            Assert.That(result.Success, Is.True);
            Assert.That(result.Impact, Is.EqualTo(TagConversionImpact.Lossy));
            Assert.That(result.Value, Is.TypeOf<IntTagValue>());
            Assert.That(((IntTagValue)result.Value).Value, Is.EqualTo(10));
            Assert.That(result.Value.ID, Is.EqualTo(source.ID));
            Assert.That(result.Value.Tag, Is.SameAs(target));
            Assert.That(source.Value, Is.EqualTo("15"));
            Assert.That(source.Tag, Is.SameAs(sourceTag));
        }

        [Test]
        public void ValueConverter_UsesPolicyAndRejectsFractionalFloatToInt()
        {
            StringTag stringTag = new("source", "migration-policy-tag");
            BoolTag boolTag = new("target", stringTag.ID);
            TagParsingPolicy policy = new(new[] { "yes" }, new[] { "no" }, Array.Empty<string>());

            TagValueConversionResult boolResult = new TagValueConversionService().TryConvert(new StringTagValue(stringTag, "YES", "migration-policy-value"), boolTag, policy);
            TagValueConversionResult intResult = new TagValueConversionService().TryConvert(new FloatTagValue(new FloatTag("float", "migration-float-tag"), 1.5f, "migration-float-value"), new IntTag("int", "migration-float-tag"), policy);

            Assert.That(boolResult.Success, Is.True);
            Assert.That(((BoolTagValue)boolResult.Value).Value, Is.True);
            Assert.That(intResult.Success, Is.False);
            Assert.That(intResult.Error, Does.Contain("cannot be converted"));
        }

        [Test]
        public void ValueConverter_CoversCoreScalarMatrix()
        {
            BoolTag boolTag = new("bool", "migration-matrix-tag");
            BoolTagValue boolValue = new(boolTag, true, "migration-matrix-bool-value");
            TagValueConversionService service = new();

            Assert.That(((IntTagValue)service.TryConvert(boolValue, new IntTag("int", boolTag.ID), TagParsingPolicy.Default).Value).Value, Is.EqualTo(1));
            Assert.That(((FloatTagValue)service.TryConvert(boolValue, new FloatTag("float", boolTag.ID), TagParsingPolicy.Default).Value).Value, Is.EqualTo(1));
            Assert.That(((StringTagValue)service.TryConvert(boolValue, new StringTag("string", boolTag.ID), TagParsingPolicy.Default).Value).Value, Is.EqualTo("true"));
            Assert.That(service.TryConvert(boolValue, new EmptyTag("empty", boolTag.ID), TagParsingPolicy.Default).Impact, Is.EqualTo(TagConversionImpact.Destructive));

            IntTagValue intValue = new(new IntTag("int", "migration-matrix-number-tag"), 4, "migration-matrix-int-value");
            Assert.That(((FloatTagValue)service.TryConvert(intValue, new FloatTag("float", intValue.Tag.ID), TagParsingPolicy.Default).Value).Value, Is.EqualTo(4));
            FloatTagValue floatValue = new(new FloatTag("float", intValue.Tag.ID), 4, "migration-matrix-float-value");
            Assert.That(((IntTagValue)service.TryConvert(floatValue, new IntTag("int", intValue.Tag.ID), TagParsingPolicy.Default).Value).Value, Is.EqualTo(4));

            EnumTag enumTag = new("enum", new[] { "label" }, "migration-matrix-enum-tag");
            EnumTagValue enumValue = new(enumTag, 0, "migration-matrix-enum-value");
            Assert.That(((StringTagValue)service.TryConvert(enumValue, new StringTag("string", enumTag.ID), TagParsingPolicy.Default).Value).Value, Is.EqualTo("label"));
        }

        [Test]
        public void ValueConverter_RejectsNullWithoutInventingEmptyTextOrEnumOption()
        {
            StringTag sourceTag = new("source", "migration-null-tag");
            StringTagValue source = new(sourceTag, null, "migration-null-value");
            TagValueConversionService service = new();

            TagValueConversionResult toString = service.TryConvert(source, new StringTag("target", sourceTag.ID), TagParsingPolicy.Default);
            EnumTag enumTarget = new("target", Array.Empty<string>(), sourceTag.ID);
            TagValueConversionResult toEnum = service.TryConvert(source, enumTarget, TagParsingPolicy.Default);
            TagValueConversionResult toEmpty = service.TryConvert(source, new EmptyTag("target", sourceTag.ID), TagParsingPolicy.Default);

            Assert.That(toString.Success, Is.False);
            Assert.That(toString.Error, Does.Contain("null"));
            Assert.That(toEnum.Success, Is.False);
            Assert.That(enumTarget.Values, Is.Empty);
            Assert.That(toEmpty.Success, Is.True);
            Assert.That(toEmpty.Impact, Is.EqualTo(TagConversionImpact.Destructive));
        }

        [Test]
        public void FilterConverter_PreservesSupportedSemanticsAndBlocksAmbiguousOnes()
        {
            TagFilterValueConversionService service = new();
            BoolTag boolTag = new("bool", "migration-filter-matrix-tag");
            IntTag intTag = new("int", boolTag.ID);
            BoolTagFilterValue boolValue = new() { Value = true, ID = "migration-filter-matrix-bool" };

            TagFilterValueConversionResult toNumber = service.TryConvert(boolValue, boolTag, intTag, TagParsingPolicy.Default);
            Assert.That(toNumber.Success, Is.True);
            Assert.That(((NumberTagFilterValue)toNumber.Value).Type, Is.EqualTo(NumberComparisonType.Equal));
            Assert.That(((NumberTagFilterValue)toNumber.Value).Value, Is.EqualTo(1));
            Assert.That(toNumber.Value.ID, Is.EqualTo(boolValue.ID));

            NumberTagFilterValue equalOne = new() { Type = NumberComparisonType.Equal, Value = 1, ID = "migration-filter-matrix-number" };
            Assert.That(((BoolTagFilterValue)service.TryConvert(equalOne, intTag, boolTag, TagParsingPolicy.Default).Value).Value, Is.True);
            NumberTagFilterValue greater = new() { Type = NumberComparisonType.Greater, Value = 1, ID = "migration-filter-matrix-greater" };
            Assert.That(service.TryConvert(greater, intTag, boolTag, TagParsingPolicy.Default).Success, Is.False);

            EnumTag enumTag = new("enum", new[] { "label" }, boolTag.ID);
            EnumTagFilterValue enumValue = new() { ID = "migration-filter-matrix-enum" };
            enumValue.SetValue(enumTag, 0);
            StringTagFilterValue stringResult = (StringTagFilterValue)service.TryConvert(enumValue, enumTag, new StringTag("string", boolTag.ID), TagParsingPolicy.Default).Value;
            Assert.That(stringResult.Value, Is.EqualTo("label"));
            Assert.That(stringResult.ExactMatch, Is.True);
            Assert.That(stringResult.CaseSensitive, Is.True);
        }

        [Test]
        public void FilterConverter_ReportsSemanticBroadeningAsLossy()
        {
            TagFilterValueConversionService service = new();
            StringTag stringTag = new("text", "migration-filter-loss-tag");
            StringTagFilterValue yes = new() { Value = "yes", ExactMatch = true, CaseSensitive = true, ID = "migration-filter-loss-text" };
            StringTagFilterValue one = new() { Value = "1", ExactMatch = true, CaseSensitive = true, ID = "migration-filter-loss-numeric-text" };
            NumberTagFilterValue number = new() { Type = NumberComparisonType.Equal, Value = 1, ID = "migration-filter-loss-number" };

            TagFilterValueConversionResult toBool = service.TryConvert(yes, stringTag, new BoolTag("bool", stringTag.ID), TagParsingPolicy.Default);
            TagFilterValueConversionResult toNumber = service.TryConvert(one, stringTag, new IntTag("int", stringTag.ID), TagParsingPolicy.Default);
            TagFilterValueConversionResult toString = service.TryConvert(number, new IntTag("int", stringTag.ID), new StringTag("text", stringTag.ID), TagParsingPolicy.Default);

            Assert.That(toBool.Impact, Is.EqualTo(TagConversionImpact.Lossy));
            Assert.That(toNumber.Impact, Is.EqualTo(TagConversionImpact.Lossy));
            Assert.That(toString.Impact, Is.EqualTo(TagConversionImpact.Lossy));
        }

        [Test]
        public void ModernEnumConversion_AppendsOnPreparedDefinitionOnly()
        {
            StringTag currentTag = new("status", "migration-enum-tag");
            TagCollection current = new(Array.Empty<BaseTag>(), new BaseTag[] { currentTag }, Array.Empty<BaseTag>(), "migration-enum-current");
            EnumTag proposedEnum = new("status", Array.Empty<string>(), currentTag.ID);
            TagCollection proposed = new(Array.Empty<BaseTag>(), new BaseTag[] { proposedEnum }, Array.Empty<BaseTag>(), "migration-enum-proposed");
            Patient patient = CreatePatient(new StringTagValue(currentTag, "ready", "migration-enum-value"));
            TagSchemaMigrationService service = new();

            TagSchemaMigrationPlan plan = service.Plan(current, proposed, new[] { patient }, new FilterConditionsPresetCollection(), new HashSet<string> { currentTag.ID }, TagParsingPolicy.Default);

            Assert.That(plan.IsValid, Is.True);
            Assert.That(proposedEnum.Values, Is.Empty, "Planning must not mutate the proposed editor graph.");
            Assert.That(current.TryGetTag(currentTag.ID, out BaseTag before), Is.True);
            Assert.That(before, Is.SameAs(currentTag));
            service.Commit(plan);
            Assert.That(current.TryGetTag(currentTag.ID, out BaseTag after), Is.True);
            Assert.That(after, Is.TypeOf<EnumTag>());
            Assert.That(((EnumTag)after).Values, Is.EqualTo(new[] { "ready" }));
            Assert.That(patient.Tags.Single(), Is.TypeOf<EnumTagValue>());
            Assert.That(((EnumTagValue)patient.Tags.Single()).StringValue, Is.EqualTo("ready"));
            Assert.That(patient.Tags.Single().Tag, Is.SameAs(after));
        }

        [Test]
        public void PlanningFilters_NeverMutatesAnUntouchedCanonicalEnum()
        {
            EnumTag untouchedEnum = new("status", new[] { "old" }, "migration-plan-purity-enum");
            StringTag changedTag = new("accepted", "migration-plan-purity-changed");
            TagCollection current = new(new BaseTag[] { untouchedEnum, changedTag }, Array.Empty<BaseTag>(), Array.Empty<BaseTag>());
            TagCollection proposed = new(new BaseTag[] { (BaseTag)untouchedEnum.Clone(), new BoolTag("accepted", changedTag.ID) }, Array.Empty<BaseTag>(), Array.Empty<BaseTag>());
            EnumTag serializedDefinition = new("status", new[] { "new" }, untouchedEnum.ID);
            EnumTagFilterValue filterValue = new();
            filterValue.SetValue(serializedDefinition, 0);
            FilterConditionsPresetCollection filters = CreateFilters(new PatientTagFilterCondition(PatientTagFilterCondition.TargetType.Patient, serializedDefinition, filterValue, false));
            TagSchemaMigrationService service = new();

            TagSchemaMigrationPlan plan = service.Plan(current, proposed, Array.Empty<Patient>(), filters, new HashSet<string> { changedTag.ID }, TagParsingPolicy.Default);

            Assert.That(plan.IsValid, Is.True);
            Assert.That(untouchedEnum.Values, Is.EqualTo(new[] { "old" }));
            service.Commit(plan);
            Assert.That(current.TryGetTag(untouchedEnum.ID, out BaseTag canonical), Is.True);
            Assert.That(canonical, Is.SameAs(untouchedEnum));
            Assert.That(((EnumTag)canonical).Values, Is.EqualTo(new[] { "old" }));
            Assert.That(filters.GetPresets(typeof(Patient)).Single().ID, Is.EqualTo("migration-filter-preset"));
            Assert.That(filters.GetPresets(typeof(Patient)).Single().Conditions, Is.Empty);
        }

        [Test]
        public void LegacyEnumConversion_IsLimitedToEnumTargetWithWarning()
        {
            EnumTag sourceTag = new("enum", new[] { "one" }, "migration-legacy-enum-tag");
            EnumTagValue legacy = new(sourceTag, 0, "migration-legacy-enum-value");
            typeof(EnumTagValue).GetField("m_StringValue", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(legacy, null);
            TagValueConversionService service = new();

            TagValueConversionResult rejected = service.TryConvert(legacy, new StringTag("text", sourceTag.ID), TagParsingPolicy.Default);
            TagValueConversionResult accepted = service.TryConvert(legacy, new EnumTag("enum", Array.Empty<string>(), sourceTag.ID), TagParsingPolicy.Default);

            Assert.That(rejected.Success, Is.False);
            Assert.That(rejected.Error, Does.Contain("historical label"));
            Assert.That(accepted.Success, Is.True);
            Assert.That(accepted.Warning, Does.Contain("current index"));
            Assert.That(((EnumTagValue)accepted.Value).StringValue, Is.EqualTo("one"));
        }

        [Test]
        public void PlanCommitRollback_MigratesPatientSiteAndFilterAtomically()
        {
            StringTag currentTag = new("accepted", "migration-atomic-tag");
            IntTag untouchedTag = new("untouched", "migration-atomic-untouched-tag");
            TagCollection current = new(new BaseTag[] { currentTag, untouchedTag }, Array.Empty<BaseTag>(), Array.Empty<BaseTag>(), "migration-atomic-current");
            BoolTag proposedTag = new("accepted", currentTag.ID);
            TagCollection proposed = new(new BaseTag[] { proposedTag, (BaseTag)untouchedTag.Clone() }, Array.Empty<BaseTag>(), Array.Empty<BaseTag>(), "migration-atomic-proposed");
            StringTagValue patientValue = new(currentTag, "yes", "migration-atomic-patient-value");
            IntTagValue untouchedValue = new(untouchedTag, 7, "migration-atomic-untouched-value");
            StringTagValue siteValue = new(currentTag, "no", "migration-atomic-site-value");
            Site site = new("site", Array.Empty<Coordinate>(), new BaseTagValue[] { siteValue }, "migration-atomic-site");
            Patient patient = CreatePatient(patientValue, new[] { site });
            patient.Tags.Add(untouchedValue);
            StringTagFilterValue filterValue = new() { Value = "yes", ExactMatch = true, CaseSensitive = false, ID = "migration-atomic-filter-value" };
            PatientTagFilterCondition condition = new(PatientTagFilterCondition.TargetType.Patient, currentTag, filterValue, false, "migration-atomic-condition");
            FilterConditionsPresetCollection filters = CreateFilters(condition);
            TagSchemaMigrationService service = new();

            TagSchemaMigrationPlan plan = service.Plan(current, proposed, new[] { patient }, filters, new HashSet<string> { currentTag.ID }, TagParsingPolicy.Default);

            Assert.That(plan.IsValid, Is.True);
            Assert.That(plan.PatientValueCount, Is.EqualTo(1));
            Assert.That(plan.SiteValueCount, Is.EqualTo(1));
            Assert.That(plan.FilterCount, Is.EqualTo(1));
            Assert.That(patient.Tags[0], Is.SameAs(patientValue), "Plan must not publish values.");
            Assert.That(site.Tags.Single(), Is.SameAs(siteValue), "Plan must not publish site values.");
            Assert.That(GetPatientCondition(filters), Is.SameAs(condition), "Plan must not publish filters.");

            service.Commit(plan);

            Assert.That(current.TryGetTag(currentTag.ID, out BaseTag canonical), Is.True);
            Assert.That(canonical, Is.TypeOf<BoolTag>());
            Assert.That(patient.Tags[0].ID, Is.EqualTo(patientValue.ID));
            Assert.That(patient.Tags[0].Tag, Is.SameAs(canonical));
            Assert.That(((BoolTagValue)patient.Tags[0]).Value, Is.True);
            Assert.That(patient.Tags[1], Is.SameAs(untouchedValue));
            Assert.That(current.TryGetTag(untouchedTag.ID, out BaseTag untouchedCanonical), Is.True);
            Assert.That(untouchedCanonical, Is.SameAs(untouchedTag));
            Assert.That(site.Tags.Single().ID, Is.EqualTo(siteValue.ID));
            Assert.That(site.Tags.Single().Tag, Is.SameAs(canonical));
            Assert.That(((BoolTagValue)site.Tags.Single()).Value, Is.False);
            PatientTagFilterCondition migratedCondition = GetPatientCondition(filters);
            Assert.That(migratedCondition.ID, Is.EqualTo(condition.ID));
            Assert.That(migratedCondition.Value.ID, Is.EqualTo(filterValue.ID));
            Assert.That(migratedCondition.Tag, Is.SameAs(canonical));
            Assert.That(migratedCondition.Value, Is.TypeOf<BoolTagFilterValue>());

            plan.Rollback();

            Assert.That(current.TryGetTag(currentTag.ID, out BaseTag restored), Is.True);
            Assert.That(restored, Is.SameAs(currentTag));
            Assert.That(patient.Tags[0], Is.SameAs(patientValue));
            Assert.That(patient.Tags[1], Is.SameAs(untouchedValue));
            Assert.That(site.Tags.Single(), Is.SameAs(siteValue));
            Assert.That(GetPatientCondition(filters).Tag, Is.SameAs(currentTag));
        }

        [Test]
        public void ContainsTextFilter_RemovesOnlyInvalidConditionWithoutBlockingTagMigration()
        {
            StringTag currentTag = new("text", "migration-filter-repair-tag");
            TagCollection current = new(Array.Empty<BaseTag>(), new BaseTag[] { currentTag }, Array.Empty<BaseTag>());
            TagCollection proposed = new(Array.Empty<BaseTag>(), new BaseTag[] { new IntTag("text", currentTag.ID) }, Array.Empty<BaseTag>());
            StringTagValue value = new(currentTag, "12", "migration-filter-repair-value");
            Patient patient = CreatePatient(value);
            StringTagFilterValue filterValue = new() { Value = "1", ExactMatch = false, ID = "migration-filter-repair-filter-value" };
            PatientTagFilterCondition condition = new(PatientTagFilterCondition.TargetType.Patient, currentTag, filterValue, false, "migration-filter-repair-condition");
            FilterConditionsPresetCollection filters = CreateFilters(condition);
            TagSchemaMigrationService service = new();

            TagSchemaMigrationPlan plan = service.Plan(current, proposed, new[] { patient }, filters, new HashSet<string> { currentTag.ID }, TagParsingPolicy.Default);

            Assert.That(plan.IsValid, Is.True);
            Assert.That(plan.Warnings.Any(warning => warning.Contains("Removed condition")), Is.True);
            service.Commit(plan);
            Assert.That(patient.Tags.Single(), Is.TypeOf<IntTagValue>());
            Assert.That(filters.GetPresets(typeof(Patient)).Single().ID, Is.EqualTo("migration-filter-preset"));
            Assert.That(filters.GetPresets(typeof(Patient)).Single().Conditions, Is.Empty);
            Assert.That(current.TryGetTag(currentTag.ID, out BaseTag canonical), Is.True);
            Assert.That(canonical, Is.TypeOf<IntTag>());

            plan.Rollback();

            Assert.That(patient.Tags.Single(), Is.SameAs(value));
            Assert.That(GetPatientCondition(filters).ID, Is.EqualTo(condition.ID));
            Assert.That(GetPatientCondition(filters).Tag, Is.SameAs(currentTag));
            Assert.That(current.TryGetTag(currentTag.ID, out BaseTag restored), Is.True);
            Assert.That(restored, Is.SameAs(currentTag));
        }

        [Test]
        public void IncompatibleValue_IsRemovedAndReportedAndRollbackRestoresIt()
        {
            StringTag currentTag = new("number", "migration-removal-tag");
            TagCollection current = new(Array.Empty<BaseTag>(), new BaseTag[] { currentTag }, Array.Empty<BaseTag>());
            TagCollection proposed = new(Array.Empty<BaseTag>(), new BaseTag[] { new IntTag("number", currentTag.ID) }, Array.Empty<BaseTag>());
            StringTagValue source = new(currentTag, "not a number", "migration-removal-value");
            Patient patient = CreatePatient(source);
            TagSchemaMigrationService service = new();

            TagSchemaMigrationPlan plan = service.Plan(current, proposed, new[] { patient }, null, new HashSet<string> { currentTag.ID }, TagParsingPolicy.Default);

            Assert.That(plan.IsValid, Is.True);
            Assert.That(plan.RemovedValueCount, Is.EqualTo(1));
            Assert.That(plan.RemovedValues[0].TagID, Is.EqualTo(currentTag.ID));
            Assert.That(plan.RemovedValues[0].ValueID, Is.EqualTo(source.ID));
            Assert.That(plan.RemovedValues[0].PatientID, Is.EqualTo(patient.ID));
            Assert.That(plan.RemovedValues[0].PatientName, Is.EqualTo(patient.Name));
            Assert.That(plan.RemovedValues[0].Reason, Does.Contain("cannot"));
            service.Commit(plan);
            Assert.That(patient.Tags, Is.Empty);

            plan.Rollback();
            Assert.That(patient.Tags.Single(), Is.SameAs(source));
        }

        [Test]
        public void DeletedTag_RemovesAndReportsItsValues()
        {
            StringTag currentTag = new("obsolete", "migration-delete-tag");
            TagCollection current = new(Array.Empty<BaseTag>(), new BaseTag[] { currentTag }, Array.Empty<BaseTag>());
            TagCollection proposed = new(Array.Empty<BaseTag>(), Array.Empty<BaseTag>(), Array.Empty<BaseTag>());
            StringTagValue source = new(currentTag, "remove me", "migration-delete-value");
            Patient patient = CreatePatient(source);
            TagSchemaMigrationService service = new();

            TagSchemaMigrationPlan plan = service.Plan(current, proposed, new[] { patient }, null, new HashSet<string> { currentTag.ID }, TagParsingPolicy.Default);

            Assert.That(plan.IsValid, Is.True);
            Assert.That(plan.RemovedValueCount, Is.EqualTo(1));
            Assert.That(plan.RemovedValues.Single().ValueID, Is.EqualTo(source.ID));
            Assert.That(plan.RemovedValues.Single().Reason, Does.Contain("removed"));
            service.Commit(plan);
            Assert.That(patient.Tags, Is.Empty);
        }

        [Test]
        public void NestedAndMultipleSiteFilters_AreMigratedWithIdsPreserved()
        {
            StringTag currentTag = new("flag", "migration-nested-tag");
            TagCollection current = new(new BaseTag[] { currentTag }, Array.Empty<BaseTag>(), Array.Empty<BaseTag>());
            TagCollection proposed = new(new BaseTag[] { new BoolTag("flag", currentTag.ID) }, Array.Empty<BaseTag>(), Array.Empty<BaseTag>());
            StringTagFilterValue patientValue = new() { Value = "yes", ExactMatch = true, ID = "migration-nested-patient-value" };
            PatientTagFilterCondition patientCondition = new(PatientTagFilterCondition.TargetType.Patient, currentTag, patientValue, false, "migration-nested-patient-condition");
            StringTagFilterValue siteValue = new() { Value = "no", ExactMatch = true, ID = "migration-nested-site-value" };
            SingleTagFilter single = new(currentTag, siteValue, "migration-nested-single");
            MultipleSiteTagsFilterCondition multiple = new(new[] { single }, false, "migration-nested-multiple");
            AllFilterCondition all = new(new BaseFilterCondition[] { patientCondition, multiple }, false, "migration-nested-all");
            FilterConditionsPresetCollection filters = new();
            filters.AddPreset(new FilterConditionsPreset("preset", new BaseFilterCondition[] { all }), typeof(Patient), false);
            TagSchemaMigrationService service = new();

            TagSchemaMigrationPlan plan = service.Plan(current, proposed, Array.Empty<Patient>(), filters, new HashSet<string> { currentTag.ID }, TagParsingPolicy.Default);
            service.Commit(plan);

            AllFilterCondition migratedAll = (AllFilterCondition)filters.GetPresets(typeof(Patient)).Single().Conditions.Single();
            PatientTagFilterCondition migratedPatient = (PatientTagFilterCondition)migratedAll.Conditions[0];
            SingleTagFilter migratedSingle = ((MultipleSiteTagsFilterCondition)migratedAll.Conditions[1]).TagFilters.Single();
            Assert.That(plan.FilterCount, Is.EqualTo(2));
            Assert.That(migratedPatient.ID, Is.EqualTo(patientCondition.ID));
            Assert.That(migratedPatient.Value.ID, Is.EqualTo(patientValue.ID));
            Assert.That(migratedPatient.Value, Is.TypeOf<BoolTagFilterValue>());
            Assert.That(migratedSingle.ID, Is.EqualTo(single.ID));
            Assert.That(migratedSingle.Value.ID, Is.EqualTo(siteValue.ID));
            Assert.That(migratedSingle.Value, Is.TypeOf<BoolTagFilterValue>());
            Assert.That(current.TryGetTag(currentTag.ID, out BaseTag canonical), Is.True);
            Assert.That(migratedPatient.Tag, Is.SameAs(canonical));
            Assert.That(migratedSingle.Tag, Is.SameAs(canonical));
        }

        [Test]
        public void CategoryMoveAndStalePlan_AreRejected()
        {
            StringTag currentTag = new("tag", "migration-category-tag");
            TagCollection current = new(Array.Empty<BaseTag>(), new BaseTag[] { currentTag }, Array.Empty<BaseTag>());
            TagCollection moved = new(Array.Empty<BaseTag>(), Array.Empty<BaseTag>(), new BaseTag[] { new BoolTag("tag", currentTag.ID) });
            TagSchemaMigrationService service = new();

            TagSchemaMigrationPlan movedPlan = service.Plan(current, moved, Array.Empty<Patient>(), new FilterConditionsPresetCollection(), new HashSet<string> { currentTag.ID }, TagParsingPolicy.Default);
            Assert.That(movedPlan.IsValid, Is.False);
            Assert.That(movedPlan.Issues.Single().Message, Does.Contain("cannot move"));

            TagCollection valid = new(Array.Empty<BaseTag>(), new BaseTag[] { new BoolTag("tag", currentTag.ID) }, Array.Empty<BaseTag>());
            TagSchemaMigrationPlan stalePlan = service.Plan(current, valid, Array.Empty<Patient>(), new FilterConditionsPresetCollection(), new HashSet<string> { currentTag.ID }, TagParsingPolicy.Default);
            current.ReplaceTagDefinition(currentTag.ID, new StringTag("changed", currentTag.ID), false);
            Assert.Throws<InvalidOperationException>(() => service.Commit(stalePlan));
        }

        [Test]
        public void PlanRejectsInPlaceDefinitionFilterAndOwnerGraphChanges()
        {
            StringTag currentTag = new("tag", "migration-content-stale-tag");
            TagCollection current = new(Array.Empty<BaseTag>(), new BaseTag[] { currentTag }, Array.Empty<BaseTag>());
            TagCollection proposed = new(Array.Empty<BaseTag>(), new BaseTag[] { new BoolTag("tag", currentTag.ID) }, Array.Empty<BaseTag>());
            Patient patient = CreatePatient(new StringTagValue(currentTag, "yes", "migration-content-stale-value"));
            StringTagFilterValue filterValue = new() { Value = "yes", ExactMatch = true, ID = "migration-content-stale-filter" };
            FilterConditionsPresetCollection filters = CreateFilters(new PatientTagFilterCondition(PatientTagFilterCondition.TargetType.Patient, currentTag, filterValue, false));
            TagSchemaMigrationService service = new();

            TagSchemaMigrationPlan definitionPlan = service.Plan(current, proposed, new[] { patient }, filters, new HashSet<string> { currentTag.ID }, TagParsingPolicy.Default);
            currentTag.Name = "edited";
            Assert.Throws<InvalidOperationException>(() => service.Commit(definitionPlan));
            currentTag.Name = "tag";

            TagSchemaMigrationPlan filterPlan = service.Plan(current, proposed, new[] { patient }, filters, new HashSet<string> { currentTag.ID }, TagParsingPolicy.Default);
            filterValue.Value = "no";
            Assert.Throws<InvalidOperationException>(() => service.Commit(filterPlan));
            filterValue.Value = "yes";

            TagSchemaMigrationPlan ownerPlan = service.Plan(current, proposed, new[] { patient }, filters, new HashSet<string> { currentTag.ID }, TagParsingPolicy.Default);
            patient.Sites.Add(new Site("new site", Array.Empty<Coordinate>(), Array.Empty<BaseTagValue>(), "migration-content-stale-site"));
            Assert.That(ownerPlan.MatchesOwnerGraph(new[] { patient, CreatePatient(new StringTagValue(currentTag, "yes")) }), Is.False);
            Assert.Throws<InvalidOperationException>(() => service.Commit(ownerPlan));
        }

        [Test]
        public void TagFilterValueClones_PreserveIds()
        {
            EnumTag enumTag = new("enum", new[] { "one" }, "migration-filter-clone-enum");
            EnumTagFilterValue enumValue = new() { ID = "migration-filter-clone-enum-value" };
            enumValue.SetValue(enumTag, 0);
            TagFilterValue[] values =
            {
                new EmptyTagFilterValue { ID = "migration-filter-clone-empty" },
                new BoolTagFilterValue { Value = true, ID = "migration-filter-clone-bool" },
                new StringTagFilterValue { Value = "text", ExactMatch = true, ID = "migration-filter-clone-string" },
                new NumberTagFilterValue { Type = NumberComparisonType.Range, Min = 1, Max = 2, ID = "migration-filter-clone-number" },
                enumValue
            };

            foreach (TagFilterValue value in values)
            {
                Assert.That(((TagFilterValue)value.Clone()).ID, Is.EqualTo(value.ID), value.GetType().Name);
            }
        }

        [Test]
        public void ReplaceTagDefinition_PreservesCategoryAndPositionAndChecksId()
        {
            StringTag first = new("first", "migration-replace-first");
            StringTag second = new("second", "migration-replace-second");
            TagCollection collection = new(Array.Empty<BaseTag>(), new BaseTag[] { first, second }, Array.Empty<BaseTag>());
            BoolTag replacement = new("first", first.ID);

            collection.ReplaceTagDefinition(first.ID, replacement, false);

            Assert.That(collection.PatientsTags, Is.EqualTo(new BaseTag[] { replacement, second }));
            Assert.That(collection.TryGetCategory(first.ID, out TagCategory category), Is.True);
            Assert.That(category, Is.EqualTo(TagCategory.Patient));
            Assert.Throws<InvalidOperationException>(() => collection.ReplaceTagDefinition(first.ID, new BoolTag("wrong", "another-id"), false));
            Assert.Throws<InvalidOperationException>(() => collection.ReplaceTagDefinition(first.ID, TagCategory.Site, new StringTag("wrong category", first.ID), replacement, false));
            Assert.Throws<InvalidOperationException>(() => collection.ReplaceTagDefinition(first.ID, TagCategory.Patient, new StringTag("stale", first.ID), first, false));
        }

        [Test]
        public void AtomicJsonSave_LeavesExistingFileUntouchedWhenReplacementFails()
        {
            using TempDirectoryScope temp = new();
            string path = temp.GetPath("atomic-save.json");
            File.WriteAllText(path, "original");

            using (new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                Assert.Catch<IOException>(() => ClassLoaderSaver.SaveToJsonAtomicOrThrow(new StringTag("tag", "migration-atomic-save-tag"), path));
            }

            Assert.That(File.ReadAllText(path), Is.EqualTo("original"));
            Assert.That(Directory.GetFiles(temp.Path, "atomic-save.json.tmp-*"), Is.Empty);
            Assert.That(Directory.GetFiles(temp.Path, "atomic-save.json.bak-*"), Is.Empty);
        }

        [Test]
        public void DatabaseDirectoryPublish_RollsBackPatientsAndDataInfosTogether()
        {
            using TempDirectoryScope temp = new();
            string patientsPath = temp.GetPath("Patients");
            string dataInfosPath = temp.GetPath("DataInfos");
            Directory.CreateDirectory(patientsPath);
            Directory.CreateDirectory(dataInfosPath);
            File.WriteAllText(Path.Combine(patientsPath, "value.txt"), "old patients");
            File.WriteAllText(Path.Combine(dataInfosPath, "value.txt"), "old data infos");
            DirectoryInfo patientsTemp = Directory.CreateDirectory(temp.GetPath("PatientsTemp-test"));
            DirectoryInfo dataInfosTemp = Directory.CreateDirectory(temp.GetPath("DataInfosTemp-test"));
            File.WriteAllText(Path.Combine(patientsTemp.FullName, "value.txt"), "new patients");
            File.WriteAllText(Path.Combine(dataInfosTemp.FullName, "value.txt"), "new data infos");

            Assert.Throws<InvalidOperationException>(() => GlobalDatabase.ReplaceDatabaseDirectoriesAtomically(temp.Path, patientsTemp, dataInfosTemp, () => throw new InvalidOperationException("stale workspace")));

            Assert.That(File.ReadAllText(Path.Combine(patientsPath, "value.txt")), Is.EqualTo("old patients"));
            Assert.That(File.ReadAllText(Path.Combine(dataInfosPath, "value.txt")), Is.EqualTo("old data infos"));
            Assert.That(Directory.GetDirectories(temp.Path, "*Backup-*"), Is.Empty);
        }

        private static Patient CreatePatient(BaseTagValue value, IEnumerable<Site> sites = null)
        {
            return new Patient("patient", Array.Empty<BaseMesh>(), Array.Empty<MRI>(), sites ?? Array.Empty<Site>(), new[] { value }, "database", "migration-patient");
        }

        private static FilterConditionsPresetCollection CreateFilters(PatientTagFilterCondition condition)
        {
            FilterConditionsPresetCollection filters = new();
            filters.AddPreset(new FilterConditionsPreset("preset", new BaseFilterCondition[] { condition }, "migration-filter-preset"), typeof(Patient), false);
            return filters;
        }

        private static PatientTagFilterCondition GetPatientCondition(FilterConditionsPresetCollection filters)
        {
            return (PatientTagFilterCondition)filters.GetPresets(typeof(Patient)).Single().Conditions.Single();
        }
    }
}
