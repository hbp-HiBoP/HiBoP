using UnityEngine.UI;
using System.Linq;
using HBP.Data.Visualisation;

namespace HBP.UI.Visualisation
{
    public class SingleVisualisationGestion : ItemGestion<SinglePatientVisualisation>
    {
        #region Properties
        Button displayButton;
        #endregion

        #region Public Methods
        public override void Save()
        {
            ApplicationState.ProjectLoaded.SetSinglePatientVisualisations(Items.ToArray());
            base.Save();
        }     
        public void Display()
        {
            FindObjectOfType<VisualisationLoader>().Load(list.GetObjectsSelected()[0]);
            base.Close();
        }
        #endregion

        #region Private Methods
        protected override void SetWindow()
        {
            displayButton = transform.FindChild("Content").FindChild("Buttons").FindChild("Display").GetComponent<Button>();
            list = transform.FindChild("Content").FindChild("SinglePatientVisualisations").FindChild("List").FindChild("Viewport").FindChild("Content").GetComponent<SingleVisualisationList>();
            (list as SingleVisualisationList).ActionEvent.AddListener((visu, type) => OpenModifier(visu,true));
            (list as SingleVisualisationList).SelectEvent.AddListener(() => SetDisplay());
            AddItem(ApplicationState.ProjectLoaded.SinglePatientVisualisations.ToArray());
        }
        void SetDisplay()
        {
            SinglePatientVisualisation[] visualisationsSelected = list.GetObjectsSelected();
            displayButton.interactable = (visualisationsSelected.Length == 1 && visualisationsSelected[0].isVisualisable());
        }
        #endregion
    }
}