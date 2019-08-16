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
    public class DynamicDataParameters
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
                    OnUpdateInfluenceDistance.Invoke();
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
        /// <summary>
        /// Minimum amplitude value
        /// </summary>
        public float MinimumAmplitude { get; set; } = float.MinValue;
        /// <summary>
        /// Maximum amplitude value
        /// </summary>
        public float MaximumAmplitude { get; set; } = float.MaxValue;

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
        /// <summary>
        /// Span Min value
        /// </summary>
        public float SpanMin { get; private set; } = 0.0f;
        /// <summary>
        /// Middle value
        /// </summary>
        public float Middle { get; private set; } = 0.0f;
        /// <summary>
        /// Span Min value
        /// </summary>
        public float SpanMax { get; private set; } = 0.0f;
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
        public UnityEvent OnUpdateInfluenceDistance = new UnityEvent();
        #endregion

        #region Public Methods
        /// <summary>
        /// Set the span values
        /// </summary>
        /// <param name="min">Span min value</param>
        /// <param name="mid">Middle value</param>
        /// <param name="max">Span max value</param>
        /// <param name="column">Column associated with these parameters</param>
        public void SetSpanValues(float min, float mid, float max)
        {
            if (Mathf.Approximately(min, 0f) && Mathf.Approximately(mid, 0f) && Mathf.Approximately(max, 0f)) return;

            if (min > max) min = max;
            mid = Mathf.Clamp(mid, min, max);
            SpanMin = min;
            Middle = mid;
            SpanMax = max;
            OnUpdateSpanValues.Invoke();
        }
        public void ResetSpanValues(Column3DDynamic column)
        {
            if (column is Column3DCCEP ccepColumn)
            {
                if (!ccepColumn.IsSourceSelected)
                {
                    SpanMin = 0;
                    Middle = 0;
                    SpanMax = 0;
                    OnUpdateSpanValues.Invoke();
                    return;
                }
            }
            float middle = column.IEEGValuesOfUnmaskedSites.Mean();
            Vector2 limits = column.IEEGValuesOfUnmaskedSites.CalculateValueLimit();
            Middle = middle;
            SpanMin = Mathf.Clamp(limits[0], MinimumAmplitude, MaximumAmplitude);
            SpanMax = Mathf.Clamp(limits[1], MinimumAmplitude, MaximumAmplitude);
            OnUpdateSpanValues.Invoke();
        }
        #endregion
    }
}