using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.ObjectModel;
using HBP.Core.Data;
using HBP.UI.Tools;
using HBP.Core.Tools;

namespace HBP.UI.Main
{
    public class PatientDataInfoSubModifier : SubModifier<PatientDataInfo>
    {
        #region Properties     
        ReadOnlyCollection<Patient> m_Patients;
        [SerializeField] Dropdown m_PatientDropdown;

        public override bool Interactable
        {
            get
            {
                return m_Interactable;
            }
            set
            {
                m_Interactable = value;
                m_PatientDropdown.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();
            m_PatientDropdown.onValueChanged.AddListener((i) => Object.Patient = m_Patients[i]);
            m_Patients = ApplicationState.ProjectLoaded.Patients;
            m_PatientDropdown.options = (from patient in m_Patients select new Dropdown.OptionData(patient.CompleteName, null)).ToList();
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(PatientDataInfo objectToDisplay)
        {
            m_PatientDropdown.value = m_Patients.IndexOf(objectToDisplay.Patient);
        }
        #endregion
    }
}