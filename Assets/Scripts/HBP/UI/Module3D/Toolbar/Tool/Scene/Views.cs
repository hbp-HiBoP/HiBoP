using HBP.Module3D;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class Views : Tool
    {
        #region Properties
        [SerializeField]
        private Button m_Add;
        [SerializeField]
        private Button m_Remove;

        public UnityEvent OnClick = new UnityEvent();
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_Add.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                SelectedScene.ColumnManager.AddViewLine();
                OnClick.Invoke();
            });

            m_Remove.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                SelectedScene.ColumnManager.RemoveViewLine();
                OnClick.Invoke();
            });
        }
        public override void DefaultState()
        {
            m_Add.interactable = false;
            m_Remove.interactable = false;
        }
        public override void UpdateInteractable()
        {
            bool canAddView = SelectedScene.ColumnManager.ViewLineNumber < HBP3DModule.MAXIMUM_VIEW_NUMBER;
            bool canRemoveView = SelectedScene.ColumnManager.ViewLineNumber > 1;

            m_Add.interactable = canAddView;
            m_Remove.interactable = canRemoveView;
        }
        #endregion
    }
}