using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using T = Tools.Unity;

namespace HBP.UI.Module3D.Tools
{
    public class CopyVisualization : Tool
    {
        #region Properties
        /// <summary>
        /// Copy the selected visualization to the project
        /// </summary>
        [SerializeField] private Button m_Copy;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_Copy.onClick.AddListener(() =>
            {
                if (ListenerLock) return;

                SelectedScene.SaveConfiguration();
                if (ApplicationState.ProjectLoaded.Visualizations.Contains(SelectedScene.Visualization))
                {
                    ApplicationState.DialogBoxManager.Open(T.DialogBoxManager.AlertType.WarningMultiOptions, "Visualization already exists", "The visualization you are trying to add to the project already exists.\n\nDo you want to create a clone of the selected visualization?\nThis will not link the selected visualization with the newly cloned visualization, but take a snapshot of the selected visualization and save it as a new visualization.", () =>
                    {
                        Data.Visualization.Visualization clonedVisualization = SelectedScene.Visualization.Clone() as Data.Visualization.Visualization;
                        clonedVisualization.GenerateID();
                        SaveVisualizationToProject(clonedVisualization);
                    }, "Clone");
                }
                else
                {
                    SaveVisualizationToProject(SelectedScene.Visualization);
                }
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_Copy.interactable = false;
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            m_Copy.interactable = true;
        }
        #endregion

        #region Private Methods
        private void SaveVisualizationToProject(Data.Visualization.Visualization visualization)
        {
            var projectVisualizations = ApplicationState.ProjectLoaded.Visualizations;
            if (projectVisualizations.Any(v => v.Name == visualization.Name))
            {
                int count = 1;
                string name = string.Format("{0}({1})", visualization.Name, count);
                while (projectVisualizations.Any(v => v.Name == name))
                {
                    count++;
                    name = string.Format("{0}({1})", visualization.Name, count);
                }
                visualization.Name = name;
            }
            ApplicationState.ProjectLoaded.AddVisualization(visualization);
            ApplicationState.DialogBoxManager.Open(T.DialogBoxManager.AlertType.Informational, "Visualization saved", "The selected visualization has been saved under the name <color=#3080ffff>" + visualization.Name + "</color>.");
        }
        #endregion
    }
}