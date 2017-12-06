using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace HBP.UI.Visualization
{
    public class VisualizationGestion : ItemGestion<Data.Visualization.Visualization>
    {
        #region Properties
        Button displayButton;
        #endregion

        #region Public Methods
        public override void Save()
        {
            if (ApplicationState.Module3D.Visualizations.Count > 0)
            {
                ApplicationState.DialogBoxManager.Open(Tools.Unity.DialogBoxManager.AlertType.WarningMultiOptions, "Reload required", "A visualization is already open. Your changes will not be applied unless you reload.\n\nWould you like to reload ?", () =>
                {
                    ApplicationState.ProjectLoaded.SetVisualizations(Items.ToArray());
                    base.Save();
                    DataManager.Clear();
                    ApplicationState.Module3D.ReloadScenes();
                });
            }
            else
            {
                ApplicationState.ProjectLoaded.SetVisualizations(Items.ToArray());
                base.Save();
            }
        }
        public void Display()
        {
            ApplicationState.Module3D.LoadScenes(m_List.ObjectsSelected);
            base.Close();
        }
        #endregion

        #region Private Methods
        protected override void SetWindow()
        {
            displayButton = transform.Find("Content").Find("Buttons").Find("Display").GetComponent<Button>();
            m_List = transform.Find("Content").Find("MultiPatientsVisualizations").Find("List").Find("Display").GetComponent<VisualizationList>();
            (m_List as VisualizationList).OnAction.AddListener((visu, type) => OpenModifier(visu,true));
            (m_List as VisualizationList).OnSelectionChanged.AddListener((visu,selected) => SetDisplay());
            AddItem(ApplicationState.ProjectLoaded.Visualizations.ToArray());
        }
        void SetDisplay()
        {
            Data.Visualization.Visualization[] visualizationsSelected = m_List.ObjectsSelected;
            displayButton.interactable = (visualizationsSelected.Length > 0 && visualizationsSelected.All(v => v.IsVisualizable));
        }
        #endregion
    }
}