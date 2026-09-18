using System;
using HBP.Sync;

namespace HBP.Sync.Scene
{
    /// <summary>Map sender-local monotonic timeline anchors into the receiver's clock.</summary>
    public static class ReplicaClock
    {
        public static StateSnapshot ToLocalClock(StateSnapshot wireState, float senderSampleTime, float receiverTime)
        {
            if (float.IsNaN(senderSampleTime) || float.IsInfinity(senderSampleTime) || senderSampleTime < 0 || float.IsNaN(receiverTime) || float.IsInfinity(receiverTime) || receiverTime < 0)
                throw new ArgumentOutOfRangeException(nameof(senderSampleTime));
            var fields = wireState.Fields;
            foreach (var entry in wireState.Fields)
            {
                if (entry.Key.Entity != EntityKind.Column || entry.Key.FieldId != 32) continue;
                var playing = new StateKey(EntityKind.Column, entry.Key.ParentId, entry.Key.Id, 28);
                if (!fields.TryGetValue(playing, out byte[] value) || value.Length != 1 || value[0] == 0) continue;
                float sourceAnchor = BitConverter.ToSingle(entry.Value, 0);
                float age = Math.Max(0f, senderSampleTime - sourceAnchor);
                fields[entry.Key] = StateValue.Float(Math.Max(0f, receiverTime - age));
            }

            return wireState.WithFields(fields, wireState.CommonRevision);
        }
    }
}
