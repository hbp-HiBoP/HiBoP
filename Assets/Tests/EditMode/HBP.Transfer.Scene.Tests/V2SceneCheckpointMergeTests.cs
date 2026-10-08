using System;
using System.Linq;
using System.Reflection;
using HBP.Sync;
using HBP.Sync.Scene;
using NUnit.Framework;

namespace HBP.Tests.Transfer.Scene
{
    [Category("Sync.SceneFocused")]
    public class V2SceneCheckpointMergeTests
    {
        private static readonly ColumnId Column = new ColumnId("column");
        private static readonly SiteId Site = new SiteId("patient_contact");
        private static V2SceneCheckpointState Empty() => V2SceneCheckpointState.FromCheckpoint((V2SceneMutationCheckpoint)Activator.CreateInstance(typeof(V2SceneMutationCheckpoint), BindingFlags.Instance | BindingFlags.NonPublic, null, new object[] { Array.Empty<SiteColorCheckpointRecord>(), Array.Empty<CutDefinitionCheckpointRecord>(), Array.Empty<TimelineAnchorCheckpointRecord>(), null, null, null, null, null }, null));
        private static SetCutDefinition Definition(CutId id, float position) => new SetCutDefinition(id, V2CutOrientation.Axial, false, 1, position, 0, 0, 1);
        private static void AddCut(V2SceneCheckpointState state, string id, int order) => state.ApplyCanonical(new CreateCut(new CutId(id), Definition(new CutId(id), 0), order));

        private static V2SceneCheckpointState SiteBaseline()
        {
            var state = Empty();
            state.ApplyCanonical(new SetSiteConfigurationBatch(new[] { new V2SiteConfigurationAssignment(Column, Site, false, false, 1, 1, 1, 1, Array.Empty<string>()) }));
            return state;
        }

        [Test]
        public void SameSite_IndependentAttributesAndIdenticalChangesMergeWithoutChoice()
        {
            var baseline = SiteBaseline();
            var desktop = baseline.Clone();
            var quest = baseline.Clone();
            desktop.ApplyCanonical(new SetSiteColor(Column, Site, 1, 0, 0, 1));
            quest.ApplyCanonical(new SetSiteLabels(Column, Site, new[] { "new label" }));
            desktop.ApplyCanonical(new SetSiteHighlight(Column, Site, true));
            quest.ApplyCanonical(new SetSiteHighlight(Column, Site, true));
            var merge = new V2SceneCheckpointMerge(baseline, desktop, quest);
            Assert.That(merge.Conflicts, Is.Empty);
            var assignment = merge.Resolve(Array.Empty<V2ConflictChoice>()).ToCheckpoint().T11Records.Select(record => record.Value).OfType<SetSiteConfigurationBatch>().Single().Assignments.Single();
            Assert.That(assignment.Green, Is.Zero);
            Assert.That(assignment.Highlighted, Is.True);
            Assert.That(assignment.Labels, Is.EqualTo(new[] { "new label" }));
        }

        [Test]
        public void SameAttribute_ChoiceKeepsOtherIndependentChanges()
        {
            var baseline = SiteBaseline();
            var desktop = baseline.Clone();
            var quest = baseline.Clone();
            desktop.ApplyCanonical(new SetSiteColor(Column, Site, 1, 0, 0, 1));
            quest.ApplyCanonical(new SetSiteColor(Column, Site, 0, 1, 0, 1));
            desktop.ApplyCanonical(new SetSiteHighlight(Column, Site, true));
            quest.ApplyCanonical(new SetSiteLabels(Column, Site, new[] { "quest" }));
            var merge = new V2SceneCheckpointMerge(baseline, desktop, quest);
            Assert.That(merge.Conflicts.Count, Is.EqualTo(1));
            var assignment = merge.Resolve(new[] { V2ConflictChoice.Quest }).ToCheckpoint().T11Records.Select(record => record.Value).OfType<SetSiteConfigurationBatch>().Single().Assignments.Single();
            Assert.That(assignment.Red, Is.Zero);
            Assert.That(assignment.Highlighted, Is.True);
            Assert.That(assignment.Labels, Is.EqualTo(new[] { "quest" }));
        }

        [TestCase(V2ConflictChoice.Desktop, 0)]
        [TestCase(V2ConflictChoice.Quest, 1)]
        public void CutDeletionVersusEdit_IsOneCoherentChoice(V2ConflictChoice choice, int count)
        {
            var baseline = Empty();
            AddCut(baseline, "cut", 0);
            var desktop = baseline.Clone();
            var quest = baseline.Clone();
            desktop.ApplyCanonical(new DeleteCut(new CutId("cut")));
            quest.ApplyCanonical(Definition(new CutId("cut"), 0.4f));
            var merge = new V2SceneCheckpointMerge(baseline, desktop, quest);
            Assert.That(merge.Conflicts.Count, Is.EqualTo(1));
            var result = merge.Resolve(new[] { choice }).ToCheckpoint();
            Assert.That(result.CutDefinitions, Has.Count.EqualTo(count));
            Assert.That(result.T10Records.Select(record => record.Value).OfType<SetCutOrder>().Single().CutIds, Has.Count.EqualTo(count));
        }

