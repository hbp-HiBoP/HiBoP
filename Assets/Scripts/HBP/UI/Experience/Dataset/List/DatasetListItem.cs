using UnityEngine;
using UnityEngine.UI;
using d = HBP.Data.Experience.Dataset;

namespace HBP.UI.Experience.Dataset
{
	public class DatasetListItem : Tools.Unity.Lists.ActionnableItem<d.Dataset> 
	{
		#region Attributs
        [SerializeField]
        Text m_datasetDataNb;
        [SerializeField]
		Text m_name;
        #endregion

        #region Private Methods
        protected override void SetObject(d.Dataset dataset)
        {
            m_name.text = dataset.Name;
            m_datasetDataNb.text = dataset.Data.Count.ToString();
        }
        #endregion
	}
}