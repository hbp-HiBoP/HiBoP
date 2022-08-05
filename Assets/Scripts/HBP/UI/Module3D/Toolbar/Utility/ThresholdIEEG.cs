using HBP.Module3D;
using System.Collections.Generic;
using Tools.CSharp;
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
        /// <summary>
        /// Textures of the histograms (iEEG: one per column; CCEP: one per column per selected source))
        /// </summary>
        private Dictionary<string, Texture2D> m_Histograms = new Dictionary<string, Texture2D>();

        private Queue<string> m_HistogramsToBeDestroyed = new Queue<string>();

        /// <summary>
        /// Used to display the current histogram
        /// </summary>
        [SerializeField] private RawImage m_Histogram;
        /// <summary>
        /// Used to set the thresholds either with min/middle/max (assymmetry) or middle/amplitude (symmetry)
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
        #endregion

        #region Events
        /// <summary>
        /// Event called when changing at least one of the three threshold values
        /// </summary>
        public GenericEvent<float, float, float> OnChangeValues = new GenericEvent<float, float, float>();
        #endregion

        #region Private Methods
        /// <summary>
        /// Update IEEG Histogram Texture
        /// </summary>
        private void UpdateIEEGHistogram(Column3DDynamic column)
        {
            UnityEngine.Profiling.Profiler.BeginSample("IEEG HISTOGRAM");
            string histogramID = GenerateHistogramID(column);
            if (!m_Histograms.TryGetValue(histogramID, out m_IEEGHistogram))
            {
                float[] iEEGValues = column.ActivityValuesOfUnmaskedSites;
                if (!m_IEEGHistogram)
                {
                    m_IEEGHistogram = new Texture2D(1, 1);
                }
                if (iEEGValues.Length > 0)
                {
                    Core.DLL.Texture texture = Core.DLL.Texture.GenerateDistributionHistogram(iEEGValues, 440, 440, m_MinAmplitude, m_MaxAmplitude);
                    texture.UpdateTexture2D(m_IEEGHistogram);
                    texture.Dispose();
                }
                else
                {
                    m_IEEGHistogram = Texture2D.blackTexture;
                }
                m_Histograms.Add(histogramID, m_IEEGHistogram);
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
        /// <summary>
        /// Generate a unique histogram ID for the column
        /// </summary>
        /// <param name="column">Column of the histogram</param>
        /// <returns>Unique ID</returns>
        private string GenerateHistogramID(Column3D column)
        {
            string histogramID = column.ColumnData.ID;
            if (column is Column3DCCEP columnCCEP)
            {
                if (columnCCEP.IsSourceSiteSelected) histogramID += columnCCEP.SelectedSourceSite.Information.Name;
                else if (columnCCEP.IsSourceMarsAtlasLabelSelected) histogramID += columnCCEP.SelectedSourceMarsAtlasLabel;
            }
            return histogramID;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize this module
        /// </summary>
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

            HBP3DModule.OnRemoveScene.AddListener((s) =>
            {
                foreach (var column in s.ColumnsDynamic)
                {
                    m_HistogramsToBeDestroyed.Enqueue(GenerateHistogramID(column));
                }
            });
        }
        /// <summary>
        /// Update IEEG values
        /// </summary>
        /// <param name="values">IEEG data values</param>
        public void UpdateIEEGValues(Column3DDynamic column)
        {
            m_Initialized = false;

            // Fixed values
            m_MinAmplitude = column.DynamicParameters.MinimumAmplitude;
            m_MaxAmplitude = column.DynamicParameters.MaximumAmplitude;
            m_Amplitude = m_MaxAmplitude - m_MinAmplitude;
            m_MinText.text = m_MinAmplitude.ToString("N2");
            m_MaxText.text = m_MaxAmplitude.ToString("N2");

            // Non-fixed values
            SetValues((column.DynamicParameters.SpanMin - m_MinAmplitude) / m_Amplitude, (column.DynamicParameters.Middle - m_MinAmplitude) / m_Amplitude, (column.DynamicParameters.SpanMax - m_MinAmplitude) / m_Amplitude);

            UpdateIEEGHistogram(column);

            m_Initialized = true;
        }
        /// <summary>
        /// Method used to clean useless histograms
        /// </summary>
        public void CleanHistograms()
        {
            while (m_HistogramsToBeDestroyed.Count > 0)
            {
                string histogramID = m_HistogramsToBeDestroyed.Dequeue();
                if (m_Histograms.TryGetValue(histogramID, out Texture2D texture))
                {
                    DestroyImmediate(texture);
                    m_Histograms.Remove(histogramID);
                }
            }
        }
        #endregion
    }
}