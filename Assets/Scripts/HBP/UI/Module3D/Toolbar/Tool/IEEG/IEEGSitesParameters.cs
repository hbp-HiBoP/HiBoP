using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class IEEGSitesParameters : Tool
    {
        #region Properties
        [SerializeField]
        private Slider m_Slider;
        [SerializeField]
        private InputField m_InputField;

        public bool IsGlobal { get; set; }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_Slider.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                HBP.Module3D.Column3DDynamic selectedColumn = (HBP.Module3D.Column3DDynamic)SelectedColumn;
                if (IsGlobal)
                {
                    foreach (HBP.Module3D.Column3DDynamic column in SelectedScene.ColumnManager.ColumnsIEEG)
                    {
                        column.DynamicParameters.Gain = value;
                    }
                }
                else
                {
                    selectedColumn.DynamicParameters.Gain = value;
                }
            });

            m_InputField.onEndEdit.AddListener((value) =>
            {
                if (ListenerLock) return;

                float val;
                global::Tools.Unity.NumberExtension.TryParseFloat(value, out val);
                HBP.Module3D.Column3DDynamic selectedColumn = (HBP.Module3D.Column3DDynamic)SelectedColumn;
                if (IsGlobal)
                {
                    foreach (HBP.Module3D.Column3DDynamic column in SelectedScene.ColumnManager.ColumnsIEEG)
                    {
                        column.DynamicParameters.InfluenceDistance = val;
                    }
                }
                else
                {
                    selectedColumn.DynamicParameters.InfluenceDistance = val;
                }
                m_InputField.text = selectedColumn.DynamicParameters.InfluenceDistance.ToString("N2");
            });
        }

        public override void DefaultState()
        {
            m_Slider.value = 0.5f;
            m_Slider.interactable = false;
            m_InputField.text = "15.00";
            m_InputField.interactable = false;
        }

        public override void UpdateInteractable()
        {
            bool isColumnIEEG = SelectedColumn.Type == Data.Enums.ColumnType.iEEG;

            m_Slider.interactable = isColumnIEEG;
            m_InputField.interactable = isColumnIEEG;
        }

        public override void UpdateStatus()
        {
            if (SelectedColumn.Type == Data.Enums.ColumnType.iEEG)
            {
                HBP.Module3D.Column3DDynamic selectedColumn = (HBP.Module3D.Column3DDynamic)SelectedColumn;
                m_Slider.value = selectedColumn.DynamicParameters.Gain;
                m_InputField.text = selectedColumn.DynamicParameters.InfluenceDistance.ToString("N2");
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