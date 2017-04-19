
/**
 * \file    ProgressBar.cs
 * \author  Lance Florian
 * \date    09/06/2016
 * \brief   Define ProgressBar class
 */

// system
using System;

// unity
using UnityEngine;
using UnityEngine.UI;


namespace HBP.Module3D
{
    /// <summary>
    /// A progressbar panel UI
    /// </summary>
    public class ProgressBar : MonoBehaviour
    {
        private LayoutElement m_fillPanel = null; /**< progessbar fill panel */
        private RectTransform m_progressBarPanelRectT = null; /**< progress bar panel rect transform */
        private Text m_stateText = null;
        
        public void init()
        {
            m_progressBarPanelRectT = transform.Find("progress panel").GetComponent<RectTransform>();
            m_fillPanel = transform.Find("progress panel").Find("fill panel").GetComponent<LayoutElement>();
            m_stateText = transform.Find("text panel").Find("state text").GetComponent<Text>();
        }

        /// <summary>
        /// Set the current progressbar state (0 - 1)
        /// </summary>
        /// <param name="value"></param>
        public void setProgessBarState(float value)
        {
            m_fillPanel.minWidth = m_progressBarPanelRectT.sizeDelta.x * value;
            m_stateText.text = "" + (Math.Truncate(value * 100)) + " %";
        }
    }
}