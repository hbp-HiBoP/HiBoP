using HBP.Module3D;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class DynamicParameters : Tool
    {
        #region Properties
        /// <summary>
        /// Inputfield to change the influence distance of the sites
        /// </summary>
        [SerializeField] private InputField m_InputField;
        /// <summary>
        /// Button to open the threshold iEEG panel
        /// </summary>
        [SerializeField] private Button m_Button;
        /// <summary>
        /// Button to set the values automatically
        /// </summary>
        [SerializeField] private Button m_Auto;
        /// <summary>
        /// Module to handle the threshold iEEG
        /// </summary>
        [SerializeField] private Module3D.ThresholdIEEG m_ThresholdIEEG;
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
            m_InputField.onEndEdit.AddListener((value) =>
            {
                if (ListenerLock) return;

                global::Tools.CSharp.NumberExtension.TryParseFloat(value, out float val);
                
                foreach (var column in GetColumnsDependingOnTypeAndGlobal(IsGlobal))
                {
                    ((Column3DDynamic)column).DynamicParameters.InfluenceDistance = val;
                }
                m_InputField.text = ((Column3DDynamic)SelectedColumn).DynamicParameters.InfluenceDistance.ToString("N2");
            });
            m_ThresholdIEEG.Initialize();
            m_ThresholdIEEG.OnChangeValues.AddListener((min, mid, max) =>
            {
                if (ListenerLock) return;

                foreach (var column in GetColumnsDependingOnTypeAndGlobal(IsGlobal))
                {
                    ((Column3DDynamic)column).DynamicParameters.SetSpanValues(min, mid, max);
                }
            });
            m_Auto.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                foreach (var column in GetColumnsDependingOnTypeAndGlobal(IsGlobal))
                {
                    Column3DDynamic dynamicColumn = column as Column3DDynamic;
                    dynamicColumn.DynamicParameters.ResetSpanValues(dynamicColumn);
                    m_ThresholdIEEG.UpdateIEEGValues(dynamicColumn);
                }
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            gameObject.SetActive(false);
            m_InputField.text = "15.00";
            m_InputField.interactable = false;
            m_Button.interactable = false;
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            bool isColumnDynamic = SelectedColumn is Column3DDynamic;
            bool isColumnIEEG = SelectedColumn is Column3DIEEG;
            bool isColumnCCEPAndSourceSelected = SelectedColumn is HBP.Module3D.Column3DCCEP ccepColumn && ccepColumn.IsSourceSelected;

            gameObject.SetActive(isColumnDynamic);
            m_InputField.interactable = isColumnDynamic;
            m_Button.interactable = isColumnIEEG || isColumnCCEPAndSourceSelected;
        }
        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public override void UpdateStatus()
        {
            if (SelectedColumn is Column3DDynamic dynamicColumn)
            {
                m_InputField.text = dynamicColumn.DynamicParameters.InfluenceDistance.ToString("N2");
                m_ThresholdIEEG.UpdateIEEGValues(dynamicColumn);
            }
            else
            {
                m_InputField.text = "15.00";
            }
        }
        #endregion
    }
}