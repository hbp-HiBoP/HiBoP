using HBP.Module3D;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
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
        [SerializeField] private Module3D.ThresholdFMRI m_ThresholdFMRI;
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
                    ((Column3DFMRI)column).FMRIParameters.SetSpanValues(negativeMin, negativeMax, positiveMin, positiveMax);
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
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            bool isColumnFMRI = SelectedColumn is Column3DFMRI;

            gameObject.SetActive(isColumnFMRI);
            m_Button.interactable = isColumnFMRI;
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
            }
        }
        #endregion
    }
}