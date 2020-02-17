using HBP.Module3D;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class IEEGSitesParameters : Tool
    {
        #region Properties
        /// <summary>
        /// Slider to control the gain of the sites
        /// </summary>
        [SerializeField] private Slider m_Slider;
        /// <summary>
        /// Inputfield to change the influence distance of the sites
        /// </summary>
        [SerializeField] private InputField m_InputField;
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
            m_Slider.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;
                
                foreach (var column in GetColumnsDependingOnTypeAndGlobal(IsGlobal))
                {
                    column.DynamicParameters.Gain = value;
                }
            });

            m_InputField.onEndEdit.AddListener((value) =>
            {
                if (ListenerLock) return;

                global::Tools.CSharp.NumberExtension.TryParseFloat(value, out float val);
                
                foreach (var column in GetColumnsDependingOnTypeAndGlobal(IsGlobal))
                {
                    column.DynamicParameters.InfluenceDistance = val;
                }
                m_InputField.text = ((Column3DDynamic)SelectedColumn).DynamicParameters.InfluenceDistance.ToString("N2");
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_Slider.value = 0.5f;
            m_Slider.interactable = false;
            m_InputField.text = "15.00";
            m_InputField.interactable = false;
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            bool isColumnDynamic = SelectedColumn is Column3DDynamic;

            m_Slider.interactable = isColumnDynamic;
            m_InputField.interactable = isColumnDynamic;
        }
        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public override void UpdateStatus()
        {
            if (SelectedColumn is Column3DDynamic dynamicColumn)
            {
                m_Slider.value = dynamicColumn.DynamicParameters.Gain;
                m_InputField.text = dynamicColumn.DynamicParameters.InfluenceDistance.ToString("N2");
            }
            else
            {
                m_Slider.value = 0.5f;
                m_InputField.text = "15.00";
            }
        }
        #endregion
    }
}