using System;

namespace HBP.Tests.Serialization
{
    internal sealed class SyntheticTimeSeriesDefinition
    {
        public SyntheticTimeSeriesDefinition(int patientCount, int channelsPerPatient, int trialCount, int recordingSampleCount, int windowSampleCount, int baselineSampleCount, int samplingFrequencyHz)
        {
            if (patientCount <= 0) throw new ArgumentOutOfRangeException(nameof(patientCount));
            if (channelsPerPatient <= 0) throw new ArgumentOutOfRangeException(nameof(channelsPerPatient));
            if (trialCount <= 0) throw new ArgumentOutOfRangeException(nameof(trialCount));
            if (recordingSampleCount <= 0) throw new ArgumentOutOfRangeException(nameof(recordingSampleCount));
            if (windowSampleCount <= 0) throw new ArgumentOutOfRangeException(nameof(windowSampleCount));
            if (baselineSampleCount < 0) throw new ArgumentOutOfRangeException(nameof(baselineSampleCount));
            if (samplingFrequencyHz <= 0) throw new ArgumentOutOfRangeException(nameof(samplingFrequencyHz));

            PatientCount = patientCount;
            ChannelsPerPatient = channelsPerPatient;
            TrialCount = trialCount;
            RecordingSampleCount = recordingSampleCount;
            WindowSampleCount = windowSampleCount;
            BaselineSampleCount = baselineSampleCount;
            SamplingFrequencyHz = samplingFrequencyHz;
        }

        public int PatientCount { get; }
        public int ChannelsPerPatient { get; }
        public int TrialCount { get; }
        public int RecordingSampleCount { get; }
        public int WindowSampleCount { get; }
        public int BaselineSampleCount { get; }
        public int SamplingFrequencyHz { get; }

        public long ManagedRawSignalBytes => Bytes(PatientCount, ChannelsPerPatient, RecordingSampleCount);
        public long ManagedEpochBytes => Bytes(PatientCount, ChannelsPerPatient, TrialCount, checked(WindowSampleCount + BaselineSampleCount));
        public long ManagedDerivedBytes => Bytes(PatientCount, ChannelsPerPatient, TrialCount, WindowSampleCount);

        private static long Bytes(params int[] dimensions)
        {
            long count = sizeof(float);
            foreach (int dimension in dimensions)
            {
                count = checked(count * dimension);
            }

            return count;
        }
    }

    internal static class SyntheticTimeSeriesFactory
    {
        public const float MinimumValue = -128.0f;
        public const float MaximumValue = 127.99609375f;

        public static int InclusiveSampleCount(int durationMilliseconds, int samplingFrequencyHz)
        {
            if (durationMilliseconds < 0) throw new ArgumentOutOfRangeException(nameof(durationMilliseconds));
            if (samplingFrequencyHz <= 0) throw new ArgumentOutOfRangeException(nameof(samplingFrequencyHz));

            return checked((int)Math.Floor(durationMilliseconds * (double)samplingFrequencyHz / 1000.0) + 1);
        }

        public static float ValueAt(int patient, int channel, int trial, int sampleIndex)
        {
            return (SampleCode(patient, channel, trial, sampleIndex) - 32768) / 256.0f;
        }

        public static float[] CreateTrial(SyntheticTimeSeriesDefinition definition, int patient, int channel, int trial)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            ValidateIndex(patient, definition.PatientCount, nameof(patient));
            ValidateIndex(channel, definition.ChannelsPerPatient, nameof(channel));
            ValidateIndex(trial, definition.TrialCount, nameof(trial));

            float[] values = new float[definition.WindowSampleCount];
            for (int sample = 0; sample < values.Length; ++sample)
            {
                values[sample] = ValueAt(patient, channel, trial, sample);
            }

            return values;
        }

        public static ulong ComputeChecksum(SyntheticTimeSeriesDefinition definition)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));

            ulong checksum = 1469598103934665603UL;
            for (int patient = 0; patient < definition.PatientCount; ++patient)
            {
                for (int channel = 0; channel < definition.ChannelsPerPatient; ++channel)
                {
                    for (int trial = 0; trial < definition.TrialCount; ++trial)
                    {
                        for (int sample = 0; sample < definition.WindowSampleCount; ++sample)
                        {
                            checksum = (checksum ^ (uint)SampleCode(patient, channel, trial, sample)) * 1099511628211UL;
                        }
                    }
                }
            }

            return checksum;
        }

        private static int SampleCode(int patient, int channel, int trial, int sampleIndex)
        {
            if (patient < 0) throw new ArgumentOutOfRangeException(nameof(patient));
            if (channel < 0) throw new ArgumentOutOfRangeException(nameof(channel));
            if (trial < 0) throw new ArgumentOutOfRangeException(nameof(trial));
            if (sampleIndex < 0) throw new ArgumentOutOfRangeException(nameof(sampleIndex));

            uint hash = 2166136261U;
            hash = Mix(hash, (uint)patient);
            hash = Mix(hash, (uint)channel);
            hash = Mix(hash, (uint)trial);
            hash = Mix(hash, (uint)sampleIndex);
            return (int)((hash ^ (hash >> 16)) & 0xFFFFU);
        }

        private static uint Mix(uint hash, uint value)
        {
            hash = (hash ^ value) * 16777619U;
            hash = (hash ^ (value >> 16)) * 16777619U;
            return hash;
        }

        private static void ValidateIndex(int value, int count, string name)
        {
            if (value < 0 || value >= count) throw new ArgumentOutOfRangeException(name);
        }
    }
}
