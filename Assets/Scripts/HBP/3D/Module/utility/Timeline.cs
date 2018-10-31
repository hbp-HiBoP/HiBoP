using System.Collections.Generic;
using System.Collections.ObjectModel;
using HBP.Data.Experience.Protocol;
using HBP.Data.Experience.Dataset;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using HBP.Data.Localizer;

namespace HBP.Data.Visualization
{
    public class Timeline
    {
        #region Properties
        /// <summary>
        /// Length of the timeline
        /// </summary>
        public int Length { get; private set; }
        /// <summary>
        /// Unit of the timeline
        /// </summary>
        public string Unit { get; private set; }
        /// <summary>
        /// Length of the timeline in unit of time
        /// </summary>
        public float TimeLength { get; private set; }

        private int m_CurrentIndex;
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
        /// Subtimelines of this timeline
        /// </summary>
        public Dictionary<SubBloc, SubTimeline> SubTimelinesBySubBloc { get; set; }
        /// <summary>
        /// Current subtimeline compared to the position of the current index
        /// </summary>
        public SubTimeline CurrentSubtimeline
        {
            get
            {
                return SubTimelinesBySubBloc.FirstOrDefault(s => s.Value.GlobalMinIndex <= m_CurrentIndex && s.Value.GlobalMaxIndex >= m_CurrentIndex).Value;
            }
        }

        /// <summary>
        /// Is the timeline looping ?
        /// </summary>
        public bool IsLooping { get; set; }
        private bool m_IsPlaying;
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
        private float m_TimeSinceLastUpdate;
        /// <summary>
        /// Step of the timeline
        /// </summary>
        public int Step { get; set; } = 1;
        /// <summary>
        /// Time between two timeline updates
        /// </summary>
        private float UpdateInterval { get { return 1.0f / Step; } }
        #endregion

        #region Events
        public UnityEvent OnUpdateCurrentIndex = new UnityEvent();
        #endregion

        #region Constructors
        public Timeline(Bloc bloc, Dictionary<SubBloc, List<SubBlocEventsStatistics>> eventStatisticsBySubBloc, Frequency frequency)
        {
            Unit = "ms";
            int startIndex = 0;
            SubTimelinesBySubBloc = new Dictionary<SubBloc, SubTimeline>(bloc.SubBlocs.Count);
            foreach (var subBloc in bloc.SubBlocs.OrderBy(sb => sb.Order).ThenBy(sb => sb.Name))
            {
                SubTimeline subTimeline = new SubTimeline(subBloc, startIndex, eventStatisticsBySubBloc[subBloc], frequency);
                startIndex += subTimeline.Length;
                SubTimelinesBySubBloc.Add(subBloc, subTimeline);
            }
            Length = SubTimelinesBySubBloc.Sum(s => s.Value.Length);
            TimeLength = SubTimelinesBySubBloc.Sum(s => s.Value.TimeLength);
        }
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
                        ApplicationState.Module3D.OnStopTimelinePlay.Invoke();
                    }
                }
            }
        }
        #endregion
    }

    public class SubTimeline
    {
        #region Properties
        /// <summary>
        /// Length of the subtimeline
        /// </summary>
        public int Length { get; set; }
        /// <summary>
        /// Length of the timeline in unit of time
        /// </summary>
        public float TimeLength { get { return MaxTime - MinTime; } }
        /// <summary>
        /// Min index of the subtimeline from the start of the global timeline
        /// </summary>
        public int GlobalMinIndex { get; set; }
        /// <summary>
        /// Max index of the subtimeline from the start of the global timeline
        /// </summary>
        public int GlobalMaxIndex { get; set; }
        /// <summary>
        /// Minimum time of the timeline
        /// </summary>
        public float MinTime { get; set; }
        /// <summary>
        /// Maximum time of the timeline
        /// </summary>
        public float MaxTime { get; set; }
        /// <summary>
        /// Time of a step
        /// </summary>
        public float TimeStep { get; set; }

        public Dictionary<Data.Experience.Protocol.Event, EventStatistics> StatisticsByEvent { get; set; }
        #endregion

        #region Constructors
        public SubTimeline(SubBloc subBloc, int startIndex, List<SubBlocEventsStatistics> eventStatistics, Frequency frequency)
        {
            // Indexes
            GlobalMinIndex = startIndex;
            Length = frequency.ConvertToFlooredNumberOfSamples(subBloc.Window.End) - frequency.ConvertToCeiledNumberOfSamples(subBloc.Window.Start) + 1;
            GlobalMaxIndex = startIndex + Length - 1;
            MinTime = subBloc.Window.Start;
            MaxTime = subBloc.Window.End;
            TimeStep = (MaxTime - MinTime) / (Length - 1);

            // Events
            StatisticsByEvent = new Dictionary<Experience.Protocol.Event, EventStatistics>();
            foreach (var e in subBloc.Events)
            {
                StatisticsByEvent.Add(e, EventStatistics.Average(eventStatistics.Select(es => es.StatisticsByEvent[e])));
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Get the time of the current global index in this timeline
        /// </summary>
        /// <param name="globalIndex">Global index</param>
        /// <returns></returns>
        public float GetLocalTime(int globalIndex)
        {
            return TimeStep * GetLocalIndex(globalIndex) + MinTime;
        }
        /// <summary>
        /// Get the index value relative to the minimum index of the subtimeline
        /// </summary>
        /// <param name="globalIndex"></param>
        /// <returns></returns>
        public int GetLocalIndex(int globalIndex)
        {
            return globalIndex - GlobalMinIndex;
        }
        #endregion
    }
}