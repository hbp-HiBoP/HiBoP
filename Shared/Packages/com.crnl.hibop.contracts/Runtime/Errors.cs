using System;

namespace CRNL.HiBoP.Contracts
{
    public enum ErrorCode : ushort
    {
        Unknown = 0,
        AuthFailed = 1,
        IdentityChanged = 2,
        ProtocolIncompatible = 3,
        SchemaIncompatible = 4,
        StateConflict = 5,
        ScopeNotFound = 6,
        AssetMissing = 7,
        HashMismatch = 8,
        CommandInvalid = 9,
        ComputeFailed = 10,
        ResourcePressure = 11,
        RateLimited = 12,
        SessionReplaced = 13,
        TransportFailure = 14,
        SessionBusy = 15,
    }

    public sealed class ContractError : IEquatable<ContractError>
    {
        public ContractError(ErrorCode code, ContractId correlationId, bool retryable, Optional<StateRevision> currentStateRevision = default, Optional<ScopeRevision> currentScopeRevision = default)
        {
            if (code <= ErrorCode.Unknown || code > ErrorCode.SessionBusy)
                throw new ArgumentOutOfRangeException(nameof(code));
            if (!correlationId.IsValid)
                throw new ArgumentException("A valid correlation identifier is required.", nameof(correlationId));
            if (code == ErrorCode.StateConflict && (!currentStateRevision.HasValue || !currentScopeRevision.HasValue))
            {
                throw new ArgumentException("A state conflict must report the current global and scope revisions.", nameof(currentStateRevision));
            }

            Code = code;
            CorrelationId = correlationId;
            Retryable = retryable;
            CurrentStateRevision = currentStateRevision;
            CurrentScopeRevision = currentScopeRevision;
        }

        public ErrorCode Code { get; }

        public ContractId CorrelationId { get; }

        public bool Retryable { get; }

        public Optional<StateRevision> CurrentStateRevision { get; }

        public Optional<ScopeRevision> CurrentScopeRevision { get; }

        public static ContractError StateConflict(ContractId correlationId, StateRevision currentStateRevision, ScopeRevision currentScopeRevision)
        {
            return new ContractError(ErrorCode.StateConflict, correlationId, true, Optional<StateRevision>.Some(currentStateRevision), Optional<ScopeRevision>.Some(currentScopeRevision));
        }

        public bool Equals(ContractError other)
        {
            return !ReferenceEquals(other, null) && Code == other.Code && CorrelationId.Equals(other.CorrelationId) && Retryable == other.Retryable && CurrentStateRevision.Equals(other.CurrentStateRevision) && CurrentScopeRevision.Equals(other.CurrentScopeRevision);
        }

        public override bool Equals(object obj) => Equals(obj as ContractError);

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = (int)Code;
                hash = (hash * 397) ^ CorrelationId.GetHashCode();
                hash = (hash * 397) ^ Retryable.GetHashCode();
                hash = (hash * 397) ^ CurrentStateRevision.GetHashCode();
                return (hash * 397) ^ CurrentScopeRevision.GetHashCode();
            }
        }

        public override string ToString()
        {
            return $"ContractError(code={Code}, correlation={CorrelationId}, retryable={Retryable})";
        }
    }
}
