using System;
using HBP.Sync;
using NUnit.Framework;

namespace HBP.Tests.Transfer.Scene
{
    [Category("Sync.SceneFocused")]
    public class ActivityProjectionTests
    {
        [Test]
        public void ActivityProjectionControl_RoundTripsBoundedGenerationAndProgressFields()
        {
            var jobId = new OperationId(Guid.Parse("60000000-0000-0000-0000-000000000014"));
            var started = new ActivityProjectionControl(ActivityProjectionControlKind.Started, jobId, generation: 7, inputGeneration: 19, canonicalSequence: 43, projectionRequested: true, automaticPolicyEnabled: true, explicitRequest: false);

            byte[] encoded = ActivityProjectionControlCodec.Encode(started);
            Assert.That(encoded.Length, Is.LessThanOrEqualTo(ActivityProjectionControlCodec.MaximumPayloadBytes));
            Assert.That(ActivityProjectionControlCodec.TryDecode(encoded, out ActivityProjectionControl decoded), Is.True);
            Assert.That(decoded.Kind, Is.EqualTo(started.Kind));
            Assert.That(decoded.JobId, Is.EqualTo(jobId));
            Assert.That(decoded.Generation, Is.EqualTo(7));
            Assert.That(decoded.InputGeneration, Is.EqualTo(19));
            Assert.That(decoded.CanonicalSequence, Is.EqualTo(43));
            Assert.That(decoded.ProjectionRequested, Is.True);
            Assert.That(decoded.AutomaticPolicyEnabled, Is.True);

            var progress = new ActivityProjectionControl(ActivityProjectionControlKind.Progress, jobId, 7, 19, progress: 0.625f, message: "column 2 of 4");
            Assert.That(ActivityProjectionControlCodec.TryDecode(ActivityProjectionControlCodec.Encode(progress), out ActivityProjectionControl decodedProgress), Is.True);
            Assert.That(decodedProgress.Progress, Is.EqualTo(0.625f));
            Assert.That(decodedProgress.Message, Is.EqualTo("column 2 of 4"));

            byte[] truncated = ActivityProjectionControlCodec.Encode(started);
            Array.Resize(ref truncated, truncated.Length - 1);
            Assert.That(ActivityProjectionControlCodec.TryDecode(truncated, out _), Is.False);

            var idleRemoval = new ActivityProjectionControl(ActivityProjectionControlKind.RemoveRequest, jobId, generation: 0, inputGeneration: 20, canonicalSequence: 44);
            Assert.That(ActivityProjectionControlCodec.TryDecode(ActivityProjectionControlCodec.Encode(idleRemoval), out ActivityProjectionControl decodedRemoval), Is.True);
            Assert.That(decodedRemoval.Kind, Is.EqualTo(ActivityProjectionControlKind.RemoveRequest));
            Assert.That(decodedRemoval.Generation, Is.Zero, "Idle removal must not depend on an active projection generation.");
            Assert.That(decodedRemoval.InputGeneration, Is.EqualTo(20UL));
            Assert.That(decodedRemoval.CanonicalSequence, Is.EqualTo(44UL));
            Assert.That(decodedRemoval.ProjectionRequested, Is.False);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void ActivityProjectionTerminalBarrier_WaitsForBothPeersInEitherArrivalOrder(bool localFirst)
        {
            var barrier = new ActivityProjectionTerminalBarrier();
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
            Assert.That(HBP.Sync.Scene.ActivityProjectionAdmission.RequiresSensitiveAdmission(new SetSiteBlacklist(columnId, siteId, true)), Is.True);
            Assert.That(HBP.Sync.Scene.ActivityProjectionAdmission.RequiresSensitiveAdmission(new SetInfluenceDistance(columnId, 25f)), Is.True);
            Assert.That(HBP.Sync.Scene.ActivityProjectionAdmission.RequiresSensitiveAdmission(new SetSelectedColumn(columnId)), Is.False);
            Assert.That(HBP.Sync.Scene.ActivityProjectionAdmission.RequiresSensitiveAdmission(new SetSceneBoolean(SceneBooleanProperty.ShowAllSites, true), new SetSceneBoolean(SceneBooleanProperty.ShowAllSites, false)), Is.True);
            Assert.That(HBP.Sync.Scene.ActivityProjectionAdmission.RequiresSensitiveAdmission(new SetSceneBoolean(SceneBooleanProperty.ShowAllSites, false), new SetSceneBoolean(SceneBooleanProperty.ShowAllSites, false)), Is.False);

            var current = new SetSiteConfigurationBatch(new[] { Assignment(columnId, siteId, blacklisted: false, highlighted: true) });
            var appearanceOnly = new SetSiteConfigurationBatch(new[] { Assignment(columnId, siteId, blacklisted: false, highlighted: false) });
            var blacklistChange = new SetSiteConfigurationBatch(new[] { Assignment(columnId, siteId, blacklisted: true, highlighted: true) });
            Assert.That(HBP.Sync.Scene.ActivityProjectionAdmission.RequiresSensitiveAdmission(appearanceOnly, current), Is.False);
            Assert.That(HBP.Sync.Scene.ActivityProjectionAdmission.RequiresSensitiveAdmission(blacklistChange, current), Is.True);
            Assert.That(HBP.Sync.Scene.ActivityProjectionAdmission.RequiresSensitiveAdmission(new SetConfigurationTransaction(new HBP.Sync.Mutation[]
            {
                new SetSceneBoolean(SceneBooleanProperty.StrongCuts, true),
                new SetInfluenceDistance(columnId, 15f)
            })), Is.True);

            var appearanceOnlyTransaction = new SetConfigurationTransaction(new HBP.Sync.Mutation[] { appearanceOnly });
            var blacklistTransaction = new SetConfigurationTransaction(new HBP.Sync.Mutation[] { blacklistChange });
            Assert.That(HBP.Sync.Scene.ActivityProjectionAdmission.RequiresSensitiveAdmission(appearanceOnlyTransaction, currentValueResolver: _ => current), Is.False);
            Assert.That(HBP.Sync.Scene.ActivityProjectionAdmission.RequiresSensitiveAdmission(blacklistTransaction, currentValueResolver: _ => current), Is.True);
        }

        [Test]
        public void ActivityProjectionGeneration_ACancelledLateResultCannotBecomeCurrentAfterBCompletes()
        {
            var registry = new JobGenerationRegistry();
            var sceneId = new SceneId(Guid.Parse("61000000-0000-0000-0000-000000000014"));
            var incarnationId = new IncarnationId(Guid.Parse("62000000-0000-0000-0000-000000000014"));
            var jobA = new OperationId(Guid.Parse("63000000-0000-0000-0000-000000000014"));
            var jobB = new OperationId(Guid.Parse("64000000-0000-0000-0000-000000000014"));

            JobIdentity identityA = registry.BeginJob(sceneId, incarnationId, JobType.ActivityProjection, jobA, inputGeneration: 1);
            JobAttempt attemptA = registry.BeginAttempt(identityA);
            Assert.That(registry.IsCurrent(attemptA), Is.True);
            Assert.That(registry.Cancel(identityA), Is.True);

            JobIdentity identityB = registry.BeginJob(sceneId, incarnationId, JobType.ActivityProjection, jobB, inputGeneration: 2);
            JobAttempt attemptB = registry.BeginAttempt(identityB);
            Assert.That(identityB.Generation, Is.GreaterThan(identityA.Generation));
            Assert.That(registry.IsCurrent(attemptA), Is.False);
            Assert.That(registry.IsCurrent(attemptB), Is.True);
            Assert.That(registry.Cancel(identityB), Is.True);
            Assert.That(registry.IsCurrent(attemptA), Is.False, "A's late native result must stay stale after B reaches terminal state.");
            Assert.That(registry.IsCurrent(attemptB), Is.False);
        }

        private static SiteConfigurationAssignment Assignment(ColumnId columnId, SiteId siteId, bool blacklisted, bool highlighted)
        {
            return new SiteConfigurationAssignment(columnId, siteId, blacklisted, highlighted, 0.1f, 0.2f, 0.3f, 1f, Array.Empty<string>());
        }
    }
}
