using HBP.Core.Object3D;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class IBCParameters : Tool
    {
        #region Properties
        /// <summary>
        /// Module to handle the threshold MRI
        /// </summary>
        [SerializeField] private Module3D.ThresholdFMRI m_ThresholdFMRI;
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
            m_ThresholdFMRI.Initialize();
            m_ThresholdFMRI.OnChangeValues.AddListener((negativeMin, negativeMax, positiveMin, positiveMax) =>
            {
                if (ListenerLock) return;

                SelectedScene.FMRIManager.FMRINegativeCalMinFactor = negativeMin;
                SelectedScene.FMRIManager.FMRINegativeCalMaxFactor = negativeMax;
                SelectedScene.FMRIManager.FMRIPositiveCalMinFactor = positiveMin;
                SelectedScene.FMRIManager.FMRIPositiveCalMaxFactor = positiveMax;
            });
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
            bool isIBC = SelectedScene.FMRIManager.DisplayIBCContrasts;

            gameObject.SetActive(isIBC);
        }
        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public override void UpdateStatus()
        {
            bool hasIBC = SelectedScene.FMRIManager.CurrentVolume != null && SelectedScene.FMRIManager.DisplayIBCContrasts;
            if (hasIBC)
            {
                m_ThresholdFMRI.UpdateFMRICalValues(Object3DManager.IBC.FMRI, SelectedScene.FMRIManager.FMRINegativeCalMinFactor, SelectedScene.FMRIManager.FMRINegativeCalMaxFactor, SelectedScene.FMRIManager.FMRIPositiveCalMinFactor, SelectedScene.FMRIManager.FMRIPositiveCalMaxFactor);
                m_AlphaSlider.value = SelectedScene.FMRIManager.FMRIAlpha;
            }
        }
        #endregion
    }
}