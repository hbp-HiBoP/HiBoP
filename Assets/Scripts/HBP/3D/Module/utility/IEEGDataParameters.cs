using System.Collections;
using System.Collections.Generic;
using Tools.CSharp;
using UnityEngine;
using UnityEngine.Events;

namespace HBP.Module3D
{
    /// <summary>
    /// Class containing the parameters for the iEEG of a column
    /// </summary>
    public class IEEGDataParameters
    {
        #region Properties
        /// <summary>
        /// Minimum influence distance
        /// </summary>
        private const float MIN_INFLUENCE = 0.0f;
        /// <summary>
        /// Maximum influence distance
        /// </summary>
        private const float MAX_INFLUENCE = 50.0f;
        private float m_InfluenceDistance = 15.0f;
        /// <summary>
        /// Influence distance of a site (sphere around the site to color the mesh)
        /// </summary>
        public float InfluenceDistance
        {
            get
            {
                return m_InfluenceDistance;
            }
            set
            {
                float val = Mathf.Clamp(value, MIN_INFLUENCE, MAX_INFLUENCE);
                if (m_InfluenceDistance != val)
                {
                    m_InfluenceDistance = val;
                    OnUpdateMaximumInfluence.Invoke();
                }
            }
        }

        private float m_Gain = 1.0f;
        /// <summary>
        /// Gain of the spheres representing the sites
        /// </summary>
        public float Gain
        {
            get
            {
                return m_Gain;
            }
            set
            {
                if (m_Gain != value)
                {
                    m_Gain = value;
                    OnUpdateGain.Invoke();
                }
            }
        }

        private float m_MinimumAmplitude = float.MinValue;
        /// <summary>
        /// Minimum amplitude value
        /// </summary>
        public float MinimumAmplitude
        {
            get
            {
                return m_MinimumAmplitude;
            }
            set
            {
                m_MinimumAmplitude = value;
            }
        }

        private float m_MaximumAmplitude = float.MaxValue;
        /// <summary>
        /// Maximum amplitude value
        /// </summary>
        public float MaximumAmplitude
        {
            get
            {
                return m_MaximumAmplitude;
            }
            set
            {
                m_MaximumAmplitude = value;
            }
        }

        private float m_AlphaMin = 0.8f;
        /// <summary>
        /// Minimum Alpha
        /// </summary>
        public float AlphaMin
        {
            get
            {
                return m_AlphaMin;
            }
            set
            {
                if (m_AlphaMin != value)
                {
                    m_AlphaMin = value;
                    OnUpdateAlphaValues.Invoke();
                }
            }
        }

        private float m_AlphaMax = 1.0f;
        /// <summary>
        /// Maximum Alpha
        /// </summary>
        public float AlphaMax
        {
            get
            {
                return m_AlphaMax;
            }
            set
            {
                if (m_AlphaMax != value)
                {
                    m_AlphaMax = value;
                    OnUpdateAlphaValues.Invoke();
                }
            }
        }

        private float m_SpanMin = 0.0f;
        /// <summary>
        /// Span Min value
        /// </summary>
        public float SpanMin
        {
            get
            {
                return m_SpanMin;
            }
        }

        private float m_Middle = 0.0f;
        /// <summary>
        /// Middle value
        /// </summary>
        public float Middle
        {
            get
            {
                return m_Middle;
            }
        }

        private float m_SpanMax = 0.0f;
        /// <summary>
        /// Span Min value
        /// </summary>
        public float SpanMax
        {
            get
            {
                return m_SpanMax;
            }
        }
        #endregion

        #region Events
        /// <summary>
        /// Event called when updating the span values (min, mid or max)
        /// </summary>
        public UnityEvent OnUpdateSpanValues = new UnityEvent();
        /// <summary>
        /// Event called when updating the alpha values
        /// </summary>
        public UnityEvent OnUpdateAlphaValues = new UnityEvent();
        /// <summary>
        /// Event called when updating the sphere gain
        /// </summary>
        public UnityEvent OnUpdateGain = new UnityEvent();
        /// <summary>
        /// Event called when updating the maximum influence
        /// </summary>
        public UnityEvent OnUpdateMaximumInfluence = new UnityEvent();
        #endregion

        #region Public Methods
        /// <summary>
        /// Set the span values
        /// </summary>
        /// <param name="min">Span min value</param>
        /// <param name="mid">Middle value</param>
        /// <param name="max">Span max value</param>
        /// <param name="column">Column associated with these parameters</param>
        public void SetSpanValues(float min, float mid, float max, Column3DIEEG column)
        {
            if (min > max) min = max;
            mid = Mathf.Clamp(mid, min, max);
            if (Mathf.Approximately(min, mid) && Mathf.Approximately(min, max) && Mathf.Approximately(mid, max))
            {
                float amplitude = m_MaximumAmplitude - m_MinimumAmplitude;
                float middle = column.IEEGValuesOfUnmaskedSites.Median();
                mid = middle;
                min = Mathf.Clamp(middle - 0.05f * amplitude, m_MinimumAmplitude, m_MaximumAmplitude);
                max = Mathf.Clamp(middle + 0.05f * amplitude, m_MinimumAmplitude, m_MaximumAmplitude);
            }
            m_SpanMin = min;
            m_Middle = mid;
            m_SpanMax = max;
            OnUpdateSpanValues.Invoke();
        }
        #endregion
    }
}