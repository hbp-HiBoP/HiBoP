using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using Tools.Unity.Lists;

namespace HBP.UI
{
    /// <summary>
    /// Component to display group in list.
    /// </summary>
	public class GroupItem : ActionnableItem<Core.Data.Group> 
	{
		#region Properties
		[SerializeField] Text m_NameText;
		[SerializeField] Text m_PatientsText;
        [SerializeField] HBP.Theme.State m_ErrorState;

        /// <summary>
        /// Object to display.
        /// </summary>
        public override Core.Data.Group Object
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