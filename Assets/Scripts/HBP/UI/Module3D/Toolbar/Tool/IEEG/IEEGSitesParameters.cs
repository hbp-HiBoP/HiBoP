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

                HBP.Module3D.Column3DIEEG selectedColumn = (HBP.Module3D.Column3DIEEG)SelectedColumn;
                if (IsGlobal)
                {
                    foreach (HBP.Module3D.Column3DIEEG column in SelectedScene.ColumnManager.ColumnsIEEG)
                    {
                        column.IEEGParameters.Gain = value;
                    }
                }
                else
                {
                    selectedColumn.IEEGParameters.Gain = value;
                }
            });

            m_InputField.onEndEdit.AddListener((value) =>
            {
                if (ListenerLock) return;

                float val = float.Parse(value);
                HBP.Module3D.Column3DIEEG selectedColumn = (HBP.Module3D.Column3DIEEG)SelectedColumn;
                if (IsGlobal)
                {
                    foreach (HBP.Module3D.Column3DIEEG column in SelectedScene.ColumnManager.ColumnsIEEG)
                    {
                        column.IEEGParameters.InfluenceDistance = val;
                    }
                }
                else
                {
                    selectedColumn.IEEGParameters.InfluenceDistance = val;
                }
                m_InputField.text = selectedColumn.IEEGParameters.InfluenceDistance.ToString("N2");
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
                HBP.Module3D.Column3DIEEG selectedColumn = (HBP.Module3D.Column3DIEEG)SelectedColumn;
                m_Slider.value = selectedColumn.IEEGParameters.Gain;
                m_InputField.text = selectedColumn.IEEGParameters.InfluenceDistance.ToString("N2");
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