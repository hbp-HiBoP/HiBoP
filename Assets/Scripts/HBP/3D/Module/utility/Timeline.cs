using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

namespace HBP.Module3D
{
    public class Timeline
    {
        #region Properties
        /// <summary>
        /// Length of the timeline
        /// </summary>
        public int Length { get; set; }
        /// <summary>
        /// Unit of the timeline
        /// </summary>
        public string Unit { get; set; }

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
        private SubTimeline[] m_SubTimelines;
        /// <summary>
        /// Current subtimeline compared to the position of the current index
        /// </summary>
        public SubTimeline CurrentSubtimeline
        {
            get
            {
                return m_SubTimelines.FirstOrDefault(s => s.GlobalMinIndex <= m_CurrentIndex && s.GlobalMaxIndex >= m_CurrentIndex);
            }
        }
        /// <summary>
        /// Get the current times of each timeline
        /// </summary>
        public float[] CurrentTimes
        {
            get
            {
                return m_SubTimelines.Select(s => s.GetCurrentTime(m_CurrentIndex)).ToArray();
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
        // TODO : DELETE THIS CONSTRUCTOR, THIS IS ONLY FOR DEBUG PURPOSES
        public Timeline(Data.Visualization.Timeline timeline)
        {
            Length = timeline.Lenght;
            Unit = timeline.Start.Unite;
            m_SubTimelines = new SubTimeline[]
            {
                new SubTimeline()
                {
                    GlobalMinIndex = 0,
                    GlobalMaxIndex = Length - 1,
                    MinTime = timeline.Start.Value,
                    MaxTime = timeline.End.Value,
                    TimeStep = timeline.Step
                }
            };
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
        #endregion

        #region Public Methods
        /// <summary>
        /// Get the time of the current global index in this timeline
        /// </summary>
        /// <param name="globalIndex">Global index</param>
        /// <returns></returns>
        public float GetCurrentTime(int globalIndex)
        {
            return TimeStep * (globalIndex - GlobalMinIndex) + MinTime;
        }
        #endregion
    }
}