using System;
using CRNL.HiBoP.Contracts;
using CRNL.HiBoP.RenderModel;

namespace CRNL.HiBoP.Protocol
{
    public readonly struct CutPlaneIntent : IEquatable<CutPlaneIntent>
    {
        public CutPlaneIntent(Plane3F plane)
        {
            Plane = Normalize(plane);
        }

        public Plane3F Plane { get; }

        public ContractValue ToContractValue()
        {
            return ContractValue.FromNumbers(new double[] { Plane.Normal.X, Plane.Normal.Y, Plane.Normal.Z, Plane.Distance });
        }

        public bool Equals(CutPlaneIntent other)
        {
            return Plane.Normal.Equals(other.Plane.Normal) && Plane.Distance.Equals(other.Plane.Distance);
        }

        public override bool Equals(object obj) => obj is CutPlaneIntent other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Plane.Normal, Plane.Distance);

        public static bool TryParse(ContractValue value, out CutPlaneIntent intent)
        {
            intent = default;
            if (value == null || value.Kind != ContractValueKind.NumberVector || value.Numbers.Count != 4)
                return false;
            try
            {
                intent = new CutPlaneIntent(new Plane3F(new Float3((float)value.Numbers[0], (float)value.Numbers[1], (float)value.Numbers[2]), (float)value.Numbers[3]));
                return true;
            }
            catch (ArgumentException)
            {
                return false;
            }
        }

        private static Plane3F Normalize(Plane3F plane)
        {
            double squaredLength = (double)plane.Normal.X * plane.Normal.X + (double)plane.Normal.Y * plane.Normal.Y + (double)plane.Normal.Z * plane.Normal.Z;
            if (double.IsNaN(squaredLength) || double.IsInfinity(squaredLength) || squaredLength <= 0d)
                throw new ArgumentException("The cut plane normal must be finite and non-zero.", nameof(plane));
            float inverseLength = (float)(1d / Math.Sqrt(squaredLength));
            return new Plane3F(new Float3(plane.Normal.X * inverseLength, plane.Normal.Y * inverseLength, plane.Normal.Z * inverseLength), plane.Distance * inverseLength);
        }
    }

    public static class CutCommands
    {
        public static Command Create(SessionEpoch session, ContractId commandId, ContractId correlationId, ScopeKey cutScope, ScopeRevision baseRevision, CutPlaneIntent intent, ContractId interactionId, InteractionSequence sequence)
        {
            if (cutScope.Type != ScopeType.Cut || cutScope.Owner != ScopeOwner.Desktop)
                throw new ArgumentException("A cut command requires a Desktop-owned cut scope.", nameof(cutScope));
            return new Command(session, commandId, correlationId, cutScope, baseRevision, CommandKind.SetCut, intent.ToContractValue(), 1, Optional<ContractId>.Some(interactionId), Optional<InteractionSequence>.Some(sequence));
        }

        public static bool TryRead(Command command, out CutPlaneIntent intent)
        {
            intent = default;
            return command != null && command.Kind == CommandKind.SetCut && command.Scope.Type == ScopeType.Cut && command.Scope.Owner == ScopeOwner.Desktop && command.PayloadVersion == 1 && command.InteractionId.HasValue && command.Sequence.HasValue && CutPlaneIntent.TryParse(command.Payload, out intent);
        }
    }
}
