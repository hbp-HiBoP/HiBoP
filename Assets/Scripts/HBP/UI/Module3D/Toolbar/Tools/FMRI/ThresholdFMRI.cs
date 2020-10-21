using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class ThresholdFMRI : Tool
    {
        #region Properties
        /// <summary>
        /// Button to open the threshold MRI panel
        /// </summary>
        [SerializeField] private Button m_Button;
        /// <summary>
        /// Module to handle the threshold MRI
        /// </summary>
        [SerializeField] private Module3D.ThresholdFMRI m_ThresholdFMRI;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_ThresholdFMRI.Initialize();
            m_ThresholdFMRI.OnChangeValues.AddListener((negativeMin, negativeMax, positiveMin, positiveMax) =>
            {
                if (ListenerLock) return;

                SelectedScene.FMRIManager.FMRINegativeCalMinFactor = negativeMin;
                SelectedScene.FMRIManager.FMRINegativeCalMaxFactor = negativeMax;
                SelectedScene.FMRIManager.FMRIPositiveCalMinFactor = positiveMin;
                SelectedScene.FMRIManager.FMRIPositiveCalMaxFactor = positiveMax;
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_Button.interactable = false;
            gameObject.SetActive(false);
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            bool hasFMRI = SelectedScene.FMRIManager.FMRI != null;

            gameObject.SetActive(hasFMRI);
            m_Button.interactable = hasFMRI;
        }
        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public override void UpdateStatus()
        {
            if (SelectedScene.FMRIManager.FMRI != null)
            {
                m_ThresholdFMRI.UpdateFMRICalValues(SelectedScene.FMRIManager.FMRI.Volume, SelectedScene.FMRIManager.FMRINegativeCalMinFactor, SelectedScene.FMRIManager.FMRINegativeCalMaxFactor, SelectedScene.FMRIManager.FMRIPositiveCalMinFactor, SelectedScene.FMRIManager.FMRIPositiveCalMaxFactor);
            }
        }
        #endregion
    }
}