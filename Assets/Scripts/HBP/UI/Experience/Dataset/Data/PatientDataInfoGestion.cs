using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using d = HBP.Data.Experience.Dataset;
using System.Collections.ObjectModel;

namespace HBP.UI.Experience.Dataset
{
    public class PatientDataInfoGestion : MonoBehaviour
    {
        #region Properties     
        ReadOnlyCollection<Data.Patient> m_Patients;
        [SerializeField] Dropdown m_PatientDropdown;

        protected bool m_Interactable;
        public virtual bool Interactable
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
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }
        public void Set(d.PatientDataInfo patientDataInfo)
        {
            m_Patients = ApplicationState.ProjectLoaded.Patients;
            m_PatientDropdown.options = (from patient in m_Patients select new Dropdown.OptionData(patient.CompleteName, null)).ToList();
            m_PatientDropdown.value = m_Patients.IndexOf(patientDataInfo.Patient);
            m_PatientDropdown.onValueChanged.RemoveAllListeners();
            m_PatientDropdown.onValueChanged.AddListener((i) => patientDataInfo.Patient = m_Patients[i]);
        }
        #endregion
    }
}