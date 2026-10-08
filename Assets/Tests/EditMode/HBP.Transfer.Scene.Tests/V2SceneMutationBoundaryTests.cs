using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Text;
using HBP.Core.Data;
using HBP.Core.Enums;
using HBP.Core.Object3D;
using HBP.Data.Module3D;
using HBP.Quest;
using HBP.Sync;
using HBP.Sync.Scene;
using HBP.Transfer.Scene;
using NUnit.Framework;
using Newtonsoft.Json.Linq;
using UnityEngine;
using CoreVolume = HBP.Core.DLL.Volume;
using RoiSphere = HBP.Data.Module3D.Sphere;
using SceneCut = HBP.Core.Object3D.Cut;
using Object = UnityEngine.Object;

namespace HBP.Tests.Transfer.Scene
{
    [Category("Sync.SceneFocused")]
    public class V2SceneMutationBoundaryTests
    {
        private static readonly SceneId SceneIdForT09 = new(Guid.Parse("10000000-0000-0000-0000-000000000009"));

        [TestCase(V2OriginDevice.Quest)]
        [TestCase(V2OriginDevice.Desktop)]
        public void LocalCutDefinition_CanonicalizesNegativeZeroBeforePublishing(V2OriginDevice origin)
        {
            using var fixture = new BoundSceneFixture(origin, seedCuts: true, configureCutCreation: true);
            float negativeZero = BitConverter.Int32BitsToSingle(int.MinValue);
            var proposals = new List<V2Mutation>();
            fixture.Boundary.MutationProposed += (_, mutation, _) => proposals.Add(mutation);

            fixture.Scene.SetCutDefinition(fixture.Scene.Cuts[0], CutOrientation.Custom, false, negativeZero, new Vector3(negativeZero, 1, negativeZero));

            Assert.That(proposals, Has.Count.EqualTo(1));
            var definition = (SetCutDefinition)proposals[0];
            Assert.That(BitConverter.SingleToInt32Bits(definition.Position), Is.Zero);
            Assert.That(BitConverter.SingleToInt32Bits(definition.NormalX), Is.Zero);
            Assert.That(definition.NormalY, Is.EqualTo(1));
            Assert.That(BitConverter.SingleToInt32Bits(definition.NormalZ), Is.Zero);
        }

        [TestCase(true, false)]
        [TestCase(true, true)]
        [TestCase(false, false)]
        [TestCase(false, true)]
        public void QuestControls_RealSiteSetterPublishesOneIntentAndLatestSelectionSurvivesDelayedEchoes(bool fromQuest, bool acrossColumns)
        {
            using var desktop = new BoundSceneFixture(V2OriginDevice.Desktop);
            using var quest = new BoundSceneFixture(V2OriginDevice.Quest);
            desktop.Boundary.Dispose();
            quest.Boundary.Dispose();
            var desktopB = AddSelectionFixtureSite(desktop, acrossColumns ? desktop.StaticColumn : desktop.Column, "site-b");
            var questB = AddSelectionFixtureSite(quest, acrossColumns ? quest.StaticColumn : quest.Column, "site-b");
            WireExclusiveSelectionCallbacks(desktop.Scene);
            WireExclusiveSelectionCallbacks(quest.Scene);
            using var desktopBoundary = new V2SceneMutationBoundary(desktop.Scene, V2OriginDevice.Desktop);
            using var questBoundary = new V2SceneMutationBoundary(quest.Scene, V2OriginDevice.Quest);
            using var authority = new V2DesktopMutationAuthority(SceneIdForT09, IncarnationIdForT09, desktopBoundary);
            using var driver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, questBoundary, new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest));
            var proposals = new List<V2QuestMutationProposal>();
            var canonicals = new List<V2CanonicalMutation>();
            driver.ProposalQueued += proposals.Add;
            authority.CanonicalReady += canonicals.Add;
            var source = fromQuest ? quest : desktop;
            var b = fromQuest ? questB : desktopB;
            Column3D bColumn = acrossColumns ? source.StaticColumn : source.Column;
            source.Scene.SelectSite(source.Column, source.Site);
            source.Scene.SelectSite(bColumn, b);
            Assert.That(source.Scene.SelectedColumn, Is.SameAs(bColumn));
            Assert.That(bColumn.SelectedSite, Is.SameAs(b));
            source.Scene.SelectSite(source.Column, source.Site);
            if (fromQuest)
                for (int contact = 0; contact < 15; contact++)
                {
                    source.Scene.SelectSite(bColumn, b);
                    source.Scene.SelectSite(source.Column, source.Site);
                }

            Assert.That(source.Scene.SelectedColumn, Is.SameAs(source.Column));
            Assert.That(source.Column.SelectedSite, Is.SameAs(source.Site));
            if (acrossColumns) Assert.That(bColumn.SelectedSite, Is.Null, "Desktop clears the previous column's site.");
            if (fromQuest)
            {
                Assert.That(proposals, Has.Count.EqualTo(1), "One in flight; intermediate nulls and column callbacks must not be separate proposals.");
                Assert.That(driver.PendingProposalCount, Is.EqualTo(2), "Only the in-flight and latest unsent intent are retained.");
                var first = authority.AcceptQuestProposal(proposals[0]);
                Assert.That(first.Outcome, Is.EqualTo(V2ProposalOutcome.Accepted));
                driver.ReceiveCanonical(first.CanonicalMutation);
                Assert.That(quest.Column.SelectedSite, Is.SameAs(quest.Site), "An older echo cannot roll back the latest local contact.");
                Assert.That(proposals, Has.Count.EqualTo(2));
                Assert.That(proposals[1].ObservedCanonicalSequence, Is.EqualTo(first.CanonicalMutation.CanonicalSequence));
                var latest = authority.AcceptQuestProposal(proposals[1]);
                Assert.That(latest.Outcome, Is.EqualTo(V2ProposalOutcome.Accepted));
                driver.ReceiveCanonical(latest.CanonicalMutation);
                Assert.That(driver.ReceiveCanonical(first.CanonicalMutation), Is.False);
            }
            else
            {
                Assert.That(canonicals, Has.Count.EqualTo(3), "Exactly one final site intent per Desktop action.");
                foreach (var canonical in canonicals) driver.ReceiveCanonical(canonical);
                Assert.That(proposals, Is.Empty, "Remote UI callbacks must not echo.");
            }

