using System.Linq;
using d = HBP.Data.Experience.Dataset;

namespace HBP.UI.Experience.Dataset
{
	public class DatasetGestion : ItemGestion<d.Dataset>
    {
		#region Public Methods
		public override void Save()
		{
            ApplicationState.ProjectLoaded.SetDatasets(Items.ToArray());
            base.Save();
        }
        #endregion

        #region Private Methods
        protected override void SetWindow()
		{
            m_List = transform.Find("Content").Find("List").Find("List").Find("Viewport").Find("Content").GetComponent<DatasetList>();
            (m_List as DatasetList).OnAction.AddListener((dataset, i) => OpenModifier(dataset,true));
            AddItem(ApplicationState.ProjectLoaded.Datasets.ToArray());
        }
        #endregion
    }
}
