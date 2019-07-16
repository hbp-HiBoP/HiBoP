using HBP.Module3D;
using System.Collections.Generic;
using Tools.Unity;
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

        private Dictionary<string, Texture2D> m_HistogramByColumn = new Dictionary<string, Texture2D>();

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
            Column3DDynamic column = (Column3DDynamic)ApplicationState.Module3D.SelectedColumn;
            string histogramID = column.name + "_" + (column is Column3DCCEP columnCCEP && columnCCEP.IsSourceSelected ? columnCCEP.SelectedSource.Information.ChannelName : "");
            if (!m_HistogramByColumn.TryGetValue(histogramID, out m_IEEGHistogram))
            {
                float[] iEEGValues = column.IEEGValuesOfUnmaskedSites;
                if (!m_IEEGHistogram)
                {
                    m_IEEGHistogram = new Texture2D(1, 1);
                }
                if (iEEGValues.Length > 0)
                {
                    HBP.Module3D.DLL.Texture texture = HBP.Module3D.DLL.Texture.GenerateDistributionHistogram(iEEGValues, 4 * 110, 4 * 110, m_MinAmplitude, m_MaxAmplitude);
                    texture.UpdateTexture2D(m_IEEGHistogram);
                    texture.Dispose();
                }
                else
                {
                    m_IEEGHistogram = Texture2D.blackTexture;
                }
                m_HistogramByColumn.Add(histogramID, m_IEEGHistogram);
            }
            m_Histogram.texture = m_IEEGHistogram;
            UnityEngine.Profiling.Profiler.EndSample();
        }
        /// <summary>
        /// Set the values of the threshold
        /// </summary>
        /// <param name="minFactor"></param>
        /// <param name="middleFactor"></param>
        /// <param name="maxFactor"></param>
        private void SetValues(float minFactor, float middleFactor, float maxFactor)
        {
            // Logical values
            m_SpanMinFactor = minFactor;
            m_MiddleFactor = middleFactor;
            m_SpanMaxFactor = maxFactor;

            // Handlers
            m_MinHandler.MaximumPosition = middleFactor;
            m_MidHandler.MinimumPosition = minFactor;
            m_MidHandler.MaximumPosition = maxFactor;
            m_MaxHandler.MinimumPosition = middleFactor;
            m_MinHandler.Position = minFactor;
            m_MidHandler.Position = middleFactor;
            m_MaxHandler.Position = maxFactor;

            // Textfields
            m_SpanMinInput.text = SpanMin.ToString("N2");
            m_MiddleInput.text = Middle.ToString("N2");
            m_SpanMaxInput.text = SpanMax.ToString("N2");
            m_AmplitudeInput.text = ((SpanMax - SpanMin) / 2).ToString("N2");

            // Event
            if (m_Initialized)
            {
                OnChangeValues.Invoke(SpanMin, Middle, SpanMax);
            }
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
                if (NumberExtension.TryParseFloat(value, out float spanMinValue))
                {
                    if (spanMinValue > Middle) spanMinValue = Middle;
                    float spanMinFactor = (spanMinValue - m_MinAmplitude) / m_Amplitude;
                    SetValues(spanMinFactor, m_MiddleFactor, m_SpanMaxFactor);
                }
                else
                {
                    SetValues(m_SpanMinFactor, m_MiddleFactor, m_SpanMaxFactor);
                }
            });

            m_MiddleInput.onEndEdit.AddListener((value) =>
            {
                if (NumberExtension.TryParseFloat(value, out float middleValue))
                {
                    if (m_SymmetryToggle.isOn)
                    {
                        float middleFactor = (middleValue - m_MinAmplitude) / m_Amplitude;
                        float factorAmplitude = m_SpanMaxFactor - m_SpanMinFactor;
                        float spanMinFactor = middleFactor - (factorAmplitude / 2);
                        float spanMaxFactor = middleFactor + (factorAmplitude / 2);
                        SetValues(spanMinFactor, middleFactor, spanMaxFactor);
                    }
                    else
                    {
                        middleValue = Mathf.Clamp(middleValue, SpanMin, SpanMax);
                        float middleFactor = (middleValue - m_MinAmplitude) / m_Amplitude;
                        SetValues(m_SpanMinFactor, middleFactor, m_SpanMaxFactor);
                    }
                }
                else
                {
                    SetValues(m_SpanMinFactor, m_MiddleFactor, m_SpanMaxFactor);
                }
            });

            m_SpanMaxInput.onEndEdit.AddListener((value) =>
            {
                if (NumberExtension.TryParseFloat(value, out float spanMaxValue))
                {
                    if (spanMaxValue < Middle) spanMaxValue = Middle;
                    float spanMaxFactor = (spanMaxValue - m_MinAmplitude) / m_Amplitude;
                    SetValues(m_SpanMinFactor, m_MiddleFactor, spanMaxFactor);
                }
                else
                {
                    SetValues(m_SpanMinFactor, m_MiddleFactor, m_SpanMaxFactor);
                }
            });

            m_AmplitudeInput.onEndEdit.AddListener((value) =>
            {
                if (NumberExtension.TryParseFloat(value, out float amplitudeValue))
                {
                    float spanMinValue = Middle - amplitudeValue;
                    float spanMaxValue = Middle + amplitudeValue;
                    float spanMinFactor = (spanMinValue - m_MinAmplitude) / m_Amplitude;
                    float spanMaxFactor = (spanMaxValue - m_MinAmplitude) / m_Amplitude;
                    SetValues(spanMinFactor, m_MiddleFactor, spanMaxFactor);
                }
                else
                {
                    SetValues(m_SpanMinFactor, m_MiddleFactor, m_SpanMaxFactor);
                }
            });

            m_MinHandler.OnChangePosition.AddListener((deplacement) =>
            {
                float spanMinFactor = m_MinHandler.Position;
                if (m_SymmetryToggle.isOn)
                {
                    float spanMaxFactor = m_MiddleFactor + (m_MiddleFactor - spanMinFactor);
                    SetValues(spanMinFactor, m_MiddleFactor, spanMaxFactor);
                }
                else
                {
                    SetValues(spanMinFactor, m_MiddleFactor, m_SpanMaxFactor);
                }
            });

            m_MidHandler.OnChangePosition.AddListener((deplacement) =>
            {
                float middleFactor = m_MidHandler.Position;
                if (m_SymmetryToggle.isOn)
                {
                    float spanMinFactor = m_SpanMinFactor + deplacement;
                    float spanMaxFactor = m_SpanMaxFactor + deplacement;
                    SetValues(spanMinFactor, middleFactor, spanMaxFactor);
                }
                else
                {
                    SetValues(m_SpanMinFactor, middleFactor, m_SpanMaxFactor);
                }
            });

            m_MaxHandler.OnChangePosition.AddListener((deplacement) =>
            {
                float spanMaxFactor = m_MaxHandler.Position;
                if (m_SymmetryToggle.isOn)
                {
                    float spanMinFactor = m_MiddleFactor - (spanMaxFactor - m_MiddleFactor);
                    SetValues(spanMinFactor, m_MiddleFactor, spanMaxFactor);
                }
                else
                {
                    SetValues(m_SpanMinFactor, m_MiddleFactor, spanMaxFactor);
                }
            });

            ApplicationState.Module3D.OnRemoveScene.AddListener((s) =>
            {
                foreach (var column in s.ColumnManager.ColumnsDynamic)
                {
                    string histogramID = column.name + "_" + (column is Column3DCCEP columnCCEP && columnCCEP.IsSourceSelected ? columnCCEP.SelectedSource.Information.ChannelName : "");
                    Texture2D texture;
                    if (m_HistogramByColumn.TryGetValue(histogramID, out texture))
                    {
                        Destroy(texture);
                        m_HistogramByColumn.Remove(histogramID);
                    }
                }
            });
        }
        /// <summary>
        /// Update IEEG values
        /// </summary>
        /// <param name="values">IEEG data values</param>
        public void UpdateIEEGValues(DynamicDataParameters values)
        {
            m_Initialized = false;

            // Fixed values
            m_MinAmplitude = values.MinimumAmplitude;
            m_MaxAmplitude = values.MaximumAmplitude;
            m_Amplitude = m_MaxAmplitude - m_MinAmplitude;
            m_MinText.text = m_MinAmplitude.ToString("N2");
            m_MaxText.text = m_MaxAmplitude.ToString("N2");

            // Non-fixed values
            SetValues((values.SpanMin - m_MinAmplitude) / m_Amplitude, (values.Middle - m_MinAmplitude) / m_Amplitude, (values.SpanMax - m_MinAmplitude) / m_Amplitude);

            UpdateIEEGHistogram();

            m_Initialized = true;
        }
        #endregion
    }
}