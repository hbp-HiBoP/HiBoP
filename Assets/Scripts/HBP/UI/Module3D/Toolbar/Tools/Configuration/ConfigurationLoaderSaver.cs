using HBP.Module3D;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

                SelectedScene.SaveConfiguration();
                ApplicationState.DialogBoxManager.Open(T.DialogBoxManager.AlertType.Informational, "Configuration saved", "The configuration of the selected scene has been saved in the visualization <color=#3080ffff>" + SelectedScene.Name + "</color>.\n\nPlease save the project to apply changes in the project files.");
            });
            m_Load.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                ObjectSelector<Data.Visualization.Visualization> selector = ApplicationState.WindowsManager.OpenSelector(ApplicationState.ProjectLoaded.Visualizations,false);
                selector.OnSave.AddListener(() =>
                {
                    if (selector.ObjectsSelected.Length > 0)
                    {
                        SelectedScene.Visualization.Configuration = selector.ObjectsSelected[0].Configuration.Clone() as Data.Visualization.VisualizationConfiguration;
                        SelectedScene.LoadConfiguration();
                    }
                });
            });
            m_Reset.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                SelectedScene.ResetConfiguration();
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