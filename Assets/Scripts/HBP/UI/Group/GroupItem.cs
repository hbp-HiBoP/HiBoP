using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Tools.Unity.Lists;

namespace HBP.UI
{
	public class GroupItem : ActionnableItem<Data.Group> 
	{
		#region Properties
		[SerializeField] Text m_NameText;
		[SerializeField] Text m_PatientsText;
        [SerializeField] State m_ErrorState;

        public override Data.Group Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;
                m_NameText.text = value.Name;

                m_PatientsText.SetIEnumerableFieldInItem("Patients", from patient in m_Object.Patients select patient.Name, m_ErrorState);
            }
        }
        #endregion
    }
}