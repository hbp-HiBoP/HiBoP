using System;
using System.Collections.Generic;
using System.Linq;
using HBP.Core.DLL;
using HBP.Core.Exceptions;

namespace HBP.Core.Data
{
    public class SubTrial
    {
        private float[] m_CompactSource;
        private EpochChannelLayout m_ChannelLayout;
        private int m_CompactChannelLength;
        private Treatment[] m_Treatments;
        private Tools.Frequency m_Frequency;
        private Dictionary<Event, EventInformation> m_EmptyInformationsByEvent;
        private EpochRange m_StoredWindow;
        private EpochRange m_StoredBaseline;
        private bool[] m_NormalizedChannels;
        private long m_ManagedBytes;

        #region Properties

        public bool Found { get; private set; }
        public EpochDescriptor Descriptor { get; private set; }
        public Dictionary<Event, EventInformation> InformationsByEvent => Descriptor?.InformationsByEvent ?? m_EmptyInformationsByEvent;
        public Dictionary<string, string> UnitByChannel { get; private set; }
        public Dictionary<string, float[]> ValuesByChannel { get; set; }
        public IEnumerable<string> Channels => m_ChannelLayout?.Channels ?? Array.Empty<string>();

        #endregion

        #region Constructors

        public SubTrial(bool found)
        {
            Found = found;
            Descriptor = null;
            m_CompactSource = Array.Empty<float>();
            m_ChannelLayout = null;
            UnitByChannel = new Dictionary<string, string>();
            ValuesByChannel = new Dictionary<string, float[]>();
            m_Treatments = Array.Empty<Treatment>();
            m_Frequency = new Tools.Frequency();
            m_EmptyInformationsByEvent = new Dictionary<Event, EventInformation>();
            m_NormalizedChannels = Array.Empty<bool>();
        }

        public SubTrial(Dictionary<string, float[]> sourceByChannel, Dictionary<string, string> unitByChannel, EpochDescriptor descriptor, SubBloc subBloc, Tools.Frequency frequency) : this(sourceByChannel, unitByChannel, descriptor, subBloc, frequency, new EpochCompatibilityBuffer())
        {
        }

        internal SubTrial(Dictionary<string, float[]> sourceByChannel, Dictionary<string, string> unitByChannel, EpochDescriptor descriptor, SubBloc subBloc, Tools.Frequency frequency, EpochCompatibilityBuffer compatibilityBuffer)
        {
            if (sourceByChannel == null) throw new ArgumentNullException(nameof(sourceByChannel));
            UnitByChannel = unitByChannel ?? throw new ArgumentNullException(nameof(unitByChannel));
            Descriptor = descriptor ?? throw new ArgumentNullException(nameof(descriptor));
            m_Treatments = compatibilityBuffer.GetOrderedTreatments(subBloc);
            m_Frequency = frequency ?? throw new ArgumentNullException(nameof(frequency));
            m_EmptyInformationsByEvent = new Dictionary<Event, EventInformation>();
            Found = true;

            InitializeCompactSources(sourceByChannel, compatibilityBuffer);
            ValuesByChannel = new Dictionary<string, float[]>(m_ChannelLayout.Channels.Length);
            foreach (string channel in m_ChannelLayout.Channels)
            {
                float[] values = new float[Descriptor.Window.Length];
                float[] processedValues = MaterializeProcessedValues(channel, values, compatibilityBuffer);
                ValuesByChannel.Add(channel, processedValues);
                m_ManagedBytes += processedValues.LongLength * sizeof(float);
            }
        }

