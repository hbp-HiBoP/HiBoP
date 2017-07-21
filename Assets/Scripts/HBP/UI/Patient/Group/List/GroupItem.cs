using UnityEngine;
using UnityEngine.UI;
using HBP.Data;

namespace HBP.UI.Anatomy
{
	public class GroupItem : Tools.Unity.Lists.ActionnableItem<Group> 
	{
		#region Properties
		[SerializeField] Text m_Name;
		[SerializeField] Text m_SizeGroup;
        public override Group Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;
                m_Name.text = value.Name;
                m_SizeGroup.text = value.Patients.Count.ToString();
            }
        }
        #endregion
	}
}