using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using HBP.Core.Data;
using HBP.Core.Preferences;
using HBP.Core.Tools;
using HBP.Tests.Serialization.Helpers;
using NUnit.Framework;

namespace HBP.Tests.Serialization
{
    public class TagCollectionIndexTests
    {
        [Test]
        public void ViewsAndIndex_StayStableBetweenMutationsAndRefreshAfterEachMutation()
        {
            StringTag general = new("general", "tag-index-general");
            BoolTag patient = new("patient", "tag-index-patient");
            IntTag site = new("site", "tag-index-site");
            TagCollection collection = new(new BaseTag[] { general }, new BaseTag[] { patient }, new BaseTag[] { site }, "tag-index-collection");

            Assert.That(collection.AllTags, Is.SameAs(collection.AllTags));
            Assert.That(collection.GeneralTags, Is.SameAs(collection.GeneralTags));
            Assert.That(collection.PatientsTags, Is.SameAs(collection.PatientsTags));
            Assert.That(collection.SitesTags, Is.SameAs(collection.SitesTags));
            AssertIndexed(collection, general);
            AssertIndexed(collection, patient);
            AssertIndexed(collection, site);

            FloatTag addedGeneral = new("added-general", "tag-index-added-general");
            var previousView = collection.AllTags;
            collection.AddGeneralTag(addedGeneral, false);
            Assert.That(collection.AllTags, Is.Not.SameAs(previousView));
            AssertIndexed(collection, addedGeneral);
            collection.RemoveGeneralTag(addedGeneral, false);
            Assert.That(collection.ContainsTagId(addedGeneral.ID), Is.False);

            EmptyTag addedPatient = new("added-patient", "tag-index-added-patient");
            collection.AddPatientTag(addedPatient, false);
            AssertIndexed(collection, addedPatient);
            collection.RemovePatientTag(addedPatient, false);
            Assert.That(collection.ContainsTagId(addedPatient.ID), Is.False);

            EnumTag addedSite = new("added-site", new[] { "one", "two" }, "tag-index-added-site");
            collection.AddSiteTag(addedSite, false);
            AssertIndexed(collection, addedSite);
            collection.RemoveSiteTag(addedSite, false);
            Assert.That(collection.ContainsTagId(addedSite.ID), Is.False);

            StringTag replacementGeneral = new("replacement-general", "tag-index-replacement-general");
            BoolTag replacementPatient = new("replacement-patient", "tag-index-replacement-patient");
            FloatTag replacementSite = new("replacement-site", "tag-index-replacement-site");
            collection.SetGeneralTags(new BaseTag[] { replacementGeneral }, false);
            collection.SetPatientTags(new BaseTag[] { replacementPatient }, false);
            collection.SetSiteTags(new BaseTag[] { replacementSite }, false);

            Assert.That(collection.ContainsTagId(general.ID), Is.False);
            Assert.That(collection.ContainsTagId(patient.ID), Is.False);
            Assert.That(collection.ContainsTagId(site.ID), Is.False);
            AssertIndexed(collection, replacementGeneral);
            AssertIndexed(collection, replacementPatient);
            AssertIndexed(collection, replacementSite);
            Assert.That(collection.AllTags.Select(tag => tag.ID), Is.EqualTo(new[] { replacementPatient.ID, replacementSite.ID, replacementGeneral.ID }));
        }

        [Test]
        public void DuplicateIds_AreRejectedWithoutCorruptingTheExistingIndex()
        {
            StringTag original = new("original", "tag-index-duplicate");
            TagCollection collection = new(new BaseTag[] { original }, Array.Empty<BaseTag>(), Array.Empty<BaseTag>(), "tag-index-duplicates");
            var originalView = collection.AllTags;

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => collection.AddSiteTag(new BoolTag("conflict", original.ID), false));

            Assert.That(exception.Message, Does.Contain(original.ID));
            Assert.That(collection.AllTags, Is.SameAs(originalView));
            AssertIndexed(collection, original);

            Assert.Throws<InvalidOperationException>(() => new TagCollection(new BaseTag[] { new StringTag("first", "tag-index-constructor-duplicate") }, new BaseTag[] { new BoolTag("second", "tag-index-constructor-duplicate") }, Array.Empty<BaseTag>()));
        }

        [Test]
        public void SameTagInstance_CanRemainVisibleInMultipleCategories()
        {
            StringTag shared = new("shared", "tag-index-shared");

            TagCollection collection = new(new BaseTag[] { shared }, Array.Empty<BaseTag>(), new BaseTag[] { shared }, "tag-index-shared-collection");

            Assert.That(collection.AllTags.Count(tag => ReferenceEquals(tag, shared)), Is.EqualTo(2));
            AssertIndexed(collection, shared);
        }

        [Test]
        public void CopyAndGenerateId_RebuildTheIndex()
        {
            TagCollection source = new(new BaseTag[] { new StringTag("general", "tag-index-copy-general") }, new BaseTag[] { new BoolTag("patient", "tag-index-copy-patient") }, new BaseTag[] { new FloatTag("site", "tag-index-copy-site") }, "tag-index-copy-source");
            TagCollection target = new(Array.Empty<BaseTag>(), Array.Empty<BaseTag>(), Array.Empty<BaseTag>(), "tag-index-copy-target");

            target.Copy(source);
            foreach (BaseTag tag in target.AllTags)
            {
                AssertIndexed(target, tag);
            }

            string[] previousIds = target.AllTags.Select(tag => tag.ID).ToArray();
            target.GenerateID();

            Assert.That(previousIds.All(id => !target.ContainsTagId(id)), Is.True);
            foreach (BaseTag tag in target.AllTags)
            {
                AssertIndexed(target, tag);
            }
        }

