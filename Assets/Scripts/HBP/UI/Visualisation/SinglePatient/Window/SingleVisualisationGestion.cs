using UnityEngine.UI;
using System.Linq;
using HBP.Data.Visualization;

namespace HBP.UI.Visualization
{
    public class SingleVisualizationGestion : ItemGestion<SinglePatientVisualization>
    {
        #region Properties
        Button displayButton;
        #endregion

        #region Public Methods
        public override void Save()
        {
            ApplicationState.ProjectLoaded.SetSinglePatientVisualizations(Items.ToArray());
            base.Save();
        }     
        public void Display()
        {
            FindObjectOfType<VisualizationLoader>().Load(list.GetObjectsSelected()[0]);
            base.Close();
        }
        #endregion

        #region Private Methods
        protected override void SetWindow()
        {
            displayButton = transform.FindChild("Content").FindChild("Buttons").FindChild("Display").GetComponent<Button>();
            list = transform.FindChild("Content").FindChild("SinglePatientVisualizations").FindChild("List").FindChild("Viewport").FindChild("Content").GetComponent<SingleVisualizationList>();
            (list as SingleVisualizationList).ActionEvent.AddListener((visu, type) => OpenModifier(visu,true));
            (list as SingleVisualizationList).SelectEvent.AddListener(() => SetDisplay());
            AddItem(ApplicationState.ProjectLoaded.SinglePatientVisualizations.ToArray());
        }
        void SetDisplay()
        {
            SinglePatientVisualization[] visualizationsSelected = list.GetObjectsSelected();
            displayButton.interactable = (visualizationsSelected.Length == 1 && visualizationsSelected[0].IsVisualizable);
        }
        #endregion
    }
}