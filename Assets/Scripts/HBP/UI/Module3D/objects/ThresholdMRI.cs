using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class ThresholdMRI : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Texture to be applied to the image
        /// </summary>
        private Texture2D m_MRIHistogram;
        /// <summary>
        /// Minimum Cal value
        /// </summary>
        private float m_MRICalMin = 0.0f;
        /// <summary>
        /// Maximum Cal value
        /// </summary>
        private float m_MRICalMax = 1.0f;

        private Dictionary<MRI3D, Texture2D> m_HistogramByMRI = new Dictionary<MRI3D, Texture2D>();

        /// <summary>
        /// MRI Histogram
        /// </summary>
        [SerializeField]
        private RawImage m_Histogram;
        /// <summary>
        /// Zone in which the handlers can move
        /// </summary>
        [SerializeField]
        private RectTransform m_HandlerZone;
        /// <summary>
        /// Handler responsible for the minimum value
        /// </summary>
        [SerializeField]
        private ThresholdHandler m_MinHandler;
        /// <summary>
        /// Handler responsible for the maximum value
        /// </summary>
        [SerializeField]
        private ThresholdHandler m_MaxHandler;

        /// <summary>
        /// Is the module initialized ?
        /// </summary>
        private bool m_Initialized;
        #endregion

        #region Events
        public GenericEvent<float, float> OnChangeValues = new GenericEvent<float, float>();
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_MRIHistogram = new Texture2D(1, 1);

            m_MinHandler.MinimumPosition = 0.0f;
            m_MinHandler.MaximumPosition = 0.9f;
            m_MaxHandler.MinimumPosition = 0.1f;
            m_MaxHandler.MaximumPosition = 1.0f;

            m_MinHandler.OnChangePosition.AddListener((deplacement) =>
            {
                m_MaxHandler.MinimumPosition = m_MinHandler.Position + 0.1f;
                m_MaxHandler.ClampPosition();

                m_MRICalMin = m_MinHandler.Position;

                if (m_Initialized)
                {
                    OnChangeValues.Invoke(m_MRICalMin, m_MRICalMax);
                }
            });

            m_MaxHandler.OnChangePosition.AddListener((deplacement) =>
            {
                m_MinHandler.MaximumPosition = m_MaxHandler.Position - 0.1f;
                m_MinHandler.ClampPosition();

                m_MRICalMax = m_MaxHandler.Position;

                if (m_Initialized)
                {
                    OnChangeValues.Invoke(m_MRICalMin, m_MRICalMax);
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
        /// Update MRI Histogram Texture
        /// </summary>
        private void UpdateMRIHistogram(MRI3D mri3D)
        {
            UnityEngine.Profiling.Profiler.BeginSample("HISTOGRAM MRI");
            if (!m_HistogramByMRI.TryGetValue(mri3D, out m_MRIHistogram))
            {
                if (!m_MRIHistogram)
                {
                    m_MRIHistogram = new Texture2D(1, 1);
                }
                HBP.Module3D.DLL.Texture texture = HBP.Module3D.DLL.Texture.GenerateDistributionHistogram(mri3D.Volume, 4 * 110, 4 * 110, m_MRICalMin, m_MRICalMax);
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
        /// Update Maximum and Minimum Cal value
        /// </summary>
        /// <param name="values">Cal values</param>
        public void UpdateMRICalValues(Base3DScene scene)
        {
            m_Initialized = false;

            MRICalValues values = scene.MRIManager.SelectedMRI.Volume.ExtremeValues;
            float amplitude = values.Max - values.Min;
            float min = (values.ComputedCalMin - values.Min) / amplitude;
            float max = 1.0f - (values.Max - values.ComputedCalMax) / amplitude;
            m_HandlerZone.anchorMin = new Vector2(min, m_HandlerZone.anchorMin.y);
            m_HandlerZone.anchorMax = new Vector2(max, m_HandlerZone.anchorMax.y);

            m_MRICalMin = scene.MRIManager.MRICalMinFactor;
            m_MRICalMax = scene.MRIManager.MRICalMaxFactor;

            m_MinHandler.MinimumPosition = 0.0f;
            m_MinHandler.MaximumPosition = 0.9f;
            m_MaxHandler.MinimumPosition = 0.1f;
            m_MaxHandler.MaximumPosition = 1.0f;
            m_MinHandler.Position = m_MRICalMin;
            m_MaxHandler.Position = m_MRICalMax;
            m_MinHandler.MaximumPosition = m_MRICalMax - 0.1f;
            m_MaxHandler.MinimumPosition = m_MRICalMin + 0.1f;

            UpdateMRIHistogram(scene.MRIManager.SelectedMRI);

            m_Initialized = true;
        }
        #endregion
    }
}