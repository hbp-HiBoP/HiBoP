using HBP.Module3D;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using T = Tools.Unity;

namespace HBP.UI.Module3D.Tools
{
    public class ConfigurationLoaderSaver : Tool
    {
        #region Properties
        [SerializeField]
        private Button m_Save;
        [SerializeField]
        private Button m_Load;
        [SerializeField]
        private Button m_Reset;
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_Save.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                Base3DScene scene = ApplicationState.Module3D.SelectedScene;
                scene.SaveConfiguration();
                ApplicationState.DialogBoxManager.Open(T.DialogBoxManager.AlertType.Informational, "Configuration saved", "The configuration of the selected scene has been saved in the visualization <color=#3080ffff>" + scene.Name + "</color>.\n\nPlease save the project to apply changes in the project files.");
            });
            m_Load.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                ApplicationState.Module3D.SelectedScene.LoadConfiguration();
            });
            m_Reset.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                ApplicationState.Module3D.SelectedScene.ResetConfiguration();
            });
        }
        public override void DefaultState()
        {
            m_Save.interactable = false;
            m_Load.interactable = false;
            m_Reset.interactable = false;
        }
        public override void UpdateInteractable()
        {
            m_Save.interactable = true;
            m_Load.interactable = true;
            m_Reset.interactable = true;
        }
        #endregion
    }
}