using HBP.Module3D;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class MarsAtlas : Tool
    {
        #region Properties
        [SerializeField]
        private Toggle m_Toggle;
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_Toggle.onValueChanged.AddListener((isOn) =>
            {
                if (ListenerLock) return;

                SelectedScene.IsMarsAtlasEnabled = isOn;
            });
        }
        public override void DefaultState()
        {
            m_Toggle.isOn = false;
            m_Toggle.interactable = false;
        }
        public override void UpdateInteractable()
        {
            bool canUseMarsAtlas = SelectedScene.ColumnManager.SelectedMesh.IsMarsAtlasLoaded;

            m_Toggle.interactable = canUseMarsAtlas;
        }
        public override void UpdateStatus()
        {
            m_Toggle.isOn = SelectedScene.IsMarsAtlasEnabled;
        }
        public void ChangeBrainTypeCallback()
        {
            if (!SelectedScene.ColumnManager.SelectedMesh.IsMarsAtlasLoaded && SelectedScene.IsMarsAtlasEnabled)
            {
                m_Toggle.isOn = false;
            }
        }
        #endregion
    }
}