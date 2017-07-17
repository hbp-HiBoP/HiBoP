using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tools.Unity
{
    public class Tooltip : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        #region Properties
        /// <summary>
        /// Text to be displayed
        /// </summary>
        [SerializeField]
        private string m_Text;

        private bool m_Entered = false;
        private float m_TimeSinceEntered = 0.0f;
        private const float TIME_TO_DISPLAY = 1.0f;
        #endregion

        #region Private Methods
        private void Update()
        {
            if (m_Entered)
            {
                m_TimeSinceEntered += Time.deltaTime;
                if (m_TimeSinceEntered > TIME_TO_DISPLAY && !ApplicationState.TooltipManager.IsTooltipDisplayed)
                {
                    ApplicationState.TooltipManager.ShowTooltip(m_Text, GetComponent<RectTransform>().position);
                }
            }
        }
        #endregion

        #region Public Methods
        public void OnPointerEnter(PointerEventData data)
        {
            m_Entered = true;
        }
        public void OnPointerExit(PointerEventData data)
        {
            ApplicationState.TooltipManager.HideTooltip();
            m_Entered = false;
            m_TimeSinceEntered = 0.0f;
        }
        #endregion
    }
}