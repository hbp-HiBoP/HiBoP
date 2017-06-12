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

            m_MinHandler.OnChangePosition.AddListener(() =>
            {
                m_MaxHandler.MinimumPosition = m_MinHandler.Position + 0.1f;
                m_MaxHandler.ClampPosition();

                m_MRICalMin = m_MinHandler.Position;
                ApplicationState.Module3D.SelectedScene.ColumnManager.MRICalMinFactor = m_MRICalMin;

                OnChangeValues.Invoke(m_MRICalMin, m_MRICalMax);
            });

            m_MaxHandler.OnChangePosition.AddListener(() =>
            {
                m_MinHandler.MaximumPosition = m_MaxHandler.Position - 0.1f;
                m_MinHandler.ClampPosition();

                m_MRICalMax = m_MaxHandler.Position;
                ApplicationState.Module3D.SelectedScene.ColumnManager.MRICalMaxFactor = m_MRICalMax;

                OnChangeValues.Invoke(m_MRICalMin, m_MRICalMax);
            });
        }
        /// <summary>
        /// Update MRI Histogram Texture
        /// </summary>
        private void UpdateMRIHistogram()
        {
            UnityEngine.Profiling.Profiler.BeginSample("HISTOGRAM");
            if (!m_MRIHistogram)
            {
                m_MRIHistogram = new Texture2D(1, 1);
            }
            HBP.Module3D.DLL.Texture.GenerateDistributionHistogram(ApplicationState.Module3D.SelectedScene.ColumnManager.DLLVolume, 4 * 110, 4 * 110, m_MRICalMin, m_MRICalMax).UpdateTexture2D(m_MRIHistogram);
            
            Image histogramImage = transform.GetComponent<Image>();
            Destroy(histogramImage.sprite);
            histogramImage.sprite = Sprite.Create(m_MRIHistogram, new Rect(0, 0, m_MRIHistogram.width, m_MRIHistogram.height), new Vector2(0.5f, 0.5f), 400f);
            UnityEngine.Profiling.Profiler.EndSample();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Update Maximum and Minimum Cal value
        /// </summary>
        /// <param name="values">Cal values</param>
        public void UpdateMRICalValues(MRICalValues values)
        {
            float amplitude = values.max - values.min;
            float min = (values.computedCalMin - values.min) / amplitude;
            float max = 1.0f - (values.max - values.computedCalMax) / amplitude;
            m_HandlerZone.anchorMin = new Vector2(min, m_HandlerZone.anchorMin.y);
            m_HandlerZone.anchorMax = new Vector2(max, m_HandlerZone.anchorMax.y);

            m_MRICalMin = ApplicationState.Module3D.SelectedScene.ColumnManager.MRICalMinFactor;
            m_MRICalMax = ApplicationState.Module3D.SelectedScene.ColumnManager.MRICalMaxFactor;

            m_MinHandler.MinimumPosition = 0.0f;
            m_MinHandler.MaximumPosition = 0.9f;
            m_MaxHandler.MinimumPosition = 0.1f;
            m_MaxHandler.MaximumPosition = 1.0f;
            m_MinHandler.Position = m_MRICalMin;
            m_MaxHandler.Position = m_MRICalMax;
            m_MinHandler.MaximumPosition = m_MRICalMax - 0.1f;
            m_MaxHandler.MinimumPosition = m_MRICalMin + 0.1f;

            UpdateMRIHistogram();
        }
        #endregion
    }
}