using UnityEngine;
using UnityEngine.UI;
using d = HBP.Data.Experience.Dataset;

namespace HBP.UI.Experience.Dataset
{
	public class DatasetListItem : Tools.Unity.Lists.ActionnableItem<d.Dataset> 
	{
		#region Attributs
        [SerializeField]
        Text m_DatasetDataNb;
        [SerializeField]
		Text m_Name;

        public override d.Dataset Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;
                m_Name.text = value.Name;
                m_DatasetDataNb.text = value.Data.Count.ToString();
            }
        }
        #endregion
	}
}