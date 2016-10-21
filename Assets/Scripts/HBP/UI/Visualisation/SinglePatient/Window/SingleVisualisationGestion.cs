using UnityEngine.UI;
using System.Linq;
using HBP.Data.Visualisation;

namespace HBP.UI
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
            (list as SingleVisualisationList).SelectEvent.AddListener(() => SetLoad());
            AddItem(ApplicationState.ProjectLoaded.SinglePatientVisualisations.ToArray());
        }
        void SetLoad()
        {
            bool isOk = true;
            SinglePatientVisualisation[] selected = list.GetObjectsSelected();
            if (selected.Length != 0)
            {
                SinglePatientVisualisation visu = selected[0];
                if (visu.Patient != null && visu.Columns.Count != 0)
                {
                    foreach (Column column in visu.Columns)
                    {
                        if (!column.IsCompatible(visu.Patient))
                        {
                            isOk = false;
                            break;
                        }
                    }
                }
                else
                {
                    isOk = false;
                }
            }
            else
            {
                isOk = false;
            }
            displayButton.interactable = isOk;
        }
        #endregion
    }
}