            Assert.That(desktop.Scene.SelectedColumn, Is.SameAs(desktop.Column));
            Assert.That(quest.Scene.SelectedColumn, Is.SameAs(quest.Column));
            Assert.That(desktop.Column.SelectedSite, Is.SameAs(desktop.Site));
            Assert.That(quest.Column.SelectedSite, Is.SameAs(quest.Site));
            Assert.That(driver.PendingProposalCount, Is.Zero);
            Assert.That(driver.ConnectionState, Is.EqualTo(V2QuestMutationConnectionState.Connected));
            // A null for an old column never reactivates that column.
            questBoundary.Apply(new SetSelectedSite(new ColumnId(quest.StaticColumn.ColumnData.ID), null), V2MutationApplicationOrigin.Remote, T09Operation(400100));
            Assert.That(quest.Scene.SelectedColumn, Is.SameAs(quest.Column));
        }

        [TestCase(false)]
        [TestCase(true)]
        public void QuestControls_CorrectionReplaysLatestContactUnlessItBecameHidden(bool hideWaiting)
        {
            using var desktop = new BoundSceneFixture(V2OriginDevice.Desktop);
            using var quest = new BoundSceneFixture(V2OriginDevice.Quest);
            desktop.Boundary.Dispose();
            quest.Boundary.Dispose();
            var desktopB = AddSelectionFixtureSite(desktop, desktop.Column, "site-b");
            var questB = AddSelectionFixtureSite(quest, quest.Column, "site-b");
            var desktopC = AddSelectionFixtureSite(desktop, desktop.Column, "site-c");
            AddSelectionFixtureSite(quest, quest.Column, "site-c");
            WireExclusiveSelectionCallbacks(desktop.Scene);
            WireExclusiveSelectionCallbacks(quest.Scene);
            using var desktopBoundary = new V2SceneMutationBoundary(desktop.Scene, V2OriginDevice.Desktop);
            using var questBoundary = new V2SceneMutationBoundary(quest.Scene, V2OriginDevice.Quest);
            using var authority = new V2DesktopMutationAuthority(SceneIdForT09, IncarnationIdForT09, desktopBoundary);
            using var driver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, questBoundary, new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest));
            var proposals = new List<V2QuestMutationProposal>();
            driver.ProposalQueued += proposals.Add;
            quest.Scene.SelectSite(quest.Column, quest.Site);
            quest.Scene.SelectSite(quest.Column, questB);
            desktop.Scene.SelectSite(desktop.Column, desktopC);
            questB.State.IsMasked = hideWaiting;
            var rejected = authority.AcceptQuestProposal(proposals[0]);
            Assert.That(rejected.Outcome, Is.EqualTo(V2ProposalOutcome.Rejected));
            driver.ReceiveCorrection(rejected.Correction);
            Assert.That(proposals, Has.Count.EqualTo(hideWaiting ? 1 : 2));
            if (!hideWaiting)
            {
                Assert.That(quest.Column.SelectedSite, Is.SameAs(questB));
                var accepted = authority.AcceptQuestProposal(proposals[1]);
                Assert.That(accepted.Outcome, Is.EqualTo(V2ProposalOutcome.Accepted));
                driver.ReceiveCanonical(accepted.CanonicalMutation);
            }

            Assert.That(driver.PendingProposalCount, Is.Zero);
            Assert.That(driver.ConnectionState, Is.EqualTo(V2QuestMutationConnectionState.Connected));
        }

        [Test]
        public void QuestControls_HiddenWaitingContactFallsBackToConfirmedSelection()
        {
            using var desktop = new BoundSceneFixture(V2OriginDevice.Desktop);
            using var quest = new BoundSceneFixture(V2OriginDevice.Quest);
            desktop.Boundary.Dispose();
            quest.Boundary.Dispose();
            AddSelectionFixtureSite(desktop, desktop.StaticColumn, "site-b");
            var questB = AddSelectionFixtureSite(quest, quest.StaticColumn, "site-b");
            WireExclusiveSelectionCallbacks(desktop.Scene);
            WireExclusiveSelectionCallbacks(quest.Scene);
            using var desktopBoundary = new V2SceneMutationBoundary(desktop.Scene, V2OriginDevice.Desktop);
            using var questBoundary = new V2SceneMutationBoundary(quest.Scene, V2OriginDevice.Quest);
            using var authority = new V2DesktopMutationAuthority(SceneIdForT09, IncarnationIdForT09, desktopBoundary);
            using var driver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, questBoundary, new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest));
            var proposals = new List<V2QuestMutationProposal>();
            driver.ProposalQueued += proposals.Add;
            quest.Scene.SelectSite(quest.Column, quest.Site);
            quest.Scene.SelectSite(quest.StaticColumn, questB);
            questB.State.IsMasked = true;
            var accepted = authority.AcceptQuestProposal(proposals[0]);
            driver.ReceiveCanonical(accepted.CanonicalMutation);
            Assert.That(proposals, Has.Count.EqualTo(1));
            Assert.That(driver.PendingProposalCount, Is.Zero);
            Assert.That(quest.Scene.SelectedColumn, Is.SameAs(quest.Column));
            Assert.That(quest.Column.SelectedSite, Is.SameAs(quest.Site));
            Assert.That(quest.StaticColumn.SelectedSite, Is.Null);
        }

        [Test]
        public void QuestControls_OlderOtherColumnCanonicalCannotUndoNewerSelection()
        {
            using var quest = new BoundSceneFixture(V2OriginDevice.Quest);
            quest.Boundary.Dispose();
            var b = AddSelectionFixtureSite(quest, quest.StaticColumn, "site-b");
            WireExclusiveSelectionCallbacks(quest.Scene);
            using var boundary = new V2SceneMutationBoundary(quest.Scene, V2OriginDevice.Quest);
            using var driver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, boundary, new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest));
            var columnA = new ColumnId(quest.Column.ColumnData.ID);
            var columnB = new ColumnId(quest.StaticColumn.ColumnData.ID);
            var newer = new V2CanonicalMutation(SceneIdForT09, IncarnationIdForT09, T09Operation(400110), 2, new SetSelectedSite(columnB, new SiteId(b.Information.FullID)), V2OriginDevice.Desktop);
            Assert.That(driver.ReceiveCanonical(newer), Is.True);
            var older = new V2CanonicalMutation(SceneIdForT09, IncarnationIdForT09, T09Operation(400111), 1, new SetSelectedSite(columnA, new SiteId(quest.Site.Information.FullID)), V2OriginDevice.Desktop);
            Assert.That(driver.ReceiveCanonical(older), Is.False);
            var olderColumn = new V2CanonicalMutation(SceneIdForT09, IncarnationIdForT09, T09Operation(400112), 1, new SetSelectedColumn(columnA), V2OriginDevice.Desktop);
            Assert.That(driver.ReceiveCanonical(olderColumn), Is.False);
            driver.AdvanceCanonicalWatermark(5, includesSelection: true);
            Assert.That(driver.ReceiveCanonical(new V2CanonicalMutation(SceneIdForT09, IncarnationIdForT09, T09Operation(400113), 4, new SetSelectedSite(columnA, new SiteId(quest.Site.Information.FullID)), V2OriginDevice.Desktop)), Is.False, "A checkpoint also supersedes old selections.");
            Assert.That(quest.Scene.SelectedColumn, Is.SameAs(quest.StaticColumn));
            Assert.That(quest.StaticColumn.SelectedSite, Is.SameAs(b));
            Assert.That(quest.Column.SelectedSite, Is.Null);
        }

        [Test]
        public void QuestControls_OlderSiteForTheSameActiveColumnStillFillsItsIndependentSelectionState()
        {
            using var quest = new BoundSceneFixture(V2OriginDevice.Quest);
            using var driver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, quest.Boundary, new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest));
            var column = new ColumnId(quest.Column.ColumnData.ID);
            driver.ReceiveCanonical(new V2CanonicalMutation(SceneIdForT09, IncarnationIdForT09, T09Operation(400114), 2, new SetSelectedColumn(column), V2OriginDevice.Desktop));
            Assert.That(driver.ReceiveCanonical(new V2CanonicalMutation(SceneIdForT09, IncarnationIdForT09, T09Operation(400115), 1, new SetSelectedSite(column, new SiteId(quest.Site.Information.FullID)), V2OriginDevice.Desktop)), Is.True, "A later column-only value preserves that column's independent selected-site state.");
            Assert.That(quest.Column.SelectedSite, Is.SameAs(quest.Site));
        }

        [Test]
        public void QuestControls_MatchingEchoRestoresItsColumnAfterAnInterveningCanonical()
        {
            using var quest = new BoundSceneFixture(V2OriginDevice.Quest);
            quest.Boundary.Dispose();
            var b = AddSelectionFixtureSite(quest, quest.StaticColumn, "site-b");
            WireExclusiveSelectionCallbacks(quest.Scene);
            using var boundary = new V2SceneMutationBoundary(quest.Scene, V2OriginDevice.Quest);
            using var driver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, boundary, new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest));
            V2QuestMutationProposal proposal = null;
            driver.ProposalQueued += value => proposal = value;
            quest.Scene.SelectSite(quest.StaticColumn, b);
            driver.ReceiveCanonical(new V2CanonicalMutation(SceneIdForT09, IncarnationIdForT09, T09Operation(400120), 1, new SetSelectedSite(new ColumnId(quest.Column.ColumnData.ID), new SiteId(quest.Site.Information.FullID)), V2OriginDevice.Desktop));
            Assert.That(quest.Scene.SelectedColumn, Is.SameAs(quest.Column));
            driver.ReceiveCanonical(new V2CanonicalMutation(SceneIdForT09, IncarnationIdForT09, proposal.OperationId, 2, proposal.Mutation, V2OriginDevice.Quest));
            Assert.That(quest.Scene.SelectedColumn, Is.SameAs(quest.StaticColumn));
            Assert.That(quest.StaticColumn.SelectedSite, Is.SameAs(b));
            Assert.That(quest.Column.SelectedSite, Is.Null);
            Assert.That(driver.PendingProposalCount, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void QuestControls_WaitingSelectionSurvivesShortReconnectAndIsClearedWhenOffline(bool shortReconnect)
        {
            using var quest = new BoundSceneFixture(V2OriginDevice.Quest);
            quest.Boundary.Dispose();
            var b = AddSelectionFixtureSite(quest, quest.StaticColumn, "site-b");
            WireExclusiveSelectionCallbacks(quest.Scene);
            using var boundary = new V2SceneMutationBoundary(quest.Scene, V2OriginDevice.Quest);
            var clock = new TestClock(0);
            using var driver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, boundary, new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest, clock));
            var proposals = new List<V2QuestMutationProposal>();
            driver.ProposalQueued += proposals.Add;
            quest.Scene.SelectSite(quest.Column, quest.Site);
            quest.Scene.SelectSite(quest.StaticColumn, b);
            Assert.That(driver.PendingProposalCount, Is.EqualTo(2));
            driver.BeginDisconnectGrace();
            if (shortReconnect)
            {
                Assert.That(driver.TryReconnect(), Is.True);
                driver.ReceiveCanonical(new V2CanonicalMutation(SceneIdForT09, IncarnationIdForT09, proposals[0].OperationId, 1, proposals[0].Mutation, V2OriginDevice.Quest));
                Assert.That(proposals, Has.Count.EqualTo(2));
                Assert.That(quest.Scene.SelectedColumn, Is.SameAs(quest.StaticColumn));
                Assert.That(quest.StaticColumn.SelectedSite, Is.SameAs(b));
                driver.ReceiveCanonical(new V2CanonicalMutation(SceneIdForT09, IncarnationIdForT09, proposals[1].OperationId, 2, proposals[1].Mutation, V2OriginDevice.Quest));
            }
            else
            {
                clock.Timestamp = 600;
                Assert.That(driver.ConnectionState, Is.EqualTo(V2QuestMutationConnectionState.OfflineLocal));
                Assert.That(driver.AbandonedProposalCount, Is.EqualTo(2));
                quest.Scene.SelectSite(quest.Column, quest.Site);
                Assert.That(quest.Column.SelectedSite, Is.SameAs(quest.Site), "Offline local selection stays immediate.");
                Assert.That(proposals, Has.Count.EqualTo(1), "No waiting selection may leak into the closed online stream.");
            }

            Assert.That(driver.PendingProposalCount, Is.Zero);
        }

        private static HBP.Core.Object3D.Site AddSelectionFixtureSite(BoundSceneFixture fixture, Column3D column, string name)
        {
            var obj = new GameObject(name);
            obj.transform.SetParent(fixture.Root.transform, false);
            var site = obj.AddComponent<HBP.Core.Object3D.Site>();
            site.Information = new SiteInformation { Patient = fixture.Site.Information.Patient, Name = name };
            site.State = new SiteState();
            column.Sites.Add(site);
            // Same selection callback as Column3D.InitSites, including the intermediate deselection.
            site.OnSelectSite.AddListener(selected =>
            {
                if (selected) column.UnselectSite();
                SetAutoProperty(column, "SelectedSite", selected ? site : null);
                column.OnSelectSite.Invoke(column.SelectedSite);
            });
            return site;
        }

        private static void WireExclusiveSelectionCallbacks(Base3DScene scene)
        {
            // The lightweight prepared-scene fixture omits Base3DScene's authored-column callbacks.
            foreach (var column in scene.Columns)
            {
                column.OnSelect.AddListener(() =>
                {
                    foreach (var other in scene.Columns.Where(other => other != column))
                    {
                        other.IsSelected = false;
                        other.UnselectSite();
                    }
                });
                column.OnSelectSite.AddListener(_ =>
                {
                    foreach (var other in scene.Columns.Where(other => !other.IsSelected)) other.UnselectSite();
                });
            }
        }

        [TestCase("unknown")]
        [TestCase("masked")]
        [TestCase("blacklisted")]
        [TestCase("filtered")]
        [TestCase("roi")]
        public void QuestControls_InvalidSiteCannotPublishAndDriverAcceptsNextSelection(string reason)
        {
            using var desktop = new BoundSceneFixture(V2OriginDevice.Desktop);
            using var quest = new BoundSceneFixture(V2OriginDevice.Quest);
            using var authority = new V2DesktopMutationAuthority(SceneIdForT09, IncarnationIdForT09, desktop.Boundary);
            var scheduler = new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest);
            using var driver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, quest.Boundary, scheduler);
            var proposals = new List<V2QuestMutationProposal>();
            driver.ProposalQueued += proposals.Add;
            var column = new ColumnId(quest.Column.ColumnData.ID);
            var site = new SiteId(quest.Site.Information.FullID);
            if (reason == "masked") quest.Site.State.IsMasked = true;
            if (reason == "blacklisted")
            {
                SetPrivateField(quest.Site.State, "m_IsBlackListed", true);
                SetPrivateField(quest.Scene, "m_HideBlacklistedSites", true);
            }

            if (reason == "filtered") quest.Site.State.IsFiltered = false;
            if (reason == "roi")
            {
                SetPrivateField(quest.Scene.ROIManager, "m_SelectedROI", quest.Roi);
                quest.Site.State.IsOutOfROI = true;
            }

            var invalid = new SetSelectedSite(column, reason == "unknown" ? new SiteId("unknown-site") : site);
            if (reason == "unknown") Assert.Throws<KeyNotFoundException>(() => driver.ApplyOptimistic(invalid, T09Operation(400001)));
            else Assert.Throws<InvalidOperationException>(() => driver.ApplyOptimistic(invalid, T09Operation(400001)));
            Assert.That(proposals, Is.Empty);
            Assert.That(quest.Column.SelectedSite, Is.Null);
            Assert.That(driver.PendingProposalCount, Is.Zero);
            quest.Site.State.IsMasked = quest.Site.State.IsOutOfROI = false;
            SetPrivateField(quest.Site.State, "m_IsBlackListed", false); // Fixture setup, not an additional shared control under test.
            quest.Site.State.IsFiltered = true;
            var next = driver.ApplyOptimistic(new SetSelectedSite(column, site), T09Operation(400002));
            var result = authority.AcceptQuestProposal(next);
            Assert.That(result.Outcome, Is.EqualTo(V2ProposalOutcome.Accepted));
            Assert.That(driver.ReceiveCanonical(result.CanonicalMutation), Is.False);
            Assert.That(desktop.Column.SelectedSite, Is.SameAs(desktop.Site));
            Assert.That(driver.PendingProposalCount, Is.Zero);
        }

        [Test]
        public void QuestControls_PendingSiteBecomingMaskedIsCorrectedWithoutEchoAndNextSelectionWorks()
        {
            using var desktop = new BoundSceneFixture(V2OriginDevice.Desktop);
            using var quest = new BoundSceneFixture(V2OriginDevice.Quest);
            using var authority = new V2DesktopMutationAuthority(SceneIdForT09, IncarnationIdForT09, desktop.Boundary);
            var scheduler = new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest);
            using var driver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, quest.Boundary, scheduler);
            int proposals = 0;
            driver.ProposalQueued += _ => proposals++;
            var mutation = new SetSelectedSite(new ColumnId(quest.Column.ColumnData.ID), new SiteId(quest.Site.Information.FullID));
            var pending = driver.ApplyOptimistic(mutation, T09Operation(400010));
            Assert.That(quest.Column.SelectedSite, Is.SameAs(quest.Site));
            desktop.Site.State.IsMasked = true;
            var rejected = authority.AcceptQuestProposal(pending);
            Assert.That(rejected.Outcome, Is.EqualTo(V2ProposalOutcome.Rejected));
            Assert.That(driver.ReceiveCorrection(rejected.Correction), Is.True);
            Assert.That(quest.Column.SelectedSite, Is.Null);
            Assert.That(driver.PendingProposalCount, Is.Zero);
            Assert.That(proposals, Is.EqualTo(1));
            desktop.Site.State.IsMasked = false;
            var accepted = authority.AcceptQuestProposal(driver.ApplyOptimistic(mutation, T09Operation(400011)));
            Assert.That(accepted.Outcome, Is.EqualTo(V2ProposalOutcome.Accepted));
            driver.ReceiveCanonical(accepted.CanonicalMutation);
            Assert.That(quest.Column.SelectedSite, Is.SameAs(quest.Site));
            Assert.That(desktop.Column.SelectedSite, Is.SameAs(desktop.Site));
            Assert.That(driver.PendingProposalCount, Is.Zero);
            Assert.That(driver.ConnectionState, Is.EqualTo(V2QuestMutationConnectionState.Connected));
            Assert.That(proposals, Is.EqualTo(2));
        }

        [TestCase(true, false)]
        [TestCase(false, false)]
        [TestCase(true, true)]
        [TestCase(false, true)]
        public void QuestControls_VisibleExcludedSiteConvergesInBothDirections(bool fromQuest, bool outsideRoi)
        {
            using var desktop = new BoundSceneFixture(V2OriginDevice.Desktop);
            using var quest = new BoundSceneFixture(V2OriginDevice.Quest);
            foreach (var fixture in new[] { desktop, quest })
            {
                SetPrivateField(fixture.Site.State, "m_IsBlackListed", !outsideRoi);
                fixture.Site.State.IsOutOfROI = outsideRoi;
                SetPrivateField(fixture.Scene, "m_ShowAllSites", outsideRoi);
                SetPrivateField(fixture.Scene, "m_HideBlacklistedSites", false);
            }

            using var authority = new V2DesktopMutationAuthority(SceneIdForT09, IncarnationIdForT09, desktop.Boundary);
            var scheduler = new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest);
            using var driver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, quest.Boundary, scheduler);
            var canonicals = new List<V2CanonicalMutation>();
            authority.CanonicalReady += canonicals.Add;
            int proposals = 0;
            driver.ProposalQueued += _ => proposals++;
            var mutation = new SetSelectedSite(new ColumnId(quest.Column.ColumnData.ID), new SiteId(quest.Site.Information.FullID));
            if (fromQuest)
            {
                var accepted = authority.AcceptQuestProposal(driver.ApplyOptimistic(mutation, T09Operation(400030)));
                Assert.That(accepted.Outcome, Is.EqualTo(V2ProposalOutcome.Accepted));
                Assert.That(driver.ReceiveCanonical(accepted.CanonicalMutation), Is.False);
                Assert.That(proposals, Is.EqualTo(1));
            }
            else
            {
                desktop.Boundary.Apply(mutation, V2MutationApplicationOrigin.LocalDesktop, T09Operation(400030));
                Assert.That(canonicals, Has.Count.EqualTo(1));
                Assert.That(driver.ReceiveCanonical(canonicals[0]), Is.True);
                Assert.That(proposals, Is.Zero);
            }

            Assert.That(desktop.Column.SelectedSite, Is.SameAs(desktop.Site));
            Assert.That(quest.Column.SelectedSite, Is.SameAs(quest.Site));
            Assert.That(driver.PendingProposalCount, Is.Zero);
            Assert.That(driver.ConnectionState, Is.EqualTo(V2QuestMutationConnectionState.Connected));
            if (!outsideRoi) Assert.That(quest.Site.State.IsEffectivelyMasked(false), Is.True, "A visible blacklist remains excluded from scientific computations.");
        }

        [TestCase("visible-blacklist")]
        [TestCase("hidden-blacklist")]
        [TestCase("masked")]
        [TestCase("filtered")]
        [TestCase("roi")]
        public void QuestControls_CheckpointRestoresRetainedSelectionIndependentlyOfCurrentVisibility(string kind)
        {
            using var source = new BoundSceneFixture(V2OriginDevice.Desktop);
            using var target = new BoundSceneFixture(V2OriginDevice.Quest);
            SetPrivateField(source.Site.State, "m_IsBlackListed", kind.Contains("blacklist"));
            source.Scene.SelectSite(source.Column, source.Site);
            SetPrivateField(source.Scene, "m_HideBlacklistedSites", kind == "hidden-blacklist");
            source.Site.State.IsMasked = kind == "masked";
            source.Site.State.IsFiltered = kind != "filtered";
            source.Site.State.IsOutOfROI = kind == "roi";
            SetPrivateField(target.Scene, "m_HideBlacklistedSites", true);
            SetPrivateField(target.Site.State, "m_IsBlackListed", true);
            target.Site.State.IsFiltered = false;
            var checkpoint = V2SceneMutationCheckpointCodec.Decode(V2SceneMutationCheckpointCodec.Encode(33, source.Boundary.CaptureCheckpoint())).Checkpoint;
            int proposals = 0;
            target.Boundary.MutationProposed += (_, _, _) => proposals++;
            target.Boundary.ApplyCheckpoint(checkpoint, T09Operation(400040));
            Assert.That(target.Column.SelectedSite, Is.SameAs(target.Site));
            Assert.That(target.Scene.HideBlacklistedSites, Is.EqualTo(source.Scene.HideBlacklistedSites));
            Assert.That(proposals, Is.Zero);
            // Canonical corrections also restore retained identity after a local visibility change.
            target.Column.UnselectSite();
            target.Site.State.IsMasked = true;
            target.Boundary.Apply(new SetSelectedSite(new ColumnId(target.Column.ColumnData.ID), new SiteId(target.Site.Information.FullID)), V2MutationApplicationOrigin.Remote, T09Operation(400041));
            Assert.That(target.Column.SelectedSite, Is.SameAs(target.Site));
            Assert.Throws<InvalidOperationException>(() => target.Boundary.Apply(new SetSelectedSite(new ColumnId(target.Column.ColumnData.ID), new SiteId(target.Site.Information.FullID)), V2MutationApplicationOrigin.LocalQuest, T09Operation(400042)));
            Assert.Throws<KeyNotFoundException>(() => target.Boundary.Apply(new SetSelectedSite(new ColumnId(target.Column.ColumnData.ID), new SiteId("unknown-site")), V2MutationApplicationOrigin.Remote, T09Operation(400043)));
        }

        [Test]
        public void QuestControls_StaleCutPreviewCorrectsThenCurrentPositionSetterPreservesRemoteFields()
        {
            using var desktop = new BoundSceneFixture(V2OriginDevice.Desktop, seedCuts: true);
            using var quest = new BoundSceneFixture(V2OriginDevice.Quest, seedCuts: true);
            using var authority = new V2DesktopMutationAuthority(SceneIdForT09, IncarnationIdForT09, desktop.Boundary);
            var scheduler = new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest);
            using var driver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, quest.Boundary, scheduler);
            var canonicals = new List<V2CanonicalMutation>();
            var proposals = new List<V2QuestMutationProposal>();
            authority.CanonicalReady += canonicals.Add;
            driver.ProposalQueued += proposals.Add;
            var id = new CutId("t10-cut-first");
            desktop.Boundary.Apply(new SetCutDefinition(id, V2CutOrientation.Custom, true, 3, .2f, 0, 1, 0), V2MutationApplicationOrigin.LocalDesktop, T09Operation(400020));
            var stale = driver.ApplyOptimistic(new SetCutDefinition(id, V2CutOrientation.Custom, false, 1, .4f, 1, 0, 0), T09Operation(400021));
            var result = authority.AcceptQuestProposal(stale);
            Assert.That(result.Outcome, Is.EqualTo(V2ProposalOutcome.Rejected));
            Assert.That(result.RejectionCode, Is.EqualTo("stale_sequence"));
            driver.ReceiveCorrection(result.Correction);
            driver.ReceiveCanonical(canonicals[0]);
            var cut = quest.Scene.Cuts[0];
            Assert.That(cut.Flip, Is.True);
            Assert.That(cut.Position, Is.EqualTo(.2f));
            Assert.That(driver.PendingProposalCount, Is.Zero);
            Assert.That(proposals, Has.Count.EqualTo(1));
            cut.Position = .7f; // Real setter builds a complete definition from the updated live object.
            Assert.That(proposals, Has.Count.EqualTo(2));
            var fresh = (SetCutDefinition)proposals[1].Mutation;
            Assert.That(fresh.Flip, Is.True);
            Assert.That(fresh.NumberOfCuts, Is.EqualTo(3));
            Assert.That(fresh.NormalY, Is.EqualTo(1));
            var accepted = authority.AcceptQuestProposal(proposals[1]);
            Assert.That(accepted.Outcome, Is.EqualTo(V2ProposalOutcome.Accepted));
            Assert.That(driver.ReceiveCanonical(accepted.CanonicalMutation), Is.False);
            Assert.That(desktop.Scene.Cuts[0].Position, Is.EqualTo(.7f));
            Assert.That(desktop.Scene.Cuts[0].Flip, Is.True);
            Assert.That(driver.PendingProposalCount, Is.Zero);
            Assert.That(driver.ConnectionState, Is.EqualTo(V2QuestMutationConnectionState.Connected));
            Assert.That(proposals, Has.Count.EqualTo(2));
        }

        [Test]
        public void QuestCutControls_DelayedConfirmationKeepsLatestPositionAndMergedNormalIntent()
        {
            using var desktop = new BoundSceneFixture(V2OriginDevice.Desktop, seedCuts: true);
            using var quest = new BoundSceneFixture(V2OriginDevice.Quest, seedCuts: true);
            using var authority = new V2DesktopMutationAuthority(SceneIdForT09, IncarnationIdForT09, desktop.Boundary);
            var scheduler = new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest);
            using var driver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, quest.Boundary, scheduler);
            var proposals = new List<V2QuestMutationProposal>();
            driver.ProposalQueued += proposals.Add;
            var cut = quest.Scene.Cuts[0];
            for (int i = 1; i <= 100; i++) HBP.Quest.QuestCutCommands.Position(quest.Scene, cut, .5f + i * .003f);
            Assert.That(HBP.Quest.QuestCutCommands.Normal(quest.Scene, cut, "1", "1", "0"), Is.True);
            Assert.That(proposals, Has.Count.EqualTo(1), "A gesture can finish entirely before any ACK.");
            Assert.That(driver.PendingProposalCount, Is.EqualTo(2));
            Assert.That(cut.Position, Is.EqualTo(.8f).Within(1e-6));
            DrainCutProposals(authority, driver, proposals);
            Assert.That(proposals, Has.Count.EqualTo(2));
            Assert.That(quest.Scene.Cuts[0].Position, Is.EqualTo(.8f).Within(1e-6));
            Assert.That(desktop.Scene.Cuts[0].Position, Is.EqualTo(.8f).Within(1e-6));
            Assert.That(desktop.Scene.Cuts[0].Normal, Is.EqualTo(new Vector3(1, 1, 0)));
            Assert.That(driver.PendingProposalCount, Is.Zero);
        }

        [Test]
        public void QuestCutControls_CorrectionRebasesOnlyPositionOntoRemoteDefinition()
        {
            using var desktop = new BoundSceneFixture(V2OriginDevice.Desktop, seedCuts: true);
            using var quest = new BoundSceneFixture(V2OriginDevice.Quest, seedCuts: true);
            using var authority = new V2DesktopMutationAuthority(SceneIdForT09, IncarnationIdForT09, desktop.Boundary);
            var scheduler = new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest);
            using var driver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, quest.Boundary, scheduler);
            var proposals = new List<V2QuestMutationProposal>();
            driver.ProposalQueued += proposals.Add;
            var cut = quest.Scene.Cuts[0];
            HBP.Quest.QuestCutCommands.Position(quest.Scene, cut, .6f);
            HBP.Quest.QuestCutCommands.Position(quest.Scene, cut, .7f);
            desktop.Boundary.Apply(new SetCutDefinition(new CutId(cut.ID), V2CutOrientation.Custom, true, 17, .2f, 0, 1, 0), V2MutationApplicationOrigin.LocalDesktop, T09Operation(400080));
            var rejected = authority.AcceptQuestProposal(proposals[0]);
            Assert.That(rejected.Outcome, Is.EqualTo(V2ProposalOutcome.Rejected));
            driver.ReceiveCorrection(rejected.Correction);
            Assert.That(proposals, Has.Count.EqualTo(2));
            var latest = (SetCutDefinition)proposals[1].Mutation;
            Assert.That(latest.Position, Is.EqualTo(.7f));
            Assert.That(latest.Flip, Is.True);
            Assert.That(latest.NumberOfCuts, Is.EqualTo(17));
            Assert.That(latest.NormalY, Is.EqualTo(1));
            var accepted = authority.AcceptQuestProposal(proposals[1]);
            Assert.That(accepted.Outcome, Is.EqualTo(V2ProposalOutcome.Accepted));
            driver.ReceiveCanonical(accepted.CanonicalMutation);
            Assert.That(cut.Position, Is.EqualTo(.7f));
            Assert.That(cut.Flip, Is.True);
            Assert.That(driver.PendingProposalCount, Is.Zero);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void QuestCutControls_CreateEditAndDeleteBeforeAckRespectStructuralBarriers(bool deleteBeforeAck)
        {
            using var desktop = new BoundSceneFixture(V2OriginDevice.Desktop, configureCutCreation: true);
            using var quest = new BoundSceneFixture(V2OriginDevice.Quest, configureCutCreation: true);
            desktop.Scene.Columns.Clear();
            quest.Scene.Columns.Clear();
            using var authority = new V2DesktopMutationAuthority(SceneIdForT09, IncarnationIdForT09, desktop.Boundary);
            var scheduler = new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest);
            using var driver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, quest.Boundary, scheduler);
            var proposals = new List<V2QuestMutationProposal>();
            driver.ProposalQueued += proposals.Add;
            var first = quest.Scene.AddCutPlane();
            HBP.Quest.QuestCutCommands.Position(quest.Scene, first, .8f);
            HBP.Quest.QuestCutCommands.Flip(quest.Scene, first, true);
            HBP.Quest.QuestCutCommands.Orientation(quest.Scene, first, HBP.Core.Enums.CutOrientation.Coronal);
            if (deleteBeforeAck) quest.Scene.RemoveCutPlane(first);
            var second = quest.Scene.AddCutPlane();
            HBP.Quest.QuestCutCommands.Position(quest.Scene, second, .3f);
            Assert.That(proposals, Has.Count.EqualTo(1));
            DrainCutProposals(authority, driver, proposals);
            Assert.That(desktop.Scene.Cuts.Select(c => c.ID), Is.EqualTo(quest.Scene.Cuts.Select(c => c.ID)));
            Assert.That(desktop.Scene.Cuts.Last().Position, Is.EqualTo(.3f));
            if (!deleteBeforeAck)
            {
                Assert.That(desktop.Scene.Cuts[0].Position, Is.EqualTo(.2f).Within(1e-6));
                Assert.That(desktop.Scene.Cuts[0].Flip, Is.True);
                Assert.That(desktop.Scene.Cuts[0].Orientation, Is.EqualTo(HBP.Core.Enums.CutOrientation.Coronal));
            }

            Assert.That(driver.PendingProposalCount, Is.Zero);
        }

        [Test]
        public void QuestCutControls_RemoteDefinitionDuringPendingDeletionRestoresAuthoritativeCutOnRejection()
        {
            using var desktop = new BoundSceneFixture(V2OriginDevice.Desktop, seedCuts: true, configureCutCreation: true);
            using var quest = new BoundSceneFixture(V2OriginDevice.Quest, seedCuts: true, configureCutCreation: true);
            using var authority = new V2DesktopMutationAuthority(SceneIdForT09, IncarnationIdForT09, desktop.Boundary);
            var scheduler = new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest);
            using var driver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, quest.Boundary, scheduler);
            var proposals = new List<V2QuestMutationProposal>();
            var canonical = new List<V2CanonicalMutation>();
            driver.ProposalQueued += proposals.Add;
            authority.CanonicalReady += canonical.Add;
            var cut = quest.Scene.Cuts[1];
            var id = new CutId(cut.ID);
            quest.Scene.RemoveCutPlane(cut);
            desktop.Boundary.Apply(new SetCutDefinition(id, V2CutOrientation.Custom, true, 17, .2f, 0, 1, 0), V2MutationApplicationOrigin.LocalDesktop, T09Operation(400081));
            Assert.DoesNotThrow(() => driver.ReceiveCanonical(canonical[0]));
            Assert.That(quest.Scene.Cuts.Any(c => c.ID == id.Value), Is.False);
            var rejected = authority.AcceptQuestProposal(proposals[0]);
            Assert.That(rejected.Outcome, Is.EqualTo(V2ProposalOutcome.Rejected));
            Assert.DoesNotThrow(() => driver.ReceiveCorrection(rejected.Correction));
            Assert.That(quest.Scene.Cuts.Single(c => c.ID == id.Value).Position, Is.EqualTo(.2f));
            Assert.That(quest.Scene.Cuts.Select(c => c.ID), Is.EqualTo(desktop.Scene.Cuts.Select(c => c.ID)));
            Assert.That(driver.PendingProposalCount, Is.Zero);
        }

        [TestCase("create", false)]
        [TestCase("delete", false)]
        [TestCase("order", false)]
        [TestCase("create", true)]
        [TestCase("delete", true)]
        [TestCase("order", true)]
        public void QuestCutControls_CheckpointReconcilesStructuralEditsBeforeTheirConfirmation(string kind, bool queuedBehindPosition)
        {
            using var desktop = new BoundSceneFixture(V2OriginDevice.Desktop, seedCuts: true, configureCutCreation: true);
            using var quest = new BoundSceneFixture(V2OriginDevice.Quest, seedCuts: true, configureCutCreation: true);
            using var authority = new V2DesktopMutationAuthority(SceneIdForT09, IncarnationIdForT09, desktop.Boundary);
            var scheduler = new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest);
            using var driver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, quest.Boundary, scheduler);
            var proposals = new List<V2QuestMutationProposal>();
            driver.ProposalQueued += proposals.Add;
            if (queuedBehindPosition) HBP.Quest.QuestCutCommands.Position(quest.Scene, quest.Scene.Cuts[0], .6f);
            var id = new CutId("checkpoint-new-cut");
            V2Mutation structural = kind == "create" ? new CreateCut(id, new SetCutDefinition(id, V2CutOrientation.Custom, false, 1, .4f, 1, 0, 0), 1) : kind == "delete" ? new DeleteCut(new CutId(quest.Scene.Cuts[1].ID)) : new SetCutOrder(quest.Scene.Cuts.Select(c => new CutId(c.ID)).Reverse());
            driver.ApplyOptimistic(structural, T09Operation(400090));
            quest.Boundary.ApplyCheckpoint(desktop.Boundary.CaptureCheckpoint(), T09Operation(400091));
            driver.AdvanceCanonicalWatermark(0, includesSelection: true);
            // The checkpoint predates acceptance, so the original structural proposal remains eligible.
            DrainCutProposals(authority, driver, proposals);
            Assert.That(quest.Scene.Cuts.Select(c => c.ID), Is.EqualTo(desktop.Scene.Cuts.Select(c => c.ID)));
            Assert.That(driver.PendingProposalCount, Is.Zero);
        }

        private static void DrainCutProposals(V2DesktopMutationAuthority authority, V2QuestMutationDriver driver, List<V2QuestMutationProposal> proposals)
        {
            for (int i = 0; i < proposals.Count; i++)
            {
                Assert.That(i, Is.LessThan(20), "Bounded cut queue must drain without retries looping.");
                var accepted = authority.AcceptQuestProposal(proposals[i]);
                Assert.That(accepted.Outcome, Is.EqualTo(V2ProposalOutcome.Accepted), accepted.RejectionCode);
                driver.ReceiveCanonical(accepted.CanonicalMutation);
            }
        }

        [Test]
        public void RejectedOptimisticCutCreateEditDelete_ResolvesAbsenceWithoutGhostOrCompletionGap()
        {
            using var desktop = new BoundSceneFixture(V2OriginDevice.Desktop, configureCutCreation: true);
            using var quest = new BoundSceneFixture(V2OriginDevice.Quest, configureCutCreation: true);
            desktop.Scene.Columns.Clear();
            quest.Scene.Columns.Clear();
            using var authority = new V2DesktopMutationAuthority(SceneIdForT09, IncarnationIdForT09, desktop.Boundary);
            var scheduler = new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest);
            using var driver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, quest.Boundary, scheduler);
            authority.CanonicalReady += canonical => driver.ReceiveCanonical(canonical);
            var cut = new CutId("rejected-optimistic-cut");
            var proposals = new[]
            {
                driver.ApplyOptimistic(new CreateCut(cut, new SetCutDefinition(cut, V2CutOrientation.Custom, false, 1, 0, 1, 0, 0), 0), T09Operation(2100000)),
                driver.ApplyOptimistic(new SetCutDefinition(cut, V2CutOrientation.Custom, true, 2, 0.5f, 0, 1, 0), T09Operation(2100001)),
                driver.ApplyOptimistic(new DeleteCut(cut), T09Operation(2100002))
            };
            Assert.That(quest.Scene.Cuts, Is.Empty);
            desktop.Boundary.Apply(new SetConfigurationTransaction(new V2Mutation[] { new SetSceneBoolean(V2SceneBooleanProperty.ShowAllSites, !desktop.Scene.ShowAllSites) }), V2MutationApplicationOrigin.LocalDesktop, T09Operation(2100003));
            var completion = new V2ApplicationCompletionWatermark();
            ulong origin = 0;
            foreach (V2QuestMutationProposal proposal in proposals)
            {
                V2DesktopProposalResult result = authority.AcceptQuestProposal(proposal);
                Assert.That(result.Outcome, Is.EqualTo(V2ProposalOutcome.Rejected));
                Assert.That(result.Correction?.AuthoritativeMutation, Is.TypeOf<DeleteCut>());
                driver.ReceiveCorrection(result.Correction);
                completion.Complete(++origin);
                Assert.That(quest.Scene.Cuts, Is.Empty);
                authority.RetireOperation(proposal.OperationId);
            }

            Assert.That(completion.CompletedThrough, Is.EqualTo(3UL));
            Assert.That(driver.PendingProposalCount, Is.Zero);
            Assert.That(driver.ConnectionState, Is.EqualTo(V2QuestMutationConnectionState.Connected));
            Assert.That(quest.Boundary.OptimisticRollbackOrderCount, Is.Zero);
            Assert.That(desktop.Scene.Cuts, Is.Empty);
            Assert.Throws<KeyNotFoundException>(() => quest.Boundary.Apply(new DeleteCut(cut), V2MutationApplicationOrigin.LocalQuest, T09Operation(2100004)), "A local deletion still requires an existing cut.");
        }

        [Test]
        public void ApplicationRetirement_RepeatedCutCreationDeletionKeepsBothScenesAndHistoriesBounded()
        {
            using var desktop = new BoundSceneFixture(V2OriginDevice.Desktop, configureCutCreation: true);
            using var quest = new BoundSceneFixture(V2OriginDevice.Quest, configureCutCreation: true);
            desktop.Scene.Columns.Clear();
            quest.Scene.Columns.Clear();
            using var authority = new V2DesktopMutationAuthority(SceneIdForT09, IncarnationIdForT09, desktop.Boundary, maximumOperations: 8, maximumKeys: 8);
            var scheduler = new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest);
            using var driver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, quest.Boundary, scheduler);
            authority.CanonicalReady += canonical => driver.ReceiveCanonical(canonical);
            int nextOperation = 2000000;
            for (int i = 0; i < 2048; i++)
            {
                var desktopCut = new CutId("retired-desktop-cut-" + i);
                var questCut = new CutId("retired-quest-cut-" + i);
                Apply(new CreateCut(desktopCut, new SetCutDefinition(desktopCut, V2CutOrientation.Custom, false, 1, 0, 1, 0, 0), 0), false);
                Apply(new DeleteCut(desktopCut), false);
                Apply(new CreateCut(questCut, new SetCutDefinition(questCut, V2CutOrientation.Custom, false, 1, 0, 1, 0, 0), 0), true);
                Apply(new DeleteCut(questCut), true);
                Assert.That(desktop.Scene.Cuts, Is.Empty);
                Assert.That(quest.Scene.Cuts, Is.Empty);
                Assert.That(authority.IndexedKeyCount, Is.Zero);
                Assert.That(driver.AppliedKeyCount, Is.Zero);
            }

            Assert.That(authority.CanonicalSequence, Is.EqualTo(8192UL));
            Assert.That(authority.State, Is.EqualTo(V2DesktopMutationAuthorityState.Active));
            Assert.That(authority.RetainedDecisionCount, Is.Zero);
            Assert.That(driver.ReceivedOperationCount, Is.Zero);
            Assert.That(quest.Boundary.OptimisticRollbackOrderCount, Is.Zero);

            void Apply(V2Mutation mutation, bool fromQuest)
            {
                OperationId id = T09Operation(nextOperation++);
                if (fromQuest)
                {
                    V2QuestMutationProposal proposal = driver.ApplyOptimistic(mutation, id);
                    Assert.That(scheduler.TryGetNextTransmission(out V2TransmissionAttempt sent), Is.True);
                    Assert.That(authority.AcceptQuestProposal(id, mutation, proposal.ObservedCanonicalSequence, sent.Frame.OriginSequence.Value).Outcome, Is.EqualTo(V2ProposalOutcome.Accepted));
                    scheduler.Acknowledge(sent.Frame.StreamId, sent.Frame.ReliableFrameSequence.Value);
                }
                else desktop.Boundary.Apply(mutation, V2MutationApplicationOrigin.LocalDesktop, id);

                authority.RetireOperation(id);
                authority.AdvanceConflictFloor(driver.MinimumPendingObservedSequence);
                driver.RetireCanonicalHistory(authority.RetiredCanonicalThrough);
            }
        }

        private static readonly IncarnationId IncarnationIdForT09 = new(Guid.Parse("20000000-0000-0000-0000-000000000009"));
        private static readonly SessionId SessionIdForT09 = new(Guid.Parse("30000000-0000-0000-0000-000000000009"));

        [Test]
        public void ReceivedActivityAlphaBurst_AppliesOnlyTheLastValue()
        {
            using var fixture = new BoundSceneFixture();
            fixture.Column.ActivityAlpha = 0.1f;
            var scheduler = new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest);
            using var driver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, fixture.Boundary, scheduler);
            int changes = 0;
            fixture.Column.OnUpdateActivityAlpha.AddListener(() => changes++);
            var burst = Enumerable.Range(1, 100).Select(index => new V2CanonicalMutation(SceneIdForT09, IncarnationIdForT09, new OperationId(Guid.NewGuid()), (ulong)index, new SetActivityAlpha(new ColumnId(fixture.Column.ColumnData.ID), index / 100f))).ToArray();
            driver.ReceiveCanonicalBatch(burst);
            Assert.That(fixture.Column.ActivityAlpha, Is.EqualTo(1f));
            Assert.That(changes, Is.EqualTo(1));
            Assert.That(driver.LastObservedCanonicalSequence, Is.EqualTo(100UL));
        }

        [Test]
        public void SiteColor_UsesConstantTimeTargetLookupAcrossThirtyThousandSites()
        {
            const int siteCount = 30000;
            var states = Enumerable.Range(0, siteCount).Select(_ => new SiteState()).ToArray();
            var targets = Enumerable.Range(0, siteCount).Select(index => (states[index], new ColumnId("column-" + index), new SiteId("site-" + index))).ToArray();
            using var boundary = new V2SceneMutationBoundary(targets, Array.Empty<(SceneCut, CutId)>(), Array.Empty<(BasicTimeline, ColumnId)>(), V2OriginDevice.Desktop, new TestClock(0));
            var proposals = new List<V2Mutation>();
            boundary.MutationProposed += (_, mutation, _) => proposals.Add(mutation);

            states[siteCount - 1].Color = new Color(0.2f, 0.4f, 0.6f, 1f);

            Assert.That(proposals, Has.Count.EqualTo(1));
            Assert.That(proposals[0], Is.TypeOf<SetSiteColor>());
            SetSiteColor proposal = (SetSiteColor)proposals[0];
            Assert.That(proposal.ColumnId.Value, Is.EqualTo("column-29999"));
            Assert.That(proposal.FullSiteId.Value, Is.EqualTo("site-29999"));
        }

        [Test]
        public void SitePresentation_UsesConstantTimeTargetLookupAcrossThirtyThousandSites()
        {
            const int siteCount = 30000;
            var states = Enumerable.Range(0, siteCount).Select(_ => new SiteState()).ToArray();
            var targets = Enumerable.Range(0, siteCount).Select(index => (states[index], new ColumnId("column-" + index), new SiteId("site-" + index))).ToArray();
            using var boundary = new V2SceneMutationBoundary(targets, Array.Empty<(SceneCut, CutId)>(), Array.Empty<(BasicTimeline, ColumnId)>(), V2OriginDevice.Desktop, new TestClock(0));
            var proposals = new List<V2Mutation>();
            boundary.MutationProposed += (_, mutation, _) => proposals.Add(mutation);
            var stopwatch = Stopwatch.StartNew();

            states[siteCount - 1].IsHighlighted = true;

            stopwatch.Stop();
            Assert.That(proposals, Has.Count.EqualTo(1));
            Assert.That(proposals[0], Is.TypeOf<SetSiteHighlight>());
            Assert.That(((SetSiteHighlight)proposals[0]).SiteId.Value, Is.EqualTo("site-29999"));
            TestContext.WriteLine($"HBP_SYNC_T11_SITE_PRESENTATION siteCount={siteCount} elapsedMs={stopwatch.Elapsed.TotalMilliseconds:F3}");
        }

        [Test]
        public void T11Mutations_RoundTripAndMixedConfigurationTransactionUsesStrongestBarrier()
        {
            var columnId = new ColumnId("column-a");
            var siteId = new SiteId("site-a");
            var assignments = new SetSiteConfigurationBatch(new[]
            {
                new V2SiteConfigurationAssignment(columnId, siteId, true, true, 0.1f, 0.2f, 0.3f, 1f, new[] { "reviewed", "T11" })
            });
            V2Mutation[] mutations =
            {
                new SetSiteBlacklist(columnId, siteId, true),
                new SetInfluenceDistance(columnId, 23.5f),
                new SetColumnResource(columnId, V2ColumnResourceKind.StaticLabel, "prepared-label-reference"),
                new SetCcepSource(columnId, V2CcepSourceMode.MarsAtlas, null, -1),
                assignments,
                new SetConfigurationTransaction(new V2Mutation[]
                {
                    new SetSceneBoolean(V2SceneBooleanProperty.ShowAllSites, true),
                    assignments
                })
            };

            foreach (V2Mutation mutation in mutations)
                Assert.That(V2MutationPayloadCodec.Encode(V2MutationPayloadCodec.Decode(V2MutationPayloadCodec.Encode(mutation))), Is.EqualTo(V2MutationPayloadCodec.Encode(mutation)), mutation.Type.ToString());

            V2MutationDescriptor descriptor = V2MutationDescriptor.Create(SceneIdForT09, IncarnationIdForT09, mutations[^1]);
            Assert.That(descriptor.BarrierScope, Is.EqualTo(V2BarrierScope.AllScene));
            Assert.That(descriptor.CoalescingKey, Is.Null);
            Assert.That(descriptor.TouchedKeys, Has.Count.EqualTo(2));
        }

        [Test]
        public void T12SiteFilterResult_AppliesWholeRosterAndSurvivesTypedCheckpoint()
        {
            var columnId = new ColumnId("t12-column");
            var firstId = new SiteId("t12-site-a");
            var secondId = new SiteId("t12-site-b");
            var firstSource = new SiteState { IsFiltered = true };
            var secondSource = new SiteState { IsFiltered = false };
            using var source = new V2SceneMutationBoundary(new[] { (firstSource, columnId, firstId), (secondSource, columnId, secondId) }, Array.Empty<(SceneCut, CutId)>(), Array.Empty<(BasicTimeline, ColumnId)>(), V2OriginDevice.Desktop, new TestClock(0));

            OperationId jobId = new(Guid.Parse("12000000-0000-0000-0000-000000000001"));
            Assert.Throws<InvalidOperationException>(() => source.CreateSiteFilterResult(jobId, 1, new[] { false, true }, new byte[32]));
            SetSiteFilterResult result = source.CreateSiteFilterResult(jobId, 1, new[] { false, true });
            source.Apply(result, V2MutationApplicationOrigin.Remote, jobId);
            Assert.That(firstSource.IsFiltered, Is.False);
            Assert.That(secondSource.IsFiltered, Is.True);

            byte[] encodedCheckpoint = V2SceneMutationCheckpointCodec.Encode(1, source.CaptureCheckpoint());
            V2PublishedSceneCheckpoint publishedCheckpoint = V2SceneMutationCheckpointCodec.Decode(encodedCheckpoint);
            Assert.That(publishedCheckpoint.Checkpoint.T12Records, Has.Count.EqualTo(1));

            var firstReplica = new SiteState { IsFiltered = true };
            var secondReplica = new SiteState { IsFiltered = true };
            using var replica = new V2SceneMutationBoundary(new[] { (firstReplica, columnId, firstId), (secondReplica, columnId, secondId) }, Array.Empty<(SceneCut, CutId)>(), Array.Empty<(BasicTimeline, ColumnId)>(), V2OriginDevice.Quest, new TestClock(0));
            replica.ApplyCheckpoint(publishedCheckpoint.Checkpoint, new OperationId(Guid.Parse("12000000-0000-0000-0000-000000000002")));

            Assert.That(firstReplica.IsFiltered, Is.False);
            Assert.That(secondReplica.IsFiltered, Is.True);
        }

        [Test]
        public void T13CorrelationResultAndControls_RoundTripBoundedJobAndSceneWideBarrier()
        {
            var jobId = new OperationId(Guid.Parse("13000000-0000-0000-0000-000000000001"));
            byte[] resource = Enumerable.Repeat((byte)0xA5, 8192).ToArray();
            var result = new SetCorrelationResult(jobId, 3, resource);
            byte[] encoded = V2MutationPayloadCodec.Encode(result);
            var decoded = (SetCorrelationResult)V2MutationPayloadCodec.Decode(encoded);
            Assert.That(decoded.JobId, Is.EqualTo(jobId));
            Assert.That(decoded.Generation, Is.EqualTo(3UL));
            CollectionAssert.AreEqual(resource, decoded.ResultBytes);
            Assert.That(V2MutationPayloadCodec.Encode(decoded), Is.EqualTo(encoded));

            V2ScheduleDescriptor descriptor = V2ScheduleDescriptor.ForMutation(SceneIdForT09, IncarnationIdForT09, result);
            Assert.That(descriptor.BarrierScope, Is.EqualTo(V2BarrierScope.AllScene));
            Assert.That(descriptor.TouchedKeys.Single().Kind, Is.EqualTo(V2TouchedKeyKind.CorrelationResult));

            byte[] command = V2CorrelationRequestCodec.EncodeComputeRequest(V2CorrelationRequest.Compute());
            Assert.That(command, Has.Length.EqualTo(V2CorrelationRequestCodec.MaximumEncodedBytes));
            Assert.That(V2CorrelationRequestCodec.DecodeComputeRequest(command).Kind, Is.EqualTo(V2CorrelationCommandKind.Compute));
            Assert.Throws<InvalidDataException>(() => V2CorrelationRequestCodec.DecodeComputeRequest(command.Concat(new byte[] { 0 }).ToArray()));

            V2CorrelationControl[] controls =
            {
                new(V2CorrelationControlKind.Request, jobId, 0, command),
                new(V2CorrelationControlKind.Started, jobId, 3),
                new(V2CorrelationControlKind.Cancel, jobId, 3),
                new(V2CorrelationControlKind.Ready, jobId, 3),
                new(V2CorrelationControlKind.Failed, jobId, 3, failureCode: "quest_apply_failed")
            };
            foreach (V2CorrelationControl control in controls)
            {
                byte[] controlBytes = V2CorrelationControlCodec.Encode(control);
                Assert.That(controlBytes.Length, Is.LessThanOrEqualTo(V2CorrelationControlCodec.MaximumPayloadBytes));
                Assert.That(V2CorrelationControlCodec.TryDecode(controlBytes, out V2CorrelationControl roundTrip), Is.True);
                Assert.That(roundTrip.Kind, Is.EqualTo(control.Kind));
                Assert.That(roundTrip.JobId, Is.EqualTo(control.JobId));
                Assert.That(roundTrip.Generation, Is.EqualTo(control.Generation));
                Assert.That(roundTrip.FailureCode, Is.EqualTo(control.FailureCode));
                CollectionAssert.AreEqual(control.Command, roundTrip.Command);
            }
        }

        [Test]
        public void T13CorrelationApply_RejectsInvalidLaterColumnBeforePublishingEarlierMatrices()
        {
            var root = new GameObject("T13 correlation atomic apply");
            try
            {
                Base3DScene scene = root.AddComponent<Base3DScene>();
                var patient = new Patient { ID = "t13-patient", Name = "T13 patient" };
                var columns = new Column3DIEEG[2];
                var sites = new HBP.Core.Object3D.Site[2];
                for (int index = 0; index < columns.Length; index++)
                {
                    string id = "t13-column-" + index;
                    Column3DIEEG column = root.AddComponent<Column3DIEEG>();
                    SetAutoProperty(column, "ColumnData", new IEEGColumn(id, new BaseConfiguration(), null, string.Empty, null, new DynamicConfiguration(), id));
                    var siteObject = new GameObject("t13-site-" + index);
                    siteObject.transform.SetParent(root.transform, false);
                    HBP.Core.Object3D.Site site = siteObject.AddComponent<HBP.Core.Object3D.Site>();
                    site.Information = new SiteInformation { Patient = patient, Name = siteObject.name };
                    SetAutoProperty(column, "Sites", new List<HBP.Core.Object3D.Site> { site });
                    column.CorrelationBySitePair[site] = new Dictionary<HBP.Core.Object3D.Site, float> { [site] = 0.1f + index };
                    columns[index] = column;
                    sites[index] = site;
                    scene.Columns.Add(column);
                }

                var first = new CorrelationResultData(columns[0].ColumnData.ID, new Dictionary<HBP.Core.Object3D.Site, Dictionary<HBP.Core.Object3D.Site, float>> { [sites[0]] = new() { [sites[0]] = 0.9f } }, new Dictionary<HBP.Core.Object3D.Site, Dictionary<HBP.Core.Object3D.Site, float>>(), CorrelationProvenance.Unknown(columns[0].ColumnData.ID));
                var invalidSecond = new CorrelationResultData(columns[1].ColumnData.ID, new Dictionary<HBP.Core.Object3D.Site, Dictionary<HBP.Core.Object3D.Site, float>> { [sites[0]] = new() { [sites[0]] = 0.8f } }, new Dictionary<HBP.Core.Object3D.Site, Dictionary<HBP.Core.Object3D.Site, float>>(), CorrelationProvenance.Unknown(columns[1].ColumnData.ID));

                Assert.Throws<InvalidOperationException>(() => scene.ApplyCorrelationResults(new[] { first, invalidSecond }));
                Assert.That(columns[0].CorrelationBySitePair[sites[0]][sites[0]], Is.EqualTo(0.1f));
                Assert.That(columns[1].CorrelationBySitePair[sites[1]][sites[1]], Is.EqualTo(1.1f));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void T13CorrelationState_SurvivesTypedSceneCheckpointRoundTrip()
        {
            var root = new GameObject("T13 correlation checkpoint");
            try
            {
                Base3DScene scene = root.AddComponent<Base3DScene>();
                var patient = new Patient { ID = "t13-checkpoint-patient", Name = "T13 checkpoint patient" };
                var columnData = new IEEGColumn("T13 checkpoint", new BaseConfiguration(), null, string.Empty, null, new DynamicConfiguration(), "t13-checkpoint-column");
                SetAutoProperty(scene, "Visualization", new Visualization("T13 checkpoint", new[] { patient }, new Column[] { columnData }, new VisualizationConfiguration(), SceneIdForT09.Value.ToString()));
                Column3DIEEG column = root.AddComponent<Column3DIEEG>();
                SetAutoProperty(column, "ColumnData", columnData);
                var siteObject = new GameObject("T13 checkpoint site");
                siteObject.transform.SetParent(root.transform, false);
                HBP.Core.Object3D.Site site = siteObject.AddComponent<HBP.Core.Object3D.Site>();
                site.Information = new SiteInformation { Patient = patient, Name = siteObject.name };
                site.State = new SiteState();
                SetAutoProperty(column, "Sites", new List<HBP.Core.Object3D.Site> { site });
                scene.Columns.Add(column);

                using var boundary = new V2SceneMutationBoundary(scene, V2OriginDevice.Desktop);
                var provenance = new CorrelationProvenance(CorrelationResultSource.Imported, patient.ID, patient.Name, columnData.ID, "t13-dataset-id", "dataset", "t13-protocol-id", "protocol", "t13-bloc-id", "bloc", "t13-data-id", "data", NormalizationType.Trial, 0.025f, true);
                for (int visibility = 0; visibility < 2; visibility++)
                {
                    bool displayed = visibility == 1;
                    column.CorrelationBySitePair.Clear();
                    column.CorrelationMeanBySitePair.Clear();
                    column.CorrelationBySitePair[site] = new Dictionary<HBP.Core.Object3D.Site, float> { [site] = 0.25f };
                    column.CorrelationMeanBySitePair[site] = new Dictionary<HBP.Core.Object3D.Site, float> { [site] = 0.75f };
                    column.CorrelationProvenance = provenance;
                    scene.DisplayCorrelations = displayed;

                    byte[] encodedCheckpoint = V2SceneMutationCheckpointCodec.Encode(9, boundary.CaptureCheckpoint());
                    V2PublishedSceneCheckpoint published = V2SceneMutationCheckpointCodec.Decode(encodedCheckpoint);
                    Assert.That(published.Checkpoint.T13Records, Has.Count.EqualTo(1));
                    Assert.That(published.Checkpoint.T13Records[0].HasResult, Is.True);
                    Assert.That(published.Checkpoint.T13Records[0].DisplayCorrelations, Is.EqualTo(displayed));

                    column.CorrelationBySitePair.Clear();
                    column.CorrelationMeanBySitePair.Clear();
                    column.CorrelationProvenance = null;
                    scene.DisplayCorrelations = !displayed;
                    boundary.ApplyCheckpoint(published.Checkpoint, new OperationId(Guid.NewGuid()));

                    Assert.That(column.CorrelationBySitePair[site][site], Is.EqualTo(0.25f));
                    Assert.That(column.CorrelationMeanBySitePair[site][site], Is.EqualTo(0.75f));
                    Assert.That(column.CorrelationProvenance.Equals(provenance), Is.True);
                    Assert.That(scene.DisplayCorrelations, Is.EqualTo(displayed));
                    CollectionAssert.AreEqual(encodedCheckpoint, V2SceneMutationCheckpointCodec.Encode(9, boundary.CaptureCheckpoint()));
                }

                byte[] legacyResultBytes = CorrelationResultResource.Capture(scene).Encode();
                byte[] legacyRecordBytes;
                using (var legacyRecordStream = new MemoryStream())
                using (var legacyRecordWriter = new BinaryWriter(legacyRecordStream))
                {
                    legacyRecordWriter.Write((ushort)0x5433);
                    legacyRecordWriter.Write((ushort)1);
                    legacyRecordWriter.Write((byte)1);
                    legacyRecordWriter.Write((byte)0);
                    legacyRecordWriter.Write(legacyResultBytes.Length);
                    legacyRecordWriter.Write(legacyResultBytes);
                    legacyRecordBytes = legacyRecordStream.ToArray();
                }

                V2CorrelationCheckpointRecord legacyRecord = V2CorrelationCheckpointRecord.Decode(legacyRecordBytes);
                Assert.That(legacyRecord.HasResult, Is.True);
                Assert.That(legacyRecord.DisplayCorrelations, Is.True);
                CollectionAssert.AreEqual(legacyResultBytes, legacyRecord.ResultBytes);
                V2CorrelationCheckpointRecord legacyEmptyRecord = V2CorrelationCheckpointRecord.Decode(new byte[] { 0x33, 0x54, 0x01, 0x00, 0x00, 0x00 });
                Assert.That(legacyEmptyRecord.HasResult, Is.False);
                Assert.That(legacyEmptyRecord.DisplayCorrelations, Is.False);

                byte[] legacyOuterCheckpointBytes;
                using (var legacyCheckpointStream = new MemoryStream())
                using (var legacyCheckpointWriter = new BinaryWriter(legacyCheckpointStream))
                {
                    legacyCheckpointWriter.Write(Encoding.ASCII.GetBytes("HBCP"));
                    legacyCheckpointWriter.Write((ushort)5);
                    legacyCheckpointWriter.Write(9UL);
                    for (int countIndex = 0; countIndex < 7; countIndex++) legacyCheckpointWriter.Write(0);
                    legacyOuterCheckpointBytes = legacyCheckpointStream.ToArray();
                }

                V2SceneMutationCheckpoint legacyOuterCheckpoint = V2SceneMutationCheckpointCodec.Decode(legacyOuterCheckpointBytes).Checkpoint;
                Assert.That(legacyOuterCheckpoint.T13Records, Is.Empty);
                scene.DisplayCorrelations = false;
                boundary.ApplyCheckpoint(legacyOuterCheckpoint, new OperationId(Guid.NewGuid()));
                Assert.That(scene.DisplayCorrelations, Is.False);
                Assert.That(column.CorrelationBySitePair[site][site], Is.EqualTo(0.25f));
                Assert.That(column.CorrelationMeanBySitePair[site][site], Is.EqualTo(0.75f));
                Assert.That(column.CorrelationProvenance.Equals(provenance), Is.True);

                for (int visibility = 0; visibility < 2; visibility++)
                {
                    bool displayed = visibility == 1;
                    column.CorrelationBySitePair.Clear();
                    column.CorrelationMeanBySitePair.Clear();
                    column.CorrelationProvenance = null;
                    scene.DisplayCorrelations = displayed;

                    byte[] encodedCheckpoint = V2SceneMutationCheckpointCodec.Encode(9, boundary.CaptureCheckpoint());
                    V2PublishedSceneCheckpoint published = V2SceneMutationCheckpointCodec.Decode(encodedCheckpoint);
                    Assert.That(published.Checkpoint.T13Records, Has.Count.EqualTo(1));
                    Assert.That(published.Checkpoint.T13Records[0].HasResult, Is.False);
                    Assert.That(published.Checkpoint.T13Records[0].DisplayCorrelations, Is.EqualTo(displayed));

                    column.CorrelationBySitePair[site] = new Dictionary<HBP.Core.Object3D.Site, float> { [site] = 0.5f };
                    column.CorrelationMeanBySitePair[site] = new Dictionary<HBP.Core.Object3D.Site, float> { [site] = 0.6f };
                    column.CorrelationProvenance = provenance;
                    scene.DisplayCorrelations = !displayed;
                    boundary.ApplyCheckpoint(published.Checkpoint, new OperationId(Guid.NewGuid()));

                    Assert.That(column.CorrelationBySitePair, Is.Empty);
                    Assert.That(column.CorrelationMeanBySitePair, Is.Empty);
                    Assert.That(column.CorrelationProvenance, Is.Null);
                    Assert.That(scene.DisplayCorrelations, Is.EqualTo(displayed));
                    CollectionAssert.AreEqual(encodedCheckpoint, V2SceneMutationCheckpointCodec.Encode(9, boundary.CaptureCheckpoint()));
                }
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void T12SiteFilterRequestCodec_RoundTripsSiteConditionsAndRejectsOtherFilterFamilies()
        {
            V2SiteFilterRequest source = V2SiteFilterRequest.FromConditions(new BaseFilterCondition[] { new NameFilterCondition("A1", true, false, false, "t12-condition") });
            V2SiteFilterRequest decoded = V2SiteFilterRequestCodec.Decode(V2SiteFilterRequestCodec.Encode(source));
            Assert.That(decoded.Conditions.Single(), Is.TypeOf<NameFilterCondition>());
            Assert.That(((NameFilterCondition)decoded.Conditions.Single()).Name, Is.EqualTo("A1"));
            Assert.Throws<ArgumentException>(() => V2SiteFilterRequest.FromConditions(new BaseFilterCondition[] { new ProtocolFilterCondition() }));

            V2SiteFilterRequest channels = V2SiteFilterRequest.FromChannels(new[] { new V2SiteFilterChannel("A1", "patient-1") });
            V2SiteFilterRequest decodedChannels = V2SiteFilterRequestCodec.Decode(V2SiteFilterRequestCodec.Encode(channels));
            Assert.That(decodedChannels.Channels.Single().PatientId, Is.EqualTo("patient-1"));
            Assert.That(decodedChannels.Channels.Single().Channel, Is.EqualTo("A1"));
        }

        [Test]
        public void T12OfflineCapabilityGate_UsesPreparedLocalDataAndRejectsUnavailableResources()
        {
            using var fixture = new BoundSceneFixture();
            V2SiteFilterRequest nameRequest = V2SiteFilterRequest.FromConditions(new BaseFilterCondition[] { new NameFilterCondition("site", true, false, false) });
            Assert.That(V2SiteFilterOfflineCapabilityGate.CanEvaluate(nameRequest, fixture.Scene, fixture.Boundary, out string nameExplanation), Is.True, nameExplanation);

            V2SiteFilterRequest supportedGroup = V2SiteFilterRequest.FromConditions(new BaseFilterCondition[]
            {
                new AllFilterCondition(new BaseFilterCondition[] { new NameFilterCondition("site", true, false, false), new AttributesFilterCondition() }, false)
            });
            Assert.That(V2SiteFilterOfflineCapabilityGate.CanEvaluate(supportedGroup, fixture.Scene, fixture.Boundary, out string groupExplanation), Is.True, groupExplanation);

            V2SiteFilterRequest missingStatistics = V2SiteFilterRequest.FromConditions(new BaseFilterCondition[] { new ActivityFilterCondition() });
            Assert.That(V2SiteFilterOfflineCapabilityGate.CanEvaluate(missingStatistics, fixture.Scene, fixture.Boundary, out string statisticsExplanation), Is.False);
            StringAssert.Contains("statistics", statisticsExplanation);

            V2SiteFilterRequest externalMask = V2SiteFilterRequest.FromConditions(new BaseFilterCondition[] { new MRIMaskFilterCondition() });
            Assert.That(V2SiteFilterOfflineCapabilityGate.CanEvaluate(externalMask, fixture.Scene, fixture.Boundary, out string maskExplanation), Is.False);
            StringAssert.Contains(nameof(MRIMaskFilterCondition), maskExplanation);

            fixture.Site.Information.SiteData = new HBP.Core.Data.Site();
            fixture.Site.State.IsFiltered = false;
            var unresolvedTag = new BaseTag("unavailable local tag", "t12-unresolved-offline-tag");
            V2SiteFilterRequest unresolvedTagRequest = V2SiteFilterRequest.FromConditions(new BaseFilterCondition[]
            {
                new SiteTagFilterCondition(SiteTagFilterCondition.TargetType.Site, unresolvedTag, new EmptyTagFilterValue(), false)
            });
            Assert.That(V2SiteFilterOfflineCapabilityGate.CanEvaluate(unresolvedTagRequest, fixture.Scene, fixture.Boundary, out string tagExplanation), Is.False);
            StringAssert.Contains("cannot resolve a tag", tagExplanation);
            Assert.That(fixture.Site.State.IsFiltered, Is.False, "An unresolved offline tag must be rejected before the filter mask changes.");
        }

        [Test]
        public void T11Codec_ContinuesDecodingLegacySchemaSitePresentationRecords()
        {
            byte[] payload;
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream, Encoding.UTF8, true))
            {
                writer.Write((ushort)1); // Legacy V2 mutation payload schema.
                writer.Write((ushort)V2OperationType.SetSiteLabels);
                WriteLegacyText(writer, "column-a");
                WriteLegacyText(writer, "site-a");
                writer.Write((ushort)2);
                WriteLegacyText(writer, "reviewed");
                WriteLegacyText(writer, "legacy");
                payload = stream.ToArray();
            }

            SetSiteLabels decoded = (SetSiteLabels)V2MutationPayloadCodec.Decode(payload);
            Assert.That(decoded.ColumnId.Value, Is.EqualTo("column-a"));
            Assert.That(decoded.SiteId.Value, Is.EqualTo("site-a"));
            Assert.That(decoded.Labels, Is.EqualTo(new[] { "reviewed", "legacy" }));
        }

        [Test]
        public void NestedColorChange_OnlyInvalidatesSiteRenderingInTheScene()
        {
            AssertNestedSiteStateChangeInvalidation(state => state.Color = Color.green, expectGeneratorUpdate: false);
        }

        [Test]
        public void NestedNonColorChange_InvalidatesActivityInTheSceneEvenDuringColorCallback()
        {
            AssertNestedSiteStateChangeInvalidation(state => state.IsBlackListed = true, expectGeneratorUpdate: true);
        }

        [Test]
        public void NestedHighlightChange_OnlyInvalidatesSiteRenderingInTheScene()
        {
            AssertNestedSiteStateChangeInvalidation(state => state.IsHighlighted = true, expectGeneratorUpdate: false);
        }

        [Test]
        public void NestedLabelChange_OnlyInvalidatesSiteRenderingInTheScene()
        {
            AssertNestedSiteStateChangeInvalidation(state => state.AddLabel("reviewed"), expectGeneratorUpdate: false);
        }

        [Test]
        public void SiteStateChangeKind_DistinguishesPresentationFromScientificMask()
        {
            var state = new SiteState();
            SiteStateChangeKind observed = SiteStateChangeKind.Other;
            state.OnChangeState.AddListener(() => observed = state.CurrentChangeKind);

            state.ApplySynchronizedState(true, false, true, Color.green, Array.Empty<string>());
            Assert.That(observed, Is.EqualTo(SiteStateChangeKind.Presentation));

            state.ApplySynchronizedState(false, true, true, Color.green, Array.Empty<string>());
            Assert.That(observed, Is.EqualTo(SiteStateChangeKind.ScientificMask));
        }

        [Test]
        public void SensitiveActivityAdmission_OrdersMutationBeforeStartAndRejectsAfterStart()
        {
            GameObject root = new("activity projection admission test");
            root.SetActive(false);
            var scene = root.AddComponent<Base3DScene>();
            BrainMaterials brainMaterials = null;
            try
            {
                brainMaterials = InitializeTestBrainMaterials(scene);
                using var boundary = new V2SceneMutationBoundary(scene, V2OriginDevice.Quest, new TestClock(0));
                scene.SceneInformation.GeometryNeedsUpdate = false;
                scene.SceneInformation.ProjectionGridNeedsUpdate = false;
                scene.SceneInformation.SurfaceProjectionNeedsUpdate = false;
                scene.RequestActivityProjection();

                Assert.That(boundary.TryBeginSensitiveActivityOperation(out IDisposable mutationScope), Is.True);
                using (mutationScope)
                {
                    scene.InvalidateActivityField(clearRenderedActivity: false);
                    Assert.That(scene.TryBeginActivityProjection(out _), Is.False);
                }

                Assert.That(scene.TryBeginActivityProjection(out ActivityProjectionInputLease lease), Is.True);
                Assert.That(lease.InputGeneration, Is.EqualTo(scene.ActivityInputGeneration));
                Assert.That(lease.ProjectionGeneration, Is.EqualTo(scene.ProjectionGeneration));
                Assert.That(scene.ProjectionState, Is.EqualTo(ActivityProjectionState.Computing));
                Assert.That(boundary.TryBeginSensitiveActivityOperation(out _), Is.False);

                scene.InvalidateActivityField(clearRenderedActivity: false);
                Assert.That(scene.IsCurrentActivityProjection(lease), Is.False);
                Assert.That(scene.ShouldStartActivityProjection(automaticPolicyEnabled: false), Is.False);
            }
            finally
            {
                SetPrivateField(scene, "m_UpdatingGenerators", false);
                SetPrivateField(scene, "m_ActiveActivityProjection", null);
                if (brainMaterials != null) DestroyTestBrainMaterials(brainMaterials);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CancelledProjectionLease_RemainsStaleAfterANewerGenerationStarts()
        {
            GameObject root = new("stale activity projection lease test");
            root.SetActive(false);
            var scene = root.AddComponent<Base3DScene>();
            BrainMaterials brainMaterials = null;
            try
            {
                brainMaterials = InitializeTestBrainMaterials(scene);
                scene.SceneInformation.GeometryNeedsUpdate = false;
                scene.SceneInformation.ProjectionGridNeedsUpdate = false;
                scene.SceneInformation.SurfaceProjectionNeedsUpdate = false;
                scene.RequestActivityProjection();

                Assert.That(scene.TryBeginActivityProjection(out ActivityProjectionInputLease leaseA), Is.True);
                scene.CancelActivityProjection();
                Assert.That(scene.IsCurrentActivityProjection(leaseA), Is.False);
                Assert.That(scene.ProjectionState, Is.EqualTo(ActivityProjectionState.Stale));
                Assert.That(scene.ExplicitProjectionRequestPending, Is.False);
                SetPrivateField(scene, "m_UpdatingGenerators", false);

                scene.RequestActivityProjection();
                Assert.That(scene.TryBeginActivityProjection(out ActivityProjectionInputLease leaseB), Is.True);
                Assert.That(leaseB.ProjectionGeneration, Is.GreaterThan(leaseA.ProjectionGeneration));
                Assert.That(scene.IsCurrentActivityProjection(leaseB), Is.True);
                Assert.That(scene.IsCurrentActivityProjection(leaseA), Is.False, "A late native completion cannot publish after B starts.");

                scene.CancelActivityProjection();
                Assert.That(scene.IsCurrentActivityProjection(leaseB), Is.False);
            }
            finally
            {
                SetPrivateField(scene, "m_UpdatingGenerators", false);
                SetPrivateField(scene, "m_ActiveActivityProjection", null);
                if (brainMaterials != null) DestroyTestBrainMaterials(brainMaterials);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void AutomaticStartupProjectionRequest_IsReadyAfterPreparationAndExplicitRemovalStaysRemoved()
        {
            GameObject root = new("automatic startup projection request test");
            root.SetActive(false);
            var scene = root.AddComponent<Base3DScene>();
            BrainMaterials brainMaterials = null;
            try
            {
                brainMaterials = InitializeTestBrainMaterials(scene);
                scene.SceneInformation.Initialized = true;
                scene.SceneInformation.CompletelyLoaded = true;
                scene.SceneInformation.GeometryNeedsUpdate = false;
                scene.SceneInformation.ProjectionGridNeedsUpdate = false;
                scene.SceneInformation.SurfaceProjectionNeedsUpdate = false;
                scene.SceneInformation.GeneratorNeedsUpdate = false;
                scene.SceneInformation.GeneratorUpdateRequested = false;

                scene.InitializeAutomaticActivityProjection(automaticPolicyEnabled: true);

                Assert.That(scene.ProjectionRequested, Is.True);
                Assert.That(scene.ProjectionState, Is.EqualTo(ActivityProjectionState.Stale));
                Assert.That(scene.SceneInformation.GeneratorNeedsUpdate, Is.True);
                Assert.That(scene.SceneInformation.GeneratorUpdateRequested, Is.False);
                Assert.That(scene.ShouldStartActivityProjection(automaticPolicyEnabled: true), Is.True);

                scene.SetProjectionEnabled(false);
                scene.InitializeAutomaticActivityProjection(automaticPolicyEnabled: true);

                Assert.That(scene.ProjectionRequested, Is.False);
                Assert.That(scene.ProjectionState, Is.EqualTo(ActivityProjectionState.Absent));
                Assert.That(scene.SceneInformation.GeneratorUpdateRequested, Is.False);
                Assert.That(scene.ShouldStartActivityProjection(automaticPolicyEnabled: true), Is.False);
            }
            finally
            {
                if (brainMaterials != null) DestroyTestBrainMaterials(brainMaterials);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ExplicitGeneratorUpdate_RequestsProjectionWhileAutomaticStaleStateRemainsGated()
        {
            GameObject root = new("explicit generator update request test");
            root.SetActive(false);
            var scene = root.AddComponent<Base3DScene>();
            BrainMaterials brainMaterials = null;
            try
            {
                brainMaterials = InitializeTestBrainMaterials(scene);
                scene.SceneInformation.GeometryNeedsUpdate = false;
                scene.SceneInformation.ProjectionGridNeedsUpdate = false;
                scene.SceneInformation.SurfaceProjectionNeedsUpdate = false;
                scene.InitializeAutomaticActivityProjection(automaticPolicyEnabled: true);
                scene.InvalidateActivityField(clearRenderedActivity: false);

                Assert.That(scene.ShouldStartActivityProjection(automaticPolicyEnabled: false), Is.False);

                scene.SceneInformation.SurfaceProjectionNeedsUpdate = true;
                scene.UpdateGenerator();
                Assert.That(scene.ProjectionRequested, Is.True);
                Assert.That(scene.SceneInformation.GeneratorNeedsUpdate, Is.True);
                Assert.That(scene.SceneInformation.GeneratorUpdateRequested, Is.True);

                scene.SceneInformation.SurfaceProjectionNeedsUpdate = false;
                Assert.That(scene.ShouldStartActivityProjection(automaticPolicyEnabled: false), Is.True);
            }
            finally
            {
                if (brainMaterials != null) DestroyTestBrainMaterials(brainMaterials);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void QuestRecalculateProjection_RequestsAnExplicitUpdateWhenAutomaticPolicyIsDisabled()
        {
            GameObject root = new("Quest explicit projection request test");
            root.SetActive(false);
            var scene = root.AddComponent<Base3DScene>();
            var view = root.AddComponent<QuestAnatomyView>();
            BrainMaterials brainMaterials = null;
            try
            {
                brainMaterials = InitializeTestBrainMaterials(scene);
                scene.SceneInformation.GeometryNeedsUpdate = false;
                scene.SceneInformation.ProjectionGridNeedsUpdate = false;
                scene.SceneInformation.SurfaceProjectionNeedsUpdate = false;
                scene.InitializeAutomaticActivityProjection(automaticPolicyEnabled: true);
                scene.InvalidateActivityField(clearRenderedActivity: false);
                Assert.That(scene.ShouldStartActivityProjection(automaticPolicyEnabled: false), Is.False);

                var restoredScene = (RestoredScene)Activator.CreateInstance(typeof(RestoredScene), BindingFlags.Instance | BindingFlags.NonPublic, binder: null, args: new object[] { scene, null, null }, culture: null);
                typeof(QuestAnatomyView).GetField("current", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(view, restoredScene);

                scene.SceneInformation.SurfaceProjectionNeedsUpdate = true;
                view.RecalculateProjection();

                Assert.That(scene.ProjectionRequested, Is.True);
                Assert.That(scene.SceneInformation.GeneratorNeedsUpdate, Is.True);
                Assert.That(scene.SceneInformation.GeneratorUpdateRequested, Is.True);

                scene.SceneInformation.SurfaceProjectionNeedsUpdate = false;
                Assert.That(scene.ShouldStartActivityProjection(automaticPolicyEnabled: false), Is.True);
            }
            finally
            {
                if (brainMaterials != null) DestroyTestBrainMaterials(brainMaterials);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SafeSceneUpdates_ContinueWhileControlledProjectionIsComputing()
        {
            GameObject root = new("activity projection safe update test");
            root.SetActive(false);
            var scene = root.AddComponent<Base3DScene>();
            BrainMaterials brainMaterials = null;
            try
            {
                brainMaterials = InitializeTestBrainMaterials(scene);
                scene.SceneInformation.Initialized = true;
                scene.SceneInformation.CompletelyLoaded = true;
                scene.SceneInformation.GeometryNeedsUpdate = false;
                scene.SceneInformation.ProjectionGridNeedsUpdate = false;
                scene.SceneInformation.SurfaceProjectionNeedsUpdate = false;
                scene.SceneInformation.SitesNeedUpdate = true;
                scene.RequestActivityProjection();
                Assert.That(scene.TryBeginActivityProjection(out _), Is.True);

                bool siteRenderingUpdated = false;
                scene.OnSitesRenderingUpdated.AddListener(() => siteRenderingUpdated = true);
                typeof(Base3DScene).GetMethod("Update", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(scene, null);

                Assert.That(siteRenderingUpdated, Is.True);
                Assert.That(scene.SceneInformation.SitesNeedUpdate, Is.False);
                Assert.That(scene.ProjectionState, Is.EqualTo(ActivityProjectionState.Computing));
                Assert.That(scene.CanApplyPreparedState, Is.True);
                Assert.That(scene.CanApplyLegacyStateSnapshot, Is.False);
            }
            finally
            {
                SetPrivateField(scene, "m_UpdatingGenerators", false);
                SetPrivateField(scene, "m_ActiveActivityProjection", null);
                if (brainMaterials != null) DestroyTestBrainMaterials(brainMaterials);
                Object.DestroyImmediate(root);
            }
        }

        [TestCase(CutOrientation.Axial)]
        [TestCase(CutOrientation.Coronal)]
        [TestCase(CutOrientation.Sagittal)]
        public void NonCustomCutRemoteApply_PreservesDefinitionAndTargetsSceneInvalidation(CutOrientation orientation)
        {
            GameObject root = new("non-custom cut scene test");
            root.SetActive(false);
            var scene = root.AddComponent<Base3DScene>();
            var mriManager = root.AddComponent<MRIManager>();
            var meshManager = root.AddComponent<MeshManager>();
            CoreVolume volume = null;
            SceneCut cut = null;
            string volumePath = Path.Combine(Application.temporaryCachePath, "sync-scene-cut-" + Guid.NewGuid().ToString("N") + ".nii");

            try
            {
                CreateMinimalNifti(volumePath);
                volume = new CoreVolume();
                Assert.That(volume.LoadNIFTIFile(volumePath), Is.True);
                mriManager.MRIs.Add(new MRI3D("MNI", volume));
                SetPrivateField(scene, "m_MRIManager", mriManager);
                SetPrivateField(scene, "m_MeshManager", meshManager);

                cut = new SceneCut { ID = "shared-cut" };
                scene.Cuts.Add(cut);
                ResetSceneInvalidationFlags(scene);
                using var boundary = new V2SceneMutationBoundary(scene, V2OriginDevice.Quest, new TestClock(1000));
                Vector3 receivedNormal = new(0.25f, 0.5f, 0.75f);
                var mutation = new SetCutDefinition(new CutId(cut.ID), (V2CutOrientation)orientation, true, 3, 0.75f, receivedNormal.x, receivedNormal.y, receivedNormal.z);

                boundary.Apply(mutation, V2MutationApplicationOrigin.Remote, new OperationId(Guid.NewGuid()));

                Assert.That(cut.Orientation, Is.EqualTo(orientation));
                Assert.That(cut.Flip, Is.True);
                Assert.That(cut.NumberOfCuts, Is.EqualTo(3));
                Assert.That(cut.Position, Is.EqualTo(0.75f));
                Assert.That(cut.Normal, Is.EqualTo(receivedNormal));
                Assert.That(scene.SceneInformation.CutsNeedUpdate, Is.True);
                Assert.That(scene.SceneInformation.BaseCutTexturesNeedUpdate, Is.True);
                Assert.That(scene.SceneInformation.FunctionalCutTexturesNeedUpdate, Is.True);
                Assert.That(scene.SceneInformation.GUICutTexturesNeedUpdate, Is.True);
                Assert.That(scene.SceneInformation.GeometryNeedsUpdate, Is.False);
                Assert.That(scene.SceneInformation.GeneratorNeedsUpdate, Is.False);
                Assert.That(scene.SceneInformation.SitesNeedUpdate, Is.False);
                Assert.That(scene.SceneInformation.CollidersNeedUpdate, Is.False);
                Assert.That(scene.SceneInformation.FunctionalSurfaceNeedsUpdate, Is.False);
            }
            finally
            {
                if (cut != null)
                {
                    scene.Cuts.Remove(cut);
                    cut.Dispose();
                }

                volume?.Dispose();
                Object.DestroyImmediate(root);
                if (File.Exists(volumePath)) File.Delete(volumePath);
            }
        }

        [Test]
        public void AxialCutWithSignedZeroNormal_PublishesPositionAndCheckpoint()
        {
            using var source = new Fixture("source", V2OriginDevice.Desktop, 1000);
            float negativeZero = BitConverter.Int32BitsToSingle(int.MinValue);
            source.Cut.Normal = new Vector3(negativeZero, 0f, 1f);
            source.Cut.Orientation = CutOrientation.Axial;
            source.Rebind(updateCut: null);

            source.Cut.Position = 0.8f;

            Assert.That(source.LastProposal, Is.TypeOf<SetCutDefinition>());
            SetCutDefinition proposal = (SetCutDefinition)source.LastProposal;
            Assert.That(proposal.Position, Is.EqualTo(0.8f));
            Assert.That(BitConverter.SingleToInt32Bits(proposal.NormalX), Is.Zero);
            Assert.That(source.Boundary.CaptureCheckpoint().CutDefinitions, Has.Count.EqualTo(1));
        }

        [Test]
        public void CutDefinition_AppliesCompleteStateAndKeepsNewestPreview()
        {
            using var source = new Fixture("source", V2OriginDevice.Desktop, 1000);
            using var target = new Fixture("target", V2OriginDevice.Quest, 1000);
            var sourceProposals = new List<(OperationId Id, V2Mutation Mutation, V2OriginDevice Device)>();
            source.Boundary.MutationProposed += (id, mutation, device) => sourceProposals.Add((id, mutation, device));

            source.Cut.Orientation = HBP.Core.Enums.CutOrientation.Custom;
            source.Cut.Flip = true;
            source.Cut.NumberOfCuts = 321;
            source.Cut.Normal = new Vector3(0.25f, 0.5f, 0.75f);
            source.Cut.Position = 0.25f;
            source.Cut.Position = 0.75f;

            Assert.That(sourceProposals, Has.Count.EqualTo(6));
            SetCutDefinition newest = (SetCutDefinition)sourceProposals[sourceProposals.Count - 1].Mutation;
            Assert.That(newest.Position, Is.EqualTo(0.75f));
            Assert.That(newest.Orientation, Is.EqualTo(V2CutOrientation.Custom));
            Assert.That(newest.Flip, Is.True);
            Assert.That(newest.NumberOfCuts, Is.EqualTo(321));
            Assert.That(newest.NormalX, Is.EqualTo(0.25f));
            Assert.That(newest.NormalY, Is.EqualTo(0.5f));
            Assert.That(newest.NormalZ, Is.EqualTo(0.75f));

            int cutInvalidations = 0;
            target.Boundary.Dispose();
            target.Rebind(updateCut: _ => cutInvalidations++);
            target.Boundary.MutationProposed += (_, _, _) => Assert.Fail("Remote cut application echoed a mutation.");
            target.Boundary.Apply(newest, V2MutationApplicationOrigin.Remote, sourceProposals[sourceProposals.Count - 1].Id);

            Assert.That(target.Cut.Position, Is.EqualTo(0.75f));
            Assert.That(target.Cut.Orientation, Is.EqualTo(source.Cut.Orientation));
            Assert.That(target.Cut.Flip, Is.EqualTo(source.Cut.Flip));
            Assert.That(target.Cut.NumberOfCuts, Is.EqualTo(source.Cut.NumberOfCuts));
            Assert.That(target.Cut.Normal, Is.EqualTo(source.Cut.Normal));
            Assert.That(cutInvalidations, Is.EqualTo(1));
        }

        [Test]
        public void TimelineAnchor_AdvancesAtReceiverAndSuppressesNestedRemoteCallbacks()
        {
            var sourceClock = new TestClock(1000, 1000);
            var targetClock = new TestClock(6000, 1000);
            using var source = new Fixture("source", V2OriginDevice.Desktop, sourceClock);
            using var target = new Fixture("target", V2OriginDevice.Quest, targetClock, timelineAgeSeconds: _ => 5d);
            source.Timeline.CurrentIndex = 2;
            source.Timeline.Step = 5;
            source.Timeline.IsPlaying = true;
            var targetProposals = new List<V2Mutation>();
            int timelineCallbacks = 0;
            target.Boundary.MutationProposed += (_, mutation, _) => targetProposals.Add(mutation);
            target.Timeline.OnUpdateCurrentIndex.AddListener(() =>
            {
                timelineCallbacks++;
                target.Cut.Position = target.Cut.Position == 0.5f ? 0.75f : 0.5f;
            });

            SetTimelineAnchor proposal = (SetTimelineAnchor)source.LastProposal;
            Assert.That(proposal.Intent, Is.EqualTo(V2TimelineAnchorIntent.Play));
            target.Boundary.Apply(proposal, V2MutationApplicationOrigin.Remote, source.LastOperationId);

            Assert.That(target.Timeline.CurrentIndex, Is.EqualTo(27));
            Assert.That(target.Timeline.IsPlaying, Is.True);
            Assert.That(target.Timeline.Step, Is.EqualTo(5));
            Assert.That(timelineCallbacks, Is.EqualTo(1));
            Assert.That(targetProposals, Is.Empty);
        }

        [Test]
        public void PausedTimelineAnchor_DoesNotAdvanceWithElapsedClockEstimate()
        {
            using var target = new Fixture("paused", V2OriginDevice.Quest, new TestClock(6000), timelineAgeSeconds: _ => 5d);
            var anchor = new SetTimelineAnchor(target.ColumnId, 2, false, false, 5, 1000, 1000);

            target.Boundary.Apply(anchor, V2MutationApplicationOrigin.Remote, new OperationId(Guid.NewGuid()));

            Assert.That(target.Timeline.CurrentIndex, Is.EqualTo(2));
            Assert.That(target.Timeline.IsPlaying, Is.False);
        }

        [Test]
        public void PausedTimelineAnchor_DoesNotAskTheClockEstimator()
        {
            int estimateCalls = 0;
            using var target = new Fixture("paused-estimator", V2OriginDevice.Quest, new TestClock(6000), timelineTimingEstimate: _ =>
            {
                estimateCalls++;
                return new V2TimelineAnchorTimingEstimate(5d, 0d);
            });
            var anchor = new SetTimelineAnchor(target.ColumnId, 2, false, false, 5, 1000, 1000);

            target.Boundary.Apply(anchor, V2MutationApplicationOrigin.Remote, new OperationId(Guid.NewGuid()));

            Assert.That(target.Timeline.CurrentIndex, Is.EqualTo(2));
            Assert.That(estimateCalls, Is.Zero);
        }

        [TestCase(V2TimelineAnchorIntent.Pause)]
        [TestCase(V2TimelineAnchorIntent.Seek)]
        [TestCase(V2TimelineAnchorIntent.Step)]
        [TestCase(V2TimelineAnchorIntent.Loop)]
        public void NonPlayTimelineAnchors_PreserveIntentAndApplyExactIndexWithoutEstimating(V2TimelineAnchorIntent intent)
        {
            using var source = new Fixture("intent-source", V2OriginDevice.Desktop, new TestClock(1000, 1000));
            int estimateCalls = 0;
            using var target = new Fixture("intent-target", V2OriginDevice.Quest, new TestClock(6000, 1000), timelineTimingEstimate: _ =>
            {
                estimateCalls++;
                return new V2TimelineAnchorTimingEstimate(5d, 0d);
            });
            source.Timeline.CurrentIndex = 40;
            source.Timeline.Step = 2;
            source.Timeline.IsPlaying = true;
            target.Timeline.CurrentIndex = 80;
            target.Timeline.IsPlaying = true;

            switch (intent)
            {
                case V2TimelineAnchorIntent.Pause:
                    source.Timeline.IsPlaying = false;
                    break;
                case V2TimelineAnchorIntent.Seek:
                    source.Timeline.CurrentIndex = 55;
                    break;
                case V2TimelineAnchorIntent.Step:
                    source.Timeline.Step = 7;
                    break;
                case V2TimelineAnchorIntent.Loop:
                    source.Timeline.IsLooping = true;
                    break;
                default:
                    Assert.Fail("Test case must represent a non-play anchor.");
                    break;
            }

            var published = (SetTimelineAnchor)source.LastProposal;
            Assert.That(published.Intent, Is.EqualTo(intent));
            SetTimelineAnchor received = (SetTimelineAnchor)V2MutationPayloadCodec.Decode(V2MutationPayloadCodec.Encode(published));
            Assert.That(received.Intent, Is.EqualTo(intent), "The wire payload must preserve the anchor intent.");
            target.Boundary.Apply(received, V2MutationApplicationOrigin.Remote, source.LastOperationId);

            Assert.That(target.Timeline.CurrentIndex, Is.EqualTo(received.Index));
            Assert.That(target.Timeline.IsPlaying, Is.EqualTo(received.Playing));
            Assert.That(target.Timeline.Step, Is.EqualTo(received.Step));
            Assert.That(target.Timeline.IsLooping, Is.EqualTo(received.Looping));
            Assert.That(estimateCalls, Is.Zero);
        }

        [Test]
        public void PlayingTimelineAnchor_WithUnusableEstimateStartsAtTransmittedFinalIndex()
        {
            int estimateCalls = 0;
            using var target = new Fixture("play-final-index", V2OriginDevice.Quest, new TestClock(6000), timelineTimingEstimate: _ =>
            {
                estimateCalls++;
                return null;
            });
            var anchor = new SetTimelineAnchor(target.ColumnId, target.Timeline.Length - 1, true, false, 5, 1000, 1000, V2TimelineAnchorIntent.Play);

            target.Boundary.Apply(anchor, V2MutationApplicationOrigin.Remote, new OperationId(Guid.NewGuid()));

            Assert.That(target.Timeline.CurrentIndex, Is.EqualTo(target.Timeline.Length - 1));
            Assert.That(target.Timeline.IsPlaying, Is.True);
            Assert.That(estimateCalls, Is.EqualTo(1));
        }

        [Test]
        public void PlayingTimelineAnchor_OnlyCorrectsDriftBeyondOneSamplePlusUncertainty()
        {
            using var target = new Fixture("playing-threshold", V2OriginDevice.Quest, new TestClock(6000), timelineTimingEstimate: anchor => anchor.Index switch
            {
                10 => new V2TimelineAnchorTimingEstimate(0.1d, 0.05d),
                11 => new V2TimelineAnchorTimingEstimate(0.2d, 0.05d),
                _ => new V2TimelineAnchorTimingEstimate(0.3d, 0.05d)
            });
            target.Timeline.CurrentIndex = 10;
            target.Timeline.IsPlaying = true;

            target.Boundary.Apply(new SetTimelineAnchor(target.ColumnId, 10, true, false, 10, 1000, 1000), V2MutationApplicationOrigin.Remote, new OperationId(Guid.NewGuid()));
            Assert.That(target.Timeline.CurrentIndex, Is.EqualTo(10), "One-sample drift must not seek while playing.");

            target.Timeline.CurrentIndex = 14;
            target.Boundary.Apply(new SetTimelineAnchor(target.ColumnId, 11, true, false, 10, 1000, 1000), V2MutationApplicationOrigin.Remote, new OperationId(Guid.NewGuid()));
            Assert.That(target.Timeline.CurrentIndex, Is.EqualTo(14), "One-sample drift must not seek while the accepted estimate adds uncertainty to the threshold.");

            target.Timeline.CurrentIndex = 20;
            target.Boundary.Apply(new SetTimelineAnchor(target.ColumnId, 12, true, false, 10, 1000, 1000), V2MutationApplicationOrigin.Remote, new OperationId(Guid.NewGuid()));
            Assert.That(target.Timeline.CurrentIndex, Is.EqualTo(15), "Drift beyond one sample plus uncertainty must seek to the estimated position.");
        }

        [Test]
        public void ScenePresentationCheckpoint_AppliesShowAllMaskPolicyWithoutEcho()
        {
            using var source = new BoundSceneFixture();
            using var target = new BoundSceneFixture();
            var sourceMutations = new List<V2Mutation>();
            source.Boundary.MutationProposed += (_, mutation, _) => sourceMutations.Add(mutation);

            source.Scene.StrongCuts = true;
            source.Scene.ShowAllSites = true;
            source.Scene.SiteGain = 1.5f;
            source.Scene.AtlasManager.AtlasAlpha = 0.4f;
            source.Scene.BrainMaterials.SetAlpha(0.6f);

            Assert.That(sourceMutations.Select(mutation => mutation.Type), Is.EquivalentTo(new[]
            {
                V2OperationType.SetSceneBoolean,
                V2OperationType.SetSceneBoolean,
                V2OperationType.SetSceneFloat,
                V2OperationType.SetSceneFloat,
                V2OperationType.SetSceneFloat
            }));

            V2SceneMutationCheckpoint captured;
            try
            {
                captured = source.Boundary.CaptureCheckpoint();
            }
            catch (Exception exception)
            {
                throw new AssertionException("checkpoint capture failed: " + exception);
            }

            byte[] encoded;
            try
            {
                encoded = V2SceneMutationCheckpointCodec.Encode(9, captured);
            }
            catch (Exception exception)
            {
                throw new AssertionException("checkpoint encoding failed: " + exception);
            }

            V2SceneMutationCheckpoint checkpoint = V2SceneMutationCheckpointCodec.Decode(encoded).Checkpoint;
            Assert.That(checkpoint.T09Records, Has.Count.EqualTo(33));
            Assert.That(checkpoint.T09Records.Select(record => record.Value.Type), Does.Contain(V2OperationType.SetSceneBoolean));
            Assert.That(checkpoint.T09Records.Select(record => record.Value).OfType<SetSceneBoolean>().Any(value => value.Property == V2SceneBooleanProperty.AutomaticCutAroundSelectedSite), Is.True);
            Assert.That(checkpoint.T09Records.Select(record => record.Value).OfType<SetSceneBoolean>().Any(value => value.Property == V2SceneBooleanProperty.ShowAllSites), Is.True);

            var targetMutations = new List<V2Mutation>();
            target.Boundary.MutationProposed += (_, mutation, _) => targetMutations.Add(mutation);
            ResetSceneInvalidationFlags(target.Scene);
            target.Scene.SceneInformation.ProjectionGridNeedsUpdate = false;
            target.Scene.SceneInformation.SurfaceProjectionNeedsUpdate = false;
            ulong inputGeneration = target.Scene.ActivityInputGeneration;
            try
            {
                target.Boundary.ApplyCheckpoint(checkpoint, new OperationId(Guid.NewGuid()));
            }
            catch (Exception exception)
            {
                throw new AssertionException("checkpoint apply failed: " + exception);
            }

            Assert.That(target.Scene.StrongCuts, Is.True);
            Assert.That(target.Scene.ShowAllSites, Is.True);
            Assert.That(target.Scene.SiteGain, Is.EqualTo(1.5f));
            Assert.That(target.Scene.AtlasManager.AtlasAlpha, Is.EqualTo(0.4f));
            Assert.That(target.Scene.BrainMaterials.Alpha, Is.EqualTo(0.6f));
            Assert.That(targetMutations, Is.Empty);
            Assert.That(target.Scene.SceneInformation.GeneratorNeedsUpdate, Is.True);
            Assert.That(target.Scene.SceneInformation.ProjectionGridNeedsUpdate, Is.False);
            Assert.That(target.Scene.SceneInformation.GeometryNeedsUpdate, Is.False);
            Assert.That(target.Scene.ActivityInputGeneration, Is.EqualTo(inputGeneration + 1));
        }

        [Test]
        public void T11BlacklistInfluenceAndShowAll_ApplyThroughPreparedSceneTargets()
        {
            using var fixture = new BoundSceneFixture(V2OriginDevice.Desktop);
            var proposals = new List<V2Mutation>();
            fixture.Boundary.MutationProposed += (_, mutation, _) => proposals.Add(mutation);

            fixture.Site.State.IsBlackListed = true;
            Assert.That(proposals.Select(mutation => mutation.Type), Is.EqualTo(new[] { V2OperationType.SetSiteBlacklist }));
            proposals.Clear();

            fixture.Boundary.Apply(new SetSiteBlacklist(new ColumnId(fixture.Column.ColumnData.ID), new SiteId(fixture.Site.Information.FullID), false), V2MutationApplicationOrigin.Remote, T09Operation(351));
            fixture.Boundary.Apply(new SetInfluenceDistance(new ColumnId(fixture.Column.ColumnData.ID), 27.5f), V2MutationApplicationOrigin.Remote, T09Operation(352));
            fixture.Boundary.Apply(new SetInfluenceDistance(new ColumnId(fixture.DynamicColumn.ColumnData.ID), 42.5f), V2MutationApplicationOrigin.Remote, T09Operation(357));
            Assert.That(fixture.Site.State.IsBlackListed, Is.False);
            Assert.That(fixture.Column.AnatomyParameters.InfluenceDistance, Is.EqualTo(27.5f));
            Assert.That(fixture.DynamicColumn.DynamicParameters.InfluenceDistance, Is.EqualTo(42.5f));
            Assert.That(proposals, Is.Empty, "Remote application must not echo a T11 operation.");

            ulong inputGeneration = fixture.Scene.ActivityInputGeneration;
            fixture.Boundary.Apply(new SetSceneBoolean(V2SceneBooleanProperty.ShowAllSites, true), V2MutationApplicationOrigin.Remote, T09Operation(353));
            Assert.That(fixture.Scene.ShowAllSites, Is.True);
            Assert.That(fixture.Site.State.IsOutOfROI, Is.True);
            Assert.That(fixture.Scene.ActivityInputGeneration, Is.EqualTo(inputGeneration + 1), "ShowAllSites must invalidate the effective ROI mask once.");
        }

        [Test]
        public void T11SiteConfigurationBatch_AppliesPersistedFieldsOnceAndCheckpointRestoresThem()
        {
            using var source = new BoundSceneFixture(V2OriginDevice.Desktop);
            using var target = new BoundSceneFixture(V2OriginDevice.Quest);
            source.Site.State.ApplySynchronizedState(true, true, true, new Color(0.15f, 0.35f, 0.55f, 0.75f), new[] { "reviewed", "T11" });
            source.Site.State.IsFiltered = false;
            var assignment = new V2SiteConfigurationAssignment(new ColumnId(source.Column.ColumnData.ID), new SiteId(source.Site.Information.FullID), false, false, 0.8f, 0.6f, 0.4f, 1f, new[] { "authoritative", "ordered" });
            var batch = new SetSiteConfigurationBatch(new[] { assignment });
            int stateChanges = 0;
            target.Site.State.OnChangeState.AddListener(() => stateChanges++);
            target.Site.State.IsFiltered = false;
            stateChanges = 0;
            var echoes = new List<V2Mutation>();
            target.Boundary.MutationProposed += (_, mutation, _) => echoes.Add(mutation);

            var invalidBatch = new SetSiteConfigurationBatch(new[]
            {
                new V2SiteConfigurationAssignment(new ColumnId(target.Column.ColumnData.ID), new SiteId(target.Site.Information.FullID), true, true, 0f, 1f, 0f, 1f, Array.Empty<string>()),
                new V2SiteConfigurationAssignment(new ColumnId(target.Column.ColumnData.ID), new SiteId("missing-site"), true, true, 0f, 1f, 0f, 1f, Array.Empty<string>())
            });
            Assert.Throws<KeyNotFoundException>(() => target.Boundary.Apply(invalidBatch, V2MutationApplicationOrigin.Remote, T09Operation(360)));
            Assert.That(target.Site.State.IsBlackListed, Is.False, "All assignment identities must validate before the first site changes.");

            target.Boundary.Apply(batch, V2MutationApplicationOrigin.Remote, T09Operation(354));

            Assert.That(target.Site.State.IsBlackListed, Is.False);
            Assert.That(target.Site.State.IsHighlighted, Is.False);
            Assert.That(target.Site.State.Color, Is.EqualTo(new Color(0.8f, 0.6f, 0.4f, 1f)));
            Assert.That(target.Site.State.Labels, Is.EqualTo(new[] { "authoritative", "ordered" }));
            Assert.That(target.Site.State.IsFiltered, Is.False, "Filter state is derived/local and is excluded from the persisted assignment payload.");
            Assert.That(stateChanges, Is.EqualTo(1));
            Assert.That(echoes, Is.Empty);

            V2SceneMutationCheckpoint checkpoint = V2SceneMutationCheckpointCodec.Decode(V2SceneMutationCheckpointCodec.Encode(22, source.Boundary.CaptureCheckpoint())).Checkpoint;
            Assert.That(checkpoint.T11Records.Any(record => record.Value is SetSiteConfigurationBatch), Is.True);
            target.Site.State.IsFiltered = true;
            stateChanges = 0;
            target.Boundary.ApplyCheckpoint(checkpoint, T09Operation(355));

            Assert.That(target.Site.State.IsBlackListed, Is.True);
            Assert.That(target.Site.State.IsHighlighted, Is.True);
            Assert.That(target.Site.State.Color, Is.EqualTo(new Color(0.15f, 0.35f, 0.55f, 0.75f)));
            Assert.That(target.Site.State.Labels, Is.EqualTo(new[] { "reviewed", "T11" }));
            Assert.That(target.Site.State.IsFiltered, Is.False, "The typed checkpoint restores the canonical T12 filter mask captured from Desktop.");
            Assert.That(echoes, Is.Empty);
        }

        [Test]
        public void T11ConfigurationTransaction_PrevalidatesEveryChildBeforeApplyingAny()
        {
            using var fixture = new BoundSceneFixture(V2OriginDevice.Desktop);
            var transaction = new SetConfigurationTransaction(new V2Mutation[]
            {
                new SetSiteBlacklist(new ColumnId(fixture.Column.ColumnData.ID), new SiteId(fixture.Site.Information.FullID), true),
                new SetInfluenceDistance(new ColumnId("missing-column"), 40f)
            });

            Assert.Throws<KeyNotFoundException>(() => fixture.Boundary.Apply(transaction, V2MutationApplicationOrigin.Remote, T09Operation(356)));
            Assert.That(fixture.Site.State.IsBlackListed, Is.False);

            var validTransaction = new SetConfigurationTransaction(new V2Mutation[]
            {
                new SetSiteBlacklist(new ColumnId(fixture.Column.ColumnData.ID), new SiteId(fixture.Site.Information.FullID), true),
                new SetInfluenceDistance(new ColumnId(fixture.Column.ColumnData.ID), 31f)
            });
            fixture.Boundary.Apply(validTransaction, V2MutationApplicationOrigin.Remote, T09Operation(361));

            Assert.That(fixture.Site.State.IsBlackListed, Is.True);
            Assert.That(fixture.Column.AnatomyParameters.InfluenceDistance, Is.EqualTo(31f));
        }

        [Test]
        public void T11ConfigurationTransaction_RejectsNonReversibleMovementAndTimelineChildrenBeforeApplying()
        {
            using var fixture = new BoundSceneFixture(V2OriginDevice.Desktop);
            var moveSites = new SetConfigurationTransaction(new V2Mutation[]
            {
                new SetSiteBlacklist(new ColumnId(fixture.Column.ColumnData.ID), new SiteId(fixture.Site.Information.FullID), true),
                new MoveSites(V2SiteMoveCommand.Left)
            });

            Assert.Throws<InvalidOperationException>(() => fixture.Boundary.Apply(moveSites, V2MutationApplicationOrigin.Remote, T09Operation(368)));
            Assert.That(fixture.Site.State.IsBlackListed, Is.False);

            var timeline = new SetConfigurationTransaction(new V2Mutation[]
            {
                new SetSceneBoolean(V2SceneBooleanProperty.ShowAllSites, true),
                new SetTimelineAnchor(new ColumnId(fixture.Column.ColumnData.ID), 0, false, false, 1, 0, 1000)
            });

            Assert.Throws<InvalidOperationException>(() => fixture.Boundary.Apply(timeline, V2MutationApplicationOrigin.Remote, T09Operation(369)));
            Assert.That(fixture.Scene.ShowAllSites, Is.False);
        }

        [Test]
        public void T11ConfigurationTransaction_ValidatesStagedCutsRoisAndSelections()
        {
            using var fixture = new BoundSceneFixture(V2OriginDevice.Desktop, configureCutCreation: true);
            fixture.Scene.Columns.Clear();
            var firstCutId = new CutId("t11-staged-cut-first");
            var secondCutId = new CutId("t11-staged-cut-second");
            var createCuts = new SetConfigurationTransaction(new V2Mutation[]
            {
                new CreateCut(firstCutId, new SetCutDefinition(firstCutId, V2CutOrientation.Custom, false, 1, 0f, 1f, 0f, 0f), 0),
                new CreateCut(secondCutId, new SetCutDefinition(secondCutId, V2CutOrientation.Custom, false, 1, 0.5f, 1f, 0f, 0f), 1)
            });

            try
            {
                fixture.Boundary.Apply(createCuts, V2MutationApplicationOrigin.Remote, T09Operation(362));
            }
            catch (Exception exception)
            {
                Assert.Fail("Creating staged cuts failed: " + exception);
            }

            Assert.That(fixture.Scene.Cuts.Select(cut => cut.ID), Is.EqualTo(new[] { firstCutId.Value, secondCutId.Value }));

            var replacement = new SetConfigurationTransaction(new V2Mutation[]
            {
                new DeleteCut(firstCutId),
                new CreateCut(firstCutId, new SetCutDefinition(firstCutId, V2CutOrientation.Custom, true, 3, 0.25f, 0f, 1f, 0f), 0)
            });
            try
            {
                fixture.Boundary.Apply(replacement, V2MutationApplicationOrigin.Remote, T09Operation(363));
            }
            catch (Exception exception)
            {
                Assert.Fail("Replacing a staged cut failed: " + exception);
            }

            Assert.That(fixture.Scene.Cuts.Select(cut => cut.ID), Is.EqualTo(new[] { firstCutId.Value, secondCutId.Value }));
            Assert.That(fixture.Scene.Cuts[0].Flip, Is.True);
            Assert.That(fixture.Scene.Cuts[0].NumberOfCuts, Is.EqualTo(3));

            var stagedRoiId = new RoiId("t11-staged-roi");
            var stagedSphereId = new SphereId("t11-staged-sphere");
            var createThenSelect = new SetConfigurationTransaction(new V2Mutation[]
            {
                new CreateRoi(stagedRoiId, "staged ROI", new[] { new V2RoiSphereDefinition(stagedSphereId, 1f, 2f, 3f, 4f) }, 1),
                new SetSelectedRoiSphere(stagedRoiId.Value, stagedSphereId.Value),
                new SetActiveRoi(stagedRoiId)
            });
            try
            {
                fixture.Boundary.Apply(createThenSelect, V2MutationApplicationOrigin.Remote, T09Operation(364));
            }
            catch (Exception exception)
            {
                Assert.Fail("Creating and selecting a staged ROI failed: " + exception);
            }

            ROI stagedRoi = fixture.Scene.ROIManager.ROIs.Single(roi => roi.ID == stagedRoiId.Value);
            Assert.That(stagedRoi.SelectedSphereID, Is.EqualTo(0));
            Assert.That(fixture.Scene.ROIManager.SelectedROI, Is.SameAs(stagedRoi));
        }

        [Test]
        public void T11ConfigurationTransaction_RestoresCapturedStateAfterChildFailureAndSurfacesRollbackFailure()
        {
            using var fixture = new BoundSceneFixture(V2OriginDevice.Quest);
            var transaction = new SetConfigurationTransaction(new V2Mutation[]
            {
                new SetSiteBlacklist(new ColumnId(fixture.Column.ColumnData.ID), new SiteId(fixture.Site.Information.FullID), true),
                new SetInfluenceDistance(new ColumnId(fixture.Column.ColumnData.ID), 31f)
            });
            int eventCount = 0;
            UnityEngine.Events.UnityAction throwOnce = () =>
            {
                if (++eventCount == 1) throw new InvalidOperationException("injected application failure");
            };
            fixture.Column.AnatomyParameters.OnUpdateInfluenceDistance.AddListener(throwOnce);
            var echoes = new List<V2Mutation>();
            fixture.Boundary.MutationProposed += (_, mutation, _) => echoes.Add(mutation);
            try
            {
                Assert.Throws<InvalidOperationException>(() => fixture.Boundary.Apply(transaction, V2MutationApplicationOrigin.Remote, T09Operation(365)));
            }
            finally
            {
                fixture.Column.AnatomyParameters.OnUpdateInfluenceDistance.RemoveListener(throwOnce);
            }

            Assert.That(fixture.Site.State.IsBlackListed, Is.False);
            Assert.That(fixture.Column.AnatomyParameters.InfluenceDistance, Is.EqualTo(15f));
            Assert.That(echoes, Is.Empty);

            UnityEngine.Events.UnityAction alwaysThrow = () => throw new InvalidOperationException("injected rollback failure");
            fixture.Column.AnatomyParameters.OnUpdateInfluenceDistance.AddListener(alwaysThrow);
            try
            {
                Assert.Throws<AggregateException>(() => fixture.Boundary.Apply(transaction, V2MutationApplicationOrigin.Remote, T09Operation(366)));
            }
            finally
            {
                fixture.Column.AnatomyParameters.OnUpdateInfluenceDistance.RemoveListener(alwaysThrow);
            }
        }

        [Test]
        public void T11ConfigurationTransaction_QuestRejectionRestoresCompleteOptimisticCheckpointWithoutEcho()
        {
            using var fixture = new BoundSceneFixture(V2OriginDevice.Quest);
            var scheduler = new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest, new TestClock(0));
            using var driver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, fixture.Boundary, scheduler);
            bool originalBlacklist = fixture.Site.State.IsBlackListed;
            bool originalHighlight = fixture.Site.State.IsHighlighted;
            Color originalColor = fixture.Site.State.Color;
            string[] originalLabels = fixture.Site.State.Labels.ToArray();
            bool originalShowAllSites = fixture.Scene.ShowAllSites;
            var assignment = new V2SiteConfigurationAssignment(new ColumnId(fixture.Column.ColumnData.ID), new SiteId(fixture.Site.Information.FullID), !originalBlacklist, !originalHighlight, 0.12f, 0.34f, 0.56f, 0.78f, new[] { "optimistic", "configuration" });
            var transaction = new SetConfigurationTransaction(new V2Mutation[]
            {
                new SetSiteConfigurationBatch(new[] { assignment }),
                new SetSceneBoolean(V2SceneBooleanProperty.ShowAllSites, !originalShowAllSites)
            });
            var echoes = new List<V2Mutation>();
            fixture.Boundary.MutationProposed += (_, mutation, _) => echoes.Add(mutation);

            V2QuestMutationProposal proposal = driver.ApplyOptimistic(transaction, T09Operation(367));

            Assert.That(proposal, Is.Not.Null);
            Assert.That(proposal.Mutation, Is.TypeOf<SetConfigurationTransaction>());
            Assert.That(fixture.Site.State.IsBlackListed, Is.EqualTo(!originalBlacklist));
            Assert.That(fixture.Site.State.IsHighlighted, Is.EqualTo(!originalHighlight));
            Assert.That(fixture.Site.State.Color, Is.EqualTo(new Color(0.12f, 0.34f, 0.56f, 0.78f)));
            Assert.That(fixture.Site.State.Labels, Is.EqualTo(new[] { "optimistic", "configuration" }));
            Assert.That(fixture.Scene.ShowAllSites, Is.EqualTo(!originalShowAllSites));
            Assert.That(echoes, Has.Count.EqualTo(1));

            Assert.That(driver.ReceiveRejection(proposal.OperationId, "configuration_rejected"), Is.True);

            Assert.That(fixture.Site.State.IsBlackListed, Is.EqualTo(originalBlacklist));
            Assert.That(fixture.Site.State.IsHighlighted, Is.EqualTo(originalHighlight));
            Assert.That(fixture.Site.State.Color, Is.EqualTo(originalColor));
            Assert.That(fixture.Site.State.Labels, Is.EqualTo(originalLabels));
            Assert.That(fixture.Scene.ShowAllSites, Is.EqualTo(originalShowAllSites));
            Assert.That(driver.PendingProposalCount, Is.Zero);
            Assert.That(echoes, Has.Count.EqualTo(1), "Restoring an authoritative checkpoint after rejection must not echo child mutations.");
        }

        [Test]
        public void T11D34BatchRejection_RebasesLaterTransactionRollbackCheckpointWithoutEcho()
        {
            using var fixture = new BoundSceneFixture(V2OriginDevice.Quest);
            var scheduler = new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest, new TestClock(0));
            using var driver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, fixture.Boundary, scheduler);
            bool originalBlacklist = fixture.Site.State.IsBlackListed;
            bool originalHighlight = fixture.Site.State.IsHighlighted;
            Color originalColor = fixture.Site.State.Color;
            string[] originalLabels = fixture.Site.State.Labels.ToArray();
            bool originalShowAllSites = fixture.Scene.ShowAllSites;
            var columnId = new ColumnId(fixture.Column.ColumnData.ID);
            var siteId = new SiteId(fixture.Site.Information.FullID);
            var assignment = new V2SiteConfigurationAssignment(columnId, siteId, !originalBlacklist, !originalHighlight, 0.14f, 0.28f, 0.42f, 0.86f, new[] { "rejected", "batch", "ordered" });
            var echoes = new List<V2Mutation>();
            fixture.Boundary.MutationProposed += (_, mutation, _) => echoes.Add(mutation);

            V2QuestMutationProposal batchProposal = driver.ApplyOptimistic(new SetSiteConfigurationBatch(new[] { assignment }), T09Operation(936));
            var transaction = new SetConfigurationTransaction(new V2Mutation[]
            {
                new SetSceneBoolean(V2SceneBooleanProperty.ShowAllSites, !originalShowAllSites)
            });
            V2QuestMutationProposal transactionProposal = driver.ApplyOptimistic(transaction, T09Operation(937));

            Assert.That(batchProposal, Is.Not.Null);
            Assert.That(transactionProposal, Is.Not.Null);
            Assert.That(driver.PendingProposalCount, Is.EqualTo(2));
            Assert.That(fixture.Site.State.IsBlackListed, Is.EqualTo(!originalBlacklist));
            Assert.That(fixture.Site.State.IsHighlighted, Is.EqualTo(!originalHighlight));
            Assert.That(fixture.Site.State.Color, Is.EqualTo(new Color(0.14f, 0.28f, 0.42f, 0.86f)));
            Assert.That(fixture.Site.State.Labels, Is.EqualTo(new[] { "rejected", "batch", "ordered" }));

            Assert.That(driver.ReceiveRejection(batchProposal.OperationId, "batch_rejected"), Is.True);
            Assert.That(driver.PendingProposalCount, Is.EqualTo(1));
            Assert.That(fixture.Site.State.IsBlackListed, Is.EqualTo(originalBlacklist));
            Assert.That(fixture.Site.State.IsHighlighted, Is.EqualTo(originalHighlight));
            Assert.That(fixture.Site.State.Color, Is.EqualTo(originalColor));
            Assert.That(fixture.Site.State.Labels, Is.EqualTo(originalLabels));

            Assert.That(driver.ReceiveRejection(transactionProposal.OperationId, "transaction_rejected"), Is.True);

            Assert.That(fixture.Site.State.IsBlackListed, Is.EqualTo(originalBlacklist));
            Assert.That(fixture.Site.State.IsHighlighted, Is.EqualTo(originalHighlight));
            Assert.That(fixture.Site.State.Color, Is.EqualTo(originalColor));
            Assert.That(fixture.Site.State.Labels, Is.EqualTo(originalLabels));
            Assert.That(fixture.Scene.ShowAllSites, Is.EqualTo(originalShowAllSites));
            Assert.That(driver.PendingProposalCount, Is.Zero, "Both rejected proposals must be removed.");
            Assert.That(echoes, Has.Count.EqualTo(2), "Batch and transaction rollback must not publish mutation echoes.");
        }

        [Test]
        public void T11D33TransactionBeforeD34Batch_RejectionPreservesEarlierCheckpointWithoutEcho()
        {
            using var fixture = new BoundSceneFixture(V2OriginDevice.Quest);
            var scheduler = new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest, new TestClock(0));
            using var driver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, fixture.Boundary, scheduler);
            bool originalBlacklist = fixture.Site.State.IsBlackListed;
            bool originalHighlight = fixture.Site.State.IsHighlighted;
            Color originalColor = fixture.Site.State.Color;
            string[] originalLabels = fixture.Site.State.Labels.ToArray();
            var columnId = new ColumnId(fixture.Column.ColumnData.ID);
            var siteId = new SiteId(fixture.Site.Information.FullID);
            var echoes = new List<V2Mutation>();
            fixture.Boundary.MutationProposed += (_, mutation, _) => echoes.Add(mutation);

            var transaction = new SetConfigurationTransaction(new V2Mutation[]
            {
                new SetSiteBlacklist(columnId, siteId, !originalBlacklist)
            });
            V2QuestMutationProposal transactionProposal = driver.ApplyOptimistic(transaction, T09Operation(938));
            var assignment = new V2SiteConfigurationAssignment(columnId, siteId, originalBlacklist, !originalHighlight, 0.18f, 0.36f, 0.54f, 0.72f, new[] { "later", "rejected", "batch" });
            V2QuestMutationProposal batchProposal = driver.ApplyOptimistic(new SetSiteConfigurationBatch(new[] { assignment }), T09Operation(939));

            Assert.That(transactionProposal, Is.Not.Null);
            Assert.That(batchProposal, Is.Not.Null);
            Assert.That(driver.PendingProposalCount, Is.EqualTo(2));
            Assert.That(fixture.Site.State.IsBlackListed, Is.EqualTo(originalBlacklist));
            Assert.That(fixture.Site.State.IsHighlighted, Is.EqualTo(!originalHighlight));
            Assert.That(fixture.Site.State.Color, Is.EqualTo(new Color(0.18f, 0.36f, 0.54f, 0.72f)));
            Assert.That(fixture.Site.State.Labels, Is.EqualTo(new[] { "later", "rejected", "batch" }));

            Assert.That(driver.ReceiveRejection(batchProposal.OperationId, "batch_rejected"), Is.True);
            Assert.That(driver.PendingProposalCount, Is.EqualTo(1));
            Assert.That(fixture.Site.State.IsBlackListed, Is.EqualTo(!originalBlacklist));

            Assert.That(driver.ReceiveRejection(transactionProposal.OperationId, "transaction_rejected"), Is.True);

            Assert.That(fixture.Site.State.IsBlackListed, Is.EqualTo(originalBlacklist));
            Assert.That(fixture.Site.State.IsHighlighted, Is.EqualTo(originalHighlight));
            Assert.That(fixture.Site.State.Color, Is.EqualTo(originalColor));
            Assert.That(fixture.Site.State.Labels, Is.EqualTo(originalLabels));
            Assert.That(driver.PendingProposalCount, Is.Zero, "Both rejected proposals must be removed.");
            Assert.That(echoes, Has.Count.EqualTo(2), "Batch and transaction rollback must not publish mutation echoes.");
        }

        [Test]
        public void T11RejectedD34Batch_RebasesLaterBatchRollbackAndTransactionCheckpointWithoutEcho()
        {
            using var fixture = new BoundSceneFixture(V2OriginDevice.Quest);
            var scheduler = new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest, new TestClock(0));
            using var driver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, fixture.Boundary, scheduler);
            bool originalBlacklist = fixture.Site.State.IsBlackListed;
            bool originalHighlight = fixture.Site.State.IsHighlighted;
            Color originalColor = fixture.Site.State.Color;
            string[] originalLabels = fixture.Site.State.Labels.ToArray();
            bool originalShowAllSites = fixture.Scene.ShowAllSites;
            var columnId = new ColumnId(fixture.Column.ColumnData.ID);
            var siteId = new SiteId(fixture.Site.Information.FullID);
            var firstAssignment = new V2SiteConfigurationAssignment(columnId, siteId, !originalBlacklist, !originalHighlight, 0.11f, 0.22f, 0.33f, 0.44f, new[] { "first", "rejected" });
            var originalAssignment = new V2SiteConfigurationAssignment(columnId, siteId, originalBlacklist, originalHighlight, originalColor.r, originalColor.g, originalColor.b, originalColor.a, originalLabels);
            var echoes = new List<V2Mutation>();
            fixture.Boundary.MutationProposed += (_, mutation, _) => echoes.Add(mutation);

            V2QuestMutationProposal firstBatch = driver.ApplyOptimistic(new SetSiteConfigurationBatch(new[] { firstAssignment }), T09Operation(940));
            V2QuestMutationProposal secondBatch = driver.ApplyOptimistic(new SetSiteConfigurationBatch(new[] { originalAssignment }), T09Operation(941));
            var transaction = new SetConfigurationTransaction(new V2Mutation[]
            {
                new SetSceneBoolean(V2SceneBooleanProperty.ShowAllSites, !originalShowAllSites)
            });
            V2QuestMutationProposal configuration = driver.ApplyOptimistic(transaction, T09Operation(942));

            Assert.That(firstBatch, Is.Not.Null);
            Assert.That(secondBatch, Is.Not.Null);
            Assert.That(configuration, Is.Not.Null);
            Assert.That(driver.PendingProposalCount, Is.EqualTo(3));
            Assert.That(fixture.Site.State.IsBlackListed, Is.EqualTo(originalBlacklist));
            Assert.That(fixture.Scene.ShowAllSites, Is.EqualTo(!originalShowAllSites));

            Assert.That(driver.ReceiveRejection(firstBatch.OperationId, "first_batch_rejected"), Is.True);
            Assert.That(driver.PendingProposalCount, Is.EqualTo(2));
            Assert.That(fixture.Site.State.IsBlackListed, Is.EqualTo(originalBlacklist));
            Assert.That(fixture.Site.State.IsHighlighted, Is.EqualTo(originalHighlight));
            Assert.That(fixture.Site.State.Color, Is.EqualTo(originalColor));
            Assert.That(fixture.Site.State.Labels, Is.EqualTo(originalLabels));

            Assert.That(driver.ReceiveRejection(secondBatch.OperationId, "second_batch_rejected"), Is.True);
            Assert.That(driver.PendingProposalCount, Is.EqualTo(1));
            Assert.That(fixture.Site.State.IsBlackListed, Is.EqualTo(originalBlacklist));

            Assert.That(driver.ReceiveRejection(configuration.OperationId, "configuration_rejected"), Is.True);

            Assert.That(fixture.Site.State.IsBlackListed, Is.EqualTo(originalBlacklist));
            Assert.That(fixture.Site.State.IsHighlighted, Is.EqualTo(originalHighlight));
            Assert.That(fixture.Site.State.Color, Is.EqualTo(originalColor));
            Assert.That(fixture.Site.State.Labels, Is.EqualTo(originalLabels));
            Assert.That(fixture.Scene.ShowAllSites, Is.EqualTo(originalShowAllSites));
            Assert.That(driver.PendingProposalCount, Is.Zero, "All three rejected proposals must be removed.");
            Assert.That(echoes, Has.Count.EqualTo(3), "Batch and transaction rollback must not publish mutation echoes.");
        }

        [Test]
        public void T11RejectedD34Batch_PreservesCanonicalSiteValueBeforeLaterTransactionCheckpoint()
        {
            using var desktop = new BoundSceneFixture(V2OriginDevice.Desktop);
            using var quest = new BoundSceneFixture(V2OriginDevice.Quest);
            using var authority = new V2DesktopMutationAuthority(SceneIdForT09, IncarnationIdForT09, desktop.Boundary);
            var scheduler = new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest, new TestClock(0));
            using var driver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, quest.Boundary, scheduler);
            var columnId = new ColumnId(quest.Column.ColumnData.ID);
            var siteId = new SiteId(quest.Site.Information.FullID);
            Color white = Color.white;
            Color red = new(0.85f, 0.12f, 0.18f, 1f);
            Color blue = new(0.12f, 0.24f, 0.88f, 1f);
            bool originalShowAllSites = quest.Scene.ShowAllSites;
            desktop.Boundary.Apply(new SetSiteColor(new ColumnId(desktop.Column.ColumnData.ID), new SiteId(desktop.Site.Information.FullID), white.r, white.g, white.b, white.a), V2MutationApplicationOrigin.Remote, T09Operation(943));
            quest.Boundary.Apply(new SetSiteColor(columnId, siteId, white.r, white.g, white.b, white.a), V2MutationApplicationOrigin.Remote, T09Operation(944));
            var echoes = new List<V2Mutation>();
            quest.Boundary.MutationProposed += (_, mutation, _) => echoes.Add(mutation);
            var canonicalMutations = new List<V2CanonicalMutation>();
            authority.CanonicalReady += canonicalMutations.Add;

            V2QuestMutationProposal batchProposal = driver.ApplyOptimistic(new SetSiteConfigurationBatch(new[]
            {
                new V2SiteConfigurationAssignment(columnId, siteId, quest.Site.State.IsBlackListed, quest.Site.State.IsHighlighted, red.r, red.g, red.b, red.a, quest.Site.State.Labels)
            }), T09Operation(945));
            desktop.Boundary.Apply(new SetSiteColor(new ColumnId(desktop.Column.ColumnData.ID), new SiteId(desktop.Site.Information.FullID), blue.r, blue.g, blue.b, blue.a), V2MutationApplicationOrigin.LocalDesktop, T09Operation(946));
            Assert.That(canonicalMutations, Has.Count.EqualTo(1));
            Assert.That(driver.ReceiveCanonical(canonicalMutations[0]), Is.True);
            Assert.That(quest.Site.State.Color, Is.EqualTo(blue));
            V2QuestMutationProposal transactionProposal = driver.ApplyOptimistic(new SetConfigurationTransaction(new V2Mutation[]
            {
                new SetSceneBoolean(V2SceneBooleanProperty.ShowAllSites, !quest.Scene.ShowAllSites)
            }), T09Operation(947));

            Assert.That(batchProposal, Is.Not.Null);
            Assert.That(transactionProposal, Is.Not.Null);
            Assert.That(driver.PendingProposalCount, Is.EqualTo(2));
            Assert.That(quest.Scene.ShowAllSites, Is.EqualTo(!originalShowAllSites));
            Assert.That(driver.ReceiveRejection(batchProposal.OperationId, "batch_rejected"), Is.True);
            Assert.That(quest.Site.State.Color, Is.EqualTo(blue), "Rejecting the earlier batch must preserve the intervening canonical color.");
            Assert.That(driver.PendingProposalCount, Is.EqualTo(1));
            Assert.That(driver.LastObservedCanonicalSequence, Is.EqualTo(1UL));
            Assert.That(driver.ReceiveRejection(transactionProposal.OperationId, "transaction_rejected"), Is.True);

            Assert.That(quest.Site.State.Color, Is.EqualTo(blue), "The later transaction checkpoint must retain the canonical color it captured.");
            Assert.That(quest.Site.State.Color, Is.Not.EqualTo(white));
            Assert.That(quest.Scene.ShowAllSites, Is.EqualTo(originalShowAllSites));
            Assert.That(driver.LastObservedCanonicalSequence, Is.EqualTo(1UL), "Rollback must not rewind the canonical watermark.");
            Assert.That(driver.PendingProposalCount, Is.Zero);
            Assert.That(authority.CanonicalSequence, Is.EqualTo(1UL));
            Assert.That(echoes, Has.Count.EqualTo(2), "Conditional rollback and checkpoint restore must not publish echoes.");
            Assert.That(driver.ReceiveCanonical(canonicalMutations[0]), Is.False, "Canonical deduplication must survive both rejections.");
            Assert.That(quest.Site.State.Color, Is.EqualTo(blue));
        }

        [Test]
        public void T11RejectedEarlierD34Batch_PreservesLaterPendingBatchThroughCanonicalConfirmation()
        {
            using var desktop = new BoundSceneFixture(V2OriginDevice.Desktop);
            using var quest = new BoundSceneFixture(V2OriginDevice.Quest);
            using var authority = new V2DesktopMutationAuthority(SceneIdForT09, IncarnationIdForT09, desktop.Boundary);
            var scheduler = new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest, new TestClock(0));
            using var driver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, quest.Boundary, scheduler);
            var columnId = new ColumnId(quest.Column.ColumnData.ID);
            var siteId = new SiteId(quest.Site.Information.FullID);
            Color white = Color.white;
            Color red = new(0.82f, 0.1f, 0.16f, 1f);
            Color green = new(0.08f, 0.76f, 0.24f, 1f);
            desktop.Boundary.Apply(new SetSiteColor(new ColumnId(desktop.Column.ColumnData.ID), new SiteId(desktop.Site.Information.FullID), white.r, white.g, white.b, white.a), V2MutationApplicationOrigin.Remote, T09Operation(948));
            quest.Boundary.Apply(new SetSiteColor(columnId, siteId, white.r, white.g, white.b, white.a), V2MutationApplicationOrigin.Remote, T09Operation(949));
            var echoes = new List<V2Mutation>();
            quest.Boundary.MutationProposed += (_, mutation, _) => echoes.Add(mutation);

            V2QuestMutationProposal firstProposal = driver.ApplyOptimistic(new SetSiteConfigurationBatch(new[]
            {
                new V2SiteConfigurationAssignment(columnId, siteId, quest.Site.State.IsBlackListed, quest.Site.State.IsHighlighted, red.r, red.g, red.b, red.a, quest.Site.State.Labels)
            }), T09Operation(950));
            V2QuestMutationProposal secondProposal = driver.ApplyOptimistic(new SetSiteConfigurationBatch(new[]
            {
                new V2SiteConfigurationAssignment(columnId, siteId, quest.Site.State.IsBlackListed, quest.Site.State.IsHighlighted, green.r, green.g, green.b, green.a, quest.Site.State.Labels)
            }), T09Operation(951));

            Assert.That(firstProposal, Is.Not.Null);
            Assert.That(secondProposal, Is.Not.Null);
            Assert.That(driver.PendingProposalCount, Is.EqualTo(2));
            Assert.That(quest.Site.State.Color, Is.EqualTo(green));
            Assert.That(driver.ReceiveRejection(firstProposal.OperationId, "first_batch_rejected"), Is.True);

            Assert.That(quest.Site.State.Color, Is.EqualTo(green), "Rejecting the older batch must leave the newer pending assignment visible.");
            Assert.That(driver.PendingProposalCount, Is.EqualTo(1));
            V2DesktopProposalResult acceptedSecond = authority.AcceptQuestProposal(secondProposal);
            Assert.That(acceptedSecond.Outcome, Is.EqualTo(V2ProposalOutcome.Accepted));
            Assert.That(driver.ReceiveCanonical(acceptedSecond.CanonicalMutation), Is.False, "The matching canonical should confirm the pending green batch without applying it again.");

            Assert.That(quest.Site.State.Color, Is.EqualTo(green));
            Assert.That(driver.PendingProposalCount, Is.Zero);
            Assert.That(authority.CanonicalSequence, Is.EqualTo(1UL));
            Assert.That(driver.LastObservedCanonicalSequence, Is.EqualTo(1UL));
            Assert.That(echoes, Has.Count.EqualTo(2), "Rejecting and confirming batches must not publish mutation echoes.");
            Assert.That(driver.ReceiveCanonical(acceptedSecond.CanonicalMutation), Is.False, "Canonical deduplication must remain active after confirmation.");
            Assert.That(quest.Site.State.Color, Is.EqualTo(green));
            Assert.That(driver.LastObservedCanonicalSequence, Is.EqualTo(1UL));
        }

        [Test]
        public void T11RejectedD34Batch_PreservesSameValueAuthoritativeWriteAcrossTransactionRollback()
        {
            using var desktop = new BoundSceneFixture(V2OriginDevice.Desktop);
            using var quest = new BoundSceneFixture(V2OriginDevice.Quest);
            var desktopColumnId = new ColumnId(desktop.Column.ColumnData.ID);
            var desktopSiteId = new SiteId(desktop.Site.Information.FullID);
            var questColumnId = new ColumnId(quest.Column.ColumnData.ID);
            var questSiteId = new SiteId(quest.Site.Information.FullID);
            Color white = Color.white;
            Color red = new(0.86f, 0.13f, 0.19f, 1f);
            desktop.Boundary.Apply(new SetSiteColor(desktopColumnId, desktopSiteId, white.r, white.g, white.b, white.a), V2MutationApplicationOrigin.Remote, T09Operation(952));
            quest.Boundary.Apply(new SetSiteColor(questColumnId, questSiteId, white.r, white.g, white.b, white.a), V2MutationApplicationOrigin.Remote, T09Operation(953));
            using var authority = new V2DesktopMutationAuthority(SceneIdForT09, IncarnationIdForT09, desktop.Boundary);
            var scheduler = new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest, new TestClock(0));
            using var driver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, quest.Boundary, scheduler);
            var echoes = new List<V2Mutation>();
            quest.Boundary.MutationProposed += (_, mutation, _) => echoes.Add(mutation);
            var canonicalMutations = new List<V2CanonicalMutation>();
            authority.CanonicalReady += canonicalMutations.Add;

            V2QuestMutationProposal rejectedBatch = driver.ApplyOptimistic(new SetSiteConfigurationBatch(new[]
            {
                new V2SiteConfigurationAssignment(questColumnId, questSiteId, quest.Site.State.IsBlackListed, quest.Site.State.IsHighlighted, red.r, red.g, red.b, red.a, quest.Site.State.Labels)
            }), T09Operation(954));
            // Desktop accepts the same red value from another operation. Applying the
            // canonical on Quest is a value no-op, but it must replace O's field owner.
            desktop.Boundary.Apply(new SetSiteColor(desktopColumnId, desktopSiteId, red.r, red.g, red.b, red.a), V2MutationApplicationOrigin.LocalDesktop, T09Operation(955));
            Assert.That(canonicalMutations, Has.Count.EqualTo(1));
            Assert.That(driver.ReceiveCanonical(canonicalMutations[0]), Is.True);
            Assert.That(quest.Site.State.Color, Is.EqualTo(red));
            bool originalShowAllSites = quest.Scene.ShowAllSites;
            V2QuestMutationProposal rejectedTransaction = driver.ApplyOptimistic(new SetConfigurationTransaction(new V2Mutation[]
            {
                new SetSceneBoolean(V2SceneBooleanProperty.ShowAllSites, !originalShowAllSites)
            }), T09Operation(956));

            Assert.That(driver.PendingProposalCount, Is.EqualTo(2));
            Assert.That(driver.ReceiveRejection(rejectedBatch.OperationId, "batch_rejected"), Is.True);
            Assert.That(quest.Site.State.Color, Is.EqualTo(red), "The matching-value canonical owns the field after O is rejected.");
            Assert.That(driver.PendingProposalCount, Is.EqualTo(1));
            Assert.That(driver.ReceiveRejection(rejectedTransaction.OperationId, "transaction_rejected"), Is.True);

            Assert.That(quest.Site.State.Color, Is.EqualTo(red), "The transaction checkpoint must preserve the canonical writer it captured.");
            Assert.That(quest.Site.State.Color, Is.Not.EqualTo(white));
            Assert.That(quest.Scene.ShowAllSites, Is.EqualTo(originalShowAllSites));
            Assert.That(driver.PendingProposalCount, Is.Zero);
            Assert.That(authority.CanonicalSequence, Is.EqualTo(1UL));
            Assert.That(driver.LastObservedCanonicalSequence, Is.EqualTo(1UL));
            Assert.That(echoes, Has.Count.EqualTo(2), "Both rejections must suppress mutation echoes.");
            Assert.That(driver.ReceiveCanonical(canonicalMutations[0]), Is.False, "Canonical deduplication must survive both rejections.");
            Assert.That(quest.Site.State.Color, Is.EqualTo(red));
            Assert.That(driver.LastObservedCanonicalSequence, Is.EqualTo(1UL));
        }

        [Test]
        public void T11RejectedEarlierD34Batch_PreservesLaterSameValuePendingAssignmentByProvenance()
        {
            using var fixture = new BoundSceneFixture(V2OriginDevice.Quest);
            var scheduler = new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest, new TestClock(0));
            using var driver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, fixture.Boundary, scheduler);
            var columnId = new ColumnId(fixture.Column.ColumnData.ID);
            var siteId = new SiteId(fixture.Site.Information.FullID);
            Color white = Color.white;
            Color red = new(0.84f, 0.11f, 0.17f, 1f);
            Color green = new(0.09f, 0.77f, 0.23f, 1f);
            fixture.Boundary.Apply(new SetSiteColor(columnId, siteId, white.r, white.g, white.b, white.a), V2MutationApplicationOrigin.Remote, T09Operation(957));
            var echoes = new List<V2Mutation>();
            fixture.Boundary.MutationProposed += (_, mutation, _) => echoes.Add(mutation);

            V2QuestMutationProposal first = driver.ApplyOptimistic(new SetSiteConfigurationBatch(new[]
            {
                new V2SiteConfigurationAssignment(columnId, siteId, fixture.Site.State.IsBlackListed, fixture.Site.State.IsHighlighted, red.r, red.g, red.b, red.a, fixture.Site.State.Labels)
            }), T09Operation(958));
            V2QuestMutationProposal second = driver.ApplyOptimistic(new SetSiteConfigurationBatch(new[]
            {
                new V2SiteConfigurationAssignment(columnId, siteId, fixture.Site.State.IsBlackListed, fixture.Site.State.IsHighlighted, green.r, green.g, green.b, green.a, fixture.Site.State.Labels)
            }), T09Operation(959));
            V2QuestMutationProposal third = driver.ApplyOptimistic(new SetSiteConfigurationBatch(new[]
            {
                new V2SiteConfigurationAssignment(columnId, siteId, fixture.Site.State.IsBlackListed, fixture.Site.State.IsHighlighted, red.r, red.g, red.b, red.a, fixture.Site.State.Labels)
            }), T09Operation(960));

            Assert.That(first, Is.Not.Null);
            Assert.That(second, Is.Not.Null);
            Assert.That(third, Is.Not.Null);
            Assert.That(driver.PendingProposalCount, Is.EqualTo(3));
            Assert.That(fixture.Site.State.Color, Is.EqualTo(red));
            Assert.That(driver.ReceiveRejection(first.OperationId, "first_batch_rejected"), Is.True);

            Assert.That(fixture.Site.State.Color, Is.EqualTo(red), "The newest red write must remain even though it matches the rejected operation's value.");
            Assert.That(driver.PendingProposalCount, Is.EqualTo(2));
            Assert.That(driver.LastObservedCanonicalSequence, Is.Zero);
            Assert.That(echoes, Has.Count.EqualTo(3), "Rejecting O1 must not echo a rollback over O3.");

            Assert.That(driver.ReceiveRejection(third.OperationId, "third_batch_rejected"), Is.True);
            Assert.That(fixture.Site.State.Color, Is.EqualTo(green), "O3 rejection must restore the O2-owned green value.");
            Assert.That(driver.PendingProposalCount, Is.EqualTo(1));
            Assert.That(driver.ReceiveRejection(second.OperationId, "second_batch_rejected"), Is.True);

            Assert.That(fixture.Site.State.Color, Is.EqualTo(white), "Rebased O2 rollback must not resurrect rejected O1's red value.");
            Assert.That(driver.PendingProposalCount, Is.Zero);
            Assert.That(driver.LastObservedCanonicalSequence, Is.Zero);
            Assert.That(echoes, Has.Count.EqualTo(3), "All three rollback applications must suppress mutation echoes.");
        }

        [Test]
        public void T11ConfigurationTransaction_QuestProposalAcceptsStagedRoiSelectionAndSuppressesCanonicalEcho()
        {
            using var desktop = new BoundSceneFixture(V2OriginDevice.Desktop);
            using var quest = new BoundSceneFixture(V2OriginDevice.Quest);
            using var authority = new V2DesktopMutationAuthority(SceneIdForT09, IncarnationIdForT09, desktop.Boundary);
            var scheduler = new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest, new TestClock(0));
            using var driver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, quest.Boundary, scheduler);
            int questProposalEvents = 0;
            int confirmations = 0;
            quest.Boundary.MutationProposed += (_, _, _) => questProposalEvents++;
            driver.ProposalConfirmed += _ => confirmations++;
            V2DesktopProposalResult admission = null;
            bool expectStaleRejection = false;
            driver.ProposalQueued += proposal =>
            {
                admission = authority.AcceptQuestProposal(proposal);
                if (expectStaleRejection) return;
                Assert.That(admission.Outcome, Is.EqualTo(V2ProposalOutcome.Accepted));
                Assert.That(driver.ReceiveCanonical(admission.CanonicalMutation), Is.False, "A matching canonical echo must only confirm the optimistic transaction.");
            };

            var roiId = new RoiId("t11-quest-staged-roi");
            var sphereId = new SphereId("t11-quest-staged-sphere");
            var transaction = new SetConfigurationTransaction(new V2Mutation[]
            {
                new CreateRoi(roiId, "Quest staged ROI", new[] { new V2RoiSphereDefinition(sphereId, 1f, 2f, 3f, 4f) }, 1),
                new SetSelectedRoiSphere(roiId.Value, sphereId.Value)
            });

            V2QuestMutationProposal proposal = driver.ApplyOptimistic(transaction, T09Operation(370));

            Assert.That(proposal, Is.Not.Null);
            Assert.That(admission?.CanonicalMutation?.Mutation, Is.TypeOf<SetConfigurationTransaction>());
            ROI questRoi = quest.Scene.ROIManager.ROIs.Single(roi => roi.ID == roiId.Value);
            ROI desktopRoi = desktop.Scene.ROIManager.ROIs.Single(roi => roi.ID == roiId.Value);
            Assert.That(questRoi.SelectedSphereID, Is.EqualTo(0));
            Assert.That(desktopRoi.SelectedSphereID, Is.EqualTo(questRoi.SelectedSphereID));
            Assert.That(questProposalEvents, Is.EqualTo(1), "The staged child applications must publish only the transaction envelope.");
            Assert.That(confirmations, Is.EqualTo(1));
            Assert.That(driver.PendingProposalCount, Is.Zero);
            Assert.That(authority.CanonicalSequence, Is.EqualTo(1UL));
            Assert.That(driver.LastObservedCanonicalSequence, Is.EqualTo(1UL));

            driver.AdvanceCanonicalWatermark(2);
            expectStaleRejection = true;
            var staleRoiId = new RoiId("t11-quest-stale-staged-roi");
            var staleSphereId = new SphereId("t11-quest-stale-staged-sphere");
            var staleTransaction = new SetConfigurationTransaction(new V2Mutation[]
            {
                new CreateRoi(staleRoiId, "Stale staged ROI", new[] { new V2RoiSphereDefinition(staleSphereId, 5f, 6f, 7f, 8f) }, 2),
                new SetSelectedRoiSphere(staleRoiId.Value, staleSphereId.Value)
            });
            V2QuestMutationProposal staleProposal = driver.ApplyOptimistic(staleTransaction, T09Operation(375));
            Assert.That(admission.Outcome, Is.EqualTo(V2ProposalOutcome.Rejected));
            Assert.That(admission.RejectionCode, Is.EqualTo("stale_sequence"));
            Assert.That(admission.Correction, Is.Null, "A transaction with staged identities uses checkpoint rollback instead of an invalid per-child correction.");
            Assert.That(driver.ReceiveRejection(staleProposal.OperationId, admission.RejectionCode), Is.True);
            Assert.That(quest.Scene.ROIManager.ROIs.Any(roi => roi.ID == staleRoiId.Value), Is.False);
            Assert.That(quest.Scene.ROIManager.ROIs.Any(roi => roi.ID == roiId.Value), Is.True);
            Assert.That(questProposalEvents, Is.EqualTo(2), "The rejected transaction and its child rollback must not emit extra proposals.");
        }

        [Test]
        public void T11ConfigurationTransaction_RejectionReplaysInterveningCanonicalsAndKeepsSequenceHistory()
        {
            using var desktop = new BoundSceneFixture(V2OriginDevice.Desktop);
            using var quest = new BoundSceneFixture(V2OriginDevice.Quest);
            using var authority = new V2DesktopMutationAuthority(SceneIdForT09, IncarnationIdForT09, desktop.Boundary);
            var scheduler = new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest, new TestClock(0));
            using var driver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, quest.Boundary, scheduler);
            bool originalBlacklist = quest.Site.State.IsBlackListed;
            float originalInfluence = quest.Column.AnatomyParameters.InfluenceDistance;
            bool originalShowAllSites = quest.Scene.ShowAllSites;
            var echoes = new List<V2Mutation>();
            quest.Boundary.MutationProposed += (_, mutation, _) => echoes.Add(mutation);
            var transaction = new SetConfigurationTransaction(new V2Mutation[]
            {
                new SetSiteBlacklist(new ColumnId(quest.Column.ColumnData.ID), new SiteId(quest.Site.Information.FullID), !originalBlacklist),
                new SetInfluenceDistance(new ColumnId(quest.Column.ColumnData.ID), originalInfluence + 11f)
            });
            V2QuestMutationProposal proposal = driver.ApplyOptimistic(transaction, T09Operation(371));
            var acceptedCanonicals = new List<V2CanonicalMutation>();
            authority.CanonicalReady += acceptedCanonicals.Add;
            V2QuestMutationProposal laterOptimistic = driver.ApplyOptimistic(new SetSceneBoolean(V2SceneBooleanProperty.ShowAllSites, !originalShowAllSites), T09Operation(374));
            Assert.That(laterOptimistic, Is.Not.Null);

            desktop.Boundary.Apply(new SetSiteColor(new ColumnId(desktop.Column.ColumnData.ID), new SiteId(desktop.Site.Information.FullID), 0.15f, 0.25f, 0.35f, 1f), V2MutationApplicationOrigin.LocalDesktop, T09Operation(372));
            Assert.That(acceptedCanonicals, Has.Count.EqualTo(1));
            Assert.That(driver.ReceiveCanonical(acceptedCanonicals[0]), Is.True);
            Assert.That(quest.Site.State.Color, Is.EqualTo(desktop.Site.State.Color));
            Assert.That(driver.LastObservedCanonicalSequence, Is.EqualTo(1UL));

            Assert.That(driver.ReceiveRejection(proposal.OperationId, "configuration_rejected"), Is.True);

            Assert.That(quest.Site.State.IsBlackListed, Is.EqualTo(originalBlacklist));
            Assert.That(quest.Column.AnatomyParameters.InfluenceDistance, Is.EqualTo(originalInfluence));
            Assert.That(quest.Site.State.Color, Is.EqualTo(desktop.Site.State.Color), "Rollback must replay an accepted canonical that arrived after the transaction began.");
            Assert.That(quest.Scene.ShowAllSites, Is.EqualTo(!originalShowAllSites), "Rollback must replay a later optimistic edit as well as intervening canonicals.");
            Assert.That(driver.LastObservedCanonicalSequence, Is.EqualTo(1UL));
            Assert.That(driver.PendingProposalCount, Is.EqualTo(1), "The later optimistic proposal must remain pending after replay.");
            Assert.That(echoes, Has.Count.EqualTo(2), "Checkpoint rollback and replay must not publish duplicate optimistic proposals.");
            Assert.That(driver.ReceiveCanonical(acceptedCanonicals[0]), Is.False, "Canonical operation deduplication must survive rollback.");

            V2DesktopProposalResult acceptedLaterOptimistic = authority.AcceptQuestProposal(laterOptimistic);
            Assert.That(acceptedLaterOptimistic.Outcome, Is.EqualTo(V2ProposalOutcome.Accepted));
            Assert.That(driver.ReceiveCanonical(acceptedLaterOptimistic.CanonicalMutation), Is.False);
            Assert.That(driver.PendingProposalCount, Is.Zero);

            desktop.Boundary.Apply(new SetSiteColor(new ColumnId(desktop.Column.ColumnData.ID), new SiteId(desktop.Site.Information.FullID), 0.65f, 0.55f, 0.45f, 1f), V2MutationApplicationOrigin.LocalDesktop, T09Operation(373));
            Assert.That(acceptedCanonicals, Has.Count.EqualTo(3));
            Assert.That(driver.ReceiveCanonical(acceptedCanonicals[2]), Is.True);
            Assert.That(quest.Site.State.Color, Is.EqualTo(desktop.Site.State.Color));
            Assert.That(driver.LastObservedCanonicalSequence, Is.EqualTo(3UL));
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public void T11ConfigurationTransaction_RejectionReplaysCorrectionAtItsApplicationOrder()
        {
            using var desktop = new BoundSceneFixture(V2OriginDevice.Desktop);
            using var quest = new BoundSceneFixture(V2OriginDevice.Quest);
            using var authority = new V2DesktopMutationAuthority(SceneIdForT09, IncarnationIdForT09, desktop.Boundary);
            var scheduler = new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest, new TestClock(0));
            using var driver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, quest.Boundary, scheduler);
            bool originalShowAllSites = quest.Scene.ShowAllSites;
            var echoes = new List<V2Mutation>();
            quest.Boundary.MutationProposed += (_, mutation, _) => echoes.Add(mutation);
            var transaction = new SetConfigurationTransaction(new V2Mutation[]
            {
                new SetSceneBoolean(V2SceneBooleanProperty.ShowAllSites, !originalShowAllSites)
            });
            V2QuestMutationProposal rejectedTransaction = driver.ApplyOptimistic(transaction, T09Operation(378));
            Assert.That(rejectedTransaction, Is.Not.Null);

            var optimisticColor = driver.ApplyOptimistic(new SetSiteColor(new ColumnId(quest.Column.ColumnData.ID), new SiteId(quest.Site.Information.FullID), 0.9f, 0.1f, 0.2f, 1f), T09Operation(379));
            Assert.That(optimisticColor, Is.Not.Null);
            var canonicalMutations = new List<V2CanonicalMutation>();
            authority.CanonicalReady += canonicalMutations.Add;

            desktop.Boundary.Apply(new SetSiteColor(new ColumnId(desktop.Column.ColumnData.ID), new SiteId(desktop.Site.Information.FullID), 0.1f, 0.2f, 0.8f, 1f), V2MutationApplicationOrigin.LocalDesktop, T09Operation(380));
            Assert.That(driver.ReceiveCanonical(canonicalMutations[0]), Is.True);
            Color blue = quest.Site.State.Color;

            // The newer green canonical is accepted but delayed in transit. Desktop
            // returns it as the current-value correction for the older optimistic O.
            desktop.Boundary.Apply(new SetSiteColor(new ColumnId(desktop.Column.ColumnData.ID), new SiteId(desktop.Site.Information.FullID), 0.1f, 0.8f, 0.2f, 1f), V2MutationApplicationOrigin.LocalDesktop, T09Operation(381));
            Assert.That(canonicalMutations, Has.Count.EqualTo(2));
            V2DesktopProposalResult rejectedColor = authority.AcceptQuestProposal(optimisticColor);
            Assert.That(rejectedColor.Outcome, Is.EqualTo(V2ProposalOutcome.Rejected));
            Assert.That(rejectedColor.Correction, Is.Not.Null);
            Assert.That(rejectedColor.Correction.OperationId, Is.EqualTo(optimisticColor.OperationId));
            Assert.That(rejectedColor.Correction.CanonicalSequence, Is.EqualTo(2UL));
            Assert.That(driver.ReceiveCorrection(rejectedColor.Correction), Is.True);
            Color green = quest.Site.State.Color;
            Assert.That(green, Is.Not.EqualTo(blue));

            Assert.That(driver.ReceiveRejection(rejectedTransaction.OperationId, "configuration_rejected"), Is.True);

            Assert.That(quest.Site.State.Color, Is.EqualTo(green), "A correction must replay after an earlier canonical that it superseded.");
            Assert.That(quest.Scene.ShowAllSites, Is.EqualTo(originalShowAllSites));
            Assert.That(driver.LastObservedCanonicalSequence, Is.EqualTo(2UL));
            Assert.That(echoes, Has.Count.EqualTo(2), "Transaction rollback and authoritative replay must not emit optimistic echoes.");
            Assert.That(driver.ReceiveCanonical(canonicalMutations[1]), Is.False, "The delayed green canonical must be ignored after its newer correction was applied.");
            Assert.That(quest.Site.State.Color, Is.EqualTo(green));
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public void T11ConfigurationTransaction_ReplayFailureAbandonsPendingProposalsAndRequiresFreshSynchronization()
        {
            using var fixture = new BoundSceneFixture(V2OriginDevice.Quest);
            var scheduler = new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest, new TestClock(0));
            using var driver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, fixture.Boundary, scheduler);
            var columnId = new ColumnId(fixture.Column.ColumnData.ID);
            var siteId = new SiteId(fixture.Site.Information.FullID);
            bool originalShowAllSites = fixture.Scene.ShowAllSites;
            float originalInfluenceDistance = fixture.Column.AnatomyParameters.InfluenceDistance;
            float optimisticInfluenceDistance = originalInfluenceDistance + 6f;
            Color red = new(0.87f, 0.12f, 0.18f, 1f);
            var echoes = new List<V2Mutation>();
            fixture.Boundary.MutationProposed += (_, mutation, _) => echoes.Add(mutation);

            V2QuestMutationProposal transaction = driver.ApplyOptimistic(new SetConfigurationTransaction(new V2Mutation[]
            {
                new SetSceneBoolean(V2SceneBooleanProperty.ShowAllSites, !originalShowAllSites)
            }), T09Operation(961));
            V2QuestMutationProposal batch = driver.ApplyOptimistic(new SetSiteConfigurationBatch(new[]
            {
                new V2SiteConfigurationAssignment(columnId, siteId, fixture.Site.State.IsBlackListed, fixture.Site.State.IsHighlighted, red.r, red.g, red.b, red.a, fixture.Site.State.Labels)
            }), T09Operation(962));
            V2QuestMutationProposal influence = driver.ApplyOptimistic(new SetInfluenceDistance(columnId, optimisticInfluenceDistance), T09Operation(963));

            Assert.That(transaction, Is.Not.Null);
            Assert.That(batch, Is.Not.Null, "The D34 batch remains a pending proposal before replay begins.");
            Assert.That(influence, Is.Not.Null);
            Assert.That(driver.PendingProposalCount, Is.EqualTo(3));
            Assert.That(driver.ConnectionState, Is.EqualTo(V2QuestMutationConnectionState.Connected));
            Assert.That(fixture.Site.State.Color, Is.EqualTo(red));
            Assert.That(fixture.Column.AnatomyParameters.InfluenceDistance, Is.EqualTo(optimisticInfluenceDistance));
            Assert.That(fixture.Scene.ShowAllSites, Is.EqualTo(!originalShowAllSites));
            Assert.That(echoes, Has.Count.EqualTo(3));

            int influenceUpdateCount = 0;
            bool failNextInfluenceUpdate = true;
            UnityEngine.Events.UnityAction throwOnce = () =>
            {
                influenceUpdateCount++;
                if (!failNextInfluenceUpdate || influenceUpdateCount < 2) return;
                failNextInfluenceUpdate = false;
                throw new InvalidOperationException("injected one-shot rejection replay failure");
            };
            fixture.Column.AnatomyParameters.OnUpdateInfluenceDistance.AddListener(throwOnce);
            try
            {
                InvalidOperationException failure = Assert.Throws<InvalidOperationException>(() => driver.ReceiveRejection(transaction.OperationId, "configuration_rejected"));
                Assert.That(failure.Message, Does.Contain("fresh scene publish is required"));
            }
            finally
            {
                fixture.Column.AnatomyParameters.OnUpdateInfluenceDistance.RemoveListener(throwOnce);
            }

            Assert.That(influenceUpdateCount, Is.GreaterThanOrEqualTo(2));
            Assert.That(failNextInfluenceUpdate, Is.False, "The injected failure must occur during replay.");
            Assert.That(fixture.Scene.ShowAllSites, Is.EqualTo(!originalShowAllSites), "Recovery restores the captured visible scene until a fresh scene is published.");
            Assert.That(fixture.Site.State.Color, Is.EqualTo(red), "Recovery preserves the remaining D34 assignment visibly.");
            Assert.That(fixture.Column.AnatomyParameters.InfluenceDistance, Is.EqualTo(optimisticInfluenceDistance));
            Assert.That(driver.PendingProposalCount, Is.Zero, "A failed replay abandons T, the D34 batch and later proposals together.");
            Assert.That(driver.AbandonedProposalCount, Is.EqualTo(3));
            Assert.That(driver.ConnectionState, Is.EqualTo(V2QuestMutationConnectionState.OfflineLocal));
            Assert.That(driver.TryGetNextTransmission(out _), Is.False, "Abandoned proposals must no longer be sent.");
            Assert.That(driver.ReceiveRejection(batch.OperationId, "batch_rejected"), Is.False, "The abandoned D34 proposal cannot be rolled back independently after recovery failure.");
            Assert.That(echoes, Has.Count.EqualTo(3), "Checkpoint restoration and proposal invalidation must not echo mutations.");
        }

        private static void WriteLegacyText(BinaryWriter writer, string value)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(value);
            writer.Write(checked((ushort)bytes.Length));
            writer.Write(bytes);
        }

        [Test]
        public void T11PreparedColumnResources_ObserveStableSelectionsAndRestoreCheckpoint()
        {
            using var source = new T11PreparedResourceFixture();
            using var target = new T11PreparedResourceFixture();
            source.Boundary.BindPreparedResources(CreateT11PreparedBinding(source.Scene));
            target.Boundary.BindPreparedResources(CreateT11PreparedBinding(target.Scene));
            var proposed = new List<V2Mutation>();
            source.Boundary.MutationProposed += (_, mutation, _) => proposed.Add(mutation);

            source.StaticColumn.SelectedLabelIndex = 1;
            source.StaticColumn.StaticParameters.InfluenceDistance = 17f;
            source.FmriColumn.SelectedFMRIIndex = 1;
            source.MegColumn.SelectedMEGIndex = 1;

            SetColumnResource[] resourceProposals = proposed.OfType<SetColumnResource>().ToArray();
            Assert.That(resourceProposals.Select(mutation => mutation.Kind), Is.EqualTo(new[]
            {
                V2ColumnResourceKind.StaticLabel,
                V2ColumnResourceKind.FmriResource,
                V2ColumnResourceKind.MegResource
            }));
            Assert.That(resourceProposals.Select(mutation => mutation.ResourceReference).All(reference => reference.Length > 0), Is.True);
            Assert.That(proposed.OfType<SetInfluenceDistance>().Any(mutation => mutation.ColumnId.Value == source.StaticColumn.ColumnData.ID), Is.True);

            V2SceneMutationCheckpoint checkpoint = V2SceneMutationCheckpointCodec.Decode(V2SceneMutationCheckpointCodec.Encode(23, source.Boundary.CaptureCheckpoint())).Checkpoint;
            var echoes = new List<V2Mutation>();
            target.Boundary.MutationProposed += (_, mutation, _) => echoes.Add(mutation);
            target.Boundary.ApplyCheckpoint(checkpoint, T09Operation(358));

            Assert.That(target.StaticColumn.SelectedLabelIndex, Is.EqualTo(1));
            Assert.That(target.StaticColumn.StaticParameters.InfluenceDistance, Is.EqualTo(17f));
            Assert.That(target.FmriColumn.SelectedFMRIIndex, Is.EqualTo(1));
            Assert.That(target.MegColumn.SelectedMEGIndex, Is.EqualTo(1));
            Assert.That(echoes, Is.Empty);
        }

        [Test]
        public void T11CcepSource_CheckpointRestoresTheCompleteSiteSelectionWithoutEcho()
        {
            using var source = new T11CcepFixture();
            using var target = new T11CcepFixture();
            var proposed = new List<V2Mutation>();
            source.Boundary.MutationProposed += (_, mutation, _) => proposed.Add(mutation);

            source.Column.ApplySynchronizedSource(Column3DCCEP.CCEPMode.Site, source.Site, -1);
            source.Column.DynamicParameters.InfluenceDistance = 19f;
            source.Column.DynamicParameters.ApplySynchronizedSpanValues(-2f, 0.25f, 3f);

            SetCcepSource proposal = proposed.OfType<SetCcepSource>().Single();
            Assert.That(proposal.Mode, Is.EqualTo(V2CcepSourceMode.Site));
            Assert.That(proposal.SourceSiteId.Value, Is.EqualTo(source.Site.Information.FullID));
            Assert.That(proposal.MarsAtlasLabel, Is.EqualTo(-1));

            V2SceneMutationCheckpoint checkpoint = V2SceneMutationCheckpointCodec.Decode(V2SceneMutationCheckpointCodec.Encode(24, source.Boundary.CaptureCheckpoint())).Checkpoint;
            Assert.That(checkpoint.T11Records.Any(record => record.Value is SetCcepSource), Is.True);
            var echoes = new List<V2Mutation>();
            target.Boundary.MutationProposed += (_, mutation, _) => echoes.Add(mutation);
            target.Boundary.ApplyCheckpoint(checkpoint, T09Operation(359));

            Assert.That(target.Column.Mode, Is.EqualTo(Column3DCCEP.CCEPMode.Site));
            Assert.That(target.Column.SelectedSourceSite, Is.SameAs(target.Site));
            Assert.That(target.Column.DynamicParameters.InfluenceDistance, Is.EqualTo(19f));
            Assert.That(target.Column.DynamicParameters.SpanMin, Is.EqualTo(-2f));
            Assert.That(target.Column.DynamicParameters.Middle, Is.EqualTo(0.25f));
            Assert.That(target.Column.DynamicParameters.SpanMax, Is.EqualTo(3f));
            Assert.That(echoes, Is.Empty);
        }

        [Test]
        public void SelectionAndSitePresentation_CheckpointAppliesWithoutEchoOrActivityInvalidation()
        {
            using var source = new BoundSceneFixture();
            using var target = new BoundSceneFixture();
            var proposals = new List<V2Mutation>();
            source.Boundary.MutationProposed += (_, mutation, _) => proposals.Add(mutation);

            source.Column.IsSelected = true;
            source.Scene.SelectSiteForSynchronization(source.Column, source.Site);
            source.Site.State.IsHighlighted = true;
            source.Site.State.AddLabel("reviewed");
            source.Scene.ShowAllSites = true;
            source.Column.ActivityAlpha = 0.35f;

            Assert.That(proposals.Select(mutation => mutation.Type), Is.EquivalentTo(new[]
            {
                V2OperationType.SetSelectedColumn,
                V2OperationType.SetSelectedSite,
                V2OperationType.SetSiteHighlight,
                V2OperationType.SetSiteLabels,
                V2OperationType.SetSceneBoolean,
                V2OperationType.SetActivityAlpha
            }));

            byte[] bytes = V2SceneMutationCheckpointCodec.Encode(12, source.Boundary.CaptureCheckpoint());
            V2SceneMutationCheckpoint checkpoint = V2SceneMutationCheckpointCodec.Decode(bytes).Checkpoint;
            var appliedProposals = new List<V2Mutation>();
            target.Boundary.MutationProposed += (_, mutation, _) => appliedProposals.Add(mutation);
            target.Scene.SceneInformation.ProjectionGridNeedsUpdate = false;
            target.Scene.SceneInformation.SurfaceProjectionNeedsUpdate = false;
            target.Scene.SceneInformation.GeneratorNeedsUpdate = false;
            ulong projectionGeneration = target.Scene.ProjectionGeneration;
            ulong activityInputGeneration = target.Scene.ActivityInputGeneration;

            target.Boundary.ApplyCheckpoint(checkpoint, new OperationId(Guid.NewGuid()));

            Assert.That(target.Column.IsSelected, Is.True);
            Assert.That(target.Column.SelectedSite, Is.SameAs(target.Site));
            Assert.That(target.Site.State.IsHighlighted, Is.True);
            Assert.That(target.Site.State.Labels, Is.EqualTo(new[] { "reviewed" }));
            Assert.That(target.Column.ActivityAlpha, Is.EqualTo(0.35f));
            Assert.That(appliedProposals, Is.Empty);
            Assert.That(target.Scene.ProjectionGeneration, Is.EqualTo(projectionGeneration));
            Assert.That(target.Scene.ActivityInputGeneration, Is.EqualTo(activityInputGeneration + 1));
            Assert.That(target.Scene.SceneInformation.GeneratorNeedsUpdate, Is.True, "ShowAllSites changes the effective ROI mask and invalidates activity inputs.");
            Assert.That(target.Scene.SceneInformation.ProjectionGridNeedsUpdate, Is.False);
            Assert.That(target.Scene.SceneInformation.SurfaceProjectionNeedsUpdate, Is.False);
        }

        [Test]
        public void PreparedSpansAndFunctionalThresholds_CheckpointApplyAtomicallyWithoutEcho()
        {
            using var source = new BoundSceneFixture();
            using var target = new BoundSceneFixture();
            var proposals = new List<V2Mutation>();
            source.Boundary.MutationProposed += (_, mutation, _) => proposals.Add(mutation);

            source.StaticColumn.StaticParameters.ApplySynchronizedSpanValues(-1f, 0.25f, 1f);
            source.DynamicColumn.DynamicParameters.ApplySynchronizedSpanValues(-2f, 0f, 3f);
            source.FmriColumn.FMRIParameters.ApplySynchronizedCalibration(0.1f, 0.4f, 0.2f, 0.8f);
            source.FmriColumn.FMRIParameters.SetHideValues(true, false, true);
            source.MegColumn.MEGParameters.ApplySynchronizedCalibration(0.05f, 0.45f, 0.3f, 0.95f);
            source.MegColumn.MEGParameters.SetHideValues(false, true, false);

            Assert.That(proposals.Count(mutation => mutation.Type == V2OperationType.SetColumnSpan), Is.EqualTo(2));
            Assert.That(proposals.Count(mutation => mutation.Type == V2OperationType.SetFunctionalDisplay), Is.EqualTo(4));

            V2SceneMutationCheckpoint checkpoint = V2SceneMutationCheckpointCodec.Decode(V2SceneMutationCheckpointCodec.Encode(13, source.Boundary.CaptureCheckpoint())).Checkpoint;
            var targetProposals = new List<V2Mutation>();
            target.Boundary.MutationProposed += (_, mutation, _) => targetProposals.Add(mutation);
            ulong projectionGeneration = target.Scene.ProjectionGeneration;
            ulong activityInputGeneration = target.Scene.ActivityInputGeneration;

            target.Boundary.ApplyCheckpoint(checkpoint, new OperationId(Guid.NewGuid()));

            Assert.That(target.StaticColumn.StaticParameters.SpanMin, Is.EqualTo(-1f));
            Assert.That(target.StaticColumn.StaticParameters.Middle, Is.EqualTo(0.25f));
            Assert.That(target.StaticColumn.StaticParameters.SpanMax, Is.EqualTo(1f));
            Assert.That(target.DynamicColumn.DynamicParameters.SpanMin, Is.EqualTo(-2f));
            Assert.That(target.DynamicColumn.DynamicParameters.Middle, Is.EqualTo(0f));
            Assert.That(target.DynamicColumn.DynamicParameters.SpanMax, Is.EqualTo(3f));
            Assert.That(target.FmriColumn.FMRIParameters.FMRINegativeCalMinFactor, Is.EqualTo(0.1f));
            Assert.That(target.FmriColumn.FMRIParameters.HideLowerValues, Is.True);
            Assert.That(target.FmriColumn.FMRIParameters.HideHigherValues, Is.True);
            Assert.That(target.MegColumn.MEGParameters.FMRIPositiveCalMaxFactor, Is.EqualTo(0.95f));
            Assert.That(target.MegColumn.MEGParameters.HideMiddleValues, Is.True);
            Assert.That(targetProposals, Is.Empty);
            Assert.That(target.Scene.ProjectionGeneration, Is.EqualTo(projectionGeneration));
            Assert.That(target.Scene.ActivityInputGeneration, Is.EqualTo(activityInputGeneration));
        }

        [Test]
        public void T09DesktopCanonicalMutations_ConvergeOnQuestDriverWithoutEchoOrActivityInvalidation()
        {
            using var desktop = new BoundSceneFixture(V2OriginDevice.Desktop);
            using var quest = new BoundSceneFixture(V2OriginDevice.Quest);
            using var authority = new V2DesktopMutationAuthority(SceneIdForT09, IncarnationIdForT09, desktop.Boundary);
            var clock = new TestClock(0);
            var scheduler = new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest, clock);
            using var questDriver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, quest.Boundary, scheduler);
            var canonicals = new List<V2CanonicalMutation>();
            var questEchoes = new List<V2Mutation>();
            authority.CanonicalReady += canonical =>
            {
                canonicals.Add(canonical);
                Assert.That(questDriver.ReceiveCanonical(canonical), Is.True);
            };
            quest.Boundary.MutationProposed += (_, mutation, _) => questEchoes.Add(mutation);
            V2Mutation[] mutations = CreateT09DriverMutations(desktop, 19);
            ulong projectionGeneration = quest.Scene.ProjectionGeneration;
            ulong activityInputGeneration = quest.Scene.ActivityInputGeneration;
            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < mutations.Length; i++)
            {
                try
                {
                    desktop.Boundary.Apply(mutations[i], V2MutationApplicationOrigin.LocalDesktop, T09Operation(i + 1));
                }
                catch (Exception exception)
                {
                    throw new Exception($"Desktop T09 operation {i} ({mutations[i].Type}) failed: {exception}");
                }
            }

            stopwatch.Stop();
            Assert.That(canonicals, Has.Count.EqualTo(mutations.Length));
            Assert.That(authority.CanonicalSequence, Is.EqualTo((ulong)mutations.Length));
            Assert.That(questDriver.LastObservedCanonicalSequence, Is.EqualTo((ulong)mutations.Length));
            Assert.That(questEchoes, Is.Empty);
            AssertT09DriverValuesMatch(desktop, quest, 19);
            Assert.That(quest.Scene.ProjectionGeneration, Is.EqualTo(projectionGeneration));
            Assert.That(quest.Scene.ActivityInputGeneration, Is.EqualTo(activityInputGeneration));
            TestContext.WriteLine($"HBP_SYNC_T09_DESKTOP_TO_QUEST operations={mutations.Length} canonical={canonicals.Count} echoes={questEchoes.Count} elapsedMs={stopwatch.Elapsed.TotalMilliseconds:F3}");
        }

        [Test]
        public void T09QuestProposals_ConvergeOnDesktopAuthorityWithoutEchoOrActivityInvalidation()
        {
            using var desktop = new BoundSceneFixture(V2OriginDevice.Desktop);
            using var quest = new BoundSceneFixture(V2OriginDevice.Quest);
            using var authority = new V2DesktopMutationAuthority(SceneIdForT09, IncarnationIdForT09, desktop.Boundary);
            var clock = new TestClock(0);
            var scheduler = new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest, clock);
            using var questDriver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, quest.Boundary, scheduler);
            var canonicals = new List<V2CanonicalMutation>();
            var desktopEchoes = new List<V2Mutation>();
            authority.CanonicalReady += canonicals.Add;
            desktop.Boundary.MutationProposed += (_, mutation, _) => desktopEchoes.Add(mutation);
            questDriver.ProposalQueued += proposal =>
            {
                V2DesktopProposalResult accepted = authority.AcceptQuestProposal(proposal);
                Assert.That(accepted.Outcome, Is.EqualTo(V2ProposalOutcome.Accepted));
                Assert.That(questDriver.ReceiveCanonical(accepted.CanonicalMutation), Is.False, "A matching T09 proposal is confirmed without applying it a second time.");
            };
            V2Mutation[] mutations = CreateT09DriverMutations(quest, 23);
            ulong desktopProjectionGeneration = desktop.Scene.ProjectionGeneration;
            ulong desktopActivityInputGeneration = desktop.Scene.ActivityInputGeneration;
            var stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < mutations.Length; i++)
            {
                try
                {
                    Assert.That(questDriver.ApplyOptimistic(mutations[i], T09Operation(100 + i)), Is.Not.Null);
                }
                catch (Exception exception)
                {
                    throw new Exception($"Quest T09 operation {i} ({mutations[i].Type}) failed: {exception}");
                }
            }

            stopwatch.Stop();
            Assert.That(canonicals, Has.Count.EqualTo(mutations.Length));
            Assert.That(authority.CanonicalSequence, Is.EqualTo((ulong)mutations.Length));
            Assert.That(questDriver.LastObservedCanonicalSequence, Is.EqualTo((ulong)mutations.Length));
            Assert.That(questDriver.PendingProposalCount, Is.Zero);
            Assert.That(desktopEchoes, Is.Empty);
            AssertT09DriverValuesMatch(quest, desktop, 23);
            Assert.That(desktop.Scene.ProjectionGeneration, Is.EqualTo(desktopProjectionGeneration));
            Assert.That(desktop.Scene.ActivityInputGeneration, Is.EqualTo(desktopActivityInputGeneration));
            TestContext.WriteLine($"HBP_SYNC_T09_QUEST_TO_DESKTOP operations={mutations.Length} canonical={canonicals.Count} echoes={desktopEchoes.Count} elapsedMs={stopwatch.Elapsed.TotalMilliseconds:F3}");
        }

        [Test]
        public void T09PreparedResourceOperations_RejectCleanlyWithoutASelectedPreparedMesh()
        {
            using var fixture = new BoundSceneFixture();

            Assert.Throws<InvalidOperationException>(() => fixture.Boundary.Apply(new SetSceneBoolean(V2SceneBooleanProperty.DisplayMarsAtlas, true), V2MutationApplicationOrigin.Remote, T09Operation(301)));
            Assert.Throws<InvalidOperationException>(() => fixture.Boundary.Apply(new SetSceneBoolean(V2SceneBooleanProperty.DisplayJuBrainAtlas, true), V2MutationApplicationOrigin.Remote, T09Operation(302)));
            Assert.Throws<InvalidOperationException>(() => fixture.Boundary.Apply(new SetIbcDifumoDisplay(true, "0", false, string.Empty, 0), V2MutationApplicationOrigin.Remote, T09Operation(303)));
            Assert.Throws<InvalidOperationException>(() => fixture.Boundary.Apply(new SetIbcDifumoDisplay(false, "invalid-contrast", false, string.Empty, 0), V2MutationApplicationOrigin.Remote, T09Operation(305)));
            Assert.Throws<InvalidOperationException>(() => fixture.Boundary.Apply(new SetLocalizerDisplay(true, "prepared-protocol", "prepared-data", "prepared-bloc", 0, 0f, 0.5f, 1f), V2MutationApplicationOrigin.Remote, T09Operation(304)));
        }

        [Test]
        public void TimelineAutomaticPlayback_DoesNotCreateMutationProposals()
        {
            using var fixture = new Fixture("playback", V2OriginDevice.Desktop, 1000);
            fixture.Timeline.IsLooping = true;
            fixture.Timeline.Step = 100;
            fixture.Timeline.IsPlaying = true;
            var proposals = new List<V2Mutation>();
            int nestedCutUpdates = 0;
            fixture.Boundary.MutationProposed += (_, mutation, _) => proposals.Add(mutation);
            fixture.Timeline.OnUpdateCurrentIndex.AddListener(() =>
            {
                nestedCutUpdates++;
                fixture.Cut.Position = fixture.Cut.Position == 0.5f ? 0.75f : 0.5f;
            });
            typeof(BasicTimeline).GetField("m_TimeSinceLastUpdate", BindingFlags.Instance | BindingFlags.NonPublic).SetValue(fixture.Timeline, 0.2f);

            fixture.Timeline.Play();

            Assert.That(fixture.Timeline.CurrentIndex, Is.GreaterThan(0));
            Assert.That(nestedCutUpdates, Is.GreaterThan(0));
            Assert.That(proposals, Is.Empty);
        }

        [Test]
        public void TypedCheckpoint_ExportsAndAppliesThroughTheSameRemoteHandlers()
        {
            using var source = new Fixture("source", V2OriginDevice.Desktop, 1000);
            using var target = new Fixture("target", V2OriginDevice.Quest, 1000);
            source.State.Color = new Color(0.1f, 0.2f, 0.3f, 0.4f);
            source.Cut.Orientation = HBP.Core.Enums.CutOrientation.Custom;
            source.Cut.Flip = true;
            source.Cut.NumberOfCuts = 128;
            source.Cut.Position = 0.25f;
            source.Cut.Normal = new Vector3(0.25f, 0.5f, 0.75f);
            source.Timeline.CurrentIndex = 12;
            source.Timeline.Step = 4;
            source.Timeline.IsLooping = true;

            V2SceneMutationCheckpoint checkpoint = source.Boundary.CaptureCheckpoint();
            Assert.That(checkpoint.SiteColors, Is.Empty);
            Assert.That(checkpoint.T11Records.Select(record => record.Value).OfType<SetSiteConfigurationBatch>().Single().Assignments, Has.Count.EqualTo(1));
            Assert.That(checkpoint.CutDefinitions, Has.Count.EqualTo(1));
            Assert.That(checkpoint.TimelineAnchors, Has.Count.EqualTo(1));
            var targetProposals = new List<V2Mutation>();
            target.Boundary.MutationProposed += (_, mutation, _) => targetProposals.Add(mutation);

            target.Boundary.ApplyCheckpoint(checkpoint, new OperationId(Guid.NewGuid()));

            Assert.That(target.State.Color, Is.EqualTo(source.State.Color));
            Assert.That(target.Cut.Orientation, Is.EqualTo(source.Cut.Orientation));
            Assert.That(target.Cut.Flip, Is.EqualTo(source.Cut.Flip));
            Assert.That(target.Cut.NumberOfCuts, Is.EqualTo(source.Cut.NumberOfCuts));
            Assert.That(target.Cut.Position, Is.EqualTo(source.Cut.Position));
            Assert.That(target.Cut.Normal, Is.EqualTo(source.Cut.Normal));
            Assert.That(target.Timeline.CurrentIndex, Is.EqualTo(source.Timeline.CurrentIndex));
            Assert.That(target.Timeline.Step, Is.EqualTo(source.Timeline.Step));
            Assert.That(target.Timeline.IsLooping, Is.EqualTo(source.Timeline.IsLooping));
            Assert.That(targetProposals, Is.Empty);
        }

        [Test]
        public void TypedCheckpoint_PrevalidatesAllTargetsBeforeApplyingAnyRecord()
        {
            using var source = new Fixture("source", V2OriginDevice.Desktop, 1000);
            using var target = new Fixture("target", V2OriginDevice.Quest, 1000, siteId: "different-site");
            source.State.Color = Color.green;
            source.Cut.Position = 0.25f;
            V2SceneMutationCheckpoint checkpoint = source.Boundary.CaptureCheckpoint();

            Assert.Throws<KeyNotFoundException>(() => target.Boundary.ApplyCheckpoint(checkpoint, new OperationId(Guid.NewGuid())));
            Assert.That(target.Cut.Position, Is.EqualTo(0.5f));
            Assert.That(target.State.Color, Is.EqualTo(SiteState.DefaultColor));
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public void T10Checkpoint_ReconcilesCutAndRoiRostersBeforeDefinitionsAndSelection()
        {
            using var source = new BoundSceneFixture(V2OriginDevice.Desktop, cutIds: new[] { "checkpoint-created-cut" }, roiId: "checkpoint-created-roi", sphereId: "checkpoint-created-sphere", configureCutCreation: true);
            using var target = new BoundSceneFixture(V2OriginDevice.Quest, cutIds: new[] { "checkpoint-obsolete-cut" }, roiId: "checkpoint-obsolete-roi", sphereId: "checkpoint-obsolete-sphere", configureCutCreation: true);

            SceneCut sourceCut = source.Scene.Cuts.Single();
            sourceCut.Orientation = HBP.Core.Enums.CutOrientation.Custom;
            sourceCut.Flip = true;
            sourceCut.NumberOfCuts = 7;
            sourceCut.Position = 0.73f;
            sourceCut.Normal = new Vector3(0.25f, 0.5f, 0.75f);
            source.Roi.Name = "checkpoint ROI";
            source.Sphere.Position = new Vector3(1.25f, -2.5f, 3.75f);
            source.Sphere.SetInfluenceRadius(4.5f);
            source.Scene.ROIManager.SelectedROI = source.Roi;
            source.Roi.SelectSphere(0);

            V2SceneMutationCheckpoint checkpoint = V2SceneMutationCheckpointCodec.Decode(V2SceneMutationCheckpointCodec.Encode(21, source.Boundary.CaptureCheckpoint())).Checkpoint;
            var proposals = new List<V2Mutation>();
            target.Boundary.MutationProposed += (_, mutation, _) => proposals.Add(mutation);

            target.Boundary.ApplyCheckpoint(checkpoint, new OperationId(Guid.NewGuid()));

            Assert.That(target.Scene.Cuts.Select(cut => cut.ID), Is.EqualTo(new[] { "checkpoint-created-cut" }));
            SceneCut restoredCut = target.Scene.Cuts.Single();
            Assert.That(restoredCut.Orientation, Is.EqualTo(HBP.Core.Enums.CutOrientation.Custom));
            Assert.That(restoredCut.Flip, Is.True);
            Assert.That(restoredCut.NumberOfCuts, Is.EqualTo(7));
            Assert.That(restoredCut.Position, Is.EqualTo(0.73f));
            Assert.That(restoredCut.Normal, Is.EqualTo(new Vector3(0.25f, 0.5f, 0.75f)));

            Assert.That(target.Scene.ROIManager.ROIs, Has.Count.EqualTo(1));
            ROI restoredRoi = target.Scene.ROIManager.ROIs.Single();
            Assert.That(restoredRoi.ID, Is.EqualTo("checkpoint-created-roi"));
            Assert.That(restoredRoi.Name, Is.EqualTo("checkpoint ROI"));
            Assert.That(restoredRoi.Spheres, Has.Count.EqualTo(1));
            Assert.That(restoredRoi.Spheres[0].ID, Is.EqualTo("checkpoint-created-sphere"));
            Assert.That(restoredRoi.Spheres[0].Position, Is.EqualTo(new Vector3(1.25f, -2.5f, 3.75f)));
            Assert.That(restoredRoi.Spheres[0].InfluenceRadius, Is.EqualTo(4.5f));
            Assert.That(target.Scene.ROIManager.SelectedROI, Is.SameAs(restoredRoi));
            Assert.That(restoredRoi.SelectedSphere, Is.SameAs(restoredRoi.Spheres[0]));
            Assert.That(proposals, Is.Empty, "Applying an authoritative checkpoint must not publish local structural echoes.");
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public void RejectedNonActiveRoiDeletion_RestoresFullRosterSelectionAndQuestDriverAcceptsNextOperation()
        {
            using var quest = new BoundSceneFixture(V2OriginDevice.Quest);
            SeedDeletionRois(quest, "t09-roi-id", "base-sphere-last", "inactive-sphere-middle");
            using var desktop = new BoundSceneFixture(V2OriginDevice.Desktop);
            SeedDeletionRois(desktop, "t09-roi-id", "base-sphere-last", "inactive-sphere-middle");
            using var authority = new V2DesktopMutationAuthority(SceneIdForT09, IncarnationIdForT09, desktop.Boundary);
            var scheduler = new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest, new TestClock(0));
            using var driver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, quest.Boundary, scheduler);

            V2QuestMutationProposal deletion = driver.ApplyOptimistic(new DeleteRoi(new RoiId("inactive-roi")), T09Operation(921));
            Assert.That(deletion, Is.Not.Null);
            Assert.That(quest.Scene.ROIManager.ROIs.Select(roi => roi.ID), Is.EqualTo(new[] { "t09-roi-id" }));
            Assert.That(driver.ReceiveRejection(deletion.OperationId, "roi_delete_rejected"), Is.True);

            AssertRoiState(quest, "t09-roi-id", ExpectedRoi("t09-roi-id", "Base ROI", new[] { BaseSphereFirst, BaseSphereLast }, "base-sphere-last"), ExpectedRoi("inactive-roi", "Inactive ROI", new[] { InactiveSphereFirst, InactiveSphereMiddle, InactiveSphereLast }, "inactive-sphere-middle"));
            Assert.That(driver.ConnectionState, Is.EqualTo(V2QuestMutationConnectionState.Connected));

            var nextDefinition = TestSphere("inactive-sphere-middle", -1f, 5f);
            V2QuestMutationProposal next = driver.ApplyOptimistic(new SetRoiSphereDefinition(new RoiId("inactive-roi"), nextDefinition), T09Operation(922));
            Assert.That(next, Is.Not.Null);
            V2DesktopProposalResult accepted = authority.AcceptQuestProposal(next);
            Assert.That(accepted.Outcome, Is.EqualTo(V2ProposalOutcome.Accepted));
            Assert.That(driver.ReceiveCanonical(accepted.CanonicalMutation), Is.False);
            Assert.That(FindRoi(desktop, "inactive-roi").Spheres[1].Position, Is.EqualTo(new Vector3(-1f, -0.75f, -0.5f)));
            Assert.That(FindRoi(quest, "inactive-roi").Spheres[1].Position, Is.EqualTo(new Vector3(-1f, -0.75f, -0.5f)));
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public void RejectedNonSelectedNonLastSphereDeletion_RestoresSelectionAndQuestDriverAcceptsNextOperation()
        {
            using var quest = new BoundSceneFixture(V2OriginDevice.Quest);
            SeedDeletionRois(quest, "t09-roi-id", "base-sphere-last", "inactive-sphere-middle");
            using var desktop = new BoundSceneFixture(V2OriginDevice.Desktop);
            SeedDeletionRois(desktop, "t09-roi-id", "base-sphere-last", "inactive-sphere-middle");
            using var authority = new V2DesktopMutationAuthority(SceneIdForT09, IncarnationIdForT09, desktop.Boundary);
            var scheduler = new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest, new TestClock(0));
            using var driver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, quest.Boundary, scheduler);

            V2QuestMutationProposal deletion = driver.ApplyOptimistic(new DeleteRoiSphere(new RoiId("t09-roi-id"), new SphereId("t09-sphere-id")), T09Operation(923));
            Assert.That(deletion, Is.Not.Null);
            AssertRoiState(quest, "t09-roi-id", ExpectedRoi("t09-roi-id", "Base ROI", new[] { BaseSphereLast }, "base-sphere-last"), ExpectedRoi("inactive-roi", "Inactive ROI", new[] { InactiveSphereFirst, InactiveSphereMiddle, InactiveSphereLast }, "inactive-sphere-middle"));
            Assert.That(driver.ReceiveRejection(deletion.OperationId, "sphere_delete_rejected"), Is.True);

            AssertRoiState(quest, "t09-roi-id", ExpectedRoi("t09-roi-id", "Base ROI", new[] { BaseSphereFirst, BaseSphereLast }, "base-sphere-last"), ExpectedRoi("inactive-roi", "Inactive ROI", new[] { InactiveSphereFirst, InactiveSphereMiddle, InactiveSphereLast }, "inactive-sphere-middle"));
            Assert.That(driver.ConnectionState, Is.EqualTo(V2QuestMutationConnectionState.Connected));

            V2RoiSphereDefinition nextDefinition = TestSphere("t09-sphere-id", 8f, 6f);
            V2QuestMutationProposal next = driver.ApplyOptimistic(new SetRoiSphereDefinition(new RoiId("t09-roi-id"), nextDefinition), T09Operation(924));
            V2DesktopProposalResult accepted = authority.AcceptQuestProposal(next);
            Assert.That(accepted.Outcome, Is.EqualTo(V2ProposalOutcome.Accepted));
            Assert.That(driver.ReceiveCanonical(accepted.CanonicalMutation), Is.False);
            Assert.That(FindRoi(desktop, "t09-roi-id").Spheres[0].Position, Is.EqualTo(new Vector3(8f, 8.25f, 8.5f)));
            Assert.That(FindRoi(quest, "t09-roi-id").Spheres[0].Position, Is.EqualTo(new Vector3(8f, 8.25f, 8.5f)));
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public void StaleNonActiveRoiDeletionCorrection_RestoresAuthoritativeRosterAndSelections()
        {
            using var quest = new BoundSceneFixture(V2OriginDevice.Quest);
            SeedDeletionRois(quest, "t09-roi-id", "base-sphere-last", "inactive-sphere-middle");
            using var desktop = new BoundSceneFixture(V2OriginDevice.Desktop);
            SeedDeletionRois(desktop, "inactive-roi", "t09-sphere-id", "inactive-sphere-last");
            using var authority = new V2DesktopMutationAuthority(SceneIdForT09, IncarnationIdForT09, desktop.Boundary);
            var scheduler = new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest, new TestClock(0));
            using var driver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, quest.Boundary, scheduler);

            V2QuestMutationProposal deletion = driver.ApplyOptimistic(new DeleteRoi(new RoiId("inactive-roi")), T09Operation(925));
            desktop.Boundary.Apply(new RenameRoi(new RoiId("inactive-roi"), "Desktop ROI"), V2MutationApplicationOrigin.LocalDesktop, T09Operation(926));
            V2DesktopProposalResult rejected = authority.AcceptQuestProposal(deletion);
            Assert.That(rejected.Outcome, Is.EqualTo(V2ProposalOutcome.Rejected));
            Assert.That(rejected.Correction, Is.Not.Null);

            V2QuestProposalDecision decoded = V2QuestProposalDecisionCodec.Decode(V2QuestProposalDecisionCodec.EncodeCorrection(rejected.Correction), SceneIdForT09, IncarnationIdForT09);
            Assert.That(driver.ReceiveCorrection(decoded.Correction), Is.True);
            AssertRoiState(quest, "inactive-roi", ExpectedRoi("t09-roi-id", "Base ROI", new[] { BaseSphereFirst, BaseSphereLast }, "base-sphere-last"), ExpectedRoi("inactive-roi", "Desktop ROI", new[] { InactiveSphereFirst, InactiveSphereMiddle, InactiveSphereLast }, "inactive-sphere-last"));
            Assert.That(driver.ConnectionState, Is.EqualTo(V2QuestMutationConnectionState.Connected));

            V2RoiSphereDefinition nextDefinition = TestSphere("base-sphere-last", -7f, 7f);
            V2QuestMutationProposal next = driver.ApplyOptimistic(new SetRoiSphereDefinition(new RoiId("t09-roi-id"), nextDefinition), T09Operation(927));
            V2DesktopProposalResult accepted = authority.AcceptQuestProposal(next);
            Assert.That(accepted.Outcome, Is.EqualTo(V2ProposalOutcome.Accepted));
            Assert.That(driver.ReceiveCanonical(accepted.CanonicalMutation), Is.False);
            Assert.That(FindRoi(quest, "t09-roi-id").Spheres[1].Position, Is.EqualTo(new Vector3(-7f, -6.75f, -6.5f)));
        }

        [Test]
        [Category("Sync.SceneFocused")]
        public void StaleNonSelectedSphereDeletionCorrection_RestoresAuthoritativeGeometryOrderAndSelections()
        {
            using var quest = new BoundSceneFixture(V2OriginDevice.Quest);
            SeedDeletionRois(quest, "t09-roi-id", "base-sphere-last", "inactive-sphere-middle");
            using var desktop = new BoundSceneFixture(V2OriginDevice.Desktop);
            SeedDeletionRois(desktop, "inactive-roi", "t09-sphere-id", "inactive-sphere-middle");
            using var authority = new V2DesktopMutationAuthority(SceneIdForT09, IncarnationIdForT09, desktop.Boundary);
            var scheduler = new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest, new TestClock(0));
            using var driver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, quest.Boundary, scheduler);

            V2QuestMutationProposal deletion = driver.ApplyOptimistic(new DeleteRoiSphere(new RoiId("t09-roi-id"), new SphereId("t09-sphere-id")), T09Operation(928));
            V2RoiSphereDefinition desktopDefinition = TestSphere("t09-sphere-id", 12f, 8f);
            desktop.Boundary.Apply(new SetRoiSphereDefinition(new RoiId("t09-roi-id"), desktopDefinition), V2MutationApplicationOrigin.LocalDesktop, T09Operation(929));
            V2DesktopProposalResult rejected = authority.AcceptQuestProposal(deletion);
            Assert.That(rejected.Outcome, Is.EqualTo(V2ProposalOutcome.Rejected));
            Assert.That(rejected.Correction, Is.Not.Null);

            V2QuestProposalDecision decoded = V2QuestProposalDecisionCodec.Decode(V2QuestProposalDecisionCodec.EncodeCorrection(rejected.Correction), SceneIdForT09, IncarnationIdForT09);
            Assert.That(driver.ReceiveCorrection(decoded.Correction), Is.True);
            AssertRoiState(quest, "inactive-roi", ExpectedRoi("t09-roi-id", "Base ROI", new[] { desktopDefinition, BaseSphereLast }, "t09-sphere-id"), ExpectedRoi("inactive-roi", "Inactive ROI", new[] { InactiveSphereFirst, InactiveSphereMiddle, InactiveSphereLast }, "inactive-sphere-middle"));
            Assert.That(driver.ConnectionState, Is.EqualTo(V2QuestMutationConnectionState.Connected));

            V2RoiSphereDefinition nextDefinition = TestSphere("t09-sphere-id", -12f, 9f);
            V2QuestMutationProposal next = driver.ApplyOptimistic(new SetRoiSphereDefinition(new RoiId("t09-roi-id"), nextDefinition), T09Operation(930));
            V2DesktopProposalResult accepted = authority.AcceptQuestProposal(next);
            Assert.That(accepted.Outcome, Is.EqualTo(V2ProposalOutcome.Accepted));
            Assert.That(driver.ReceiveCanonical(accepted.CanonicalMutation), Is.False);
            Assert.That(FindRoi(quest, "t09-roi-id").Spheres[0].Position, Is.EqualTo(new Vector3(-12f, -11.75f, -11.5f)));
        }

        private static readonly V2RoiSphereDefinition BaseSphereFirst = TestSphere("t09-sphere-id", 1f, 2f);
        private static readonly V2RoiSphereDefinition BaseSphereLast = TestSphere("base-sphere-last", 3f, 3f);
        private static readonly V2RoiSphereDefinition InactiveSphereFirst = TestSphere("inactive-sphere-first", 5f, 4f);
        private static readonly V2RoiSphereDefinition InactiveSphereMiddle = TestSphere("inactive-sphere-middle", 7f, 5f);
        private static readonly V2RoiSphereDefinition InactiveSphereLast = TestSphere("inactive-sphere-last", 9f, 6f);

        private static V2RoiSphereDefinition TestSphere(string id, float x, float radius) => new(new SphereId(id), x, x + 0.25f, x + 0.5f, radius);

        private static void SeedDeletionRois(BoundSceneFixture fixture, string activeRoiId, string baseSelectedSphereId, string inactiveSelectedSphereId)
        {
            var baseRoiId = new RoiId("t09-roi-id");
            fixture.Boundary.Apply(new RenameRoi(baseRoiId, "Base ROI"), V2MutationApplicationOrigin.Remote, T09Operation(931));
            fixture.Boundary.Apply(new SetRoiSphereDefinition(baseRoiId, BaseSphereFirst), V2MutationApplicationOrigin.Remote, T09Operation(932));
            fixture.Boundary.Apply(new CreateRoiSphere(baseRoiId, BaseSphereLast, 1), V2MutationApplicationOrigin.Remote, T09Operation(933));
            fixture.Boundary.Apply(new CreateRoi(new RoiId("inactive-roi"), "Inactive ROI", new[] { InactiveSphereFirst, InactiveSphereMiddle, InactiveSphereLast }, 1), V2MutationApplicationOrigin.Remote, T09Operation(934));
            fixture.Boundary.Apply(new SetSelectedRoiSphere(baseRoiId.Value, baseSelectedSphereId), V2MutationApplicationOrigin.Remote, T09Operation(935));
            fixture.Boundary.Apply(new SetSelectedRoiSphere("inactive-roi", inactiveSelectedSphereId), V2MutationApplicationOrigin.Remote, T09Operation(936));
            fixture.Boundary.Apply(new SetActiveRoi(new RoiId(activeRoiId)), V2MutationApplicationOrigin.Remote, T09Operation(937));
        }

        private static ROI FindRoi(BoundSceneFixture fixture, string roiId) => fixture.Scene.ROIManager.ROIs.Single(roi => roi.ID == roiId);

        private static RoiStateExpectation ExpectedRoi(string roiId, string name, V2RoiSphereDefinition[] spheres, string selectedSphereId) => new(roiId, name, spheres, selectedSphereId);

        private static void AssertRoiState(BoundSceneFixture fixture, string activeRoiId, params RoiStateExpectation[] expected)
        {
            Assert.That(fixture.Scene.ROIManager.ROIs.Select(roi => roi.ID), Is.EqualTo(expected.Select(roi => roi.RoiId)), "ROI identity and order");
            Assert.That(fixture.Scene.ROIManager.SelectedROI?.ID, Is.EqualTo(activeRoiId), "active ROI identity");
            for (int i = 0; i < expected.Length; i++)
            {
                ROI roi = fixture.Scene.ROIManager.ROIs[i];
                RoiStateExpectation expectedRoi = expected[i];
                Assert.That(roi.Name, Is.EqualTo(expectedRoi.Name), expectedRoi.RoiId + " name");
                Assert.That(roi.Spheres.Select(sphere => sphere.ID), Is.EqualTo(expectedRoi.Spheres.Select(sphere => sphere.SphereId.Value)), expectedRoi.RoiId + " sphere identity and order");
                for (int sphereIndex = 0; sphereIndex < expectedRoi.Spheres.Length; sphereIndex++)
                {
                    RoiSphere sphere = roi.Spheres[sphereIndex];
                    V2RoiSphereDefinition definition = expectedRoi.Spheres[sphereIndex];
                    Assert.That(sphere.Position, Is.EqualTo(new Vector3(definition.X, definition.Y, definition.Z)), definition.SphereId.Value + " position");
                    Assert.That(sphere.InfluenceRadius, Is.EqualTo(definition.InfluenceRadius), definition.SphereId.Value + " radius");
                }

                int selectedSphereIndex = roi.SelectedSphereID;
                string selectedSphereId = selectedSphereIndex >= 0 && selectedSphereIndex < roi.Spheres.Count ? roi.Spheres[selectedSphereIndex].ID : null;
                Assert.That(selectedSphereId, Is.EqualTo(expectedRoi.SelectedSphereId), expectedRoi.RoiId + " selected sphere identity");
            }
        }

        private sealed class RoiStateExpectation
        {
            public string RoiId { get; }
            public string Name { get; }
            public V2RoiSphereDefinition[] Spheres { get; }
            public string SelectedSphereId { get; }

            public RoiStateExpectation(string roiId, string name, V2RoiSphereDefinition[] spheres, string selectedSphereId)
            {
                RoiId = roiId;
                Name = name;
                Spheres = spheres;
                SelectedSphereId = selectedSphereId;
            }
        }

        [Test]
        public void T10CutIdentity_SurvivesNonLastDeletionReorderAndBothDriverDirections()
        {
            using var desktop = new BoundSceneFixture(V2OriginDevice.Desktop, seedCuts: true);
            using var quest = new BoundSceneFixture(V2OriginDevice.Quest, seedCuts: true);
            using var authority = new V2DesktopMutationAuthority(SceneIdForT09, IncarnationIdForT09, desktop.Boundary);
            var questScheduler = new V2OutgoingScheduler(SessionIdForT09, SceneIdForT09, IncarnationIdForT09, V2OriginDevice.Quest, new TestClock(0));
            using var questDriver = new V2QuestMutationDriver(SceneIdForT09, IncarnationIdForT09, quest.Boundary, questScheduler);
            int questProposals = 0;
            int desktopProposals = 0;
            int questCutEffects = 0;
            int desktopCutEffects = 0;
            quest.Boundary.MutationProposed += (_, _, device) =>
            {
                if (device == V2OriginDevice.Quest) questProposals++;
            };
            desktop.Boundary.MutationProposed += (_, _, device) =>
            {
                if (device == V2OriginDevice.Desktop) desktopProposals++;
            };
            quest.Scene.OnModifyPlanesCuts.AddListener(() => questCutEffects++);
            desktop.Scene.OnModifyPlanesCuts.AddListener(() => desktopCutEffects++);
            authority.CanonicalReady += canonical => questDriver.ReceiveCanonical(canonical);
            questDriver.ProposalQueued += proposal => Assert.That(authority.AcceptQuestProposal(proposal).Outcome, Is.EqualTo(V2ProposalOutcome.Accepted));

            Assert.That(questDriver.ApplyOptimistic(new DeleteCut(new CutId("t10-cut-middle")), T09Operation(811)), Is.Not.Null);
            Assert.That(quest.Scene.Cuts.Select(cut => cut.ID), Is.EqualTo(new[] { "t10-cut-first", "t10-cut-last" }));
            Assert.That(desktop.Scene.Cuts.Select(cut => cut.ID), Is.EqualTo(new[] { "t10-cut-first", "t10-cut-last" }));

            Assert.That(questDriver.ApplyOptimistic(new SetCutOrder(new[] { new CutId("t10-cut-last"), new CutId("t10-cut-first") }), T09Operation(812)), Is.Not.Null);
            Assert.That(quest.Scene.Cuts.Select(cut => cut.ID), Is.EqualTo(new[] { "t10-cut-last", "t10-cut-first" }));
            Assert.That(desktop.Scene.Cuts.Select(cut => cut.ID), Is.EqualTo(new[] { "t10-cut-last", "t10-cut-first" }));

            var lastDefinition = new SetCutDefinition(new CutId("t10-cut-last"), V2CutOrientation.Custom, true, 7, 0.7f, 0.25f, 0.5f, 0.75f);
            Assert.That(questDriver.ApplyOptimistic(lastDefinition, T09Operation(813)), Is.Not.Null);
            Assert.That(quest.Scene.Cuts[0].ID, Is.EqualTo("t10-cut-last"));
            Assert.That(quest.Scene.Cuts[0].Flip, Is.True);
            Assert.That(desktop.Scene.Cuts[0].ID, Is.EqualTo("t10-cut-last"));
            Assert.That(desktop.Scene.Cuts[0].NumberOfCuts, Is.EqualTo(7));

            var firstDefinition = new SetCutDefinition(new CutId("t10-cut-first"), V2CutOrientation.Custom, false, 2, 0.3f, 0f, 1f, 0f);
            desktop.Boundary.Apply(firstDefinition, V2MutationApplicationOrigin.LocalDesktop, T09Operation(814));
            Assert.That(quest.Scene.Cuts[1].ID, Is.EqualTo("t10-cut-first"));
            Assert.That(quest.Scene.Cuts[1].Position, Is.EqualTo(0.3f));
            Assert.That(questProposals, Is.EqualTo(3), "Remote Quest operations and their canonical echoes must not emit another proposal.");
            Assert.That(desktopProposals, Is.EqualTo(1), "Quest driver applications must remain remote on Desktop; only the explicit Desktop edit is proposed.");
            Assert.That(questDriver.PendingProposalCount, Is.Zero);
            Assert.That(questCutEffects, Is.EqualTo(4), "Matching canonical echoes must not repeat cut or derived-scene effects.");
            Assert.That(desktopCutEffects, Is.EqualTo(4), "Desktop applies each accepted cause once and never re-emits its derived cut effects.");
        }

        private static V2Mutation[] CreateT09DriverMutations(BoundSceneFixture fixture, int timelineIndex)
        {
            var columnId = new ColumnId(fixture.Column.ColumnData.ID);
            var siteId = new SiteId(fixture.Site.Information.FullID);
            return new V2Mutation[]
            {
                new SetSelectedColumn(columnId),
                new SetSelectedSite(columnId, siteId),
                new SetSceneBoolean(V2SceneBooleanProperty.StrongCuts, true),
                new SetSceneBoolean(V2SceneBooleanProperty.HideBlacklistedSites, true),
                new SetSceneBoolean(V2SceneBooleanProperty.EdgeMode, true),
                new SetSceneBoolean(V2SceneBooleanProperty.BrainTransparent, true),
                new SetSceneFloat(V2SceneFloatProperty.SiteGain, 1.5f),
                new SetSceneFloat(V2SceneFloatProperty.BrainAlpha, 0.6f),
                new SetSceneFloat(V2SceneFloatProperty.AtlasAlpha, 0.4f),
                new SetSceneColor(V2SceneColorProperty.Brain, (int)ColorType.Hot),
                new SetSceneColor(V2SceneColorProperty.Cut, (int)ColorType.Warm),
                new SetSceneColor(V2SceneColorProperty.Colormap, (int)ColorType.XRain),
                new SetSiteHighlight(columnId, siteId, true),
                new SetSiteLabels(columnId, siteId, new[] { "reviewed", "T09" }),
                new SetActivityAlpha(columnId, 0.35f),
                new SetColumnSpan(new ColumnId(fixture.StaticColumn.ColumnData.ID), V2ColumnSpanKind.Static, -1f, 0.25f, 1f),
                new SetColumnSpan(new ColumnId(fixture.DynamicColumn.ColumnData.ID), V2ColumnSpanKind.Dynamic, -2f, 0f, 3f),
                new SetFunctionalDisplay(new ColumnId(fixture.FmriColumn.ColumnData.ID), V2FunctionalModality.Fmri, 0.1f, 0.4f, 0.2f, 0.8f, true, false, true),
                new SetFunctionalDisplay(new ColumnId(fixture.MegColumn.ColumnData.ID), V2FunctionalModality.Meg, 0.05f, 0.45f, 0.3f, 0.95f, false, true, false),
                new SetTimelineAnchor(new ColumnId(fixture.FmriColumn.ColumnData.ID), timelineIndex, true, true, 3, 0, 1000),
                new SetSelectedRoiSphere(fixture.Roi.ID, fixture.Sphere.ID)
            };
        }

        private static void AssertT09DriverValuesMatch(BoundSceneFixture expected, BoundSceneFixture actual, int timelineIndex)
        {
            Assert.That(actual.Column.IsSelected, Is.True);
            Assert.That(actual.Column.SelectedSite, Is.SameAs(actual.Site));
            Assert.That(actual.Scene.StrongCuts, Is.True);
            Assert.That(actual.Scene.HideBlacklistedSites, Is.True);
            Assert.That(actual.Scene.EdgeMode, Is.True);
            Assert.That(actual.Scene.IsBrainTransparent, Is.True);
            Assert.That(actual.Scene.SiteGain, Is.EqualTo(1.5f));
            Assert.That(actual.Materials.Alpha, Is.EqualTo(0.6f));
            Assert.That(actual.Scene.AtlasManager.AtlasAlpha, Is.EqualTo(0.4f));
            Assert.That(actual.Scene.BrainColor, Is.EqualTo(ColorType.Hot));
            Assert.That(actual.Scene.CutColor, Is.EqualTo(ColorType.Warm));
            Assert.That(actual.Scene.Colormap, Is.EqualTo(ColorType.XRain));
            Assert.That(actual.Scene.AtlasManager.DisplayMarsAtlas, Is.False);
            Assert.That(actual.Scene.AtlasManager.DisplayJuBrainAtlas, Is.False);
            Assert.That(actual.Site.State.IsHighlighted, Is.True);
            Assert.That(actual.Site.State.Labels, Is.EqualTo(new[] { "reviewed", "T09" }));
            Assert.That(actual.Column.ActivityAlpha, Is.EqualTo(0.35f));
            Assert.That(actual.StaticColumn.StaticParameters.SpanMin, Is.EqualTo(-1f));
            Assert.That(actual.DynamicColumn.DynamicParameters.SpanMax, Is.EqualTo(3f));
            Assert.That(actual.FmriColumn.FMRIParameters.FMRINegativeCalMinFactor, Is.EqualTo(0.1f));
            Assert.That(actual.FmriColumn.FMRIParameters.HideLowerValues, Is.True);
            Assert.That(actual.MegColumn.MEGParameters.FMRIPositiveCalMaxFactor, Is.EqualTo(0.95f));
            Assert.That(actual.MegColumn.MEGParameters.HideMiddleValues, Is.True);
            Assert.That(actual.FmriColumn.Timeline.CurrentIndex, Is.EqualTo(timelineIndex));
            Assert.That(actual.FmriColumn.Timeline.IsPlaying, Is.True);
            Assert.That(actual.FmriColumn.Timeline.IsLooping, Is.True);
            Assert.That(actual.FmriColumn.Timeline.Step, Is.EqualTo(3));
            Assert.That(actual.Roi.SelectedSphereID, Is.EqualTo(0));
            Assert.That(actual.Roi.SelectedSphere, Is.SameAs(actual.Sphere));
            Assert.That(actual.Site.State.Color, Is.EqualTo(expected.Site.State.Color));
        }

        private static OperationId T09Operation(int value) => new(Guid.Parse($"40000000-0000-0000-0000-{value:X12}"));

        private sealed class Fixture : IDisposable
        {
            private readonly Action<SceneCut> m_UpdateCut;
            private readonly IMonotonicClock m_Clock;
            private readonly V2OriginDevice m_LocalOrigin;
            private readonly Func<SetTimelineAnchor, double?> m_TimelineAgeSeconds;
            private readonly Func<SetTimelineAnchor, V2TimelineAnchorTimingEstimate?> m_TimelineTimingEstimate;

            public SiteState State { get; } = new SiteState();
            public SceneCut Cut { get; } = new SceneCut();
            public TestTimeline Timeline { get; } = new TestTimeline(100);
            public V2SceneMutationBoundary Boundary { get; private set; }
            public V2Mutation LastProposal { get; private set; }
            public OperationId LastOperationId { get; private set; }
            public ColumnId ColumnId { get; }
            public SiteId SiteId { get; }

            public Fixture(string prefix, V2OriginDevice localOrigin, long clockTicks, string siteId = null, Func<SetTimelineAnchor, double?> timelineAgeSeconds = null, Func<SetTimelineAnchor, V2TimelineAnchorTimingEstimate?> timelineTimingEstimate = null) : this(prefix, localOrigin, new TestClock(clockTicks, 1000), siteId, timelineAgeSeconds, timelineTimingEstimate)
            {
            }

            public Fixture(string prefix, V2OriginDevice localOrigin, IMonotonicClock clock, string siteId = null, Func<SetTimelineAnchor, double?> timelineAgeSeconds = null, Func<SetTimelineAnchor, V2TimelineAnchorTimingEstimate?> timelineTimingEstimate = null)
            {
                m_LocalOrigin = localOrigin;
                m_Clock = clock;
                m_TimelineAgeSeconds = timelineAgeSeconds;
                m_TimelineTimingEstimate = timelineTimingEstimate;
                m_UpdateCut = _ => CutInvalidationCount++;
                ColumnId = new ColumnId("shared-column");
                SiteId = new SiteId(siteId ?? "shared-site");
                Cut.ID = "shared-cut";
                Rebind(m_UpdateCut);
            }

            public int CutInvalidationCount { get; private set; }

            public void Rebind(Action<SceneCut> updateCut)
            {
                Boundary?.Dispose();
                Boundary = new V2SceneMutationBoundary(new[] { (State, ColumnId, SiteId) }, new[] { (Cut, new CutId(Cut.ID)) }, new[] { ((BasicTimeline)Timeline, ColumnId) }, m_LocalOrigin, m_Clock, updateCut, m_TimelineAgeSeconds, m_TimelineTimingEstimate);
                Boundary.MutationProposed += (id, mutation, _) =>
                {
                    LastOperationId = id;
                    LastProposal = mutation;
                };
            }

            public void Dispose()
            {
                Boundary?.Dispose();
                Cut.Dispose();
            }
        }

        private sealed class T11PreparedResourceFixture : IDisposable
        {
            public GameObject Root { get; }
            public Base3DScene Scene { get; }
            public Column3DStatic StaticColumn { get; }
            public Column3DFMRI FmriColumn { get; }
            public Column3DMEG MegColumn { get; }
            public V2SceneMutationBoundary Boundary { get; }

            public T11PreparedResourceFixture()
            {
                Root = new GameObject("T11 prepared resources");
                Root.SetActive(false);
                Scene = Root.AddComponent<Base3DScene>();

                StaticColumn = Root.AddComponent<Column3DStatic>();
                var staticData = new StaticColumn("t11-static", new BaseConfiguration(), null, string.Empty, new StaticConfiguration(), "t11-static-column");
                staticData.Data.ValueByChannelIDByLabel.Add("static-a", new Dictionary<string, float> { ["site"] = 0.25f });
                staticData.Data.ValueByChannelIDByLabel.Add("static-b", new Dictionary<string, float> { ["site"] = 0.75f });
                SetAutoProperty(StaticColumn, "ColumnData", staticData);
                SetAutoProperty(StaticColumn, "Labels", new[] { "static-a", "static-b" });
                SetAutoProperty(StaticColumn, "Sites", new List<HBP.Core.Object3D.Site>());
                Scene.Columns.Add(StaticColumn);

                FmriColumn = Root.AddComponent<Column3DFMRI>();
                var fmriData = new FMRIColumn("t11-fmri", new BaseConfiguration(), null, new FMRIConfiguration(), "t11-fmri-column");
                fmriData.Data.FMRIs.Add(Tuple.Create(CreateLoadedFmri("fmri-a"), (Patient)null));
                fmriData.Data.FMRIs.Add(Tuple.Create(CreateLoadedFmri("fmri-b"), (Patient)null));
                SetAutoProperty(FmriColumn, "ColumnData", fmriData);
                SetAutoProperty(FmriColumn, "Sites", new List<HBP.Core.Object3D.Site>());
                Scene.Columns.Add(FmriColumn);

                MegColumn = Root.AddComponent<Column3DMEG>();
                var megData = new MEGColumn("t11-meg", new BaseConfiguration(), null, new MEGConfiguration(), "t11-meg-column");
                megData.Data.MEGItems.Add(new HBP.Core.Data.Processed.MEGItem { Label = "meg-a", ValuesByChannel = new Dictionary<string, float[]> { ["channel"] = Array.Empty<float>() } });
                megData.Data.MEGItems.Add(new HBP.Core.Data.Processed.MEGItem { Label = "meg-b", ValuesByChannel = new Dictionary<string, float[]> { ["channel"] = Array.Empty<float>() } });
                SetAutoProperty(MegColumn, "ColumnData", megData);
                SetAutoProperty(MegColumn, "Sites", new List<HBP.Core.Object3D.Site>());
                Scene.Columns.Add(MegColumn);

                Boundary = new V2SceneMutationBoundary(Scene, V2OriginDevice.Desktop, new TestClock(0));
            }

            private static HBP.Core.Object3D.FMRI CreateLoadedFmri(string name)
            {
                var fmri = new HBP.Core.Object3D.FMRI { Name = name };
                SetAutoProperty(fmri, "Loaded", true);
                return fmri;
            }

            public void Dispose()
            {
                Boundary.Dispose();
                Object.DestroyImmediate(Root);
            }
        }

        private sealed class T11CcepFixture : IDisposable
        {
            public GameObject Root { get; }
            public Base3DScene Scene { get; }
            public Column3DCCEP Column { get; }
            public HBP.Core.Object3D.Site Site { get; }
            public V2SceneMutationBoundary Boundary { get; }

            public T11CcepFixture()
            {
                Root = new GameObject("T11 CCEP source");
                Root.SetActive(false);
                Scene = Root.AddComponent<Base3DScene>();
                var columnData = new CCEPColumn("t11-ccep", new BaseConfiguration(), null, string.Empty, null, new CCEPConfiguration(), "t11-ccep-column");
                columnData.Data.ProjectionTimeline = (Timeline)FormatterServices.GetUninitializedObject(typeof(Timeline));

                const string patientId = "60000000-0000-0000-0000-000000000011";
                var patient = new Patient { ID = patientId, Name = "t11-patient" };
                var siteObject = new GameObject("t11-source-site");
                siteObject.transform.SetParent(Root.transform, false);
                Site = siteObject.AddComponent<HBP.Core.Object3D.Site>();
                Site.Information = new SiteInformation { Patient = patient, Name = "t11-source-site", Index = 0 };
                Site.State = new SiteState();
                columnData.Data.ProcessedValuesByChannelIDByStimulatedChannelID.Add(Site.Information.FullID, new Dictionary<string, float[]>());

                Column = Root.AddComponent<Column3DCCEP>();
                SetAutoProperty(Column, "ColumnData", columnData);
                SetAutoProperty(Column, "Sites", new List<HBP.Core.Object3D.Site> { Site });
                SetAutoProperty(Column, "Sources", new List<HBP.Core.Object3D.Site> { Site });
                Scene.Columns.Add(Column);
                Boundary = new V2SceneMutationBoundary(Scene, V2OriginDevice.Desktop, new TestClock(0));
            }

            public void Dispose()
            {
                Boundary.Dispose();
                Object.DestroyImmediate(Root);
            }
        }

        private sealed class BoundSceneFixture : IDisposable
        {
            public GameObject Root { get; }
            public Base3DScene Scene { get; }
            public BrainMaterials Materials { get; }
            public Column3DAnatomy Column { get; }
            public HBP.Core.Object3D.Site Site { get; }
            public Column3DStatic StaticColumn { get; }
            public TestDynamicColumn DynamicColumn { get; }
            public Column3DFMRI FmriColumn { get; }
            public Column3DMEG MegColumn { get; }
            public ROI Roi { get; }
            public RoiSphere Sphere { get; }
            private SharedMaterials SphereMaterials { get; }
            private CoreVolume CutTestVolume { get; }
            private string CutTestVolumePath { get; }
            private Mesh CutTemplateMesh { get; }
            public V2SceneMutationBoundary Boundary { get; }

            public BoundSceneFixture(V2OriginDevice localOrigin = V2OriginDevice.Quest, bool seedCuts = false, string[] cutIds = null, string roiId = "t09-roi-id", string sphereId = "t09-sphere-id", bool configureCutCreation = false)
            {
                Root = new GameObject("T09 typed scene fixture");
                Root.SetActive(false);
                Scene = Root.AddComponent<Base3DScene>();
                Materials = InitializeTestBrainMaterials(Scene);
                SetPrivateField(Scene, "m_MeshManager", Root.AddComponent<MeshManager>());
                MRIManager mriManager = Root.AddComponent<MRIManager>();
                SetPrivateField(mriManager, "m_Scene", Scene);
                SetPrivateField(Scene, "m_MRIManager", mriManager);
                if (configureCutCreation)
                {
                    CutTestVolumePath = Path.Combine(Application.temporaryCachePath, "sync-checkpoint-cut-" + Guid.NewGuid().ToString("N") + ".nii");
                    CreateMinimalNifti(CutTestVolumePath);
                    CutTestVolume = new CoreVolume();
                    if (!CutTestVolume.LoadNIFTIFile(CutTestVolumePath)) throw new InvalidOperationException("Unable to load the checkpoint cut fixture volume.");
                    mriManager.MRIs.Add(new MRI3D("checkpoint-test", CutTestVolume));
                }

                AtlasManager atlasManager = Root.AddComponent<AtlasManager>();
                SetPrivateField(Scene, "m_AtlasManager", atlasManager);
                SetPrivateField(atlasManager, "m_Scene", Scene);
                FMRIManager fmriManager = Root.AddComponent<FMRIManager>();
                SetPrivateField(Scene, "m_FMRIManager", fmriManager);
                SetPrivateField(fmriManager, "m_Scene", Scene);
                ROIManager roiManager = Root.AddComponent<ROIManager>();
                SetPrivateField(Scene, "m_ROIManager", roiManager);
                SetPrivateField(roiManager, "m_Scene", Scene);
                const string patientId = "60000000-0000-0000-0000-000000000009";
                var patient = new Patient { ID = patientId, Name = "t09-patient" };
                var columnData = new AnatomicColumn("t09-column", new BaseConfiguration(), new AnatomicConfiguration(), "t09-column-id");
                Column = Root.AddComponent<Column3DAnatomy>();
                SetAutoProperty(Column, "ColumnData", columnData);
                var siteObject = new GameObject("t09-site");
                siteObject.transform.SetParent(Root.transform, false);
                Site = siteObject.AddComponent<HBP.Core.Object3D.Site>();
                Site.Information = new SiteInformation { Patient = patient, Name = "t09-site" };
                Site.State = new SiteState();
                SetAutoProperty(Column, "Sites", new List<HBP.Core.Object3D.Site> { Site });
                Site.OnSelectSite.AddListener(selected =>
                {
                    if (selected)
                    {
                        Column.UnselectSite();
                        SetAutoProperty(Column, "SelectedSite", Site);
                    }
                    else if (ReferenceEquals(Column.SelectedSite, Site))
                    {
                        SetAutoProperty(Column, "SelectedSite", null);
                    }

                    Column.OnSelectSite.Invoke(Column.SelectedSite);
                });
                Scene.Columns.Add(Column);
                StaticColumn = Root.AddComponent<Column3DStatic>();
                SetAutoProperty(StaticColumn, "ColumnData", NewColumnData("t09-static-column-id"));
                SetAutoProperty(StaticColumn, "Sites", new List<HBP.Core.Object3D.Site>());
                Scene.Columns.Add(StaticColumn);
                DynamicColumn = Root.AddComponent<TestDynamicColumn>();
                SetAutoProperty(DynamicColumn, "ColumnData", NewColumnData("t09-dynamic-column-id"));
                SetAutoProperty(DynamicColumn, "Sites", new List<HBP.Core.Object3D.Site>());
                Scene.Columns.Add(DynamicColumn);
                FmriColumn = Root.AddComponent<Column3DFMRI>();
                SetAutoProperty(FmriColumn, "ColumnData", NewColumnData("t09-fmri-column-id"));
                SetAutoProperty(FmriColumn, "Sites", new List<HBP.Core.Object3D.Site>());
                FMRITimeline fmriTimeline = new();
                SetAutoProperty(fmriTimeline, "Length", 100);
                SetAutoProperty(FmriColumn, "Timeline", fmriTimeline);
                Scene.Columns.Add(FmriColumn);
                MegColumn = Root.AddComponent<Column3DMEG>();
                SetAutoProperty(MegColumn, "ColumnData", NewColumnData("t09-meg-column-id"));
                SetAutoProperty(MegColumn, "Sites", new List<HBP.Core.Object3D.Site>());
                FMRITimeline megTimeline = new();
                SetAutoProperty(megTimeline, "Length", 100);
                SetAutoProperty(MegColumn, "Timeline", megTimeline);
                Scene.Columns.Add(MegColumn);
                var roiObject = new GameObject("t09-roi");
                roiObject.transform.SetParent(Root.transform, false);
                Roi = roiObject.AddComponent<ROI>();
                Roi.ID = roiId;
                SetAutoProperty(Roi, "SelectedSphereID", -1);
                GameObject sphereObject = new("t09-sphere");
                sphereObject.transform.SetParent(Root.transform, false);
                sphereObject.AddComponent<MeshRenderer>();
                Sphere = sphereObject.AddComponent<RoiSphere>();
                Sphere.ID = sphereId;
                SphereMaterials = ScriptableObject.CreateInstance<SharedMaterials>();
                SetPrivateField(Sphere, "m_SharedMaterials", SphereMaterials);
                SetAutoProperty(Roi, "Spheres", new List<RoiSphere> { Sphere });
                SetAutoProperty(roiManager, "ROIs", new List<ROI> { Roi });
                var prefabContainer = new GameObject("T09 fixture prefab container");
                prefabContainer.transform.SetParent(Root.transform, false);
                prefabContainer.SetActive(false);
                var spherePrefab = new GameObject("T09 fixture sphere prefab");
                spherePrefab.transform.SetParent(prefabContainer.transform, false);
                spherePrefab.AddComponent<MeshFilter>();
                spherePrefab.AddComponent<MeshRenderer>();
                spherePrefab.AddComponent<SphereCollider>();
                SetPrivateField(spherePrefab.AddComponent<RoiSphere>(), "m_SharedMaterials", SphereMaterials);
                SetPrivateField(Roi, "m_SpherePrefab", spherePrefab);
                var roiPrefab = new GameObject("T09 fixture ROI prefab");
                roiPrefab.transform.SetParent(prefabContainer.transform, false);
                SetPrivateField(roiPrefab.AddComponent<ROI>(), "m_SpherePrefab", spherePrefab);
                DisplayedObjects displayedObjects = Root.AddComponent<DisplayedObjects>();
                SetPrivateField(displayedObjects, "m_Scene", Scene);
                SetPrivateField(displayedObjects, "m_ROIParent", Root.transform);
                SetPrivateField(displayedObjects, "m_ROIPrefab", roiPrefab);
                SetAutoProperty(displayedObjects, "Brain", NewRendererObject("t09-brain"));
                SetAutoProperty(displayedObjects, "SimplifiedBrain", NewRendererObject("t09-simplified-brain"));
                SetAutoProperty(displayedObjects, "BrainCutMeshes", new List<GameObject>());
                SetPrivateField(Scene, "m_DisplayedObjects", displayedObjects);
                SetPrivateField(roiManager, "m_DisplayedObjects", displayedObjects);
                if (configureCutCreation)
                {
                    var cutPrefab = new GameObject("T10 fixture cut prefab");
                    cutPrefab.transform.SetParent(prefabContainer.transform, false);
                    CutTemplateMesh = new Mesh();
                    cutPrefab.AddComponent<MeshFilter>().sharedMesh = CutTemplateMesh;
                    cutPrefab.AddComponent<MeshRenderer>();
                    SetPrivateField(displayedObjects, "m_BrainCutMeshesParent", Root.transform);
                    SetPrivateField(displayedObjects, "m_CutPrefab", cutPrefab);
                }

                foreach (Column3D column in Scene.Columns)
                    SetAutoProperty(column, "BrainMesh", NewRendererObject("t09-column-brain-" + column.ColumnData.ID));

                string[] initialCutIds = cutIds ?? (seedCuts ? new[] { "t10-cut-first", "t10-cut-middle", "t10-cut-last" } : Array.Empty<string>());
                if (initialCutIds.Length > 0)
                {
                    // Cut identity and order tests do not need scene columns; removing one should not exercise column rendering.
                    Scene.Columns.Clear();
                    var ownedCutMeshes = (List<Mesh>)typeof(DisplayedObjects).GetField("m_OwnedCutMeshes", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(displayedObjects);
                    for (int i = 0; i < initialCutIds.Length; i++)
                    {
                        var cut = new SceneCut(Vector3.zero, Vector3.right) { ID = initialCutIds[i], Index = i, Orientation = HBP.Core.Enums.CutOrientation.Custom };
                        Scene.Cuts.Add(cut);
                        displayedObjects.BrainCutMeshes.Add(NewRendererObject("t10-cut-visual-" + i));
                        ownedCutMeshes.Add(new Mesh());
                    }
                }

                Boundary = new V2SceneMutationBoundary(Scene, localOrigin, new TestClock(0));
            }

            private static AnatomicColumn NewColumnData(string id) => new AnatomicColumn(id, new BaseConfiguration(), new AnatomicConfiguration(), id);

            private GameObject NewRendererObject(string name)
            {
                var gameObject = new GameObject(name);
                gameObject.transform.SetParent(Root.transform, false);
                gameObject.AddComponent<MeshRenderer>();
                return gameObject;
            }

            public void Dispose()
            {
                Boundary.Dispose();
                CutTestVolume?.Dispose();
                DestroyTestBrainMaterials(Materials);
                Object.DestroyImmediate(SphereMaterials);
                Object.DestroyImmediate(Root);
                if (CutTemplateMesh != null) Object.DestroyImmediate(CutTemplateMesh);
                if (!string.IsNullOrEmpty(CutTestVolumePath) && File.Exists(CutTestVolumePath)) File.Delete(CutTestVolumePath);
            }
        }

        private static void AssertNestedSiteStateChangeInvalidation(Action<SiteState> nestedChange, bool expectGeneratorUpdate)
        {
            GameObject root = new("nested site state invalidation test");
            root.SetActive(false);
            try
            {
                Base3DScene scene = root.AddComponent<Base3DScene>();
                Column3D column = root.AddComponent<Column3DAnatomy>();
                HBP.Core.Object3D.Site site = root.AddComponent<HBP.Core.Object3D.Site>();
                site.State = new SiteState();
                ResetSceneInvalidationFlags(scene);

                MethodInfo sceneHandler = typeof(Base3DScene).GetMethod("OnSiteStateChanged", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.That(sceneHandler, Is.Not.Null);
                bool nested = false;
                column.OnChangeSiteState.AddListener(_ =>
                {
                    if (nested) return;
                    nested = true;
                    nestedChange(site.State);
                });
                column.OnChangeSiteState.AddListener(changedSite => sceneHandler.Invoke(scene, new object[] { changedSite }));
                site.State.OnChangeState.AddListener(() => column.OnChangeSiteState.Invoke(site));

                site.State.Color = Color.magenta;

                Assert.That(site.State.IsColorChangeInProgress, Is.False);
                Assert.That(scene.SceneInformation.SitesNeedUpdate, Is.True);
                Assert.That(scene.SceneInformation.GeneratorNeedsUpdate, Is.EqualTo(expectGeneratorUpdate));
                Assert.That(scene.SceneInformation.CollidersNeedUpdate, Is.False);
                Assert.That(scene.SceneInformation.CutsNeedUpdate, Is.False);
                Assert.That(scene.SceneInformation.BaseCutTexturesNeedUpdate, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void ResetSceneInvalidationFlags(Base3DScene scene)
        {
            scene.SceneInformation.GeometryNeedsUpdate = false;
            scene.SceneInformation.CutsNeedUpdate = false;
            scene.SceneInformation.BaseCutTexturesNeedUpdate = false;
            scene.SceneInformation.FunctionalCutTexturesNeedUpdate = false;
            scene.SceneInformation.GUICutTexturesNeedUpdate = false;
            scene.SceneInformation.SitesNeedUpdate = false;
            scene.SceneInformation.GeneratorNeedsUpdate = false;
            scene.SceneInformation.CollidersNeedUpdate = false;
            scene.SceneInformation.FunctionalSurfaceNeedsUpdate = false;
        }

        private static BrainMaterials InitializeTestBrainMaterials(Base3DScene scene)
        {
            var brainMaterials = new BrainMaterials();
            typeof(Base3DScene).GetProperty(nameof(Base3DScene.BrainMaterials)).SetValue(scene, brainMaterials);
            return brainMaterials;
        }

        private static void DestroyTestBrainMaterials(BrainMaterials brainMaterials)
        {
            foreach (string fieldName in new[] { "m_Brain", "m_TransparentBrain", "m_Cut", "m_TransparentCut" })
            {
                Material material = (Material)typeof(BrainMaterials).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(brainMaterials);
                if (material) Object.DestroyImmediate(material);
            }
        }

        private static void CreateMinimalNifti(string path)
        {
            using var stream = new FileStream(path, FileMode.CreateNew);
            using var writer = new BinaryWriter(stream);
            stream.SetLength(376);
            writer.Write(348);
            stream.Position = 40;
            writer.Write((short)3);
            writer.Write((short)2);
            writer.Write((short)3);
            writer.Write((short)4);
            stream.Position = 70;
            writer.Write((short)2);
            writer.Write((short)8);
            stream.Position = 80;
            writer.Write(1f);
            writer.Write(2f);
            writer.Write(3f);
            stream.Position = 108;
            writer.Write(352f);
            stream.Position = 344;
            writer.Write(new byte[] { (byte)'n', (byte)'+', (byte)'1', 0 });
            stream.Position = 352;
            writer.Write(Enumerable.Range(0, 24).Select(i => (byte)i).ToArray());
        }

        private static PreparedSceneDeliveryBinding CreateT11PreparedBinding(Base3DScene scene)
        {
            var columns = new JArray();
            foreach (Column3D column in scene.Columns)
            {
                var functional = new JArray();
                if (column is Column3DFMRI fmriColumn)
                    foreach (var item in fmriColumn.ColumnFMRIData.Data.FMRIs)
                        functional.Add(new JObject { ["Name"] = item.Item1.Name, ["PatientId"] = item.Item2?.ID, ["File"] = null, ["Mask"] = null });
                else if (column is Column3DMEG megColumn)
                    foreach (HBP.Core.Data.Processed.MEGItem item in megColumn.ColumnMEGData.Data.MEGItems)
                        functional.Add(new JObject { ["Name"] = item.Label, ["PatientId"] = item.Patient?.ID, ["File"] = null, ["Mask"] = null });

                columns.Add(new JObject { ["Id"] = column.ColumnData.ID, ["Functional"] = functional });
            }

            var metadata = new JObject
            {
                ["TransferId"] = "t11-transfer",
                ["SessionId"] = "t11-session",
                ["GlobalContextId"] = "60000000-0000-0000-0000-000000000012",
                ["Visualization"] = new JObject { ["ID"] = "70000000-0000-0000-0000-000000000012" },
                ["StandardFiles"] = new JObject(),
                ["Meshes"] = new JArray(),
                ["MRIs"] = new JArray(),
                ["Columns"] = columns
            };
            PreparedSceneManifest manifest = PreparedSceneManifest.FromMetadata(metadata);
            ConstructorInfo constructor = typeof(PreparedSceneDeliveryBinding).GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, new[] { typeof(string), typeof(PreparedSceneManifest) }, null);
            Assert.That(constructor, Is.Not.Null);
            return (PreparedSceneDeliveryBinding)constructor.Invoke(new object[] { new string('b', 64), manifest });
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            Type type = target.GetType();
            FieldInfo field = null;
            while (type != null && field == null)
            {
                field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                type = type.BaseType;
            }

            Assert.That(field, Is.Not.Null, "Field " + fieldName + " was not found on " + target.GetType().Name + ".");
            field.SetValue(target, value);
        }

        private static void SetAutoProperty(object target, string propertyName, object value)
        {
            Type current = target.GetType();
            FieldInfo backingField = null;
            while (current != null && backingField == null)
            {
                backingField = current.GetField("<" + propertyName + ">k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
                current = current.BaseType;
            }

            Assert.That(backingField, Is.Not.Null, "Missing backing field for " + target.GetType().Name + "." + propertyName + ".");
            backingField.SetValue(target, value);
        }

        private sealed class TestDynamicColumn : Column3DDynamic
        {
            public override Timeline Timeline => null;
            public override Timeline ProjectionTimeline => null;

            protected override void SetActivityData()
            {
            }
        }

        private sealed class TestTimeline : BasicTimeline
        {
            public TestTimeline(int length) => Length = length;
            public override SubTimeline CurrentSubtimeline => null;
        }

        private sealed class TestClock : IMonotonicClock
        {
            public long Frequency { get; }
            public long Timestamp { get; set; }

            public TestClock(long timestamp, long frequency = 1000)
            {
                Timestamp = timestamp;
                Frequency = frequency;
            }

            public long GetTimestamp() => Timestamp;
        }
    }
}
