using HBP.Core.Data;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using HBP.UI.Tools;
using HBP.Core.Tools;

namespace HBP.UI.Toolbar
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
            m_Copy.onClick.AddListener(async () =>
            {
                if (ListenerLock) return;

                SelectedScene.SaveConfiguration();
                if (ApplicationState.LoadedProject.Visualizations.Contains(SelectedScene.Visualization))
                {
                    int result = await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Warning, "Visualization already exists", "The visualization you are trying to add to the project already exists.\n\nDo you want to create a clone of the selected visualization?\nThis will not link the selected visualization with the newly cloned visualization, but take a snapshot of the selected visualization and save it as a new visualization.", "Clone", "Cancel");
                    if (result == 0)
                    {
                        Visualization clonedVisualization = SelectedScene.Visualization.Clone() as Visualization;
                        clonedVisualization.GenerateID();
                        SaveVisualizationToProject(clonedVisualization);
                    }
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

        private void SaveVisualizationToProject(Visualization visualization)
        {
            var projectVisualizations = ApplicationState.LoadedProject.Visualizations;
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

            ApplicationState.LoadedProject.AddVisualization(visualization);
            DialogBoxManager.Open(Core.Enums.DialogBoxType.Informational, "Visualization saved", "The selected visualization has been saved under the name <color=#3080ffff>" + visualization.Name + "</color>.").Forget();
        }

        #endregion
    }
}
