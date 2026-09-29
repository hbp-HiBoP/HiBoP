using System;
using System.Collections.Generic;
using HBP.Sync;
using HBP.Transfer.Transport;

namespace HBP.Sync.Scene
{
    public readonly struct V2TimelineAnchorTimingEstimate
    {
        public double AgeSeconds { get; }
        public double UncertaintySeconds { get; }

        public V2TimelineAnchorTimingEstimate(double ageSeconds, double uncertaintySeconds)
        {
            if (double.IsNaN(ageSeconds) || double.IsInfinity(ageSeconds) || ageSeconds < 0d) throw new ArgumentOutOfRangeException(nameof(ageSeconds));
            if (double.IsNaN(uncertaintySeconds) || double.IsInfinity(uncertaintySeconds) || uncertaintySeconds < 0d) throw new ArgumentOutOfRangeException(nameof(uncertaintySeconds));
            AgeSeconds = ageSeconds;
            UncertaintySeconds = uncertaintySeconds;
        }
    }

    /// <summary>Bounded four-timestamp clock estimator used only while applying playing timeline anchors.</summary>
    public sealed class V2TimelineClockEstimator
    {
        public static readonly TimeSpan ProbeInterval = TimeSpan.FromMilliseconds(250);
        public static readonly TimeSpan SampleWindow = TimeSpan.FromSeconds(2);
        public static readonly TimeSpan MaximumSampleAge = TimeSpan.FromSeconds(1);
        public const int MinimumSamples = 3;
        public const double MaximumUncertaintyInSamples = 0.5d;
        private const int MaximumRetainedSamples = 32;

        private readonly object m_Gate = new object();
        private readonly IMonotonicClock m_Clock;
        private readonly List<EstimateSample> m_Samples = new(MaximumRetainedSamples);
        private long m_LastSentTicks = -1;
        private long m_LastReceivedTicks = -1;

        public int RecentSampleCount
        {
            get
            {
                lock (m_Gate)
                {
                    Prune(m_Clock.GetTimestamp());
                    return m_Samples.Count;
                }
            }
        }

        public V2TimelineClockEstimator(IMonotonicClock clock)
        {
            m_Clock = clock ?? throw new ArgumentNullException(nameof(clock));
            if (clock.Frequency <= 0 || clock.Frequency > 1000000000000L) throw new ArgumentOutOfRangeException(nameof(clock));
        }

        public bool AddSample(V2ClockProbeSample sample)
        {
            if (sample == null) throw new ArgumentNullException(nameof(sample));
            double localElapsed = (double)(sample.LocalReceiveTicks - sample.LocalSendTicks) / sample.LocalTickFrequency;
            double remoteElapsed = (double)(sample.RemoteSendTicks - sample.RemoteReceiveTicks) / sample.RemoteTickFrequency;
            double roundTrip = localElapsed - remoteElapsed;
            if (roundTrip < 0d || double.IsNaN(roundTrip) || double.IsInfinity(roundTrip)) return false;

            double localSend = (double)sample.LocalSendTicks / sample.LocalTickFrequency;
            double localReceive = (double)sample.LocalReceiveTicks / sample.LocalTickFrequency;
            double remoteReceive = (double)sample.RemoteReceiveTicks / sample.RemoteTickFrequency;
            double remoteSend = (double)sample.RemoteSendTicks / sample.RemoteTickFrequency;
            double remoteMinusLocalOffset = ((remoteReceive - localSend) + (remoteSend - localReceive)) * 0.5d;
            double uncertainty = roundTrip * 0.5d + 0.5d / sample.LocalTickFrequency + 0.5d / sample.RemoteTickFrequency;
            long now = m_Clock.GetTimestamp();
            long ageAtAdmission = now - sample.LocalReceiveTicks;
            if (ageAtAdmission < 0 || (double)ageAtAdmission / m_Clock.Frequency > SampleWindow.TotalSeconds) return false;

            lock (m_Gate)
            {
                if (sample.LocalTickFrequency != (ulong)m_Clock.Frequency || sample.LocalSendTicks <= m_LastSentTicks || sample.LocalReceiveTicks <= m_LastReceivedTicks)
                    return false;
                m_LastSentTicks = sample.LocalSendTicks;
                m_LastReceivedTicks = sample.LocalReceiveTicks;
                m_Samples.Add(new EstimateSample(sample.LocalReceiveTicks, remoteMinusLocalOffset, roundTrip, uncertainty));
                if (m_Samples.Count > MaximumRetainedSamples) m_Samples.RemoveAt(0);
                Prune(now);
                return true;
            }
        }

