using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class ROICopy : Tool
    {
        #region Properties
        [SerializeField]
        private Button m_Button;
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            ApplicationState.Module3D.OnChangeNumberOfROI.AddListener(() =>
            {
                UpdateInteractable();
            });
            m_Button.onClick.AddListener(() =>
            {
                foreach (Column3D column in ApplicationState.Module3D.SelectedScene.ColumnManager.Columns)
                {
                    if (column == ApplicationState.Module3D.SelectedColumn) continue;

                    foreach (ROI roi in ApplicationState.Module3D.SelectedColumn.ROIs)
                    {
                        column.CopyROI(roi);
                    }
                }

                foreach (Column3D column in ApplicationState.Module3D.SelectedScene.ColumnManager.Columns)
                {
                    ApplicationState.Module3D.SelectedScene.UpdateSitesROIMask(column);
                }
            });
        }
        public override void DefaultState()
        {
            m_Button.interactable = false;
        }
        public override void UpdateInteractable()
        {
            bool hasROI = ApplicationState.Module3D.SelectedColumn.ROIs.Count > 0;

            m_Button.interactable = hasROI;
        }
        #endregion
    }
}