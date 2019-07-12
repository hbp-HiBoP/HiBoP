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
        private Button m_Auto;
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
                    foreach (HBP.Module3D.Column3DDynamic column in SelectedScene.ColumnManager.ColumnsIEEG)
                    {
                        column.DynamicParameters.SetSpanValues(min, mid, max, column);
                    }
                }
                else
                {
                    HBP.Module3D.Column3DDynamic column = (HBP.Module3D.Column3DDynamic)SelectedColumn;
                    column.DynamicParameters.SetSpanValues(min, mid, max, column);
                }
            });
            m_Auto.onClick.AddListener(() =>
            {
                HBP.Module3D.Column3DDynamic column = (HBP.Module3D.Column3DDynamic)SelectedColumn;
                column.DynamicParameters.SetSpanValues(0, 0, 0, column);
                m_ThresholdIEEG.UpdateIEEGValues(((HBP.Module3D.Column3DDynamic)SelectedColumn).DynamicParameters);
            });
        }

        public override void DefaultState()
        {
            m_Button.interactable = false;
        }

        public override void UpdateInteractable()
        {
            bool isColumnIEEG = SelectedColumn.Type == Data.Enums.ColumnType.iEEG;

            m_Button.interactable = isColumnIEEG;
        }

        public override void UpdateStatus()
        {
            if (SelectedColumn.Type == Data.Enums.ColumnType.iEEG)
            {
                m_ThresholdIEEG.UpdateIEEGValues(((HBP.Module3D.Column3DDynamic)SelectedColumn).DynamicParameters);
            }
        }
        #endregion
    }
}