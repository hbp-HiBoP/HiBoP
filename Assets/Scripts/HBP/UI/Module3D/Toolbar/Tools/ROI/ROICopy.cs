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
            m_Button.onClick.AddListener(() =>
            {
                foreach (Column3D column in SelectedScene.Columns)
                {
                    if (column == SelectedColumn) continue;

                    foreach (ROI roi in SelectedColumn.ROIs)
                    {
                        column.CopyROI(roi);
                    }
                }

                foreach (Column3D column in SelectedScene.Columns)
                {
                    column.UpdateROIMask();
                }
            });
        }
        public override void DefaultState()
        {
            m_Button.interactable = false;
        }
        public override void UpdateInteractable()
        {
            bool hasROI = SelectedColumn.ROIs.Count > 0;

            m_Button.interactable = hasROI;
        }
        #endregion
    }
}