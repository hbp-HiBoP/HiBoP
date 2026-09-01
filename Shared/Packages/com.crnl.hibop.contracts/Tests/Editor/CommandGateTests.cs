using System;
using NUnit.Framework;

namespace CRNL.HiBoP.Contracts.Tests
{
    public class CommandGateTests
    {
        private static readonly SessionEpoch s_Session = new(new ContractId(1, 1), 4);
        private static readonly ScopeKey s_Scope = new(ScopeType.Column, new ContractId(2, 2));

        [Test]
        public void MatchingBaseRevisionAllowsExecution()
        {
            CommandGateResult result = CommandGate.Evaluate(Command(new ScopeRevision(7)), s_Session, new StateRevision(20), Optional<CommandOutcome>.None, Optional<ScopeRevision>.Some(new ScopeRevision(7)));

            Assert.That(result.Disposition, Is.EqualTo(CommandGateDisposition.Execute));
            Assert.That(result.Outcome.HasValue, Is.False);
        }

        [Test]
        public void ConflictRejectsWithoutInventingResultingRevisions()
        {
            Command command = Command(new ScopeRevision(6));
            CommandGateResult result = CommandGate.Evaluate(command, s_Session, new StateRevision(20), Optional<CommandOutcome>.None, Optional<ScopeRevision>.Some(new ScopeRevision(7)));

            CommandOutcome outcome = result.Outcome.Value;
            Assert.That(result.Disposition, Is.EqualTo(CommandGateDisposition.ReturnOutcome));
            Assert.That(outcome.Accepted, Is.False);
            Assert.That(outcome.ResultingStateRevision.HasValue, Is.False);
            Assert.That(outcome.ResultingScopeRevision.HasValue, Is.False);
            Assert.That(outcome.Error.Value.Code, Is.EqualTo(ErrorCode.StateConflict));
            Assert.That(outcome.Error.Value.Retryable, Is.True);
            Assert.That(outcome.Error.Value.CurrentStateRevision.Value, Is.EqualTo(new StateRevision(20)));
            Assert.That(outcome.Error.Value.CurrentScopeRevision.Value, Is.EqualTo(new ScopeRevision(7)));
        }

        [Test]
        public void DuplicateReturnsRecordedOutcomeBeforeCheckingAdvancedRevision()
        {
            Command command = Command(new ScopeRevision(7));
            ContractValue canonicalValue = ContractValue.FromNumber(0.25);
            CommandOutcome recorded = CommandOutcome.Accept(command.CommandId, new StateRevision(21), new ScopeRevision(8), Optional<ContractValue>.Some(canonicalValue));

            CommandGateResult duplicate = CommandGate.Evaluate(command, s_Session, new StateRevision(99), Optional<CommandOutcome>.Some(recorded), Optional<ScopeRevision>.Some(new ScopeRevision(42)));

            Assert.That(duplicate.Disposition, Is.EqualTo(CommandGateDisposition.ReturnOutcome));
            Assert.That(duplicate.Outcome.Value, Is.SameAs(recorded));
            Assert.That(duplicate.Outcome.Value.CanonicalValue.Value, Is.SameAs(canonicalValue));
            Assert.That(duplicate.Outcome.Value.ToString(), Does.Not.Contain("0.25"));
        }

        [Test]
        public void DifferentEpochIsRejectedAsNonRetryable()
        {
            SessionEpoch oldSession = new(s_Session.SessionId, s_Session.Epoch - 1);
            Command command = Command(new ScopeRevision(7), oldSession);

            CommandOutcome outcome = CommandGate.Evaluate(command, s_Session, new StateRevision(20), Optional<CommandOutcome>.None, Optional<ScopeRevision>.Some(new ScopeRevision(7))).Outcome.Value;

            Assert.That(outcome.Error.Value.Code, Is.EqualTo(ErrorCode.SessionReplaced));
            Assert.That(outcome.Error.Value.Retryable, Is.False);
        }

        [Test]
        public void MissingScopeIsExplicitAndRetryableAfterReconciliation()
        {
            CommandOutcome outcome = CommandGate.Evaluate(Command(new ScopeRevision(0)), s_Session, new StateRevision(20), Optional<CommandOutcome>.None, Optional<ScopeRevision>.None).Outcome.Value;

            Assert.That(outcome.Error.Value.Code, Is.EqualTo(ErrorCode.ScopeNotFound));
            Assert.That(outcome.Error.Value.Retryable, Is.True);
        }

        [Test]
        public void InteractionIdAndSequenceAreAnAtomicOptionalPair()
        {
            Assert.Throws<ArgumentException>(() => _ = new Command(s_Session, new ContractId(3, 3), new ContractId(4, 4), s_Scope, new ScopeRevision(0), CommandKind.SetCut, ContractValue.None, interactionId: Optional<ContractId>.Some(new ContractId(5, 5))));
        }

        private static Command Command(ScopeRevision baseRevision, SessionEpoch? session = null)
        {
            return new Command(session ?? s_Session, new ContractId(3, 3), new ContractId(4, 4), s_Scope, baseRevision, CommandKind.SetOpacity, ContractValue.FromNumber(0.25));
        }
    }
}
