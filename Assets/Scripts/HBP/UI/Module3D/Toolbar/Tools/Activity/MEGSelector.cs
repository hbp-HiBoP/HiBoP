using UnityEngine;
using UnityEngine.UI;
using HBP.Core.Enums;
using HBP.Display.Module3D;

namespace HBP.UI.Toolbar
{
    public class MEGSelector : Tool
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

                if (SelectedColumn is Column3DMEG megColumn)
                    megColumn.SelectedMEGIndex = value;
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
            bool isColumnMEG = SelectedColumn is Column3DMEG;

            m_Dropdown.gameObject.SetActive(isColumnMEG);
        }
        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public override void UpdateStatus()
        {
            m_Dropdown.options.Clear();
            if (SelectedColumn is Column3DMEG megColumn)
            {
                if (SelectedScene.Type == SceneType.MultiPatients)
                {
                    foreach (var fmri in megColumn.ColumnMEGData.Data.MEGItems)
                    {
                        m_Dropdown.options.Add(new Dropdown.OptionData(string.Format("{0} ({1})", fmri.Label, fmri.Patient.Name)));
                    }
                }
                else
                {
                    foreach (var fmri in megColumn.ColumnMEGData.Data.MEGItems)
                    {
                        m_Dropdown.options.Add(new Dropdown.OptionData(fmri.Label));
                    }
                }
                m_Dropdown.value = megColumn.SelectedMEGIndex;
            }
            m_Dropdown.RefreshShownValue();
        }
        #endregion
    }
}