namespace HBP.Sync
{
    /// <summary>
    /// Raw receive facts for one local application attempt. The receive-loop owner publishes after
    /// success or failure. It does not turn a rendering callback into scientific-stability evidence.
    /// Allocate only when capture is enabled, before entering the measured apply interval.
    /// </summary>
    public sealed class SyncReceiveTelemetry
    {
        public SyncTelemetryPoint FirstReceived { get; }
        public SyncTelemetryPoint LastReceived { get; }
        public long PayloadBytes { get; }
        public SyncTelemetryPoint ApplyStart { get; private set; }
        public SyncTelemetryPoint ApplyEnd { get; private set; }
        public SyncTelemetryPoint NextVisible { get; private set; }

        public SyncReceiveTelemetry(SyncTelemetryPoint firstReceived, SyncTelemetryPoint lastReceived, long payloadBytes)
        {
            FirstReceived = firstReceived;
            LastReceived = lastReceived;
            PayloadBytes = payloadBytes;
        }

        public void CaptureApplyStart() => ApplyStart = SyncTelemetry.CapturePoint();
        public void CaptureApplyEnd() => ApplyEnd = SyncTelemetry.CapturePoint();
        public void CaptureNextVisible() => NextVisible = SyncTelemetry.CapturePoint();
        public void CaptureNextVisible(SyncTelemetryPoint point) => NextVisible = point;

        public void Publish(SyncProfile profile, SyncTelemetryIdentity identity)
        {
            SyncTelemetry.MarkAt(profile, identity, SyncMilestone.FirstByteReceived, FirstReceived, PayloadBytes);
            SyncTelemetry.MarkAt(profile, identity, SyncMilestone.LastByteReceived, LastReceived, PayloadBytes);
            SyncTelemetry.MarkAt(profile, identity, SyncMilestone.ApplyStart, ApplyStart);
            SyncTelemetry.MarkAt(profile, identity, SyncMilestone.ApplyEnd, ApplyEnd);
            SyncTelemetry.MarkAt(profile, identity, SyncMilestone.NextVisible, NextVisible);
        }
    }
}
