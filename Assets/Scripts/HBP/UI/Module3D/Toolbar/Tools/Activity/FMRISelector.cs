using UnityEngine;
using UnityEngine.UI;
using HBP.Core.Enums;
using HBP.Display.Module3D;

namespace HBP.UI.Toolbar
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

                if (SelectedColumn is Column3DFMRI fmriColumn)
                    fmriColumn.SelectedFMRIIndex = value;
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
                if (SelectedScene.Type == SceneType.MultiPatients)
                {
                    foreach (var fmri in fmriColumn.ColumnFMRIData.Data.FMRIs)
                    {
                        m_Dropdown.options.Add(new Dropdown.OptionData(string.Format("{0} ({1})", fmri.Item1.Name, fmri.Item2.Name)));
                    }
                }
                else
                {
                    foreach (var fmri in fmriColumn.ColumnFMRIData.Data.FMRIs)
                    {
                        m_Dropdown.options.Add(new Dropdown.OptionData(fmri.Item1.Name));
                    }
                }
                m_Dropdown.value = fmriColumn.SelectedFMRIIndex;
            }
            m_Dropdown.RefreshShownValue();
        }
        #endregion
    }
}