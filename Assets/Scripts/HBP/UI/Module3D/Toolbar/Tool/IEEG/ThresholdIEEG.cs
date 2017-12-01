using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class ThresholdIEEG : Tool
    {
        #region Properties
        [SerializeField]
        private Button m_Button;
        [SerializeField]
        private Module3D.ThresholdIEEG m_ThresholdIEEG;
        
        public bool IsGlobal { get; set; }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_ThresholdIEEG.Initialize();
            m_ThresholdIEEG.OnChangeValues.AddListener((min, mid, max) =>
            {
                if (IsGlobal)
                {
                    foreach (HBP.Module3D.Column3DIEEG column in ApplicationState.Module3D.SelectedScene.ColumnManager.ColumnsIEEG)
                    {
                        column.IEEGParameters.SpanMin = min;
                        column.IEEGParameters.Middle = mid;
                        column.IEEGParameters.SpanMax = max;
                    }
                }
            });
        }

        public override void DefaultState()
        {
            m_Button.interactable = false;
        }

        public override void UpdateInteractable()
        {
            bool isColumnIEEG = ApplicationState.Module3D.SelectedColumn.Type == HBP.Module3D.Column3D.ColumnType.IEEG;

            m_Button.interactable = isColumnIEEG;
        }

        public override void UpdateStatus(Toolbar.UpdateToolbarType type)
        {
            if (type == Toolbar.UpdateToolbarType.Scene || type == Toolbar.UpdateToolbarType.Column)
            {
                HBP.Module3D.Column3D selectedColumn = ApplicationState.Module3D.SelectedScene.ColumnManager.SelectedColumn;
                if (selectedColumn.Type == HBP.Module3D.Column3D.ColumnType.IEEG)
                {
                    m_ThresholdIEEG.UpdateIEEGValues(((HBP.Module3D.Column3DIEEG)selectedColumn).IEEGParameters);
                }
            }
        }
        #endregion
    }
}