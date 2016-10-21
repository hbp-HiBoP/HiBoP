using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Patient;

namespace HBP.UI.Patient
{
	public class GroupItem : Tools.Unity.Lists.ListItemWithActions<Group> 
	{
		#region Attributs
		[SerializeField]
		Text m_name;
		[SerializeField]
		Text m_sizeGroup;
		#endregion

		#region Protected Methods
		protected override void SetObject(Group groupToDisplay)
		{
            m_name.text = groupToDisplay.Name;
            m_sizeGroup.text = groupToDisplay.Patients.Count.ToString();
        }
		#endregion
	}
}