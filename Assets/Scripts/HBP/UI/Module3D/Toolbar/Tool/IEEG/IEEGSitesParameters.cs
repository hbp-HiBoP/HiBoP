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

                HBP.Module3D.Column3DIEEG selectedColumn = (HBP.Module3D.Column3DIEEG)ApplicationState.Module3D.SelectedScene.ColumnManager.SelectedColumn;
                if (IsGlobal)
                {
                    foreach (HBP.Module3D.Column3DIEEG column in ApplicationState.Module3D.SelectedScene.ColumnManager.ColumnsIEEG)
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
                HBP.Module3D.Column3DIEEG selectedColumn = (HBP.Module3D.Column3DIEEG)ApplicationState.Module3D.SelectedScene.ColumnManager.SelectedColumn;
                if (IsGlobal)
                {
                    foreach (HBP.Module3D.Column3DIEEG column in ApplicationState.Module3D.SelectedScene.ColumnManager.ColumnsIEEG)
                    {
                        column.IEEGParameters.MaximumInfluence = val;
                    }
                }
                else
                {
                    selectedColumn.IEEGParameters.MaximumInfluence = val;
                }
                m_InputField.text = selectedColumn.IEEGParameters.MaximumInfluence.ToString("N2");
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
            bool isColumnIEEG = ApplicationState.Module3D.SelectedColumn.Type == HBP.Module3D.Column3D.ColumnType.IEEG;

            m_Slider.interactable = isColumnIEEG;
            m_InputField.interactable = isColumnIEEG;
        }

        public override void UpdateStatus(Toolbar.UpdateToolbarType type)
        {
            if (type == Toolbar.UpdateToolbarType.Scene || type == Toolbar.UpdateToolbarType.Column)
            {
                if (ApplicationState.Module3D.SelectedColumn.Type == HBP.Module3D.Column3D.ColumnType.IEEG)
                {
                    HBP.Module3D.Column3DIEEG selectedColumn = (HBP.Module3D.Column3DIEEG)ApplicationState.Module3D.SelectedScene.ColumnManager.SelectedColumn;
                    m_Slider.value = selectedColumn.IEEGParameters.Gain;
                    m_InputField.text = selectedColumn.IEEGParameters.MaximumInfluence.ToString("N2");
                }
                else
                {
                    m_Slider.value = 0.5f;
                    m_InputField.text = "15.00";
                }
            }
        }
        #endregion
    }
}