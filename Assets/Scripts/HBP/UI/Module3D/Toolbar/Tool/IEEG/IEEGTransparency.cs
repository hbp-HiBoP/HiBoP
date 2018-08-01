using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class IEEGTransparency : Tool
    {
        #region Properties
        [SerializeField]
        private Slider m_Slider;

        public bool IsGlobal { get; set; }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_Slider.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                if (IsGlobal)
                {
                    foreach (HBP.Module3D.Column3DIEEG column in ApplicationState.Module3D.SelectedScene.ColumnManager.ColumnsIEEG)
                    {
                        column.IEEGParameters.AlphaMin = value;
                    }
                }
                else
                {
                    ((HBP.Module3D.Column3DIEEG)ApplicationState.Module3D.SelectedColumn).IEEGParameters.AlphaMin = value;
                    //((HBP.Module3D.Column3DIEEG)ApplicationState.Module3D.SelectedScene.ColumnManager.SelectedColumn).IEEGParameters.AlphaMax = value; // FIXME : Required / other value ?
                }
            });
        }

        public override void DefaultState()
        {
            m_Slider.value = 0.2f;
            m_Slider.interactable = false;
        }

        public override void UpdateInteractable()
        {
            bool isColumnIEEG = ApplicationState.Module3D.SelectedColumn.Type == Data.Enums.ColumnType.iEEG;

            m_Slider.interactable = isColumnIEEG;
        }

        public override void UpdateStatus(Toolbar.UpdateToolbarType type)
        {
            if (type == Toolbar.UpdateToolbarType.Column || type == Toolbar.UpdateToolbarType.Scene)
            {
                if (ApplicationState.Module3D.SelectedColumn.Type == Data.Enums.ColumnType.iEEG)
                {
                    m_Slider.value = ((HBP.Module3D.Column3DIEEG)ApplicationState.Module3D.SelectedColumn).IEEGParameters.AlphaMin;
                }
                else
                {
                    m_Slider.value = 0.2f;
                }
            }
        }
        #endregion
    }
}