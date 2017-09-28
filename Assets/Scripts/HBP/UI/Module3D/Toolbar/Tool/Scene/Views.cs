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

                ApplicationState.Module3D.ScenesManager.SelectedScene.ColumnManager.AddViewLine();
                OnClick.Invoke();
            });

            m_Remove.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                ApplicationState.Module3D.ScenesManager.SelectedScene.ColumnManager.RemoveViewLine();
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
            bool canAddView = ApplicationState.Module3D.SelectedScene.ColumnManager.ViewLineNumber < HBP3DModule.MAXIMUM_VIEW_NUMBER;
            bool canRemoveView = ApplicationState.Module3D.SelectedScene.ColumnManager.ViewLineNumber > 1;

            switch (ApplicationState.Module3D.SelectedScene.ModesManager.CurrentMode.ID)
            {
                case Mode.ModesId.NoPathDefined:
                    m_Add.interactable = false;
                    m_Remove.interactable = false;
                    break;
                case Mode.ModesId.MinPathDefined:
                    m_Add.interactable = canAddView;
                    m_Remove.interactable = canRemoveView;
                    break;
                case Mode.ModesId.AllPathDefined:
                    m_Add.interactable = canAddView;
                    m_Remove.interactable = canRemoveView;
                    break;
                case Mode.ModesId.ComputingAmplitudes:
                    m_Add.interactable = canAddView;
                    m_Remove.interactable = canRemoveView;
                    break;
                case Mode.ModesId.AmplitudesComputed:
                    m_Add.interactable = canAddView;
                    m_Remove.interactable = canRemoveView;
                    break;
                case Mode.ModesId.TriErasing:
                    m_Add.interactable = false;
                    m_Remove.interactable = false;
                    break;
                case Mode.ModesId.ROICreation:
                    m_Add.interactable = false;
                    m_Remove.interactable = false;
                    break;
                case Mode.ModesId.AmpNeedUpdate:
                    m_Add.interactable = canAddView;
                    m_Remove.interactable = canRemoveView;
                    break;
                case Mode.ModesId.Error:
                    m_Add.interactable = false;
                    m_Remove.interactable = false;
                    break;
                default:
                    break;
            }
        }
        #endregion
    }
}