using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class AtlasState : Tool
    {
        #region Properties
        [SerializeField] private Toggle m_IBCToggle;
        [SerializeField] private Toggle m_JubrainToggle;
        [SerializeField] private Toggle m_MarsAtlasToggle;
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_IBCToggle.onValueChanged.AddListener((isOn) =>
            {
                if (ListenerLock) return;

                SelectedScene.ColumnManager.FMRIManager.DisplayIBCContrasts = isOn;
            });
            m_JubrainToggle.onValueChanged.AddListener((isOn) =>
            {
                if (ListenerLock) return;

                SelectedScene.DisplayJuBrainAtlas = isOn;
            });
            m_MarsAtlasToggle.onValueChanged.AddListener((isOn) =>
            {
                if (ListenerLock) return;

                SelectedScene.IsMarsAtlasEnabled = isOn;
            });
        }

        public override void DefaultState()
        {
            m_IBCToggle.isOn = false;
            m_IBCToggle.interactable = false;
            m_JubrainToggle.isOn = false;
            m_JubrainToggle.interactable = false;
            m_MarsAtlasToggle.isOn = false;
            m_MarsAtlasToggle.interactable = false;
        }

        public override void UpdateInteractable()
        {
            bool isIBCAvailable = ApplicationState.Module3D.IBCObjects.Loaded;
            bool isJuBrainAtlasAvailable = ApplicationState.Module3D.JuBrainAtlas.Loaded;
            bool canUseMarsAtlas = SelectedScene.ColumnManager.SelectedMesh.IsMarsAtlasLoaded;

            m_IBCToggle.interactable = isIBCAvailable;
            m_JubrainToggle.interactable = isJuBrainAtlasAvailable;
            m_MarsAtlasToggle.interactable = canUseMarsAtlas;
        }

        public override void UpdateStatus()
        {
            m_IBCToggle.isOn = SelectedScene.ColumnManager.FMRIManager.DisplayIBCContrasts;
            m_JubrainToggle.isOn = SelectedScene.DisplayJuBrainAtlas;
            m_MarsAtlasToggle.isOn = SelectedScene.IsMarsAtlasEnabled;
        }
        #endregion
    }
}