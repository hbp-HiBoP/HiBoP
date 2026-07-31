using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace HBP.Core.Data
{
    public readonly struct EpochRange
    {
        public int StartIndex { get; }
        public int EndIndex { get; }
        public int Length => EndIndex - StartIndex + 1;

        public EpochRange(int startIndex, int endIndex)
        {
            if (endIndex < startIndex)
                throw new ArgumentOutOfRangeException(nameof(endIndex));

            StartIndex = startIndex;
            EndIndex = endIndex;
        }
    }

    public readonly struct EpochView : IReadOnlyList<float>
    {
        private readonly float[] m_Source;

        public int Offset { get; }
        public int Count { get; }

        public float this[int index]
        {
            get
            {
                if ((uint)index >= (uint)Count)
                    throw new ArgumentOutOfRangeException(nameof(index));
                return m_Source[Offset + index];
            }
        }

        public EpochView(float[] source, EpochRange range)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (range.StartIndex < 0 || range.EndIndex >= source.Length)
                throw new ArgumentOutOfRangeException(nameof(range));

            m_Source = source;
            Offset = range.StartIndex;
            Count = range.Length;
        }

        public void CopyTo(float[] destination)
        {
            if (destination == null)
                throw new ArgumentNullException(nameof(destination));
            if (destination.Length != Count)
                throw new ArgumentException("The destination length must match the epoch length.", nameof(destination));

            Array.Copy(m_Source, Offset, destination, 0, Count);
        }

        public float[] ToArray()
        {
            float[] result = new float[Count];
            CopyTo(result);
            return result;
        }

        public IEnumerator<float> GetEnumerator()
        {
            for (int i = 0; i < Count; ++i)
                yield return m_Source[Offset + i];
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public sealed class EpochDescriptor
    {
        public EpochRange Window { get; }
        public EpochRange Baseline { get; }
        public int MainEventIndex { get; }
        public int TrialIndex { get; internal set; }
        public int SubTrialIndex { get; }
        public Dictionary<Event, EventInformation> InformationsByEvent { get; }

        public EpochDescriptor(EpochRange window, EpochRange baseline, int mainEventIndex, int trialIndex, int subTrialIndex, Dictionary<Event, EventInformation> informationsByEvent)
        {
            Window = window;
            Baseline = baseline;
            MainEventIndex = mainEventIndex;
            TrialIndex = trialIndex;
            SubTrialIndex = subTrialIndex;
            InformationsByEvent = informationsByEvent ?? throw new ArgumentNullException(nameof(informationsByEvent));
        }
    }

    internal sealed class EpochCompatibilityBuffer
    {
        private float[] m_WindowBuffer = Array.Empty<float>();
        private float[] m_BaselineBuffer = Array.Empty<float>();
        private float[] m_TreatmentBuffer = Array.Empty<float>();
        private Dictionary<string, float[]> m_ChannelLayoutSource;
        private EpochChannelLayout m_ChannelLayout;
        private readonly Dictionary<SubBloc, Treatment[]> m_TreatmentsBySubBloc = new();

        public EpochChannelLayout GetChannelLayout(Dictionary<string, float[]> sourceByChannel)
        {
            if (!ReferenceEquals(m_ChannelLayoutSource, sourceByChannel))
            {
                m_ChannelLayoutSource = sourceByChannel;
                m_ChannelLayout = new EpochChannelLayout(sourceByChannel.Keys);
            }

            return m_ChannelLayout;
        }

        public Treatment[] GetOrderedTreatments(SubBloc subBloc)
        {
            if (subBloc == null)
                return Array.Empty<Treatment>();
            if (!m_TreatmentsBySubBloc.TryGetValue(subBloc, out Treatment[] treatments))
            {
                treatments = subBloc.Treatments.OrderBy(treatment => treatment.Order).ToArray();
                m_TreatmentsBySubBloc.Add(subBloc, treatments);
            }

            return treatments;
        }

        public float[] GetWindowBuffer(int length)
        {
            if (m_WindowBuffer.Length != length)
                m_WindowBuffer = new float[length];
            return m_WindowBuffer;
        }

        public float[] GetBaselineBuffer(int length)
        {
            if (m_BaselineBuffer.Length != length)
                m_BaselineBuffer = new float[length];
            return m_BaselineBuffer;
        }

        public float[] GetTreatmentBuffer(int length)
        {
            if (m_TreatmentBuffer.Length != length)
                m_TreatmentBuffer = new float[length];
            return m_TreatmentBuffer;
        }
    }

    internal sealed class EpochChannelLayout
    {
        private readonly Dictionary<string, int> m_IndexByChannel;
        public string[] Channels { get; }

        public EpochChannelLayout(IEnumerable<string> channels)
        {
            Channels = new List<string>(channels).ToArray();
            m_IndexByChannel = new Dictionary<string, int>(Channels.Length);
            for (int i = 0; i < Channels.Length; ++i)
                m_IndexByChannel.Add(Channels[i], i);
        }

        public bool TryGetIndex(string channel, out int index) => m_IndexByChannel.TryGetValue(channel, out index);
    }
}
