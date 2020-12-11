using Tools.CSharp;
using UnityEngine;
using UnityEngine.Events;

namespace HBP.Module3D
{
    /// <summary>
    /// Class containing the parameters for the activity of an anatomical column
    /// </summary>
    public class AnatomyDataParameters
    {
        #region Properties
        /// <summary>
        /// Minimum distance for a site to influence a vertex of the mesh
        /// </summary>
        private const float MIN_INFLUENCE = 0.0f;
        /// <summary>
        /// Maximum distance for a site to influence a vertex of the mesh
        /// </summary>
        private const float MAX_INFLUENCE = 50.0f;
        private float m_InfluenceDistance = 15.0f;
        /// <summary>
        /// Distance for a site to influence a vertex of the mesh
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
        /// Gain for the size of the sites
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
        /// Minimum amplitude value (computed externally for this column when setting activity values)
        /// </summary>
        public float MinimumAmplitude { get; set; } = float.MinValue;
        /// <summary>
        /// Maximum amplitude value (computed externally for this column when setting activity values)
        /// </summary>
        public float MaximumAmplitude { get; set; } = float.MaxValue;

        private float m_AlphaMin = 0.8f;
        /// <summary>
        /// Alpha of the activity for the lowest site density
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
        /// Alpha of the activity for the highest site density
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
        /// Minimum span value (to adjust the colormap)
        /// </summary>
        public float SpanMin { get; private set; } = 0.0f;
        /// <summary>
        /// Middle value (to adjust the colormap)
        /// </summary>
        public float Middle { get; private set; } = 0.0f;
        /// <summary>
        /// Maximum span value (to adjust the colormap)
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
        /// Set the span values all together
        /// </summary>
        /// <param name="min">Span min value</param>
        /// <param name="mid">Middle value</param>
        /// <param name="max">Span max value</param>
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
        /// <summary>
        /// Reset span values to their default values
        /// </summary>
        /// <param name="column">Column associated with this class</param>
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
            float middle = column.ActivityValuesOfUnmaskedSites.Mean();
            Vector2 limits = column.ActivityValuesOfUnmaskedSites.CalculateValueLimit();
            Middle = middle;
            SpanMin = Mathf.Clamp(limits[0], MinimumAmplitude, MaximumAmplitude);
            SpanMax = Mathf.Clamp(limits[1], MinimumAmplitude, MaximumAmplitude);
            OnUpdateSpanValues.Invoke();
        }
        #endregion
    }
}