        internal SubTrial(Dictionary<string, float[]> sourceByChannel, Dictionary<string, string> unitByChannel, EventOccurence mainEventOccurence, SubBloc subBloc, Dictionary<Event, BlocData.EventOccurences> occurencesByEvent, Tools.Frequency frequency, int trialIndex, int subTrialIndex, EpochCompatibilityBuffer compatibilityBuffer)
        {
            int startIndex = mainEventOccurence.Index + frequency.ConvertToCeiledNumberOfSamples(subBloc.Window.Start);
            int endIndex = mainEventOccurence.Index + frequency.ConvertToFlooredNumberOfSamples(subBloc.Window.End);
            int baselineStartIndex = mainEventOccurence.Index + frequency.ConvertToCeiledNumberOfSamples(subBloc.Baseline.Start);
            int baselineEndIndex = mainEventOccurence.Index + frequency.ConvertToFlooredNumberOfSamples(subBloc.Baseline.End);

            if (startIndex < 0)
            {
                Found = false;
                Descriptor = null;
                m_CompactSource = Array.Empty<float>();
                m_ChannelLayout = null;
                UnitByChannel = new Dictionary<string, string>();
                ValuesByChannel = new Dictionary<string, float[]>();
                m_Treatments = Array.Empty<Treatment>();
                m_Frequency = frequency;
                m_EmptyInformationsByEvent = new Dictionary<Event, EventInformation>();
                m_NormalizedChannels = Array.Empty<bool>();
                return;
            }

            EpochRange window = CreateRange(sourceByChannel, startIndex, endIndex);
            EpochRange baseline = CreateRange(sourceByChannel, baselineStartIndex, baselineEndIndex);
            EpochDescriptor descriptor = new(window, baseline, mainEventOccurence.Index, trialIndex, subTrialIndex, FindEvents(mainEventOccurence, subBloc, occurencesByEvent, frequency, window));

            UnitByChannel = unitByChannel;
            Descriptor = descriptor;
            m_Treatments = compatibilityBuffer.GetOrderedTreatments(subBloc);
            m_Frequency = frequency;
            m_EmptyInformationsByEvent = new Dictionary<Event, EventInformation>();
            Found = true;

            InitializeCompactSources(sourceByChannel, compatibilityBuffer);
            ValuesByChannel = new Dictionary<string, float[]>(m_ChannelLayout.Channels.Length);
            foreach (string channel in m_ChannelLayout.Channels)
            {
                float[] values = new float[Descriptor.Window.Length];
                float[] processedValues = MaterializeProcessedValues(channel, values, compatibilityBuffer);
                ValuesByChannel.Add(channel, processedValues);
                m_ManagedBytes += processedValues.LongLength * sizeof(float);
            }
        }

        #endregion

        #region Public Methods

        public EpochView GetWindow(string channel)
        {
            if (!Found || m_ChannelLayout == null || !m_ChannelLayout.TryGetIndex(channel, out int channelIndex))
                throw new KeyNotFoundException(channel);
            int channelOffset = channelIndex * m_CompactChannelLength;
            return new EpochView(m_CompactSource, new EpochRange(channelOffset + m_StoredWindow.StartIndex, channelOffset + m_StoredWindow.EndIndex));
        }

        public EpochView GetBaseline(string channel)
        {
            if (!Found || m_ChannelLayout == null || !m_ChannelLayout.TryGetIndex(channel, out int channelIndex))
                throw new KeyNotFoundException(channel);
            int channelOffset = channelIndex * m_CompactChannelLength;
            return new EpochView(m_CompactSource, new EpochRange(channelOffset + m_StoredBaseline.StartIndex, channelOffset + m_StoredBaseline.EndIndex));
        }

        public void Clear()
        {
            foreach (EventInformation eventInformation in InformationsByEvent.Values)
                eventInformation.Clear();
            InformationsByEvent.Clear();

            m_CompactSource = Array.Empty<float>();
            m_ChannelLayout = null;
            m_CompactChannelLength = 0;
            m_ManagedBytes = 0;
            UnitByChannel = new Dictionary<string, string>();
            ValuesByChannel?.Clear();
            ValuesByChannel = new Dictionary<string, float[]>();
            m_Treatments = Array.Empty<Treatment>();
            m_NormalizedChannels = Array.Empty<bool>();
            Descriptor = null;
            Found = false;
        }

        public void Normalize(float average, float standardDeviation)
        {
            EpochCompatibilityBuffer compatibilityBuffer = new();
            foreach (string channel in Channels.ToArray())
                Normalize(average, standardDeviation, channel, compatibilityBuffer);
        }

        public void Normalize(float average, float standardDeviation, string channel)
        {
            Normalize(average, standardDeviation, channel, new EpochCompatibilityBuffer());
        }

