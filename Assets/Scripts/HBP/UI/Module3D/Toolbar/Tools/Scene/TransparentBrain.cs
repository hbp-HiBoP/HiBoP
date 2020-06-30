using HBP.Module3D;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class TransparentBrain : Tool
    {
        #region Properties
        /// <summary>
        /// Toggle to make the brain transparent or not
        /// </summary>
        [SerializeField] private Toggle m_Toggle;
        /// <summary>
        /// Slider to control the alpha of the transparent brain
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

                SelectedScene.BrainMaterials.SetAlpha(value);
            });

            m_Toggle.onValueChanged.AddListener((isOn) =>
            {
                if (ListenerLock) return;

                SelectedScene.IsBrainTransparent = isOn;
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_Toggle.isOn = false;
            m_Toggle.interactable = false;
            m_Slider.value = 0.2f;
            m_Slider.interactable = false;
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            bool isBrainTransparent = SelectedScene.IsBrainTransparent;

            m_Toggle.interactable = true;
            m_Slider.interactable = isBrainTransparent;
        }
        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public override void UpdateStatus()
        {
            m_Toggle.isOn = SelectedScene.IsBrainTransparent;
            m_Slider.value = SelectedScene.BrainMaterials.Alpha;
        }
        #endregion
    }
}