        /// <summary>Retains transport liveness while admitting clock samples only during active playback.</summary>
        public bool AddSampleIfPlaying(V2ClockProbeSample sample, bool timelinePlaying)
        {
            if (sample == null) throw new ArgumentNullException(nameof(sample));
            return timelinePlaying && AddSample(sample);
        }

        public bool TryEstimate(long remoteAnchorTicks, ulong remoteTickFrequency, int samplesPerSecond, out V2TimelineAnchorTimingEstimate estimate)
        {
            estimate = default;
            if (remoteAnchorTicks < 0 || remoteTickFrequency == 0 || remoteTickFrequency > 1000000000000UL || samplesPerSecond <= 0) return false;
            long now = m_Clock.GetTimestamp();
            lock (m_Gate)
            {
                Prune(now);
                if (m_Samples.Count < MinimumSamples) return false;
                EstimateSample latest = m_Samples[m_Samples.Count - 1];
                double latestAge = (double)(now - latest.ReceivedTicks) / m_Clock.Frequency;
                if (latestAge < 0d || latestAge > MaximumSampleAge.TotalSeconds) return false;

                EstimateSample best = default;
                bool hasFreshSample = false;
                for (int i = 0; i < m_Samples.Count; i++)
                {
                    EstimateSample candidate = m_Samples[i];
                    double candidateAge = (double)(now - candidate.ReceivedTicks) / m_Clock.Frequency;
                    if (candidateAge < 0d || candidateAge > MaximumSampleAge.TotalSeconds)
                        continue;
                    if (!hasFreshSample || candidate.RoundTripSeconds < best.RoundTripSeconds)
                    {
                        best = candidate;
                        hasFreshSample = true;
                    }
                }

                if (!hasFreshSample) return false;

                if (best.UncertaintySeconds > MaximumUncertaintyInSamples / samplesPerSecond) return false;
                double localNowSeconds = (double)now / m_Clock.Frequency;
                double remoteAnchorSeconds = (double)remoteAnchorTicks / remoteTickFrequency;
                double age = localNowSeconds - remoteAnchorSeconds + best.RemoteMinusLocalOffsetSeconds;
                if (double.IsNaN(age) || double.IsInfinity(age) || age < 0d) return false;
                estimate = new V2TimelineAnchorTimingEstimate(age, best.UncertaintySeconds);
                return true;
            }
        }

        private void Prune(long now)
        {
            long windowTicks = checked((long)(SampleWindow.TotalSeconds * m_Clock.Frequency));
            for (int i = m_Samples.Count - 1; i >= 0; i--)
                if (now < m_Samples[i].ReceivedTicks || now - m_Samples[i].ReceivedTicks > windowTicks)
                    m_Samples.RemoveAt(i);
        }

        private readonly struct EstimateSample
        {
            public long ReceivedTicks { get; }
            public double RemoteMinusLocalOffsetSeconds { get; }
            public double RoundTripSeconds { get; }
            public double UncertaintySeconds { get; }

            public EstimateSample(long receivedTicks, double remoteMinusLocalOffsetSeconds, double roundTripSeconds, double uncertaintySeconds)
            {
                ReceivedTicks = receivedTicks;
                RemoteMinusLocalOffsetSeconds = remoteMinusLocalOffsetSeconds;
                RoundTripSeconds = roundTripSeconds;
                UncertaintySeconds = uncertaintySeconds;
            }
        }
    }
}