        #endregion

        #region Internal Methods

        internal void Normalize(float average, float standardDeviation, string channel, EpochCompatibilityBuffer compatibilityBuffer, bool normalizedResult = true)
        {
            if (!Found || m_ChannelLayout == null || !m_ChannelLayout.TryGetIndex(channel, out int channelIndex))
                return;

            bool hasUsableValues = ValuesByChannel.TryGetValue(channel, out float[] existing) && existing.Length == Descriptor.Window.Length;
            long previousBytes = existing?.LongLength * sizeof(float) ?? 0;
            float[] values = hasUsableValues ? existing : new float[Descriptor.Window.Length];
            if (m_NormalizedChannels[channelIndex] || !hasUsableValues)
                values = MaterializeProcessedValues(channel, values, compatibilityBuffer);
            values.Normalize(values, average, standardDeviation);
            ValuesByChannel[channel] = values;
            m_ManagedBytes += values.LongLength * sizeof(float) - previousBytes;
            m_NormalizedChannels[channelIndex] = normalizedResult;
        }

        internal void Normalize(float average, float standardDeviation, EpochCompatibilityBuffer compatibilityBuffer)
        {
            foreach (string channel in Channels.ToArray())
                Normalize(average, standardDeviation, channel, compatibilityBuffer);
        }

        internal void GetBaselineStatistics(string channel, EpochCompatibilityBuffer compatibilityBuffer, out float average, out float standardDeviation)
        {
            RunningStatistics statistics = new();
            AccumulateBaselineStatistics(channel, compatibilityBuffer, ref statistics);
            average = statistics.Mean;
            standardDeviation = statistics.StandardDeviation;
        }

        internal void AccumulateBaselineStatistics(string channel, EpochCompatibilityBuffer compatibilityBuffer, ref RunningStatistics statistics)
        {
            if (m_Treatments.Length == 0)
            {
                EpochView baseline = GetBaseline(channel);
                for (int i = 0; i < baseline.Count; ++i)
                    statistics.Add(baseline[i]);
                return;
            }

            float[] processedBaseline = MaterializeProcessedBaseline(channel, compatibilityBuffer);
            for (int i = 0; i < Descriptor.Baseline.Length; ++i)
                statistics.Add(processedBaseline[i]);
        }

        #endregion

        #region Private Methods

        private float[] MaterializeProcessedValues(string channel, float[] values, EpochCompatibilityBuffer compatibilityBuffer)
        {
            GetWindow(channel).CopyTo(values);
            if (m_Treatments.Length == 0)
                return values;

            float[] baseline = compatibilityBuffer.GetBaselineBuffer(Descriptor.Baseline.Length);
            GetBaseline(channel).CopyTo(baseline);
            ApplyTreatments(ref values, ref baseline, compatibilityBuffer);
            return values;
        }

        private float[] MaterializeProcessedBaseline(string channel, EpochCompatibilityBuffer compatibilityBuffer)
        {
            float[] values = compatibilityBuffer.GetWindowBuffer(Descriptor.Window.Length);
            float[] baseline = compatibilityBuffer.GetBaselineBuffer(Descriptor.Baseline.Length);
            GetWindow(channel).CopyTo(values);
            GetBaseline(channel).CopyTo(baseline);
            ApplyTreatments(ref values, ref baseline, compatibilityBuffer);
            return baseline;
        }

        private void ApplyTreatments(ref float[] values, ref float[] baseline, EpochCompatibilityBuffer compatibilityBuffer)
        {
            if (m_Treatments.Length == 0)
                return;

            int windowMainEventIndex = Descriptor.MainEventIndex - Descriptor.Window.StartIndex;
            int baselineMainEventIndex = Descriptor.MainEventIndex - Descriptor.Baseline.StartIndex;
            float[] treatmentBuffer = compatibilityBuffer.GetTreatmentBuffer(Descriptor.Window.Length + Descriptor.Baseline.Length);
            foreach (Treatment treatment in m_Treatments)
                treatment.Apply(ref values, ref baseline, windowMainEventIndex, baselineMainEventIndex, m_Frequency, treatmentBuffer);
        }

