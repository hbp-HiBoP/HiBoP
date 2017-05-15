using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using HBP.Data.Visualization;

namespace HBP.UI.Visualization
{
    public class MultiVisualizationGestion : ItemGestion<MultiPatientsVisualization>
    {
        #region Properties
        Button displayButton;
        #endregion

        #region Public Methods
        public override void Save()
        {
            ApplicationState.ProjectLoaded.SetMultiPatientsVisualizations(Items.ToArray());
            base.Save();
        }
        public void Display()
        {
            Debug.Log("Display");
            FindObjectOfType<VisualizationLoader>().Load(list.GetObjectsSelected()[0]);
            base.Close();
        }
        #endregion

        #region Private Methods
        protected override void SetWindow()
        {
            displayButton = transform.FindChild("Content").FindChild("Buttons").FindChild("Display").GetComponent<Button>();
            list = transform.FindChild("Content").FindChild("MultiPatientsVisualizations").FindChild("List").FindChild("Viewport").FindChild("Content").GetComponent<MultiVisualizationList>();
            (list as MultiVisualizationList).ActionEvent.AddListener((visu, type) => OpenModifier(visu,true));
            (list as MultiVisualizationList).SelectEvent.AddListener(() => SetDisplay());
            AddItem(ApplicationState.ProjectLoaded.MultiPatientsVisualizations.ToArray());
        }
        void SetDisplay()
        {
            MultiPatientsVisualization[] visualizationsSelected = list.GetObjectsSelected();
            displayButton.interactable = (visualizationsSelected.Length == 1 && visualizationsSelected[0].IsVisualizable);
        }
        #endregion
    }
}