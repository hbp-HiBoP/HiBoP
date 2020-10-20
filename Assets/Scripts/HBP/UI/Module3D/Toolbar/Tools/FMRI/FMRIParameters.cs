﻿using HBP.Module3D;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class FMRIParameters : Tool
    {
        #region Properties
        /// <summary>
        /// Slider to set the alpha of the contrast
        /// </summary>
        [SerializeField] private Slider m_AlphaSlider;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_AlphaSlider.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                SelectedScene.FMRIManager.FMRIAlpha = value;
                ListenerLock = true;
                UpdateStatus();
                ListenerLock = false;
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            gameObject.SetActive(false);
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            bool hasFMRI = SelectedScene.FMRIManager.FMRI != null;

            gameObject.SetActive(hasFMRI);
        }
        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public override void UpdateStatus()
        {
            bool hasFMRI = SelectedScene.FMRIManager.FMRI != null;
            if (hasFMRI)
            {
                MRICalValues calValues = SelectedScene.FMRIManager.FMRI.Volume.ExtremeValues;
                m_AlphaSlider.value = SelectedScene.FMRIManager.FMRIAlpha;
            }
        }
        #endregion
    }
}