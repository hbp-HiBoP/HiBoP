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
            ApplicationState.ProjectLoaded.SetVisualizations(Items.ToArray());
            base.Save();
        }
        public void Display()
        {
            ApplicationState.Module3D.LoadScene(m_List.ObjectsSelected.First());
            base.Close();
        }
        #endregion

        #region Private Methods
        protected override void SetWindow()
        {
            displayButton = transform.Find("Content").Find("Buttons").Find("Display").GetComponent<Button>();
            m_List = transform.Find("Content").Find("MultiPatientsVisualizations").Find("List").Find("Display").Find("Viewport").Find("Content").GetComponent<VisualizationList>();
            (m_List as VisualizationList).OnAction.AddListener((visu, type) => OpenModifier(visu,true));
            (m_List as VisualizationList).OnSelectionChanged.AddListener((visu,selected) => SetDisplay());
            AddItem(ApplicationState.ProjectLoaded.Visualizations.ToArray());
        }
        void SetDisplay()
        {
            Data.Visualization.Visualization[] visualizationsSelected = m_List.ObjectsSelected;
            displayButton.interactable = (visualizationsSelected.Length == 1 && visualizationsSelected[0].IsVisualizable);
        }
        #endregion
    }
}