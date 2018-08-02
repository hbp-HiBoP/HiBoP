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

                ApplicationState.Module3D.SelectedScene.IsMarsAtlasEnabled = isOn;
            });
        }
        public override void DefaultState()
        {
            m_Toggle.isOn = false;
            m_Toggle.interactable = false;
        }
        public override void UpdateInteractable()
        {
            bool canUseMarsAtlas = false;
            Base3DScene scene = ApplicationState.Module3D.SelectedScene;
            if (scene != null)
            {
                canUseMarsAtlas = scene.ColumnManager.SelectedMesh.IsMarsAtlasLoaded;
            }

            m_Toggle.interactable = canUseMarsAtlas;
        }
        public override void UpdateStatus()
        {
            m_Toggle.isOn = ApplicationState.Module3D.SelectedScene.IsMarsAtlasEnabled;
        }
        public void ChangeBrainTypeCallback()
        {
            Base3DScene selectedScene = ApplicationState.Module3D.SelectedScene;
            if (!selectedScene.ColumnManager.SelectedMesh.IsMarsAtlasLoaded && ApplicationState.Module3D.SelectedScene.IsMarsAtlasEnabled)
            {
                m_Toggle.isOn = false;
            }
        }
        #endregion
    }
}