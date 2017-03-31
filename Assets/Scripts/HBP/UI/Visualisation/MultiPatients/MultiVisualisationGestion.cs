using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using HBP.Data.Visualisation;

namespace HBP.UI
{
    public class MultiVisualisationGestion : ItemGestion<MultiPatientsVisualisation>
    {
        #region Properties
        Button displayButton;
        #endregion

        #region Public Methods
        public override void Save()
        {
            ApplicationState.ProjectLoaded.SetMultiPatientsVisualisations(Items.ToArray());
            base.Save();
        }
        public void Display()
        {
            Debug.Log("Display");
            FindObjectOfType<VisualisationLoader>().Load(list.GetObjectsSelected()[0]);
            base.Close();
        }
        #endregion

        #region Private Methods
        protected override void SetWindow()
        {
            displayButton = transform.FindChild("Content").FindChild("Buttons").FindChild("Display").GetComponent<Button>();
            list = transform.FindChild("Content").FindChild("MultiPatientsVisualisations").FindChild("List").FindChild("Viewport").FindChild("Content").GetComponent<MultiVisualisationList>();
            (list as MultiVisualisationList).ActionEvent.AddListener((visu, type) => OpenModifier(visu,true));
            (list as MultiVisualisationList).SelectEvent.AddListener(() => SetDisplay());
            AddItem(ApplicationState.ProjectLoaded.MultiPatientsVisualisations.ToArray());
        }
        void SetDisplay()
        {
            MultiPatientsVisualisation[] visualisationsSelected = list.GetObjectsSelected();
            displayButton.interactable = (visualisationsSelected.Length == 1 && visualisationsSelected[0].isVisualisable());
        }
        #endregion
    }
}