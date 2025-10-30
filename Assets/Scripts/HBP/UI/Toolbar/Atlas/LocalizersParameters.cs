using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Toolbar
{
    public class LocalizersParameters : Tool
    {
        #region Properties
        /// <summary>
        /// Module to handle the threshold IEEG for Localizers
        /// </summary>
        [SerializeField] private ThresholdIEEG m_ThresholdIEEG;

        [SerializeField] private Button m_Auto;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_ThresholdIEEG.Initialize();
            m_ThresholdIEEG.OnChangeValues.AddListener((min, middle, max) =>
            {
                if (ListenerLock) return;

                SelectedScene.FMRIManager.LocalizersMin = min;
                SelectedScene.FMRIManager.LocalizersMiddle = middle;
                SelectedScene.FMRIManager.LocalizersMax = max;
            });
            m_Auto.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                SelectedScene.FMRIManager.SetLocalizersDefaultParameters();
                m_ThresholdIEEG.UpdateIEEGValues(SelectedScene.FMRIManager);
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
            bool isLocalizers = SelectedScene.FMRIManager.DisplayLocalizers;
            gameObject.SetActive(isLocalizers);
        }
        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public override void UpdateStatus()
        {
            bool hasLocalizers = SelectedScene.FMRIManager.CurrentVolume != null && SelectedScene.FMRIManager.DisplayLocalizers;
            if (hasLocalizers)
            {
                m_ThresholdIEEG.UpdateIEEGValues(SelectedScene.FMRIManager);
            }
        }
        #endregion
    }
}