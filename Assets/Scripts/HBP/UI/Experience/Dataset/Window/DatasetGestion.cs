using System.Linq;
using d = HBP.Data.Experience.Dataset;
using UnityEngine.UI;
using UnityEngine;

namespace HBP.UI.Experience.Dataset
{
	public class DatasetGestion : ItemGestion<d.Dataset>
    {
        #region Properties
        [SerializeField] Text m_datasetsCounter;
        [SerializeField] DatasetList m_DatasetList;
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
        protected override void Initialize()
		{
            m_List = m_DatasetList;
            (m_List as DatasetList).OnAction.AddListener((dataset, i) => OpenModifier(dataset,true));
            AddItem(ApplicationState.ProjectLoaded.Datasets.ToArray());

            m_List.OnSelectionChanged.AddListener((g, b) => m_datasetsCounter.text = m_List.ObjectsSelected.Count().ToString());
        }
        protected override void SetInteractable(bool interactable)
        {
            m_DatasetList.Interactable = interactable;
        }
        #endregion
    }
}
