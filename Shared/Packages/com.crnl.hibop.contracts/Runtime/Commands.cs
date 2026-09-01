using System;

namespace CRNL.HiBoP.Contracts
{
    public enum CommandKind : ushort
    {
        Unknown = 0,
        SelectSite = 1,
        SelectColumn = 2,
        SetRepresentation = 3,
        SetLayers = 4,
        SetOpacity = 5,
        SetThresholds = 6,
        SetCut = 7,
        SetRoi = 8,
        SetTimelinePlayback = 9,
        RequestBrainInstance = 10,
        CloseBrainInstance = 11,
    }

    public sealed class Command
    {
        public Command(SessionEpoch session, ContractId commandId, ContractId correlationId, ScopeKey scope, ScopeRevision baseScopeRevision, CommandKind kind, ContractValue payload, ushort payloadVersion = 1, Optional<ContractId> interactionId = default, Optional<InteractionSequence> sequence = default)
        {
            if (!session.IsValid)
                throw new ArgumentException("A valid session epoch is required.", nameof(session));
            if (!commandId.IsValid)
                throw new ArgumentException("A valid command identifier is required.", nameof(commandId));
            if (!correlationId.IsValid)
                throw new ArgumentException("A valid correlation identifier is required.", nameof(correlationId));
            if (!scope.IsValid)
                throw new ArgumentException("A valid scope is required.", nameof(scope));
            if (kind <= CommandKind.Unknown || kind > CommandKind.CloseBrainInstance)
                throw new ArgumentOutOfRangeException(nameof(kind));
            if (payload == null)
                throw new ArgumentNullException(nameof(payload));
            if (payloadVersion == 0)
                throw new ArgumentOutOfRangeException(nameof(payloadVersion));
            if (interactionId.HasValue != sequence.HasValue)
                throw new ArgumentException("Interaction identifier and sequence must both be present or both be absent.");
            if (interactionId.HasValue && !interactionId.Value.IsValid)
                throw new ArgumentException("A valid interaction identifier is required.", nameof(interactionId));
            if (sequence.HasValue && !sequence.Value.IsValid)
                throw new ArgumentException("A valid interaction sequence is required.", nameof(sequence));

            Session = session;
            CommandId = commandId;
            CorrelationId = correlationId;
            Scope = scope;
            BaseScopeRevision = baseScopeRevision;
            Kind = kind;
            Payload = payload;
            PayloadVersion = payloadVersion;
            InteractionId = interactionId;
            Sequence = sequence;
        }

        public SessionEpoch Session { get; }

        public ContractId CommandId { get; }

        public ContractId CorrelationId { get; }

        public ScopeKey Scope { get; }

        public ScopeRevision BaseScopeRevision { get; }

        public CommandKind Kind { get; }

        public ContractValue Payload { get; }

        public ushort PayloadVersion { get; }

        public Optional<ContractId> InteractionId { get; }

        public Optional<InteractionSequence> Sequence { get; }

        public override string ToString()
        {
            return $"Command(id={CommandId}, kind={Kind}, scope={Scope}, base={BaseScopeRevision}, payloadKind={Payload.Kind})";
        }
    }

    public enum CommandOutcomeKind : byte
    {
        Unknown = 0,
        Accepted = 1,
        Rejected = 2,
    }

    public sealed class CommandOutcome : IEquatable<CommandOutcome>
    {
        private CommandOutcome(ContractId commandId, CommandOutcomeKind kind, Optional<StateRevision> resultingStateRevision, Optional<ScopeRevision> resultingScopeRevision, Optional<ContractValue> canonicalValue, Optional<ContractError> error)
        {
            CommandId = commandId;
            Kind = kind;
            ResultingStateRevision = resultingStateRevision;
            ResultingScopeRevision = resultingScopeRevision;
            CanonicalValue = canonicalValue;
            Error = error;
        }

        public ContractId CommandId { get; }

        public CommandOutcomeKind Kind { get; }

        public bool Accepted => Kind == CommandOutcomeKind.Accepted;

        public Optional<StateRevision> ResultingStateRevision { get; }

        public Optional<ScopeRevision> ResultingScopeRevision { get; }

        public Optional<ContractValue> CanonicalValue { get; }

        public Optional<ContractError> Error { get; }

        public static CommandOutcome Accept(ContractId commandId, StateRevision resultingStateRevision, ScopeRevision resultingScopeRevision, Optional<ContractValue> canonicalValue = default)
        {
            EnsureCommandId(commandId);
            return new CommandOutcome(commandId, CommandOutcomeKind.Accepted, Optional<StateRevision>.Some(resultingStateRevision), Optional<ScopeRevision>.Some(resultingScopeRevision), canonicalValue, Optional<ContractError>.None);
        }

