using HBP.Display.Module3D;
using System.Collections.Generic;
using Tools.CSharp;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Toolbar
{
    public class ThresholdFMRI : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Texture to be applied to the image
        /// </summary>
        private Texture2D m_MRIHistogram;
        /// <summary>
        /// Minimum value of the FMRI
        /// </summary>
        private float m_Min = -1f;
        /// <summary>
        /// Maximum value of the FMRI
        /// </summary>
        private float m_Max = 1f;
        /// <summary>
        /// Minimum Cal value
        /// </summary>
        private float m_NegativeMin = 0.0f;
        private float NegativeMinValue { get { return m_NegativeMin * m_Min; } }
        /// <summary>
        /// Maximum Cal value
        /// </summary>
        private float m_NegativeMax = 1.0f;
        private float NegativeMaxValue { get { return m_NegativeMax * m_Min; } }
        /// <summary>
        /// Minimum Cal value
        /// </summary>
        private float m_PositiveMin = 0.0f;
        private float PositiveMinValue { get { return m_PositiveMin * m_Max; } }
        /// <summary>
        /// Maximum Cal value
        /// </summary>
        private float m_PositiveMax = 1.0f;
        private float PositiveMaxValue { get { return m_PositiveMax * m_Max; } }
        /// <summary>
        /// Textures of the histograms (one per MRI)
        /// </summary>
        private Dictionary<Core.Object3D.FMRI, Texture2D> m_HistogramByFMRI = new Dictionary<Core.Object3D.FMRI, Texture2D>();

        private Queue<Core.Object3D.FMRI> m_HistogramsToBeDestroyed = new Queue<Core.Object3D.FMRI>();

        /// <summary>
        /// Used to display the current histogram
        /// </summary>
        [SerializeField] private RawImage m_Histogram;

        [SerializeField] private Text m_MinText;
        [SerializeField] private Text m_MaxText;
        
        [SerializeField] private RectTransform m_NegativeFields;
        [SerializeField] private InputField m_NegativeMinInputfield;
        [SerializeField] private InputField m_NegativeMaxInputfield;

        [SerializeField] private RectTransform m_PositiveFields;
        [SerializeField] private InputField m_PositiveMinInputfield;
        [SerializeField] private InputField m_PositiveMaxInputfield;

        /// <summary>
        /// Zone in which the handlers can move
        /// </summary>
        [SerializeField] private RectTransform m_NegativeHandlerZone;
        /// <summary>
        /// Handler responsible for the minimum value
        /// </summary>
        [SerializeField] private ThresholdHandler m_NegativeMinHandler;
        /// <summary>
        /// Handler responsible for the maximum value
        /// </summary>
        [SerializeField] private ThresholdHandler m_NegativeMaxHandler;
        /// <summary>
        /// Zone in which the handlers can move
        /// </summary>
        [SerializeField] private RectTransform m_PositiveHandlerZone;
        /// <summary>
        /// Handler responsible for the minimum value
        /// </summary>
        [SerializeField] private ThresholdHandler m_PositiveMinHandler;
        /// <summary>
        /// Handler responsible for the maximum value
        /// </summary>
        [SerializeField] private ThresholdHandler m_PositiveMaxHandler;

        /// <summary>
        /// Is the module initialized ?
        /// </summary>
        private bool m_Initialized;
        #endregion

        #region Events
        public GenericEvent<float, float, float, float> OnChangeValues = new GenericEvent<float, float, float, float>();
        #endregion

        #region Private Methods
        /// <summary>
        /// Update MRI Histogram Texture
        /// </summary>
        private void UpdateMRIHistogram(Core.Object3D.FMRI fmri)
        {
            UnityEngine.Profiling.Profiler.BeginSample("HISTOGRAM FMRI");
            if (!m_HistogramByFMRI.TryGetValue(fmri, out m_MRIHistogram))
            {
                if (!m_MRIHistogram)
                {
                    m_MRIHistogram = new Texture2D(1, 1);
                }
                Core.DLL.Texture texture = Core.DLL.Texture.GenerateDistributionHistogram(fmri, 440, 440, false);
                texture.UpdateTexture2D(m_MRIHistogram);
                texture.Dispose();
                m_HistogramByFMRI.Add(fmri, m_MRIHistogram);
            }
            m_Histogram.texture = m_MRIHistogram;
            UnityEngine.Profiling.Profiler.EndSample();
        }

        private void SetNegativeValues(float min, float max)
        {
            // Logical values
            m_NegativeMin = min;
            m_NegativeMax = max;

            // Handlers
            m_NegativeMinHandler.MinimumPosition = 1f - m_NegativeMax;
            m_NegativeMaxHandler.MaximumPosition = 1f - m_NegativeMin;
            m_NegativeMinHandler.Position = 1f - m_NegativeMin;
            m_NegativeMaxHandler.Position = 1f - m_NegativeMax;

            // Text fields
            m_NegativeMinInputfield.text = NegativeMinValue.ToString("N2");
            m_NegativeMaxInputfield.text = NegativeMaxValue.ToString("N2");

            // Event
            if (m_Initialized)
            {
                OnChangeValues.Invoke(m_NegativeMin, m_NegativeMax, m_PositiveMin, m_PositiveMax);
            }
        }
        private void SetPositiveValues(float min, float max)
        {
            // Logical values
            m_PositiveMin = min;
            m_PositiveMax = max;

            // Handlers
            m_PositiveMinHandler.MaximumPosition = m_PositiveMax;
            m_PositiveMaxHandler.MinimumPosition = m_PositiveMin;
            m_PositiveMinHandler.Position = m_PositiveMin;
            m_PositiveMaxHandler.Position = m_PositiveMax;

            // Text fields
            m_PositiveMinInputfield.text = PositiveMinValue.ToString("N2");
            m_PositiveMaxInputfield.text = PositiveMaxValue.ToString("N2");

            // Event
            if (m_Initialized)
            {
                OnChangeValues.Invoke(m_NegativeMin, m_NegativeMax, m_PositiveMin, m_PositiveMax);
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize this module
        /// </summary>
        public void Initialize()
        {
            m_NegativeMinHandler.MinimumPosition = 0.0f;
            m_NegativeMinHandler.MaximumPosition = 1.0f;
            m_NegativeMaxHandler.MinimumPosition = 0.0f;
            m_NegativeMaxHandler.MaximumPosition = 1.0f;
            m_PositiveMinHandler.MinimumPosition = 0.0f;
            m_PositiveMinHandler.MaximumPosition = 1.0f;
            m_PositiveMaxHandler.MinimumPosition = 0.0f;
            m_PositiveMaxHandler.MaximumPosition = 1.0f;

            m_NegativeMinHandler.OnChangePosition.AddListener((deplacement) =>
            {
                SetNegativeValues(1f - m_NegativeMinHandler.Position, m_NegativeMax);
            });
            m_NegativeMaxHandler.OnChangePosition.AddListener((deplacement) =>
            {
                SetNegativeValues(m_NegativeMin, 1f - m_NegativeMaxHandler.Position);
            });
            m_NegativeMinInputfield.onEndEdit.AddListener((value) =>
            {
                if (NumberExtension.TryParseFloat(value, out float result))
                {
                    float negativeMin = result / m_Min;
                    SetNegativeValues(negativeMin, m_NegativeMax);
                }
                else
                {
                    SetNegativeValues(m_NegativeMin, m_NegativeMax);
                }
            });
            m_NegativeMaxInputfield.onEndEdit.AddListener((value) =>
            {
                if (NumberExtension.TryParseFloat(value, out float result))
                {
                    float negativeMax = result / m_Min;
                    SetNegativeValues(m_NegativeMin, negativeMax);
                }
                else
                {
                    SetNegativeValues(m_NegativeMin, m_NegativeMax);
                }
            });

            m_PositiveMinHandler.OnChangePosition.AddListener((deplacement) =>
            {
                SetPositiveValues(m_PositiveMinHandler.Position, m_PositiveMax);
            });
            m_PositiveMaxHandler.OnChangePosition.AddListener((deplacement) =>
            {
                SetPositiveValues(m_PositiveMin, m_PositiveMaxHandler.Position);
            });
            m_PositiveMinInputfield.onEndEdit.AddListener((value) =>
            {
                if (NumberExtension.TryParseFloat(value, out float result))
                {
                    float positiveMin = result / m_Max;
                    SetPositiveValues(positiveMin, m_PositiveMax);
                }
                else
                {
                    SetPositiveValues(m_PositiveMin, m_PositiveMax);
                }
            });
            m_PositiveMaxInputfield.onEndEdit.AddListener((value) =>
            {
                if (NumberExtension.TryParseFloat(value, out float result))
                {
                    float positiveMax = result / m_Max;
                    SetPositiveValues(m_PositiveMin, positiveMax);
                }
                else
                {
                    SetPositiveValues(m_PositiveMin, m_PositiveMax);
                }
            });

            Module3DMain.OnRemoveScene.AddListener((s) =>
            {
                foreach (var column in s.ColumnsFMRI)
                {
                    foreach (var fmri in column.ColumnFMRIData.Data.FMRIs)
                    {
                        m_HistogramsToBeDestroyed.Enqueue(fmri.Item1);
                    }
                }
            });
        }
        /// <summary>
        /// Update Maximum and Minimum Cal value
        /// </summary>
        /// <param name="values">Cal values</param>
        public void UpdateFMRICalValues(Core.Object3D.FMRI fmri, float negativeMin, float negativeMax, float positiveMin, float positiveMax, bool updateHistogram = true)
        {
            m_Initialized = false;

            // Fixed values
            Core.Tools.MRICalValues values = fmri.NIFTI.ExtremeValues;
            m_Min = values.Min;
            m_Max = values.Max;
            m_MinText.text = m_Min.ToString("N2");
            m_MaxText.text = m_Max.ToString("N2");

            // Set UI elements
            m_NegativeHandlerZone.gameObject.SetActive(true);
            m_NegativeFields.gameObject.SetActive(true);
            m_PositiveHandlerZone.gameObject.SetActive(true);
            m_PositiveFields.gameObject.SetActive(true);
            if (m_Min >= 0)
            {
                m_NegativeHandlerZone.gameObject.SetActive(false);
                m_NegativeFields.gameObject.SetActive(false);
                m_PositiveHandlerZone.anchorMin = new Vector2(0, m_PositiveHandlerZone.anchorMin.y);
            }
            else if (m_Max <= 0)
            {
                m_PositiveHandlerZone.gameObject.SetActive(false);
                m_PositiveFields.gameObject.SetActive(false);
                m_NegativeHandlerZone.anchorMax = new Vector2(1, m_NegativeHandlerZone.anchorMax.y);
            }
            else
            {
                float negativeWeight = values.Min / (values.Min - values.Max);
                m_NegativeHandlerZone.anchorMax = new Vector2(negativeWeight, m_NegativeHandlerZone.anchorMax.y);
                m_PositiveHandlerZone.anchorMin = new Vector2(negativeWeight, m_PositiveHandlerZone.anchorMin.y);
            }

            // Non-fixed values
            SetNegativeValues(negativeMin, negativeMax);
            SetPositiveValues(positiveMin, positiveMax);

            if (updateHistogram) UpdateMRIHistogram(fmri);

            m_Initialized = true;
        }
        /// <summary>
        /// Method used to clean useless histograms
        /// </summary>
        public void CleanHistograms()
        {
            while (m_HistogramsToBeDestroyed.Count > 0)
            {
                Core.Object3D.FMRI histogramID = m_HistogramsToBeDestroyed.Dequeue();
                if (m_HistogramByFMRI.TryGetValue(histogramID, out Texture2D texture))
                {
                    DestroyImmediate(texture);
                    m_HistogramByFMRI.Remove(histogramID);
                }
            }
        }
        #endregion
    }
}