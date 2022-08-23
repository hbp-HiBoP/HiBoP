using HBP.Display.Module3D;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Toolbar
{
    public class FMRIParameters : Tool
    {
        #region Properties
        /// <summary>
        /// Button to open the threshold MRI panel
        /// </summary>
        [SerializeField] private Button m_Button;
        /// <summary>
        /// Module to handle the threshold MRI
        /// </summary>
        [SerializeField] private ThresholdFMRI m_ThresholdFMRI;
        /// <summary>
        /// Toggle to hide the values under the lowest cal value
        /// </summary>
        [SerializeField] private Toggle m_LowerToggle;
        /// <summary>
        /// Toggle to hide the values between the two middle cal values
        /// </summary>
        [SerializeField] private Toggle m_MiddleToggle;
        /// <summary>
        /// Toggle to hide the values above the highest cal value
        /// </summary>
        [SerializeField] private Toggle m_HigherToggle;
        /// <summary>
        /// Are the changes applied to all columns ?
        /// </summary>
        public bool IsGlobal { get; set; }
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

                foreach (var column in GetColumnsDependingOnTypeAndGlobal(IsGlobal))
                {
                    if (column is Column3DFMRI fmriColumn)
                        fmriColumn.FMRIParameters.SetSpanValues(negativeMin, negativeMax, positiveMin, positiveMax);
                    else if (column is Column3DMEG megColumn)
                        megColumn.MEGParameters.SetSpanValues(negativeMin, negativeMax, positiveMin, positiveMax);
                }
            });
            m_LowerToggle.onValueChanged.AddListener((isOn) =>
            {
                if (ListenerLock) return;

                foreach (var column in GetColumnsDependingOnTypeAndGlobal(IsGlobal))
                {
                    if (column is Column3DFMRI fmriColumn)
                        fmriColumn.FMRIParameters.SetHideValues(m_LowerToggle.isOn, m_MiddleToggle.isOn, m_HigherToggle.isOn);
                    else if (column is Column3DMEG megColumn)
                        megColumn.MEGParameters.SetHideValues(m_LowerToggle.isOn, m_MiddleToggle.isOn, m_HigherToggle.isOn);
                }
            });
            m_MiddleToggle.onValueChanged.AddListener((isOn) =>
            {
                if (ListenerLock) return;

                foreach (var column in GetColumnsDependingOnTypeAndGlobal(IsGlobal))
                {
                    if (column is Column3DFMRI fmriColumn)
                        fmriColumn.FMRIParameters.SetHideValues(m_LowerToggle.isOn, m_MiddleToggle.isOn, m_HigherToggle.isOn);
                    else if (column is Column3DMEG megColumn)
                        megColumn.MEGParameters.SetHideValues(m_LowerToggle.isOn, m_MiddleToggle.isOn, m_HigherToggle.isOn);
                }
            });
            m_HigherToggle.onValueChanged.AddListener((isOn) =>
            {
                if (ListenerLock) return;

                foreach (var column in GetColumnsDependingOnTypeAndGlobal(IsGlobal))
                {
                    if (column is Column3DFMRI fmriColumn)
                        fmriColumn.FMRIParameters.SetHideValues(m_LowerToggle.isOn, m_MiddleToggle.isOn, m_HigherToggle.isOn);
                    else if (column is Column3DMEG megColumn)
                        megColumn.MEGParameters.SetHideValues(m_LowerToggle.isOn, m_MiddleToggle.isOn, m_HigherToggle.isOn);
                }
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            gameObject.SetActive(false);
            m_Button.interactable = false;
            m_LowerToggle.interactable = false;
            m_MiddleToggle.interactable = false;
            m_HigherToggle.interactable = false;
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            bool isColumnFMRI = SelectedColumn is Column3DFMRI;
            bool isColumnMEG = SelectedColumn is Column3DMEG;
            bool isColumnMEGandFMRILoaded = SelectedColumn is Column3DMEG megColumn && megColumn.SelectedFMRI.Loaded;

            gameObject.SetActive(isColumnFMRI || isColumnMEG);
            m_Button.interactable = isColumnFMRI || isColumnMEGandFMRILoaded;
            m_LowerToggle.interactable = isColumnFMRI || isColumnMEG;
            m_MiddleToggle.interactable = isColumnFMRI || isColumnMEG;
            m_HigherToggle.interactable = isColumnFMRI || isColumnMEG;
        }
        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public override void UpdateStatus()
        {
            if (SelectedColumn is Column3DFMRI fmriColumn)
            {
                m_ThresholdFMRI.CleanHistograms();
                m_ThresholdFMRI.UpdateFMRICalValues(fmriColumn.SelectedFMRI, fmriColumn.FMRIParameters.FMRINegativeCalMinFactor, fmriColumn.FMRIParameters.FMRINegativeCalMaxFactor, fmriColumn.FMRIParameters.FMRIPositiveCalMinFactor, fmriColumn.FMRIParameters.FMRIPositiveCalMaxFactor);
                m_LowerToggle.isOn = fmriColumn.FMRIParameters.HideLowerValues;
                m_MiddleToggle.isOn = fmriColumn.FMRIParameters.HideMiddleValues;
                m_HigherToggle.isOn = fmriColumn.FMRIParameters.HideHigherValues;
            }
            else if (SelectedColumn is Column3DMEG megColumn && megColumn.SelectedFMRI.Loaded)
            {
                m_ThresholdFMRI.CleanHistograms();
                m_ThresholdFMRI.UpdateFMRICalValues(megColumn.SelectedFMRI, megColumn.MEGParameters.FMRINegativeCalMinFactor, megColumn.MEGParameters.FMRINegativeCalMaxFactor, megColumn.MEGParameters.FMRIPositiveCalMinFactor, megColumn.MEGParameters.FMRIPositiveCalMaxFactor);
                m_LowerToggle.isOn = megColumn.MEGParameters.HideLowerValues;
                m_MiddleToggle.isOn = megColumn.MEGParameters.HideMiddleValues;
                m_HigherToggle.isOn = megColumn.MEGParameters.HideHigherValues;
            }
        }
        #endregion
    }
}