using HBP.Module3D;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class FMRI : Tool
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

                //if (ApplicationState.Module3D.SelectedScene.AddFMRIColumn()) ApplicationState.Module3D.SelectedScene.DisplayScreenMessage("fMRI successfully loaded. ", 2f, 150, 80);
                //else ApplicationState.Module3D.SelectedScene.DisplayScreenMessage("Error during fMRI loading. ", 2f, 170, 80);
                OnClick.Invoke();
            });

            m_Remove.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                ApplicationState.Module3D.SelectedScene.RemoveLastFMRIColumn();
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
            bool canAddFMRI = ApplicationState.Module3D.SelectedScene.ColumnManager.Columns.Count < HBP3DModule.MAXIMUM_COLUMN_NUMBER;
            bool canRemoveFMRI = (ApplicationState.Module3D.SelectedScene.ColumnManager.ColumnsFMRI.Count > 0);

            switch (ApplicationState.Module3D.SelectedScene.ModesManager.CurrentMode.ID)
            {
                case Mode.ModesId.NoPathDefined:
                    m_Add.interactable = false;
                    m_Remove.interactable = false;
                    break;
                case Mode.ModesId.MinPathDefined:
                    m_Add.interactable = canAddFMRI;
                    m_Remove.interactable = canRemoveFMRI;
                    break;
                case Mode.ModesId.AllPathDefined:
                    m_Add.interactable = canAddFMRI;
                    m_Remove.interactable = canRemoveFMRI;
                    break;
                case Mode.ModesId.ComputingAmplitudes:
                    m_Add.interactable = false;
                    m_Remove.interactable = false;
                    break;
                case Mode.ModesId.AmplitudesComputed:
                    m_Add.interactable = canAddFMRI;
                    m_Remove.interactable = canRemoveFMRI;
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
                    m_Add.interactable = canAddFMRI;
                    m_Remove.interactable = canRemoveFMRI;
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