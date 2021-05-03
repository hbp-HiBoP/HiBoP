using HBP.Module3D;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class FMRISelector : Tool
    {
        #region Properties
        /// <summary>
        /// Dropdown to select the contrast to display
        /// </summary>
        [SerializeField] private Dropdown m_Dropdown;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_Dropdown.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                (SelectedColumn as Column3DFMRI).SelectedFMRIIndex = value;
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_Dropdown.gameObject.SetActive(false);
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            bool isColumnFMRI = SelectedColumn is Column3DFMRI;
            bool isGeneratorUpToDate = SelectedScene.IsGeneratorUpToDate;

            m_Dropdown.gameObject.SetActive(isColumnFMRI && isGeneratorUpToDate);
        }
        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public override void UpdateStatus()
        {
            m_Dropdown.options.Clear();
            if (SelectedColumn is Column3DFMRI fmriColumn)
            {
                foreach (var fmri in fmriColumn.ColumnFMRIData.Data.FMRIs)
                {
                    m_Dropdown.options.Add(new Dropdown.OptionData(fmri.Name));
                }
                m_Dropdown.value = fmriColumn.SelectedFMRIIndex;
            }
            m_Dropdown.RefreshShownValue();
        }
        #endregion
    }
}