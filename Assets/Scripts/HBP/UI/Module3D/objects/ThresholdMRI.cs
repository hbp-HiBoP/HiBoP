using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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
        /// Computed minimum Cal value
        /// </summary>
        private float m_ComputedCalMinValue;
        /// <summary>
        /// Computed maximum Cal value
        /// </summary>
        private float m_ComputedCalMaxValue;

        /// <summary>
        /// Handler responsible for the minimum value
        /// </summary>
        public ThresholdHandler MinHandler;
        /// <summary>
        /// Handler responsible for the maximum value
        /// </summary>
        public ThresholdHandler MaxHandler;
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_MRIHistogram = new Texture2D(1, 1);

            MinHandler.MinimumPosition = 0.0f;
            MinHandler.MaximumPosition = 0.95f;
            MaxHandler.MinimumPosition = 0.05f;
            MaxHandler.MaximumPosition = 1.0f;

            MinHandler.OnChangePosition.AddListener(() =>
            {
                MaxHandler.MinimumPosition = MinHandler.Position;
                MaxHandler.Position = MaxHandler.Position; // call to setter

                m_MRICalMin = MinHandler.Position;
                ApplicationState.Module3D.SelectedScene.UpdateMRICalMin(m_MRICalMin);
                UpdateMRIHistogram();
            });

            MaxHandler.OnChangePosition.AddListener(() =>
            {
                MinHandler.MaximumPosition = MaxHandler.Position;
                MinHandler.Position = MinHandler.Position; // call to setter

                m_MRICalMax = MaxHandler.Position;
                ApplicationState.Module3D.SelectedScene.UpdateMRICalMax(m_MRICalMax);
                UpdateMRIHistogram();
            });
        }
        /// <summary>
        /// Update MRI Histogram Texture
        /// </summary>
        private void UpdateMRIHistogram()
        {
            HBP.Module3D.DLL.Texture.GenerateDistributionHistogram(ApplicationState.Module3D.SelectedScene.ColumnManager.DLLVolume, 4 * 110, 4 * 110, m_MRICalMin, m_MRICalMax).UpdateTexture2D(m_MRIHistogram);
            
            Image histogramImage = transform.GetComponent<Image>();
            Destroy(histogramImage.sprite);
            histogramImage.sprite = Sprite.Create(m_MRIHistogram, new Rect(0, 0, m_MRIHistogram.width, m_MRIHistogram.height), new Vector2(0.5f, 0.5f), 400f);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Update Maximum and Minimum Cal value
        /// </summary>
        /// <param name="values">Cal values</param>
        public void UpdateMRICalValues(MRICalValues values)
        {
            m_ComputedCalMinValue = values.computedCalMin;
            m_ComputedCalMaxValue = values.computedCalMax;
            m_MRICalMin = ApplicationState.Module3D.SelectedScene.ColumnManager.MRICalMinFactor;
            m_MRICalMax = ApplicationState.Module3D.SelectedScene.ColumnManager.MRICalMaxFactor;
            MinHandler.MinimumPosition = 0.0f;
            MinHandler.MaximumPosition = 1.0f;
            MaxHandler.MinimumPosition = 0.0f;
            MaxHandler.MaximumPosition = 1.0f;
            MinHandler.Position = m_MRICalMin;
            MaxHandler.Position = m_MRICalMax;
            MinHandler.MaximumPosition = m_MRICalMax;
            MaxHandler.MinimumPosition = m_MRICalMin;
            UpdateMRIHistogram();
        }
        #endregion
    }
}