        public static CommandOutcome Reject(ContractId commandId, ContractError error, Optional<ContractValue> canonicalValue = default)
        {
            EnsureCommandId(commandId);
            if (error == null)
                throw new ArgumentNullException(nameof(error));
            return new CommandOutcome(commandId, CommandOutcomeKind.Rejected, Optional<StateRevision>.None, Optional<ScopeRevision>.None, canonicalValue, Optional<ContractError>.Some(error));
        }

        public bool Equals(CommandOutcome other)
        {
            return !ReferenceEquals(other, null) && CommandId.Equals(other.CommandId) && Kind == other.Kind && ResultingStateRevision.Equals(other.ResultingStateRevision) && ResultingScopeRevision.Equals(other.ResultingScopeRevision) && CanonicalValue.Equals(other.CanonicalValue) && Error.Equals(other.Error);
        }

        public override bool Equals(object obj) => Equals(obj as CommandOutcome);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = CommandId.GetHashCode();
                hash = (hash * 397) ^ (int)Kind;
                hash = (hash * 397) ^ ResultingStateRevision.GetHashCode();
                hash = (hash * 397) ^ ResultingScopeRevision.GetHashCode();
                hash = (hash * 397) ^ CanonicalValue.GetHashCode();
                return (hash * 397) ^ Error.GetHashCode();
            }
        }

        public override string ToString()
        {
            string errorCode = Error.HasValue ? Error.Value.Code.ToString() : "None";
            return $"CommandOutcome(id={CommandId}, kind={Kind}, error={errorCode})";
        }

        private static void EnsureCommandId(ContractId commandId)
        {
            if (!commandId.IsValid)
                throw new ArgumentException("A valid command identifier is required.", nameof(commandId));
        }
    }

    public enum CommandGateDisposition : byte
    {
        Unknown = 0,
        Execute = 1,
        ReturnOutcome = 2,
    }

    public sealed class CommandGateResult
    {
        private CommandGateResult(CommandGateDisposition disposition, Optional<CommandOutcome> outcome)
        {
            Disposition = disposition;
            Outcome = outcome;
        }

        public CommandGateDisposition Disposition { get; }

        public Optional<CommandOutcome> Outcome { get; }

        public static CommandGateResult Execute()
        {
            return new CommandGateResult(CommandGateDisposition.Execute, Optional<CommandOutcome>.None);
        }

        public static CommandGateResult Return(CommandOutcome outcome)
        {
            if (outcome == null)
                throw new ArgumentNullException(nameof(outcome));
            return new CommandGateResult(CommandGateDisposition.ReturnOutcome, Optional<CommandOutcome>.Some(outcome));
        }
    }

    /// <summary>
    /// Pure precondition evaluator. The owning adapter supplies its per-epoch idempotence lookup and current revisions.
    /// </summary>
    public static class CommandGate
    {
        public static CommandGateResult Evaluate(Command command, SessionEpoch currentSession, StateRevision currentStateRevision, Optional<CommandOutcome> priorOutcome, Optional<ScopeRevision> currentScopeRevision)
        {
            if (command == null)
                throw new ArgumentNullException(nameof(command));
            if (!currentSession.IsValid)
                throw new ArgumentException("A valid current session is required.", nameof(currentSession));

            if (command.Session != currentSession)
            {
                ContractError error = new(ErrorCode.SessionReplaced, command.CorrelationId, false);
                return CommandGateResult.Return(CommandOutcome.Reject(command.CommandId, error));
            }

            if (priorOutcome.HasValue)
            {
                if (priorOutcome.Value.CommandId != command.CommandId)
                    throw new ArgumentException("The prior outcome belongs to a different command.", nameof(priorOutcome));
                return CommandGateResult.Return(priorOutcome.Value);
            }

            if (!currentScopeRevision.HasValue)
            {
                ContractError error = new(ErrorCode.ScopeNotFound, command.CorrelationId, true);
                return CommandGateResult.Return(CommandOutcome.Reject(command.CommandId, error));
            }

            if (command.BaseScopeRevision != currentScopeRevision.Value)
            {
                ContractError error = ContractError.StateConflict(command.CorrelationId, currentStateRevision, currentScopeRevision.Value);
                return CommandGateResult.Return(CommandOutcome.Reject(command.CommandId, error));
            }

            return CommandGateResult.Execute();
        }
    }
}
