using HBP.Module3D;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class ThresholdIEEG : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Texture to be applied to the image
        /// </summary>
        private Texture2D m_IEEGHistogram;
        /// <summary>
        /// Minimum Span value
        /// </summary>
        private float m_SpanMinFactor = 0.0f;
        private float SpanMin { get { return m_SpanMinFactor * m_Amplitude + m_MinAmplitude; } }
        /// <summary>
        /// Middle Span value
        /// </summary>
        private float m_MiddleFactor = 0.5f;
        private float Middle { get { return m_MiddleFactor * m_Amplitude + m_MinAmplitude; } }
        /// <summary>
        /// Maximum Span value
        /// </summary>
        private float m_SpanMaxFactor = 1.0f;
        private float SpanMax { get { return m_SpanMaxFactor * m_Amplitude + m_MinAmplitude; } }
        /// <summary>
        /// Minimum value
        /// </summary>
        private float m_MinAmplitude = float.MinValue;
        /// <summary>
        /// Maximum value
        /// </summary>
        private float m_MaxAmplitude = float.MaxValue;
        /// <summary>
        /// Amplitude
        /// </summary>
        private float m_Amplitude = 1.0f;
        /// <summary>
        /// Is the module initialized ?
        /// </summary>
        private bool m_Initialized = false;

        private Dictionary<Column3DIEEG, Texture2D> m_HistogramByColumn = new Dictionary<Column3DIEEG, Texture2D>();

        /// <summary>
        /// IEEG Histogram
        /// </summary>
        [SerializeField] private RawImage m_Histogram;
        /// <summary>
        /// Symmetry toggle
        /// </summary>
        [SerializeField] private Toggle m_SymmetryToggle;
        /// <summary>
        /// Text field for the min value
        /// </summary>
        [SerializeField] private Text m_MinText;
        /// <summary>
        /// Text field for the max value
        /// </summary>
        [SerializeField] private Text m_MaxText;
        /// <summary>
        /// Input field for the span min value
        /// </summary>
        [SerializeField] private InputField m_SpanMinInput;
        /// <summary>
        /// Input field for the middle value
        /// </summary>
        [SerializeField] private InputField m_MiddleInput;
        /// <summary>
        /// Input field for the span max value
        /// </summary>
        [SerializeField] private InputField m_SpanMaxInput;
        /// <summary>
        /// Input field for the amplitude
        /// </summary>
        [SerializeField] private InputField m_AmplitudeInput;
        /// <summary>
        /// Zone in which the handlers can move
        /// </summary>
        [SerializeField] private RectTransform m_HandlerZone;
        /// <summary>
        /// Handler responsible for the minimum value
        /// </summary>
        [SerializeField] private ThresholdHandler m_MinHandler;
        /// <summary>
        /// Handler responsible for the middle value
        /// </summary>
        [SerializeField] private ThresholdHandler m_MidHandler;
        /// <summary>
        /// Handler responsible for the maximum value
        /// </summary>
        [SerializeField] private ThresholdHandler m_MaxHandler;

        public GenericEvent<float, float, float> OnChangeValues = new GenericEvent<float, float, float>();
        #endregion

        #region Private Methods
        /// <summary>
        /// Update IEEG Histogram Texture
        /// </summary>
        private void UpdateIEEGHistogram()
        {
            UnityEngine.Profiling.Profiler.BeginSample("IEEG HISTOGRAM");
            Column3DIEEG column = (Column3DIEEG)ApplicationState.Module3D.SelectedColumn;
            if (!m_HistogramByColumn.TryGetValue(column, out m_IEEGHistogram))
            {
                float[] iEEGValues = column.IEEGValuesOfUnmaskedSites;
                if (!m_IEEGHistogram)
                {
                    m_IEEGHistogram = new Texture2D(1, 1);
                }
                if (iEEGValues.Length > 0)
                {
                    HBP.Module3D.DLL.Texture.GenerateDistributionHistogram(iEEGValues, 4 * 110, 4 * 110, m_MinAmplitude, m_MaxAmplitude).UpdateTexture2D(m_IEEGHistogram);
                }
                else
                {
                    m_IEEGHistogram = Texture2D.blackTexture;
                }
                m_HistogramByColumn.Add(column, m_IEEGHistogram);
            }
            m_Histogram.texture = m_IEEGHistogram;
            UnityEngine.Profiling.Profiler.EndSample();
        }
        #endregion

        #region Public Methods
        public void Initialize()
        {
            m_IEEGHistogram = new Texture2D(1, 1);

            m_MinHandler.MinimumPosition = float.MinValue;
            m_MinHandler.MaximumPosition = 1.0f;
            m_MaxHandler.MinimumPosition = 0.0f;
            m_MaxHandler.MaximumPosition = float.MaxValue;
            m_MidHandler.MinimumPosition = float.MinValue;
            m_MidHandler.MaximumPosition = float.MaxValue;

            m_SpanMinInput.onEndEdit.AddListener((value) =>
            {
                float val;
                global::Tools.Unity.NumberExtension.TryParseFloat(value, out val);
                if (val > Middle) val = Middle;
                m_SpanMinInput.text = val.ToString("N2");
                m_SpanMinFactor = (val - m_MinAmplitude) / m_Amplitude;

                m_MinHandler.Position = m_SpanMinFactor;
                m_MidHandler.MinimumPosition = m_SpanMinFactor;
                m_MidHandler.ClampPosition();

                if (m_Initialized)
                {
                    OnChangeValues.Invoke(SpanMin, Middle, SpanMax);
                }
            });

            m_MiddleInput.onEndEdit.AddListener((value) =>
            {
                float val;
                global::Tools.Unity.NumberExtension.TryParseFloat(value, out val);
                val = Mathf.Clamp(val, SpanMin, SpanMax);
                m_MiddleInput.text = val.ToString("N2");
                m_MiddleFactor = (val - m_MinAmplitude) / m_Amplitude;

                m_MidHandler.Position = m_MiddleFactor;
                m_MinHandler.MaximumPosition = m_MiddleFactor;
                m_MinHandler.ClampPosition();
                m_MaxHandler.MinimumPosition = m_MiddleFactor;
                m_MaxHandler.ClampPosition();

                if (m_Initialized)
                {
                    OnChangeValues.Invoke(SpanMin, Middle, SpanMax);
                }
            });

            m_SpanMaxInput.onEndEdit.AddListener((value) =>
            {
                float val;
                global::Tools.Unity.NumberExtension.TryParseFloat(value, out val);
                if (val < Middle) val = Middle;
                m_SpanMaxInput.text = val.ToString("N2");
                m_SpanMaxFactor = (val - m_MinAmplitude) / m_Amplitude;

                m_MaxHandler.Position = m_SpanMaxFactor;
                m_MidHandler.MaximumPosition = m_SpanMaxFactor;
                m_MidHandler.ClampPosition();

                if (m_Initialized)
                {
                    OnChangeValues.Invoke(SpanMin, Middle, SpanMax);
                }
            });

            m_AmplitudeInput.onEndEdit.AddListener((value) =>
            {
                float val;
                global::Tools.Unity.NumberExtension.TryParseFloat(value, out val);
                m_AmplitudeInput.text = val.ToString("N2");
                float minVal = Middle - val;
                float maxVal = Middle + val;
                m_SpanMinInput.text = minVal.ToString("N2");
                m_SpanMaxInput.text = maxVal.ToString("N2");
                m_SpanMinInput.onEndEdit.Invoke(m_SpanMinInput.text);
                m_SpanMaxInput.onEndEdit.Invoke(m_SpanMaxInput.text);
            });

            m_MinHandler.OnChangePosition.AddListener((deplacement) =>
            {
                m_SpanMinFactor = m_MinHandler.Position;
                m_SpanMinInput.text = SpanMin.ToString("N2");
                m_SpanMinInput.onEndEdit.Invoke(m_SpanMinInput.text);
                if (m_SymmetryToggle.isOn)
                {
                    m_MaxHandler.Position = m_MidHandler.Position + (m_MidHandler.Position - m_MinHandler.Position);
                    m_SpanMaxFactor = m_MaxHandler.Position;
                    m_SpanMaxInput.text = SpanMax.ToString("N2");
                    m_SpanMaxInput.onEndEdit.Invoke(m_SpanMaxInput.text);
                    m_AmplitudeInput.text = (SpanMax - SpanMin).ToString("N2");
                }
            });

            m_MidHandler.OnChangePosition.AddListener((deplacement) =>
            {
                m_MiddleFactor = m_MidHandler.Position;
                m_MiddleInput.text = Middle.ToString("N2");
                if (m_SymmetryToggle.isOn)
                {
                    m_MinHandler.Position += deplacement;
                    m_SpanMinFactor = m_MinHandler.Position;
                    m_SpanMinInput.text = SpanMin.ToString("N2");
                    m_SpanMinInput.onEndEdit.Invoke(m_SpanMinInput.text);

                    m_MaxHandler.Position += deplacement;
                    m_SpanMaxFactor = m_MaxHandler.Position;
                    m_SpanMaxInput.text = SpanMax.ToString("N2");
                    m_SpanMaxInput.onEndEdit.Invoke(m_SpanMaxInput.text);
                }
                m_MiddleInput.onEndEdit.Invoke(m_MiddleInput.text);
            });

            m_MaxHandler.OnChangePosition.AddListener((deplacement) =>
            {
                m_SpanMaxFactor = m_MaxHandler.Position;
                m_SpanMaxInput.text = SpanMax.ToString("N2");
                m_SpanMaxInput.onEndEdit.Invoke(m_SpanMaxInput.text);
                if (m_SymmetryToggle.isOn)
                {
                    m_MinHandler.Position = m_MidHandler.Position - (m_MaxHandler.Position - m_MidHandler.Position);
                    m_SpanMinFactor = m_MinHandler.Position;
                    m_SpanMinInput.text = SpanMin.ToString("N2");
                    m_SpanMinInput.onEndEdit.Invoke(m_SpanMinInput.text);
                    m_AmplitudeInput.text = (SpanMax - SpanMin).ToString("N2");
                }
            });

            ApplicationState.Module3D.OnRemoveScene.AddListener((s) =>
            {
                foreach (var column in s.ColumnManager.ColumnsIEEG)
                {
                    Texture2D texture;
                    if (m_HistogramByColumn.TryGetValue(column, out texture))
                    {
                        Destroy(texture);
                        m_HistogramByColumn.Remove(column);
                    }
                }
            });
        }
        /// <summary>
        /// Update IEEG values
        /// </summary>
        /// <param name="values">IEEG data values</param>
        public void UpdateIEEGValues(IEEGDataParameters values)
        {
            m_Initialized = false;

            m_MinAmplitude = values.MinimumAmplitude;
            m_MaxAmplitude = values.MaximumAmplitude;
            m_Amplitude = m_MaxAmplitude - m_MinAmplitude;
            m_SpanMinFactor = (values.SpanMin - m_MinAmplitude) / m_Amplitude;
            m_MiddleFactor = (values.Middle - m_MinAmplitude) / m_Amplitude;
            m_SpanMaxFactor = (values.SpanMax - m_MinAmplitude) / m_Amplitude;

            m_MinText.text = m_MinAmplitude.ToString("N2");
            m_MaxText.text = m_MaxAmplitude.ToString("N2");
            m_SpanMinInput.text = values.SpanMin.ToString("N2");
            m_MiddleInput.text = values.Middle.ToString("N2");
            m_SpanMaxInput.text = values.SpanMax.ToString("N2");
            m_AmplitudeInput.text = ((values.SpanMax - values.SpanMin) / 2).ToString("N2");

            m_MiddleInput.onEndEdit.Invoke(m_MiddleInput.text);
            m_SpanMinInput.onEndEdit.Invoke(m_SpanMinInput.text);
            m_SpanMaxInput.onEndEdit.Invoke(m_SpanMaxInput.text);
            m_MiddleInput.onEndEdit.Invoke(m_MiddleInput.text);

            UpdateIEEGHistogram();

            m_Initialized = true;
        }
        #endregion
    }
}