using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Toolbar
{
    public class AutoRotate : Tool
    {
        #region Properties
        /// <summary>
        /// Button to show the panel
        /// </summary>
        [SerializeField] private Button m_Button;
        /// <summary>
        /// Toggle auto rotate
        /// </summary>
        [SerializeField] private Toggle m_Toggle;
        /// <summary>
        /// Slider to control the speed
        /// </summary>
        [SerializeField] private Slider m_Slider;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_Toggle.onValueChanged.AddListener((isOn) =>
            {
                if (ListenerLock) return;

                SelectedScene.AutomaticRotation = isOn;
            });

            m_Slider.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                SelectedScene.AutomaticRotationSpeed = value;
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_Button.interactable = false;
            m_Toggle.isOn = false;
            m_Toggle.interactable = false;
            m_Slider.value = 30.0f;
            m_Slider.interactable = false;
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            m_Button.interactable = true;
            m_Toggle.interactable = true;
            m_Slider.interactable = true;
        }
        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public override void UpdateStatus()
        {
            m_Toggle.isOn = SelectedScene.AutomaticRotation;
            m_Slider.value = SelectedScene.AutomaticRotationSpeed;
        }
        #endregion
    }
}