using HBP.Core.Data;
using UnityEngine;
using UnityEngine.UI;
using HBP.UI.Tools;

namespace HBP.UI.Module3D.Tools
{
    public class ConfigurationLoaderSaver : Tool
    {
        #region Properties
        /// <summary>
        /// Save the configuration to the visualization
        /// </summary>
        [SerializeField] private Button m_Save;
        /// <summary>
        /// Load the configuration from a visualization
        /// </summary>
        [SerializeField] private Button m_Load;
        /// <summary>
        /// Reset the configuration of this scene
        /// </summary>
        [SerializeField] private Button m_Reset;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_Save.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                SelectedScene.SaveConfiguration();
                DialogBoxManager.Open(DialogBoxManager.AlertType.Informational, "Configuration saved", "The configuration of the selected scene has been saved in the visualization <color=#3080ffff>" + SelectedScene.Name + "</color>.\n\nPlease save the project to apply changes in the project files.");
            });
            m_Load.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                ObjectSelector<Core.Data.Visualization> selector = WindowsManager.OpenSelector(ApplicationState.ProjectLoaded.Visualizations,false);
                selector.OnOk.AddListener(() =>
                {
                    if (selector.ObjectsSelected.Length > 0)
                    {
                        SelectedScene.Visualization.Configuration = selector.ObjectsSelected[0].Configuration.Clone() as VisualizationConfiguration;
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
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_Save.interactable = false;
            m_Load.interactable = false;
            m_Reset.interactable = false;
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            m_Save.interactable = true;
            m_Load.interactable = true;
            m_Reset.interactable = true;
        }
        #endregion
    }
}