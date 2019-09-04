using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using d = HBP.Data.Experience.Dataset;
using System.Collections.ObjectModel;

namespace HBP.UI.Experience.Dataset
{
    public class PatientDataInfoSubModifier : SubModifier<d.PatientDataInfo>
    {
        #region Properties     
        ReadOnlyCollection<Data.Patient> m_Patients;
        [SerializeField] Dropdown m_PatientDropdown;

        public override bool Interactable
        {
            get
            {
                return base.m_Interactable;
            }
            set
            {
                base.m_Interactable = value;
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
        protected override void SetFields(d.PatientDataInfo objectToDisplay)
        {
            m_PatientDropdown.value = m_Patients.IndexOf(objectToDisplay.Patient);
        }
        #endregion
    }
}