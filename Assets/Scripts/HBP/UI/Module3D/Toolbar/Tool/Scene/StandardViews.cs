using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class StandardViews : Tool
    {
        #region Properties
        [SerializeField]
        private Button m_Button;

        public UnityEvent OnClick = new UnityEvent();
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_Button.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                Base3DScene selectedScene = ApplicationState.Module3D.SelectedScene;
                while (selectedScene.ColumnManager.ViewLineNumber > 3)
                {
                    selectedScene.ColumnManager.RemoveViewLine();
                }
                while (selectedScene.ColumnManager.ViewLineNumber < 3)
                {
                    selectedScene.ColumnManager.AddViewLine();
                }
                foreach (Column3D column in selectedScene.ColumnManager.Columns)
                {
                    foreach (View3D view in column.Views)
                    {
                        view.Default();
                    }
                }
                OnClick.Invoke();
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