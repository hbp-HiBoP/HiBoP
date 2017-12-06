using System.Linq;
using d = HBP.Data.Experience.Dataset;
using UnityEngine.UI;

namespace HBP.UI.Experience.Dataset
{
	public class DatasetGestion : ItemGestion<d.Dataset>
    {
        #region Properties
        Text m_datasetsCounter;
        #endregion

        #region Public Methods
        public override void Save()
		{
            if (DataManager.HasData)
            {
                ApplicationState.DialogBoxManager.Open(Tools.Unity.DialogBoxManager.AlertType.WarningMultiOptions, "Reload required", "Some data have already been loaded. Your changes will not be applied unless you reload.\n\nWould you like to reload ?", () =>
                {
                    ApplicationState.ProjectLoaded.SetDatasets(Items.ToArray());
                    base.Save();
                    DataManager.Clear();
                    ApplicationState.Module3D.ReloadScenes();
                });
            }
            else
            {
                ApplicationState.ProjectLoaded.SetDatasets(Items.ToArray());
                base.Save();
            }
        }
        public override void Remove()
        {
            base.Remove();
            m_datasetsCounter.text = m_List.ObjectsSelected.Count().ToString();
        }
        #endregion

        #region Private Methods
        protected override void SetWindow()
		{
            m_List = transform.Find("Content").Find("Datasets").Find("List").Find("Display").GetComponent<DatasetList>();
            (m_List as DatasetList).OnAction.AddListener((dataset, i) => OpenModifier(dataset,true));
            AddItem(ApplicationState.ProjectLoaded.Datasets.ToArray());

            m_datasetsCounter = transform.Find("Content").Find("Buttons").Find("ItemSelected").Find("Counter").GetComponent<Text>();
            m_List.OnSelectionChanged.AddListener((g, b) => m_datasetsCounter.text = m_List.ObjectsSelected.Count().ToString());
        }
        #endregion
    }
}
