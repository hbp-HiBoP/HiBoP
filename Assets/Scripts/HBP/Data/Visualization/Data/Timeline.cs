using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace HBP.Core.Data // FIXME : maybe these classes have nothing to do in this namespace. They are mostly used in 3D display, and only used once to compute some stuff regarding Standardize Values during the loading, which could be done there independently.
{
    public abstract class BasicTimeline
    {
        #region Properties
        /// <summary>
        /// Length of the timeline
        /// </summary>
        public int Length { get; protected set; }
        /// <summary>
        /// Unit of the timeline
        /// </summary>
        public string Unit { get; protected set; }
        /// <summary>
        /// Length of the timeline in unit of time
        /// </summary>
        public float TimeLength { get; protected set; }

        public Tools.Frequency Frequency { get; protected set; }

        protected int m_CurrentIndex;
        /// <summary>
        /// Current index of the timeline
        /// </summary>
        public int CurrentIndex
        {
            get
            {
                return m_CurrentIndex;
            }
            set
            {
                if (IsLooping)
                {
                    m_CurrentIndex = (value % Length + Length) % Length;
                }
                else
                {
                    m_CurrentIndex = Mathf.Clamp(value, 0, Length - 1);
                }
                OnUpdateCurrentIndex.Invoke();
            }
        }
        /// <summary>
        /// Is the timeline looping ?
        /// </summary>
        public bool IsLooping { get; set; }
        protected bool m_IsPlaying;
        /// <summary>
        /// Is the timeline playing ?
        /// </summary>
        public bool IsPlaying
        {
            get
            {
                return m_IsPlaying;
            }
            set
            {
                m_IsPlaying = value;
                m_TimeSinceLastUpdate = 0f;
            }
        }

        /// <summary>
        /// Time since the last timeline update
        /// </summary>
        protected float m_TimeSinceLastUpdate;
        /// <summary>
        /// Step of the timeline
        /// </summary>
        public int Step { get; set; } = 1;
        /// <summary>
        /// Time between two timeline updates
        /// </summary>
        protected float UpdateInterval { get { return 1.0f / Step; } }

        /// <summary>
        /// Current subtimeline compared to the position of the current index
        /// </summary>
        public abstract SubTimeline CurrentSubtimeline { get; }
        #endregion

        #region Events
        public UnityEvent OnUpdateCurrentIndex = new UnityEvent();
        public UnityEvent OnStopTimelinePlay = new UnityEvent();
        #endregion

        #region Public Methods
        /// <summary>
        /// Play the timeline
        /// </summary>
        public void Play()
        {
            if (IsPlaying)
            {
                m_TimeSinceLastUpdate += Time.deltaTime;
                while (m_TimeSinceLastUpdate > UpdateInterval)
                {
                    CurrentIndex++;
                    m_TimeSinceLastUpdate -= UpdateInterval;
                    if (CurrentIndex >= Length - 1 && !IsLooping)
                    {
                        IsPlaying = false;
                        CurrentIndex = 0;
                        OnStopTimelinePlay.Invoke();
                    }
                }
            }
        }
        #endregion
    }
    public class Timeline : BasicTimeline
    {
        #region Properties
        /// <summary>
        /// Subtimelines of this timeline
        /// </summary>
        public Dictionary<SubBloc, SubTimeline> SubTimelinesBySubBloc { get; set; }
        /// <summary>
        /// Current subtimeline compared to the position of the current index
        /// </summary>
        public override SubTimeline CurrentSubtimeline
        {
            get
            {
                return SubTimelinesBySubBloc.FirstOrDefault(s => s.Value.GlobalMinIndex - s.Value.Before <= m_CurrentIndex && s.Value.GlobalMaxIndex + s.Value.After >= m_CurrentIndex).Value;
            }
        }
        #endregion

        #region Constructors
        public Timeline(Bloc bloc, Dictionary<SubBloc, List<SubBlocEventsStatistics>> eventStatisticsBySubBloc, Dictionary<SubBloc, int> indexBySubBloc, Core.Tools.Frequency frequency)
        {
            Frequency = frequency;
            Unit = "ms";
            int startIndex = 0;
            SubTimelinesBySubBloc = new Dictionary<SubBloc, SubTimeline>(bloc.SubBlocs.Count);
            foreach (var subBloc in bloc.OrderedSubBlocs)
            {
                IEnumerable<SubBloc> subBlocs = indexBySubBloc.Where(kv => kv.Value == indexBySubBloc[subBloc]).Select(kv => kv.Key);
                int before = subBlocs.Max(s => -frequency.ConvertToCeiledNumberOfSamples(s.Window.Start));
                int after = subBlocs.Max(s => frequency.ConvertToFlooredNumberOfSamples(s.Window.End));
                SubTimeline subTimeline = new SubTimeline(subBloc, startIndex, eventStatisticsBySubBloc[subBloc], before, after, frequency);
                startIndex += subTimeline.Length + subTimeline.Before + subTimeline.After;
                SubTimelinesBySubBloc.Add(subBloc, subTimeline);
            }

            // Change Before of first SubTimeline
            SubBloc firstSubBloc = bloc.OrderedSubBlocs.First();
            int firstSubBlocIndex = indexBySubBloc[firstSubBloc];
            while (indexBySubBloc.ContainsValue(--firstSubBlocIndex))
            {
                int before = indexBySubBloc.Where(kv => kv.Value == firstSubBlocIndex).Max(kv => frequency.ConvertToFlooredNumberOfSamples(kv.Key.Window.End) - frequency.ConvertToCeiledNumberOfSamples(kv.Key.Window.Start)) + 1;
                SubTimelinesBySubBloc[firstSubBloc].Before += before;
                foreach (var subBloc in bloc.SubBlocs)
                {
                    if (subBloc != firstSubBloc)
                    {
                        SubTimelinesBySubBloc[subBloc].Move(before);
                    }
                }
            }

            // Change After of last SubTimeline
            SubBloc lastSubBloc = bloc.OrderedSubBlocs.Last();
            int lastSubBlocIndex = indexBySubBloc[lastSubBloc];
            while (indexBySubBloc.ContainsValue(++lastSubBlocIndex))
            {
                SubTimelinesBySubBloc[lastSubBloc].After += indexBySubBloc.Where(kv => kv.Value == lastSubBlocIndex).Max(kv => frequency.ConvertToFlooredNumberOfSamples(kv.Key.Window.End) - frequency.ConvertToCeiledNumberOfSamples(kv.Key.Window.Start)) + 1;
            }

            Length = SubTimelinesBySubBloc.Sum(s => s.Value.Length + s.Value.Before + s.Value.After);
            TimeLength = SubTimelinesBySubBloc.Sum(s => s.Value.TimeLength);
        }
        #endregion
    }
    public class FMRITimeline : BasicTimeline
    {
        #region Properties
        protected SubTimeline m_DefaultSubtimeline;
        public override SubTimeline CurrentSubtimeline => m_DefaultSubtimeline;
        #endregion

        #region Public Methods
        public void Update(Core.Object3D.FMRI fmri)
        {
            Frequency = new Core.Tools.Frequency(1);
            Unit = "dt";
            Length = fmri.Volumes.Count;
            TimeLength = fmri.Volumes.Count - 1;
            CurrentIndex = 0;
            m_DefaultSubtimeline = new SubTimeline(fmri);
        }
        #endregion
    }
    public class SubTimeline
    {
        #region Properties
        /// <summary>
        /// Length of the subtimeline
        /// </summary>
        public int Length { get; private set; }
        /// <summary>
        /// Number of samples before this subtimeline
        /// </summary>
        public int Before { get; set; }
        /// <summary>
        /// Number of samples after this subtimeline
        /// </summary>
        public int After { get; set; }
        /// <summary>
        /// Length of the timeline in unit of time
        /// </summary>
        public float TimeLength { get { return MaxTime - MinTime; } }
        
        public Core.Tools.Frequency Frequency { get; private set; }

        private int m_GlobalMinIndex;
        /// <summary>
        /// Min index of the subtimeline from the start of the global timeline
        /// </summary>
        public int GlobalMinIndex
        {
            get
            {
                return m_GlobalMinIndex + Before;
            }
        }

        private int m_GlobalMaxIndex;
        /// <summary>
        /// Max index of the subtimeline from the start of the global timeline
        /// </summary>
        public int GlobalMaxIndex
        {
            get
            {
                return m_GlobalMaxIndex + Before;
            }
        }

        /// <summary>
        /// Minimum time of the timeline
        /// </summary>
        public float MinTime { get; set; }
        /// <summary>
        /// Maximum time of the timeline
        /// </summary>
        public float MaxTime { get; set; }
        /// <summary>
        /// Time of the first sample
        /// </summary>
        public float FirstSampleTime { get; set; }
        /// <summary>
        /// Time of the last sample
        /// </summary>
        public float LastSampleTime { get; set; }
        /// <summary>
        /// Time of a step
        /// </summary>
        public float TimeStep { get; set; }

        public Dictionary<Event, EventStatistics> StatisticsByEvent { get; set; } = new Dictionary<Event, EventStatistics>();
        #endregion

        #region Constructors
        public SubTimeline(Core.Object3D.FMRI fmri)
        {
            Length = fmri.Volumes.Count;
            Before = 0;
            After = 0;
            Frequency = new Core.Tools.Frequency(1);
            MinTime = 0;
            MaxTime = fmri.Volumes.Count - 1;
            FirstSampleTime = MinTime;
            LastSampleTime = MaxTime;
            TimeStep = 1;
        }
        public SubTimeline(SubBloc subBloc, int startIndex, List<SubBlocEventsStatistics> eventStatistics, int maxBefore, int maxAfter, Tools.Frequency frequency)
        {
            Frequency = frequency;

            // Events
            foreach (var e in subBloc.Events)
            {
                StatisticsByEvent.Add(e, EventStatistics.Average(eventStatistics.Select(es => es.StatisticsByEvent[e])));
            }
            int mainEventIndex = Frequency.ConvertToFlooredNumberOfSamples(StatisticsByEvent[subBloc.MainEvent].RoundedTimeFromStart);

            // Indexes
            Before = maxBefore - mainEventIndex;
            m_GlobalMinIndex = startIndex;
            Length = frequency.ConvertToFlooredNumberOfSamples(subBloc.Window.End) - frequency.ConvertToCeiledNumberOfSamples(subBloc.Window.Start) + 1;
            m_GlobalMaxIndex = startIndex + Length - 1;
            After = maxAfter - (Length - 1 - mainEventIndex);

            // Time
            MinTime = subBloc.Window.Start;
            MaxTime = subBloc.Window.End;
            FirstSampleTime = frequency.ConvertNumberOfSamplesToMilliseconds(-mainEventIndex);
            LastSampleTime = frequency.ConvertNumberOfSamplesToMilliseconds(Length - 1 - mainEventIndex);
            TimeStep = (LastSampleTime - FirstSampleTime) / (Length - 1);
        }
        #endregion

        #region Public Methods
        public void Move(int distance)
        {
            m_GlobalMinIndex += distance;
            m_GlobalMaxIndex += distance;
        }
        /// <summary>
        /// Get the time of the current global index in this timeline
        /// </summary>
        /// <param name="globalIndex">Global index</param>
        /// <returns></returns>
        public float GetLocalTime(int globalIndex)
        {
            return Mathf.Clamp(TimeStep * GetLocalIndex(globalIndex) + FirstSampleTime, MinTime, MaxTime);
        }
        /// <summary>
        /// Get the index value relative to the minimum index of the subtimeline
        /// </summary>
        /// <param name="globalIndex"></param>
        /// <returns></returns>
        public int GetLocalIndex(int globalIndex)
        {
            return Mathf.Clamp(globalIndex - GlobalMinIndex, 0, Length - 1);
        }
        #endregion
    }
}