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
            bool canUseMarsAtlas = ApplicationState.Module3D.SelectedScene.ColumnManager.SelectedMesh.IsMarsAtlasLoaded;

            switch (ApplicationState.Module3D.SelectedScene.ModesManager.CurrentMode.ID)
            {
                case Mode.ModesId.NoPathDefined:
                    m_Toggle.interactable = false;
                    break;
                case Mode.ModesId.MinPathDefined:
                    m_Toggle.interactable = canUseMarsAtlas;
                    break;
                case Mode.ModesId.AllPathDefined:
                    m_Toggle.interactable = canUseMarsAtlas;
                    break;
                case Mode.ModesId.ComputingAmplitudes:
                    m_Toggle.interactable = false;
                    break;
                case Mode.ModesId.AmplitudesComputed:
                    m_Toggle.interactable = canUseMarsAtlas;
                    break;
                case Mode.ModesId.TriErasing:
                    m_Toggle.interactable = false;
                    break;
                case Mode.ModesId.ROICreation:
                    m_Toggle.interactable = false;
                    break;
                case Mode.ModesId.AmpNeedUpdate:
                    m_Toggle.interactable = canUseMarsAtlas;
                    break;
                case Mode.ModesId.Error:
                    m_Toggle.interactable = false;
                    break;
                default:
                    break;
            }
        }
        public override void UpdateStatus(Toolbar.UpdateToolbarType type)
        {
            if (type == Toolbar.UpdateToolbarType.Scene)
            {
                m_Toggle.isOn = ApplicationState.Module3D.SelectedScene.IsMarsAtlasEnabled;
            }
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