        [Test]
        public void JsonRoundTrip_RebuildsTheIndexWithoutChangingTheFormat()
        {
            using TempDirectoryScope temp = new();
            TagCollection source = new(new BaseTag[] { new StringTag("general", "tag-index-json-general") }, new BaseTag[] { new BoolTag("patient", "tag-index-json-patient") }, new BaseTag[] { new EnumTag("site", new[] { "one", "two" }, "tag-index-json-site") }, "tag-index-json-collection");
            string path = temp.GetPath("tag-index.json");

            Assert.That(ClassLoaderSaver.SaveToJSon(source, path, true), Is.True);
            string json = File.ReadAllText(path);
            TagCollection loaded = ClassLoaderSaver.LoadFromJson<TagCollection>(path);

            Assert.That(json, Does.Not.Contain("m_TagById"));
            Assert.That(json, Does.Not.Contain("m_AllTagsView"));
            Assert.That(loaded.AllTags, Has.Count.EqualTo(3));
            foreach (BaseTag tag in loaded.AllTags)
            {
                AssertIndexed(loaded, tag);
            }
        }

        [Test]
        public void LegacyAssemblyCSharpTypes_RebuildTheIndexAfterDeserialization()
        {
            const string json = "{" + "\"$type\":\"HBP.Core.Data.TagCollection, Assembly-CSharp\"," + "\"ID\":\"tag-index-legacy-collection\"," + "\"m_GeneralTags\":[" + "{" + "\"$type\":\"HBP.Core.Data.BoolTag, Assembly-CSharp\"," + "\"ID\":\"tag-index-legacy-bool\"," + "\"Name\":\"legacy-bool\"" + "}" + "]," + "\"m_PatientsTags\":[]," + "\"m_SitesTags\":[]" + "}";

            TagCollection loaded = ClassLoaderSaver.LoadFromJsonString<TagCollection>(json);

            Assert.That(loaded.TryGetTag("tag-index-legacy-bool", out BaseTag tag), Is.True);
            Assert.That(tag, Is.TypeOf<BoolTag>());
            Assert.That(tag.Name, Is.EqualTo("legacy-bool"));
        }

        [Test]
        public async Task CheckTagsAsync_UsesIdsToRemoveUnknownValuesAndConvertLegacyValues()
        {
            using TempDirectoryScope temp = new();
            using PersistentDataTestScope persistentData = new(temp.Path);
            IntTag canonicalTag = new("canonical", "tag-index-check-canonical");
            StringTag unknownTag = new("unknown", "tag-index-check-unknown");
            PersistentDataManager.Tags.SetPatientTags(new BaseTag[] { canonicalTag }, false);

            Patient patient = new("tag-index-patient", Array.Empty<BaseMesh>(), Array.Empty<MRI>(), Array.Empty<Site>(), new BaseTagValue[]
            {
                new BaseTagValue(canonicalTag, 42, "tag-index-legacy-value"),
                new StringTagValue(unknownTag, "remove", "tag-index-unknown-value")
            }, "", "tag-index-patient-id");

            try
            {
                await patient.CheckTagsAsync(new HashSet<string>(StringComparer.Ordinal) { canonicalTag.ID });
            }
            finally
            {
                await UniTask.SwitchToMainThread();
            }

            Assert.That(patient.Tags, Has.Count.EqualTo(1));
            Assert.That(patient.Tags[0], Is.TypeOf<IntTagValue>());
            Assert.That(patient.Tags[0].Tag, Is.SameAs(canonicalTag));
            Assert.That(((IntTagValue)patient.Tags[0]).Value, Is.EqualTo(42));
        }

        [Test]
        public async Task CheckTagsAsync_HandlesOneHundredThousandValuesWithOneSharedIndex()
        {
            using TempDirectoryScope temp = new();
            using PersistentDataTestScope persistentData = new(temp.Path);
            StringTag tag = new("load", "tag-index-load");
            PersistentDataManager.Tags.SetPatientTags(new BaseTag[] { tag }, false);

            const int valueCount = 100_000;
            List<BaseTagValue> values = new(valueCount);
            for (int index = 0; index < valueCount; index++)
            {
                values.Add(new StringTagValue(tag, "value", $"tag-index-load-value-{index}"));
            }

            Patient patient = new("tag-index-load-patient", Array.Empty<BaseMesh>(), Array.Empty<MRI>(), Array.Empty<Site>(), values, "", "tag-index-load-patient-id");
            ISet<string> tagIds = new HashSet<string>(StringComparer.Ordinal) { tag.ID };

            try
            {
                await patient.CheckTagsAsync(tagIds);
            }
            finally
            {
                await UniTask.SwitchToMainThread();
            }

            Assert.That(patient.Tags, Has.Count.EqualTo(valueCount));
            Assert.That(patient.Tags.All(value => ReferenceEquals(value.Tag, tag)), Is.True);
        }

        private static void AssertIndexed(TagCollection collection, BaseTag expected)
        {
            Assert.That(collection.ContainsTagId(expected.ID), Is.True);
            Assert.That(collection.TryGetTag(expected.ID, out BaseTag actual), Is.True);
            Assert.That(actual, Is.SameAs(expected));
        }
    }
}
