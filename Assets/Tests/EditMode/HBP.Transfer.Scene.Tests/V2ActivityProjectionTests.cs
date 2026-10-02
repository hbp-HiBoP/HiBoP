using System;
using HBP.Sync;
using NUnit.Framework;

namespace HBP.Tests.Transfer.Scene
{
    [Category("Sync.SceneFocused")]
    public class V2ActivityProjectionTests
    {
        [Test]
        public void ActivityProjectionControl_RoundTripsBoundedGenerationAndProgressFields()
        {
            var jobId = new OperationId(Guid.Parse("60000000-0000-0000-0000-000000000014"));
            var started = new V2ActivityProjectionControl(V2ActivityProjectionControlKind.Started, jobId, generation: 7, inputGeneration: 19, canonicalSequence: 43, projectionRequested: true, automaticPolicyEnabled: true, explicitRequest: false);

            byte[] encoded = V2ActivityProjectionControlCodec.Encode(started);
            Assert.That(encoded.Length, Is.LessThanOrEqualTo(V2ActivityProjectionControlCodec.MaximumPayloadBytes));
            Assert.That(V2ActivityProjectionControlCodec.TryDecode(encoded, out V2ActivityProjectionControl decoded), Is.True);
            Assert.That(decoded.Kind, Is.EqualTo(started.Kind));
            Assert.That(decoded.JobId, Is.EqualTo(jobId));
            Assert.That(decoded.Generation, Is.EqualTo(7));
            Assert.That(decoded.InputGeneration, Is.EqualTo(19));
            Assert.That(decoded.CanonicalSequence, Is.EqualTo(43));
            Assert.That(decoded.ProjectionRequested, Is.True);
            Assert.That(decoded.AutomaticPolicyEnabled, Is.True);

            var progress = new V2ActivityProjectionControl(V2ActivityProjectionControlKind.Progress, jobId, 7, 19, progress: 0.625f, message: "column 2 of 4");
            Assert.That(V2ActivityProjectionControlCodec.TryDecode(V2ActivityProjectionControlCodec.Encode(progress), out V2ActivityProjectionControl decodedProgress), Is.True);
            Assert.That(decodedProgress.Progress, Is.EqualTo(0.625f));
            Assert.That(decodedProgress.Message, Is.EqualTo("column 2 of 4"));

            byte[] truncated = V2ActivityProjectionControlCodec.Encode(started);
            Array.Resize(ref truncated, truncated.Length - 1);
            Assert.That(V2ActivityProjectionControlCodec.TryDecode(truncated, out _), Is.False);

            var idleRemoval = new V2ActivityProjectionControl(V2ActivityProjectionControlKind.RemoveRequest, jobId, generation: 0, inputGeneration: 20, canonicalSequence: 44);
            Assert.That(V2ActivityProjectionControlCodec.TryDecode(V2ActivityProjectionControlCodec.Encode(idleRemoval), out V2ActivityProjectionControl decodedRemoval), Is.True);
            Assert.That(decodedRemoval.Kind, Is.EqualTo(V2ActivityProjectionControlKind.RemoveRequest));
            Assert.That(decodedRemoval.Generation, Is.Zero, "Idle removal must not depend on an active projection generation.");
            Assert.That(decodedRemoval.InputGeneration, Is.EqualTo(20UL));
            Assert.That(decodedRemoval.CanonicalSequence, Is.EqualTo(44UL));
            Assert.That(decodedRemoval.ProjectionRequested, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ActivityProjectionTerminalBarrier_WaitsForBothPeersInEitherArrivalOrder(bool localFirst)
        {
            var barrier = new V2ActivityProjectionTerminalBarrier();
            if (localFirst)
            {
                barrier.MarkLocalTerminal();
                Assert.That(barrier.IsComplete, Is.False);
                barrier.MarkRemoteTerminal();
            }
            else
            {
                barrier.MarkRemoteTerminal();
                Assert.That(barrier.IsComplete, Is.False);
                barrier.MarkLocalTerminal();
            }

            Assert.That(barrier.IsComplete, Is.True);
        }

        [Test]
        public void ActivityProjectionSensitiveAdmission_UsesMutationEffectsAndBlacklistedBatchChanges()
        {
            var columnId = new ColumnId("column");
            var siteId = new SiteId("site");
            Assert.That(HBP.Sync.Scene.V2ActivityProjectionAdmission.RequiresSensitiveAdmission(new SetSiteBlacklist(columnId, siteId, true)), Is.True);
            Assert.That(HBP.Sync.Scene.V2ActivityProjectionAdmission.RequiresSensitiveAdmission(new SetInfluenceDistance(columnId, 25f)), Is.True);
            Assert.That(HBP.Sync.Scene.V2ActivityProjectionAdmission.RequiresSensitiveAdmission(new SetSelectedColumn(columnId)), Is.False);
            Assert.That(HBP.Sync.Scene.V2ActivityProjectionAdmission.RequiresSensitiveAdmission(new SetSceneBoolean(V2SceneBooleanProperty.ShowAllSites, true), new SetSceneBoolean(V2SceneBooleanProperty.ShowAllSites, false)), Is.True);
            Assert.That(HBP.Sync.Scene.V2ActivityProjectionAdmission.RequiresSensitiveAdmission(new SetSceneBoolean(V2SceneBooleanProperty.ShowAllSites, false), new SetSceneBoolean(V2SceneBooleanProperty.ShowAllSites, false)), Is.False);

            var current = new SetSiteConfigurationBatch(new[] { Assignment(columnId, siteId, blacklisted: false, highlighted: true) });
            var appearanceOnly = new SetSiteConfigurationBatch(new[] { Assignment(columnId, siteId, blacklisted: false, highlighted: false) });
            var blacklistChange = new SetSiteConfigurationBatch(new[] { Assignment(columnId, siteId, blacklisted: true, highlighted: true) });
            Assert.That(HBP.Sync.Scene.V2ActivityProjectionAdmission.RequiresSensitiveAdmission(appearanceOnly, current), Is.False);
            Assert.That(HBP.Sync.Scene.V2ActivityProjectionAdmission.RequiresSensitiveAdmission(blacklistChange, current), Is.True);
            Assert.That(HBP.Sync.Scene.V2ActivityProjectionAdmission.RequiresSensitiveAdmission(new SetConfigurationTransaction(new HBP.Sync.V2Mutation[]
            {
                new SetSceneBoolean(V2SceneBooleanProperty.StrongCuts, true),
                new SetInfluenceDistance(columnId, 15f)
            })), Is.True);

            var appearanceOnlyTransaction = new SetConfigurationTransaction(new HBP.Sync.V2Mutation[] { appearanceOnly });
            var blacklistTransaction = new SetConfigurationTransaction(new HBP.Sync.V2Mutation[] { blacklistChange });
            Assert.That(HBP.Sync.Scene.V2ActivityProjectionAdmission.RequiresSensitiveAdmission(appearanceOnlyTransaction, currentValueResolver: _ => current), Is.False);
            Assert.That(HBP.Sync.Scene.V2ActivityProjectionAdmission.RequiresSensitiveAdmission(blacklistTransaction, currentValueResolver: _ => current), Is.True);
        }

        [Test]
        public void ActivityProjectionGeneration_ACancelledLateResultCannotBecomeCurrentAfterBCompletes()
        {
            var registry = new V2JobGenerationRegistry();
            var sceneId = new SceneId(Guid.Parse("61000000-0000-0000-0000-000000000014"));
            var incarnationId = new IncarnationId(Guid.Parse("62000000-0000-0000-0000-000000000014"));
            var jobA = new OperationId(Guid.Parse("63000000-0000-0000-0000-000000000014"));
            var jobB = new OperationId(Guid.Parse("64000000-0000-0000-0000-000000000014"));

            V2JobIdentity identityA = registry.BeginJob(sceneId, incarnationId, V2JobType.ActivityProjection, jobA, inputGeneration: 1);
            V2JobAttempt attemptA = registry.BeginAttempt(identityA);
            Assert.That(registry.IsCurrent(attemptA), Is.True);
            Assert.That(registry.Cancel(identityA), Is.True);

            V2JobIdentity identityB = registry.BeginJob(sceneId, incarnationId, V2JobType.ActivityProjection, jobB, inputGeneration: 2);
            V2JobAttempt attemptB = registry.BeginAttempt(identityB);
            Assert.That(identityB.Generation, Is.GreaterThan(identityA.Generation));
            Assert.That(registry.IsCurrent(attemptA), Is.False);
            Assert.That(registry.IsCurrent(attemptB), Is.True);
            Assert.That(registry.Cancel(identityB), Is.True);
            Assert.That(registry.IsCurrent(attemptA), Is.False, "A's late native result must stay stale after B reaches terminal state.");
            Assert.That(registry.IsCurrent(attemptB), Is.False);
        }

        private static V2SiteConfigurationAssignment Assignment(ColumnId columnId, SiteId siteId, bool blacklisted, bool highlighted)
        {
            return new V2SiteConfigurationAssignment(columnId, siteId, blacklisted, highlighted, 0.1f, 0.2f, 0.3f, 1f, Array.Empty<string>());
        }
    }
}
