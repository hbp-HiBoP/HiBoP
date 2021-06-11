using HBP.Module3D;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class SiteGain : Tool
    {
        #region Properties
        /// <summary>
        /// Slider to control the gain of the sites
        /// </summary>
        [SerializeField] private Slider m_Slider;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_Slider.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                SelectedScene.SiteGain = value;
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_Slider.value = 1.0f;
            m_Slider.interactable = false;
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            m_Slider.interactable = true;
        }
        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public override void UpdateStatus()
        {
            m_Slider.value = SelectedScene.SiteGain;
        }
        #endregion
    }
}