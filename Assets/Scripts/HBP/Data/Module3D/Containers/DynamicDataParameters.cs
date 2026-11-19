using HBP.Core.DLL;
using UnityEngine;
using UnityEngine.Events;
using HBP.Core.Data;
using HBP.Core.Tools;

namespace HBP.Data.Module3D
{
    /// <summary>
    /// Class containing the parameters for the activity of a dynamic column
    /// </summary>
    public class DynamicDataParameters
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
            get { return m_InfluenceDistance; }
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

        /// <summary>
        /// Minimum amplitude value (computed externally for this column when setting activity values)
        /// </summary>
        public float MinimumAmplitude { get; set; } = float.MinValue;

        /// <summary>
        /// Maximum amplitude value (computed externally for this column when setting activity values)
        /// </summary>
        public float MaximumAmplitude { get; set; } = float.MaxValue;

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

        private (float Min, float Middle, float Max)? m_SpanRatios;
        private bool m_HasConfiguredSpan;
        private bool m_HasAmplitudeData;

        #endregion

        #region Events

        /// <summary>
        /// Event called when updating the span values (min, mid or max)
        /// </summary>
        public UnityEvent OnUpdateSpanValues = new();

        /// <summary>
        /// Event called when updating the maximum influence
        /// </summary>
        public UnityEvent OnUpdateInfluenceDistance = new();

        #endregion

        #region Public Methods

        public void LoadConfiguration(DynamicConfiguration configuration) => LoadSpanValues(configuration.SpanMin, configuration.Middle, configuration.SpanMax, configuration.SpanMinRatio, configuration.MiddleRatio, configuration.SpanMaxRatio);

        public void LoadConfiguration(StaticConfiguration configuration) => LoadSpanValues(configuration.SpanMin, configuration.Middle, configuration.SpanMax, configuration.SpanMinRatio, configuration.MiddleRatio, configuration.SpanMaxRatio);

        public void CaptureConfiguration(DynamicConfiguration configuration)
        {
            configuration.SpanMinRatio = m_SpanRatios?.Min;
            configuration.MiddleRatio = m_SpanRatios?.Middle;
            configuration.SpanMaxRatio = m_SpanRatios?.Max;
            configuration.SpanMin = m_SpanRatios.HasValue ? 0 : SpanMin;
            configuration.Middle = m_SpanRatios.HasValue ? 0 : Middle;
            configuration.SpanMax = m_SpanRatios.HasValue ? 0 : SpanMax;
        }

        public void CaptureConfiguration(StaticConfiguration configuration)
        {
            configuration.SpanMinRatio = m_SpanRatios?.Min;
            configuration.MiddleRatio = m_SpanRatios?.Middle;
            configuration.SpanMaxRatio = m_SpanRatios?.Max;
            configuration.SpanMin = m_SpanRatios.HasValue ? 0 : SpanMin;
            configuration.Middle = m_SpanRatios.HasValue ? 0 : Middle;
            configuration.SpanMax = m_SpanRatios.HasValue ? 0 : SpanMax;
        }

        private void LoadSpanValues(float min, float middle, float max, float? minRatio, float? middleRatio, float? maxRatio)
        {
            m_SpanRatios = null;
            m_HasConfiguredSpan = ThresholdRatioUtility.Validate(minRatio, middleRatio, maxRatio);
            if (m_HasConfiguredSpan)
            {
                m_SpanRatios = (minRatio.Value, middleRatio.Value, maxRatio.Value);
                RestoreSpanRatios();
            }
            else if (!IsAutomaticSpan(min, middle, max))
            {
                SetSpanValues(min, middle, max);
            }
            else
            {
                RememberSpanRatios();
            }
        }

        /// <summary>Resolve pending thresholds only against actual data, after both bounds have been computed.</summary>
        public void RefreshSpanValues(Column3DDynamic column)
        {
            m_HasAmplitudeData = column.ActivityStatistics.Count > 0 && (column is not Column3DCCEP ccep || ccep.IsSourceSelected);
            if (!RefreshConfiguredSpan()) ResetSpanValues(column);
        }

        public void RefreshSpanValues(Column3DStatic column)
        {
            m_HasAmplitudeData = column.ActivityValuesOfUnmaskedSites.Length > 0;
            if (!RefreshConfiguredSpan()) ResetSpanValues(column);
        }

        private bool RefreshConfiguredSpan()
        {
            if (!m_HasConfiguredSpan) return false;
            if (m_SpanRatios.HasValue) RestoreSpanRatios();
            else RememberSpanRatios();
            return true;
        }

        private bool HasUsableAmplitudeRange => m_HasAmplitudeData && ThresholdRatioUtility.IsFinite(MinimumAmplitude) && ThresholdRatioUtility.IsFinite(MaximumAmplitude) && MaximumAmplitude > MinimumAmplitude;

