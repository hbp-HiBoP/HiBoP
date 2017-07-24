using UnityEngine;
using UnityEngine.UI;
using d = HBP.Data.Experience.Dataset;

namespace HBP.UI.Experience.Dataset
{
	public class DatasetListItem : Tools.Unity.Lists.ActionnableItem<d.Dataset> 
	{
		#region Attributs
        [SerializeField]
        Text m_DataInfosText;
        [SerializeField]
		Text m_NameText;

        public override d.Dataset Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;
                m_NameText.text = value.Name;
                m_DataInfosText.text = value.Data.Count.ToString();
            }
        }
        #endregion
	}
}