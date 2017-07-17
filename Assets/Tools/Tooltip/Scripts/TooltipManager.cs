using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity
{
    public class TooltipManager : MonoBehaviour
    {
        #region Properties
        public bool IsTooltipDisplayed
        {
            get
            {
                return m_Tooltip.gameObject.activeSelf;
            }
        }
        /// <summary>
        /// Canvas on which the tooltip is displayed
        /// </summary>
        [SerializeField]
        private RectTransform m_Canvas;
        /// <summary>
        /// Tooltip's RectTransform
        /// </summary>
        [SerializeField]
        private RectTransform m_Tooltip;
        /// <summary>
        /// Tooltip's Textfield
        /// </summary>
        [SerializeField]
        private Text m_TextField;
        #endregion

        #region Private Methods
        private void ClampToCanvas() // FIXME : high cost of performance
        {
            Vector3 l_pos = m_Tooltip.localPosition;
            Vector3 l_minPosition = m_Canvas.rect.min - m_Tooltip.rect.min;
            Vector3 l_maxPosition = m_Canvas.rect.max - m_Tooltip.rect.max;

            l_minPosition = new Vector3(l_minPosition.x, l_minPosition.y, l_minPosition.z);
            l_maxPosition = new Vector3(l_maxPosition.x, l_maxPosition.y, l_maxPosition.z);

            l_pos.x = Mathf.Clamp(m_Tooltip.localPosition.x, l_minPosition.x, l_maxPosition.x);
            l_pos.y = Mathf.Clamp(m_Tooltip.localPosition.y, l_minPosition.y, l_maxPosition.y);

            m_Tooltip.localPosition = l_pos;
        }
        #endregion

        #region Public Methods
        public void ShowTooltip(string text, Vector2 position)
        {
            m_Tooltip.gameObject.SetActive(true);
            m_TextField.text = text;
            m_Tooltip.anchoredPosition = position;
            ClampToCanvas();
        }
        public void HideTooltip()
        {
            m_Tooltip.gameObject.SetActive(false);
        }
        #endregion
    }
}