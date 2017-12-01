using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class DefaultView : Tool
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
                if (ListenerLock) return;

                int lineID = ApplicationState.Module3D.SelectedView.LineID;
                foreach (Column3D column in ApplicationState.Module3D.SelectedScene.ColumnManager.Columns)
                {
                    foreach (View3D view in column.Views)
                    {
                        if (view.LineID == lineID)
                        {
                            view.Default();
                        }
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
            m_Button.interactable = true;
        }
        #endregion
    }
}