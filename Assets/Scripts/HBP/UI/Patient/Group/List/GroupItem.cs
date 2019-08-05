using UnityEngine;
using UnityEngine.UI;
using HBP.Data;
using System.Linq;
using Tools.Unity.Lists;
using NewTheme.Components;
using Tools.Unity;
using System.Text;

namespace HBP.UI.Anatomy
{
	public class GroupItem : ActionnableItem<Group> 
	{
		#region Properties
		[SerializeField] Text m_NameText;

		[SerializeField] Text m_PatientsText;
        [SerializeField] Tooltip m_PatientTooltip;

        [SerializeField] State m_ErrorState;

        public override Group Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;
                m_NameText.text = value.Name;

                StringBuilder stringBuilder = new StringBuilder();
                stringBuilder.AppendLine("Patients :");
                string[] patients = m_Object.Patients.Select(p => p.Name).ToArray();
                for (int i = 0; i < patients.Length; i++)
                {
                    if (i < patients.Length - 1) stringBuilder.AppendLine("  \u2022 " + patients[i]);
                    else stringBuilder.Append("  \u2022 " + patients[i]);
                }
                if (patients.Length == 0)
                {
                    m_PatientsText.GetComponent<ThemeElement>().Set(m_ErrorState);
                    stringBuilder.Append("  \u2022 None");
                }
                else
                {
                    m_PatientsText.GetComponent<ThemeElement>().Set();
                }
                m_PatientTooltip.Text = stringBuilder.ToString();
                m_PatientsText.text = patients.Length.ToString();
            }
        }
        #endregion
    }
}