        private void RememberSpanRatios()
        {
            if (!HasUsableAmplitudeRange) return;
            double range = (double)MaximumAmplitude - MinimumAmplitude;
            float min = (float)(((double)SpanMin - MinimumAmplitude) / range);
            float middle = (float)(((double)Middle - MinimumAmplitude) / range);
            float max = (float)(((double)SpanMax - MinimumAmplitude) / range);
            if (ThresholdRatioUtility.IsFinite(min) && ThresholdRatioUtility.IsFinite(middle) && ThresholdRatioUtility.IsFinite(max))
            {
                m_SpanRatios = (min, middle, max);
                m_HasConfiguredSpan = true;
            }
        }

        private void RestoreSpanRatios()
        {
            if (!HasUsableAmplitudeRange) return;
            var ratios = m_SpanRatios.Value;
            double range = (double)MaximumAmplitude - MinimumAmplitude;
            float min = (float)(MinimumAmplitude + ratios.Min * range);
            float middle = (float)(MinimumAmplitude + ratios.Middle * range);
            float max = (float)(MinimumAmplitude + ratios.Max * range);
            if (!ThresholdRatioUtility.IsFinite(min) || !ThresholdRatioUtility.IsFinite(middle) || !ThresholdRatioUtility.IsFinite(max)) return;
            SpanMin = min;
            Middle = middle;
            SpanMax = max;
            OnUpdateSpanValues.Invoke();
        }

        private static bool IsAutomaticSpan(float min, float middle, float max) => Mathf.Approximately(min, 0f) && Mathf.Approximately(middle, 0f) && Mathf.Approximately(max, 0f);

        /// <summary>
        /// Set the span values all together
        /// </summary>
        /// <param name="min">Span min value</param>
        /// <param name="mid">Middle value</param>
        /// <param name="max">Span max value</param>
        public void SetSpanValues(float min, float mid, float max)
        {
            if (IsAutomaticSpan(min, mid, max)) return;

            if (min > max) min = max;
            mid = Mathf.Clamp(mid, min, max);
            SpanMin = min;
            Middle = mid;
            SpanMax = max;
            m_HasConfiguredSpan = true;
            m_SpanRatios = null;
            RememberSpanRatios();
            OnUpdateSpanValues.Invoke();
        }

        /// <summary>Apply an already validated canonical span, including the all-zero state.</summary>
        public void ApplySynchronizedSpanValues(float min, float mid, float max)
        {
            if (min > mid || mid > max || float.IsNaN(min) || float.IsNaN(mid) || float.IsNaN(max) || float.IsInfinity(min) || float.IsInfinity(mid) || float.IsInfinity(max))
                throw new System.ArgumentOutOfRangeException(nameof(min), "Invalid synchronized span.");
            bool changed = SpanMin != min || Middle != mid || SpanMax != max;
            SpanMin = min;
            Middle = mid;
            SpanMax = max;
            m_HasConfiguredSpan = true;
            m_SpanRatios = null;
            RememberSpanRatios();
            if (changed) OnUpdateSpanValues.Invoke();
        }

        /// <summary>
        /// Reset span values to their default values
        /// </summary>
        /// <param name="column">Column associated with this class</param>
        public void ResetSpanValues(Column3DDynamic column)
        {
            m_HasConfiguredSpan = false;
            m_SpanRatios = null;
            m_HasAmplitudeData = column.ActivityStatistics.Count > 0 && (column is not Column3DCCEP ccep || ccep.IsSourceSelected);
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

            if (column.ActivityStatistics.Count == 0)
            {
                SpanMin = 0;
                Middle = 0;
                SpanMax = 0;
                OnUpdateSpanValues.Invoke();
                return;
            }

            float middle = column.ActivityStatistics.Mean;
            float offset = 1.959964f * Mathf.Abs(column.ActivityStatistics.StandardDeviation);
            if (offset == 0f || float.IsNaN(offset) || float.IsInfinity(offset))
                offset = 1f;
            Vector2 limits = new(middle - offset, middle + offset);
            Middle = middle;
            SpanMin = Mathf.Clamp(limits[0], MinimumAmplitude, MaximumAmplitude);
            SpanMax = Mathf.Clamp(limits[1], MinimumAmplitude, MaximumAmplitude);
            RememberSpanRatios();
            OnUpdateSpanValues.Invoke();
        }

        public void ResetSpanValues(Column3DStatic column)
        {
            m_HasConfiguredSpan = false;
            m_SpanRatios = null;
            m_HasAmplitudeData = column.ActivityValuesOfUnmaskedSites.Length > 0;
            if (column.ActivityValuesOfUnmaskedSites.Length == 0)
            {
                SpanMin = 0;
                Middle = 0;
                SpanMax = 0;
                OnUpdateSpanValues.Invoke();
                return;
            }

            float middle = column.ActivityValuesOfUnmaskedSites.Mean();
            Vector2 limits = column.ActivityValuesOfUnmaskedSites.CalculateValueLimit();
            Middle = middle;
            SpanMin = Mathf.Clamp(limits[0], MinimumAmplitude, MaximumAmplitude);
            SpanMax = Mathf.Clamp(limits[1], MinimumAmplitude, MaximumAmplitude);
            RememberSpanRatios();
            OnUpdateSpanValues.Invoke();
        }

        #endregion
    }
}
