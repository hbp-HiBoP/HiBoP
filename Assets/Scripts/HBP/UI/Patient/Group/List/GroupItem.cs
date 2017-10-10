using UnityEngine;
using UnityEngine.UI;
using HBP.Data;
using System.Linq;
using Tools.Unity.Lists;

namespace HBP.UI.Anatomy
{
	public class GroupItem : ActionnableItem<Group> 
	{
		#region Properties
		[SerializeField] Text m_NameText;

		[SerializeField] Text m_PatientsText;
        [SerializeField] Button m_PatientsButton;
        [SerializeField] LabelList m_PatientsList;

        public override Group Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;

                // Name.
                m_NameText.text = value.Name;

                // Patients.
                int nbPatients = value.Patients.Count;
                m_PatientsText.text = nbPatients.ToString();
                if (nbPatients == 0)
                {
                    m_PatientsText.color = ApplicationState.GeneralSettings.Theme.General.Error;
                    m_PatientsButton.interactable = false;
                }
                else
                {
                    m_PatientsText.color = ApplicationState.GeneralSettings.Theme.Window.Content.Text.Color;
                    m_PatientsButton.interactable = true;
                }
            }
        }
        #endregion

        #region Public Methods
        public void SetPatients()
        {
            m_PatientsList.Objects = (from patient in m_Object.Patients.ToArray() select patient.Name).ToArray();
        }
        #endregion
    }
}