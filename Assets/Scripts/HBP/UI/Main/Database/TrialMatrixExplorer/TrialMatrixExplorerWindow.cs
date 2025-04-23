using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.Data.Database;
using HBP.UI.Tools;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Database
{
    public class TrialMatrixExplorerWindow : DialogWindow
    {
        #region Properties
        [SerializeField] private Dropdown m_PatientDropdown;
        [SerializeField] private Dropdown m_DataDropdown;
        [SerializeField] private Button m_DisplayMatrixButton;
        [SerializeField] private TrialMatrixDisplayer m_TrialMatrixDisplayer;

        List<Patient> m_Patients;
        Patient m_SelectedPatient;

        List<string> m_DataNames;
        string m_SelectedDataName;
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            base.Initialize();

            m_PatientDropdown.onValueChanged.AddListener((i) => OnChangePatient(m_Patients[i]));
            m_DataDropdown.onValueChanged.AddListener((i) => OnChangeDataName(m_DataNames[i]));
            m_DisplayMatrixButton.onClick.AddListener(DisplayTrialMatrices);
        }
        protected override void SetFields()
        {
            base.SetFields();
            SetPatients();
            SetDataNames();
        }
        protected void SetPatients()
        {
            m_Patients = DatabaseManager.Database.Patients.Where(p => DatabaseManager.Database.DataInfos.OfType<IEEGDataInfo>().Any(d => d.Patient == p)).OrderBy(p => p.Place).ThenBy(p => p.Date).ThenBy(p => p.Name).ToList();
            m_PatientDropdown.options = (from patient in m_Patients select new Dropdown.OptionData(patient.CompleteName, null)).ToList();
            if (m_SelectedPatient == null || !m_Patients.Contains(m_SelectedPatient))
            {
                m_SelectedPatient = m_Patients.FirstOrDefault();
            }
            int index = m_Patients.IndexOf(m_SelectedPatient);
            m_PatientDropdown.SetValue(index != -1 ? index : 0);
        }
        protected void SetDataNames()
        {
            m_DataNames = DatabaseManager.Database.DataInfos.OfType<IEEGDataInfo>().Where(d => d.Patient == m_SelectedPatient).Select(d => d.Name).Distinct().ToList();
            m_DataDropdown.options = (from name in m_DataNames select new Dropdown.OptionData(name, null)).ToList();
            if (string.IsNullOrEmpty(m_SelectedDataName) || !m_DataNames.Contains(m_SelectedDataName))
            {
                m_SelectedDataName = m_DataNames.FirstOrDefault();
            }
            int index = m_DataNames.IndexOf(m_SelectedDataName);
            m_DataDropdown.SetValue(index != -1 ? index : 0);
        }
        protected void OnChangePatient(Patient patient)
        {
            m_SelectedPatient = patient;
            SetDataNames();
        }
        protected void OnChangeDataName(string name)
        {
            m_SelectedDataName = name;
        }
        protected void DisplayTrialMatrices()
        {
            if (m_SelectedPatient != null && !string.IsNullOrEmpty(m_SelectedDataName))
            {
                m_TrialMatrixDisplayer.Set(m_SelectedPatient, m_SelectedDataName);
            }
        }
        #endregion
    }
}