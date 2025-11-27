using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.ObjectModel;
using HBP.Core.Data;
using HBP.UI.Tools;
using HBP.Core.Tools;
using HBP.Data.Database;

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
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(PatientDataInfo objectToDisplay)
        {
            // FIXME: this is ugly
            if (ApplicationState.LoadedProject != null && ApplicationState.LoadedProject.Datasets.Any(ds => ds.Data.Contains(objectToDisplay)))
                m_Patients = ApplicationState.LoadedProject.Patients;
            else if (DatabaseManager.Database.DataInfos.Contains(objectToDisplay))
                m_Patients = DatabaseManager.Database.Patients;
            else if (ApplicationState.LoadedProject != null && ApplicationState.LoadedProject.Patients.Count > 0)
                m_Patients = ApplicationState.LoadedProject.Patients;
            else
                throw new System.Exception("No patients available in the project or database.");
            m_PatientDropdown.options = (from patient in m_Patients select new Dropdown.OptionData(patient.Name, null)).ToList();
            m_PatientDropdown.value = m_Patients.IndexOf(objectToDisplay.Patient);
        }
        #endregion
    }
}