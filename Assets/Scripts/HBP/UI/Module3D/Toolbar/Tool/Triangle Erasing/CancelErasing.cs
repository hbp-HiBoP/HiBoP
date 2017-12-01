﻿using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class CancelErasing : Tool
    {
        #region Properties
        [SerializeField]
        private Button m_Button;
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            ApplicationState.Module3D.OnModifyInvisiblePart.AddListener(() =>
            {
                if (ApplicationState.Module3D.SelectedScene)
                {
                    UpdateInteractable();
                }
            });
            m_Button.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                ApplicationState.Module3D.SelectedScene.CancelLastTriangleErasingAction();
            });
        }
        public override void DefaultState()
        {
            m_Button.interactable = false;
        }
        public override void UpdateInteractable()
        {
            bool isCancelAvailable = ApplicationState.Module3D.SelectedScene.CanCancelLastTriangleErasingAction;

            m_Button.interactable = isCancelAvailable;
        }
        #endregion
    }
}