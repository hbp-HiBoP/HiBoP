using UnityEngine;
using UnityEngine.UI;
using HBP.Data;
using System.Linq;
using Tools.Unity.Lists;
using NewTheme.Components;
using System.Collections.Generic;

namespace HBP.UI.Anatomy
{
	public class GroupItem : ActionnableItem<Group> 
	{
		#region Properties
		[SerializeField] Text m_NameText;

		[SerializeField] Text m_PatientsText;
        [SerializeField] LabelList m_PatientsList;

        [SerializeField] State Error;

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

                int numberOfPatients = value.Patients.Count;
                m_PatientsText.text = numberOfPatients.ToString();
                if (numberOfPatients == 0) m_PatientsText.GetComponent<ThemeElement>().Set(Error);
                else m_PatientsText.GetComponent<ThemeElement>().Set();
            }
        }
        #endregion

        #region Public Methods
        public void SetPatients()
        {
            m_PatientsList.Initialize();
            IEnumerable<string> labels = from patient in m_Object.Patients.ToArray() select patient.Name;
            if (labels.Count() == 0) labels = new string[] { "No Patient" };
            m_PatientsList.Objects = labels.ToArray();
        }
        #endregion
    }
}