using UnityEngine;
using UnityEngine.UI;
using HBP.Core.Enums;
using HBP.Data.Module3D;

namespace HBP.UI.Toolbar
{
    public class StaticLabelSelector : Tool
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

                if (SelectedColumn is Column3DStatic staticColumn)
                    staticColumn.SelectedLabelIndex = value;
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
            bool isColumnStatic = SelectedColumn is Column3DStatic;
            bool isGeneratorUpToDate = SelectedScene.IsGeneratorUpToDate;

            m_Dropdown.gameObject.SetActive(isColumnStatic && isGeneratorUpToDate);
        }
        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public override void UpdateStatus()
        {
            m_Dropdown.options.Clear();
            if (SelectedColumn is Column3DStatic staticColumn)
            {
                foreach (var label in staticColumn.Labels)
                {
                    m_Dropdown.options.Add(new Dropdown.OptionData(label));
                }
                m_Dropdown.value = staticColumn.SelectedLabelIndex;
            }
            m_Dropdown.RefreshShownValue();
        }
        #endregion
    }
}