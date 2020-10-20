using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class ThresholdFMRI : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Texture to be applied to the image
        /// </summary>
        private Texture2D m_MRIHistogram;
        /// <summary>
        /// Minimum Cal value
        /// </summary>
        private float m_NegativeMin = 0.0f;
        /// <summary>
        /// Maximum Cal value
        /// </summary>
        private float m_NegativeMax = 1.0f;
        /// <summary>
        /// Minimum Cal value
        /// </summary>
        private float m_PositiveMin = 0.0f;
        /// <summary>
        /// Maximum Cal value
        /// </summary>
        private float m_PositiveMax = 1.0f;
        /// <summary>
        /// Textures of the histograms (one per MRI)
        /// </summary>
        private Dictionary<MRI3D, Texture2D> m_HistogramByMRI = new Dictionary<MRI3D, Texture2D>();

        /// <summary>
        /// Used to display the current histogram
        /// </summary>
        [SerializeField] private RawImage m_Histogram;
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
        private void UpdateMRIHistogram(MRI3D mri3D)
        {
            UnityEngine.Profiling.Profiler.BeginSample("HISTOGRAM FMRI");
            if (!m_HistogramByMRI.TryGetValue(mri3D, out m_MRIHistogram))
            {
                if (!m_MRIHistogram)
                {
                    m_MRIHistogram = new Texture2D(1, 1);
                }
                HBP.Module3D.DLL.Texture texture = HBP.Module3D.DLL.Texture.GenerateDistributionHistogram(mri3D.Volume, 440, 440, false);
                texture.UpdateTexture2D(m_MRIHistogram);
                texture.Dispose();
                m_HistogramByMRI.Add(mri3D, m_MRIHistogram);
            }
            m_Histogram.texture = m_MRIHistogram;
            UnityEngine.Profiling.Profiler.EndSample();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize this module
        /// </summary>
        public void Initialize()
        {
            m_MRIHistogram = new Texture2D(1, 1);

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
                m_NegativeMaxHandler.MaximumPosition = m_NegativeMinHandler.Position;
                m_NegativeMin = 1 - m_NegativeMinHandler.Position;

                if (m_Initialized)
                {
                    OnChangeValues.Invoke(m_NegativeMin, m_NegativeMax, m_PositiveMin, m_PositiveMax);
                }
            });

            m_NegativeMaxHandler.OnChangePosition.AddListener((deplacement) =>
            {
                m_NegativeMinHandler.MinimumPosition = m_NegativeMaxHandler.Position;
                m_NegativeMax = 1 - m_NegativeMaxHandler.Position;

                if (m_Initialized)
                {
                    OnChangeValues.Invoke(m_NegativeMin, m_NegativeMax, m_PositiveMin, m_PositiveMax);
                }
            });
            m_PositiveMinHandler.OnChangePosition.AddListener((deplacement) =>
            {
                m_PositiveMaxHandler.MinimumPosition = m_PositiveMinHandler.Position;
                m_PositiveMin = m_PositiveMinHandler.Position;

                if (m_Initialized)
                {
                    OnChangeValues.Invoke(m_NegativeMin, m_NegativeMax, m_PositiveMin, m_PositiveMax);
                }
            });

            m_PositiveMaxHandler.OnChangePosition.AddListener((deplacement) =>
            {
                m_PositiveMinHandler.MaximumPosition = m_PositiveMaxHandler.Position;
                m_PositiveMax = m_PositiveMaxHandler.Position;

                if (m_Initialized)
                {
                    OnChangeValues.Invoke(m_NegativeMin, m_NegativeMax, m_PositiveMin, m_PositiveMax);
                }
            });

            ApplicationState.Module3D.OnRemoveScene.AddListener((s) =>
            {
                foreach (var mri in s.MRIManager.MRIs)
                {
                    if (m_HistogramByMRI.TryGetValue(mri, out Texture2D texture))
                    {
                        Destroy(texture);
                        m_HistogramByMRI.Remove(mri);
                    }
                }
            });
        }
        /// <summary>
        /// Update Maximum and Minimum Cal value
        /// </summary>
        /// <param name="values">Cal values</param>
        public void UpdateFMRICalValues(Base3DScene scene)
        {
            m_Initialized = false;

            m_NegativeHandlerZone.gameObject.SetActive(true);
            m_PositiveHandlerZone.gameObject.SetActive(true);
            MRICalValues values = scene.FMRIManager.FMRI.Volume.ExtremeValues;
            if (values.Min >= 0)
            {
                m_NegativeHandlerZone.gameObject.SetActive(false);
                m_PositiveHandlerZone.anchorMin = new Vector2(0, m_PositiveHandlerZone.anchorMin.y);
            }
            else if (values.Max <= 0)
            {
                m_PositiveHandlerZone.gameObject.SetActive(false);
                m_NegativeHandlerZone.anchorMax = new Vector2(1, m_NegativeHandlerZone.anchorMax.y);
            }
            else
            {
                float negativeWeight = values.Min / (values.Min - values.Max);
                m_NegativeHandlerZone.anchorMax = new Vector2(negativeWeight, m_NegativeHandlerZone.anchorMax.y);
                m_PositiveHandlerZone.anchorMin = new Vector2(negativeWeight, m_PositiveHandlerZone.anchorMin.y);
            }

            m_NegativeMin = scene.FMRIManager.FMRINegativeCalMinFactor;
            m_NegativeMax = scene.FMRIManager.FMRINegativeCalMaxFactor;
            m_PositiveMin = scene.FMRIManager.FMRIPositiveCalMinFactor;
            m_PositiveMax = scene.FMRIManager.FMRIPositiveCalMaxFactor;

            m_NegativeMinHandler.MinimumPosition = 0.0f;
            m_NegativeMinHandler.MaximumPosition = 1.0f;
            m_NegativeMaxHandler.MinimumPosition = 0.0f;
            m_NegativeMaxHandler.MaximumPosition = 1.0f;
            m_PositiveMinHandler.MinimumPosition = 0.0f;
            m_PositiveMinHandler.MaximumPosition = 1.0f;
            m_PositiveMaxHandler.MinimumPosition = 0.0f;
            m_PositiveMaxHandler.MaximumPosition = 1.0f;

            m_NegativeMinHandler.Position = 1f - m_NegativeMin;
            m_NegativeMaxHandler.Position = 1f - m_NegativeMax;
            m_PositiveMinHandler.Position = m_PositiveMin;
            m_PositiveMaxHandler.Position = m_PositiveMax;

            UpdateMRIHistogram(scene.FMRIManager.FMRI);

            m_Initialized = true;
        }
        #endregion
    }
}