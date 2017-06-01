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
        private Texture2D m_MRIHistogram;
        private float m_MRICalMin = 0.0f;
        private float m_MRICalMax = 1.0f;
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_MRIHistogram = new Texture2D(1, 1);
        }
        private void UpdateMRIHistogram()
        {
            HBP.Module3D.DLL.Texture.GenerateDistributionHistogram(ApplicationState.Module3D.SelectedScene.ColumnManager.DLLVolume, 4 * 110, 4 * 110, m_MRICalMin, m_MRICalMax).UpdateTexture2D(m_MRIHistogram);
            
            Image histogramImage = transform.GetComponent<Image>();
            Destroy(histogramImage.sprite);
            histogramImage.sprite = Sprite.Create(m_MRIHistogram, new Rect(0, 0, m_MRIHistogram.width, m_MRIHistogram.height), new Vector2(0.5f, 0.5f), 400f);
        }
        #endregion

        #region Public Methods
        public void UpdateMRICalValues(MRICalValues values)
        {
            m_MRICalMin = values.computedCalMin;
            m_MRICalMax = values.computedCalMax;
            UpdateMRIHistogram();
        }
        #endregion
    }
}