        internal long ManagedBytes => m_ManagedBytes;

        private void InitializeCompactSources(Dictionary<string, float[]> sourceByChannel, EpochCompatibilityBuffer compatibilityBuffer)
        {
            foreach (float[] source in sourceByChannel.Values)
            {
                ValidateRange(source, Descriptor.Window);
                ValidateRange(source, Descriptor.Baseline);
            }

            int compactStart = Math.Min(Descriptor.Window.StartIndex, Descriptor.Baseline.StartIndex);
            int compactEnd = Math.Max(Descriptor.Window.EndIndex, Descriptor.Baseline.EndIndex);
            int compactLength = compactEnd - compactStart + 1;
            m_StoredWindow = new EpochRange(Descriptor.Window.StartIndex - compactStart, Descriptor.Window.EndIndex - compactStart);
            m_StoredBaseline = new EpochRange(Descriptor.Baseline.StartIndex - compactStart, Descriptor.Baseline.EndIndex - compactStart);
            m_ChannelLayout = compatibilityBuffer.GetChannelLayout(sourceByChannel);
            m_CompactChannelLength = compactLength;
            m_CompactSource = new float[checked(compactLength * m_ChannelLayout.Channels.Length)];
            m_ManagedBytes = m_CompactSource.LongLength * sizeof(float);
            for (int channelIndex = 0; channelIndex < m_ChannelLayout.Channels.Length; ++channelIndex)
            {
                string channel = m_ChannelLayout.Channels[channelIndex];
                Array.Copy(sourceByChannel[channel], compactStart, m_CompactSource, channelIndex * compactLength, compactLength);
            }

            m_NormalizedChannels = new bool[m_ChannelLayout.Channels.Length];
        }

        private static EpochRange CreateRange(Dictionary<string, float[]> sourceByChannel, int startIndex, int endIndex)
        {
            if (endIndex < startIndex)
            {
                int sourceEndIndex = sourceByChannel.Count == 0 ? -1 : sourceByChannel.First().Value.Length - 1;
                throw new CannotEpochAllTrialsException(sourceEndIndex, startIndex, endIndex);
            }

            EpochRange range = new(startIndex, endIndex);
            foreach (float[] source in sourceByChannel.Values)
                ValidateRange(source, range);
            return range;
        }

        private static void ValidateRange(float[] source, EpochRange range)
        {
            if (range.StartIndex < 0 || range.EndIndex >= source.Length)
                throw new CannotEpochAllTrialsException(source.Length - 1, range.StartIndex, range.EndIndex);
        }

        private static Dictionary<Event, EventInformation> FindEvents(EventOccurence mainEventOccurence, SubBloc subBloc, Dictionary<Event, BlocData.EventOccurences> occurencesByEvent, Tools.Frequency frequency, EpochRange window)
        {
            Dictionary<Event, EventInformation> result = new(subBloc.Events.Count);
            EventInformation.EventOccurence mainOccurence = new(mainEventOccurence.Code, mainEventOccurence.Index, mainEventOccurence.Index - window.StartIndex, 0, mainEventOccurence.Time, -subBloc.Window.Start, 0f);
            result.Add(subBloc.MainEvent, new EventInformation(new[] { mainOccurence }));

            foreach (Event secondaryEvent in subBloc.SecondaryEvents)
            {
                EventOccurence[] occurences = occurencesByEvent[secondaryEvent].GetOccurences(window.StartIndex, window.EndIndex);
                List<EventInformation.EventOccurence> eventOccurences = new(occurences.Length);
                foreach (EventOccurence occurence in occurences)
                {
                    eventOccurences.Add(new EventInformation.EventOccurence(occurence.Code, occurence.Index, occurence.Index - window.StartIndex, occurence.Index - mainOccurence.Index, occurence.Time, occurence.Time - mainOccurence.Time + mainOccurence.TimeFromStart, occurence.Time - mainOccurence.Time));
                }

                result.Add(secondaryEvent, new EventInformation(eventOccurences.ToArray()));
            }

            return result;
        }

        #endregion
    }
}