        [Test]
        public void IndependentCutAdditions_AreBothRetained()
        {
            var baseline = Empty();
            AddCut(baseline, "original", 0);
            var desktop = baseline.Clone();
            var quest = baseline.Clone();
            AddCut(desktop, "desktop", 1);
            AddCut(quest, "quest", 1);
            var merge = new V2SceneCheckpointMerge(baseline, desktop, quest);
            Assert.That(merge.Conflicts, Is.Empty);
            var result = merge.Resolve(Array.Empty<V2ConflictChoice>()).ToCheckpoint();
            Assert.That(result.CutDefinitions.Select(record => record.Value.CutId.Value), Is.EquivalentTo(new[] { "original", "desktop", "quest" }));
            Assert.That(result.T10Records.Select(record => record.Value).OfType<SetCutOrder>().Single().CutIds, Has.Count.EqualTo(3));
        }

        [Test]
        public void RoiAndSphereOrder_SurviveCheckpointRoundTrip()
        {
            var state = Empty();
            state.ApplyCanonical(new CreateRoi(new RoiId("z-first"), "first", new[] { new V2RoiSphereDefinition(new SphereId("z"), 1, 2, 3, 4), new V2RoiSphereDefinition(new SphereId("a"), 4, 3, 2, 1) }, 0));
            state.ApplyCanonical(new CreateRoi(new RoiId("a-second"), "second", Array.Empty<V2RoiSphereDefinition>(), 1));
            var restored = V2SceneCheckpointState.FromCheckpoint(V2SceneMutationCheckpointCodec.Decode(V2SceneMutationCheckpointCodec.Encode(0, state.ToCheckpoint())).Checkpoint);
            Assert.That(restored.Hash(), Is.EqualTo(state.Hash()));
            var rois = restored.ToCheckpoint().T10Records.Select(record => record.Value).OfType<CreateRoi>().OrderBy(roi => roi.Order).ToArray();
            Assert.That(rois.Select(roi => roi.Name), Is.EqualTo(new[] { "first", "second" }));
            Assert.That(rois[0].Spheres.Select(sphere => sphere.SphereId.Value), Is.EqualTo(new[] { "z", "a" }));
        }

        [Test]
        public void RoiDeletionVersusSelection_IsOneChoiceAndNeverLeavesDanglingReferences()
        {
            var baseline = Empty();
            var roi = new RoiId("roi");
            baseline.ApplyCanonical(new CreateRoi(roi, "target", new[] { new V2RoiSphereDefinition(new SphereId("sphere"), 0, 0, 0, 1) }, 0));
            baseline.ApplyCanonical(new SetActiveRoi(null));
            baseline.ApplyCanonical(new SetSelectedRoiSphere(roi.Value, ""));
            var desktop = baseline.Clone();
            var quest = baseline.Clone();
            desktop.ApplyCanonical(new DeleteRoi(roi));
            quest.ApplyCanonical(new SetActiveRoi(roi));
            quest.ApplyCanonical(new SetSelectedRoiSphere(roi.Value, "sphere"));
            var merge = new V2SceneCheckpointMerge(baseline, desktop, quest);
            Assert.That(merge.Conflicts.Count, Is.EqualTo(1));
            var result = merge.Resolve(new[] { V2ConflictChoice.Desktop }).ToCheckpoint();
            Assert.That(result.T10Records.Select(record => record.Value).OfType<CreateRoi>(), Is.Empty);
            Assert.That(result.T10Records.Select(record => record.Value).OfType<SetActiveRoi>().Single().RoiId, Is.Null);
        }

        [TestCase(V2ConflictChoice.Desktop, 0)]
        [TestCase(V2ConflictChoice.Quest, 1)]
        public void SphereDeletionVersusSelection_RequiresCoherentChoice(V2ConflictChoice choice, int count)
        {
            var baseline = Empty();
            var roi = new RoiId("roi");
            var sphere = new SphereId("sphere");
            baseline.ApplyCanonical(new CreateRoi(roi, "region", new[] { new V2RoiSphereDefinition(sphere, 0, 0, 0, 1) }, 0));
            baseline.ApplyCanonical(new SetSelectedRoiSphere(roi.Value, ""));
            var desktop = baseline.Clone();
            var quest = baseline.Clone();
            desktop.ApplyCanonical(new DeleteRoiSphere(roi, sphere));
            quest.ApplyCanonical(new SetSelectedRoiSphere(roi.Value, sphere.Value));
            var merge = new V2SceneCheckpointMerge(baseline, desktop, quest);
            Assert.That(merge.Conflicts.Count, Is.EqualTo(1));
            var result = merge.Resolve(new[] { choice }).ToCheckpoint();
            Assert.That(result.T10Records.Select(record => record.Value).OfType<CreateRoi>().Single().Spheres, Has.Count.EqualTo(count));
            Assert.That(result.T09Records.Select(record => record.Value).OfType<SetSelectedRoiSphere>().Single().SphereId, Is.EqualTo(count == 0 ? "" : sphere.Value));
        }

