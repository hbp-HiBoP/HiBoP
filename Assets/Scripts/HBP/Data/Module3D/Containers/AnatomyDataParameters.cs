using UnityEngine;
using UnityEngine.Events;
using HBP.Core.Data;
using HBP.Core.Tools;

namespace HBP.Data.Module3D
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

        public const float ReferenceInfluenceDistance = 15f;
        public float ReferenceMaximum { get; private set; }

        private float? m_ProjectionMaximum;

        // Before the first projection, absolute thresholds use the fixed reference scale.
        public float DensityMaximum => m_ProjectionMaximum ?? ReferenceMaximum;
        public float SpanMinRatio { get; private set; } = .05f;
        public float MiddleRatio { get; private set; } = .35f;
        public float SpanMaxRatio { get; private set; } = .65f;
        public float SpanMin => SpanMinRatio * DensityMaximum;
        public float Middle => MiddleRatio * DensityMaximum;
        public float SpanMax => SpanMaxRatio * DensityMaximum;

        public UnityEvent OnUpdateSpanValues = new();

        public void SetSpanRatios(float min, float middle, float max)
        {
            if (!ThresholdRatioUtility.IsFinite(min) || !ThresholdRatioUtility.IsFinite(middle) || !ThresholdRatioUtility.IsFinite(max) || min < 0 || min > middle || middle > max)
                throw new System.ArgumentOutOfRangeException(nameof(min), "Density ratios must be finite, nonnegative and ordered.");
            if (SpanMinRatio == min && MiddleRatio == middle && SpanMaxRatio == max) return;
            SpanMinRatio = min;
            MiddleRatio = middle;
            SpanMaxRatio = max;
            OnUpdateSpanValues.Invoke();
        }

        public void SetSpanValues(float min, float middle, float max)
        {
            if (DensityMaximum > 0f) SetSpanRatios(min / DensityMaximum, middle / DensityMaximum, max / DensityMaximum);
        }

        public void ResetSpanValues() => SetSpanRatios(.05f, .35f, .65f);

        public void PublishReference(float maximum)
        {
            if (!ThresholdRatioUtility.IsFinite(maximum) || maximum < 0f) throw new System.ArgumentOutOfRangeException(nameof(maximum));
            ReferenceMaximum = maximum;
        }

        public void PublishProjectionMaximum(float? maximum)
        {
            if (maximum.HasValue && (!ThresholdRatioUtility.IsFinite(maximum.Value) || maximum.Value < 0f)) throw new System.ArgumentOutOfRangeException(nameof(maximum));
            m_ProjectionMaximum = maximum;
        }

        public void LoadConfiguration(AnatomicConfiguration configuration)
        {
            if (ThresholdRatioUtility.Validate(configuration.SpanMinRatio, configuration.MiddleRatio, configuration.SpanMaxRatio))
                SetSpanRatios(configuration.SpanMinRatio, configuration.MiddleRatio, configuration.SpanMaxRatio);
            else ResetSpanValues();
        }

        public void CaptureConfiguration(AnatomicConfiguration configuration)
        {
            configuration.MaximumInfluence = InfluenceDistance;
            configuration.SpanMinRatio = SpanMinRatio;
            configuration.MiddleRatio = MiddleRatio;
            configuration.SpanMaxRatio = SpanMaxRatio;
        }

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

        #endregion

        #region Events

        /// <summary>
        /// Event called when updating the maximum influence
        /// </summary>
        public UnityEvent OnUpdateInfluenceDistance = new();

        #endregion
    }
}
