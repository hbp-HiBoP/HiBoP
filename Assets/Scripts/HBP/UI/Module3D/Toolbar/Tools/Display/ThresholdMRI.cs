using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class ThresholdMRI : Tool
    {
        #region Properties
        /// <summary>
        /// Button to open the threshold MRI panel
        /// </summary>
        [SerializeField] private Button m_Button;
        /// <summary>
        /// Module to handle the threshold MRI
        /// </summary>
        [SerializeField] private Module3D.ThresholdMRI m_ThresholdMRI;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_ThresholdMRI.Initialize();
            m_ThresholdMRI.OnChangeValues.AddListener((min, max) =>
            {
                if (ListenerLock) return;

                SelectedScene.MRIManager.SetCalValues(min, max);
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_Button.interactable = false;
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            m_Button.interactable = true;
        }
        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public override void UpdateStatus()
        {
            m_ThresholdMRI.CleanHistograms();
            m_ThresholdMRI.UpdateMRICalValues(SelectedScene);
        }
        #endregion
    }
}