        [Test]
        public void ColumnResourceAndUnrelatedSiteColor_AreIndependent()
        {
            var baseline = SiteBaseline();
            var resourceColumn = new ColumnId("scientific-column");
            baseline.ApplyCanonical(new SetColumnResource(resourceColumn, V2ColumnResourceKind.FmriResource, "old"));
            var desktop = baseline.Clone();
            var quest = baseline.Clone();
            desktop.ApplyCanonical(new SetSiteColor(Column, Site, 0, 1, 0, 1));
            quest.ApplyCanonical(new SetColumnResource(resourceColumn, V2ColumnResourceKind.FmriResource, "new"));
            var merge = new V2SceneCheckpointMerge(baseline, desktop, quest);
            Assert.That(merge.Conflicts, Is.Empty);
            var result = merge.Resolve(Array.Empty<V2ConflictChoice>()).ToCheckpoint();
            Assert.That(result.T11Records.Select(record => record.Value).OfType<SetColumnResource>().Single().ResourceReference, Is.EqualTo("new"));
            Assert.That(result.T11Records.Select(record => record.Value).OfType<SetSiteConfigurationBatch>().Single().Assignments.Single().Red, Is.Zero);
        }

        [Test]
        public void MissingCommonBaseline_RequiresExplicitWholeSceneChoice()
        {
            var desktop = SiteBaseline();
            var quest = desktop.Clone();
            quest.ApplyCanonical(new SetSiteHighlight(Column, Site, true));
            desktop.ApplyCanonical(new SetSceneBoolean(V2SceneBooleanProperty.StrongCuts, true));
            var merge = new V2SceneCheckpointMerge(null, desktop, quest);
            Assert.That(merge.Conflicts.Count, Is.EqualTo(1));
            Assert.That(merge.Resolve(new[] { V2ConflictChoice.Quest }).Hash(), Is.EqualTo(quest.Hash()));
        }

        [Test]
        public void NaturalPlayback_IsIgnoredButExplicitNavigationRemainsAConflict()
        {
            var baseline = Empty();
            SetTimelineAnchor Anchor(int index) => new SetTimelineAnchor(Column, index, true, false, 1, 0, 1, V2TimelineAnchorIntent.Play);
            baseline.ApplyCanonical(Anchor(3));
            var desktop = baseline.Clone();
            var quest = baseline.Clone();
            desktop.ApplyCanonical(Anchor(10));
            quest.ApplyCanonical(Anchor(20));
            var normalize = typeof(V2SceneReconciliationRecord).GetMethod("NormalizeTimelines", BindingFlags.Static | BindingFlags.NonPublic);
            normalize.Invoke(null, new object[] { desktop, baseline, Array.Empty<string>(), baseline });
            normalize.Invoke(null, new object[] { quest, baseline, Array.Empty<string>(), baseline });
            Assert.That(new V2SceneCheckpointMerge(baseline, desktop, quest).Conflicts, Is.Empty);
            desktop.ApplyCanonical(Anchor(10));
            quest.ApplyCanonical(Anchor(20));
            normalize.Invoke(null, new object[] { desktop, baseline, new[] { Column.Value }, baseline });
            normalize.Invoke(null, new object[] { quest, baseline, new[] { Column.Value }, baseline });
            Assert.That(new V2SceneCheckpointMerge(baseline, desktop, quest).Conflicts.Count, Is.EqualTo(1));
        }

        [Test]
        public void IndependentColumnSelections_AreOneChoice()
        {
            var baseline = Empty();
            baseline.ApplyCanonical(new SetSelectedColumn(null));
            baseline.ApplyCanonical(new SetSelectedSite(Column, null));
            var other = new ColumnId("other");
            baseline.ApplyCanonical(new SetSelectedSite(other, null));
            var desktop = baseline.Clone();
            var quest = baseline.Clone();
            desktop.ApplyCanonical(new SetSelectedSite(Column, Site));
            quest.ApplyCanonical(new SetSelectedSite(other, Site));
            var merge = new V2SceneCheckpointMerge(baseline, desktop, quest);
            Assert.That(merge.Conflicts.Count, Is.EqualTo(1));
            var checkpoint = merge.Resolve(new[] { V2ConflictChoice.Quest }).ToCheckpoint();
            Assert.That(checkpoint.T09Records.Select(record => record.Value).OfType<SetSelectedSite>().Count(value => value.SiteId != null), Is.EqualTo(1));
            Assert.That(checkpoint.T09Records.Select(record => record.Value).OfType<SetSelectedColumn>().Single().ColumnId, Is.EqualTo(other));
        }
    }
}
