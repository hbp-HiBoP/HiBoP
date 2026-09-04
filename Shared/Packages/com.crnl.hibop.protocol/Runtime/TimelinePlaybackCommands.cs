using System;
using CRNL.HiBoP.Contracts;

namespace CRNL.HiBoP.Protocol
{
    public enum TimelinePlaybackAction : byte
    {
        Play = 1,
        Pause = 2,
        Scrub = 3,
    }

    public readonly struct TimelinePlaybackIntent
    {
        public TimelinePlaybackIntent(TimelinePlaybackAction action, double logicalTime, double speed)
        {
            if (action < TimelinePlaybackAction.Play || action > TimelinePlaybackAction.Scrub)
                throw new ArgumentOutOfRangeException(nameof(action));
            if (double.IsNaN(logicalTime) || double.IsInfinity(logicalTime) || logicalTime < 0d)
                throw new ArgumentOutOfRangeException(nameof(logicalTime));
            if (double.IsNaN(speed) || double.IsInfinity(speed) || speed <= 0d)
                throw new ArgumentOutOfRangeException(nameof(speed));
            Action = action;
            LogicalTime = logicalTime;
            Speed = speed;
        }

        public TimelinePlaybackAction Action { get; }
        public double LogicalTime { get; }
        public double Speed { get; }

        public ContractValue ToContractValue() => ContractValue.FromNumbers(new[] { (double)Action, LogicalTime, Speed });

        public static bool TryParse(ContractValue value, out TimelinePlaybackIntent intent)
        {
            intent = default;
            if (value == null || value.Kind != ContractValueKind.NumberVector || value.Numbers.Count != 3)
                return false;
            double actionValue = value.Numbers[0];
            if (actionValue != Math.Truncate(actionValue) || actionValue < (double)TimelinePlaybackAction.Play || actionValue > (double)TimelinePlaybackAction.Scrub)
                return false;
            try
            {
                intent = new TimelinePlaybackIntent((TimelinePlaybackAction)actionValue, value.Numbers[1], value.Numbers[2]);
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }
    }

    public static class TimelinePlaybackCommands
    {
        public static Command Create(SessionEpoch session, ContractId commandId, ContractId correlationId, ScopeKey timelineScope, ScopeRevision baseRevision, TimelinePlaybackIntent intent, ContractId interactionId, InteractionSequence sequence)
        {
            if (timelineScope.Type != ScopeType.Timeline || timelineScope.Owner != ScopeOwner.Desktop)
                throw new ArgumentException("Timeline playback is owned by a Desktop timeline scope.", nameof(timelineScope));
            return new Command(session, commandId, correlationId, timelineScope, baseRevision, CommandKind.SetTimelinePlayback, intent.ToContractValue(), 1, Optional<ContractId>.Some(interactionId), Optional<InteractionSequence>.Some(sequence));
        }

        public static bool TryRead(Command command, out TimelinePlaybackIntent intent)
        {
            intent = default;
            return command != null && command.Kind == CommandKind.SetTimelinePlayback && command.Scope.Type == ScopeType.Timeline && command.PayloadVersion == 1 && TimelinePlaybackIntent.TryParse(command.Payload, out intent);
        }
    }
}
