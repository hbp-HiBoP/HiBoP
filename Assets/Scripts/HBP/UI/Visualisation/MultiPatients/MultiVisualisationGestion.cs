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
            (list as MultiVisualisationList).SelectEvent.AddListener(() => SetLoad());
            AddItem(ApplicationState.ProjectLoaded.MultiPatientsVisualisations.ToArray());
        }
        void SetLoad()
        {
            bool l_Isok = true;
            MultiPatientsVisualisation[] selected = list.GetObjectsSelected();
            if (selected.Length != 0)
            {
                MultiPatientsVisualisation visu = selected[0];
                if (visu.Patients.Count != 0 && visu.Columns.Count != 0)
                {
                    foreach (Column column in visu.Columns)
                    {
                        if (!column.IsCompatible(visu.Patients.ToArray()))
                        {
                            l_Isok = false;
                            break;
                        }
                    }
                }
                else
                {
                    l_Isok = false;
                }
            }
            displayButton.interactable = l_Isok;
        }
        #endregion
    }
}