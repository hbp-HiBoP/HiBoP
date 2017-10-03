using UnityEngine.UI;
using d = HBP.Data.Experience.Dataset;
using Tools.Unity;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

namespace HBP.UI.Experience.Dataset
{
    public class DataInfoModifier : ItemModifier<d.DataInfo>
    {
        #region Properties
        InputField m_NameInputField, m_MeasureInputField;
        Dropdown m_PatientDropdown, m_ProtocolDropdown;
        FileSelector m_EEGFileSelector, m_POSFileSelector;
        List<Data.Patient> m_Patients;
        List<Data.Experience.Protocol.Protocol> m_Protocols;
        #endregion

        #region Private Methods
        protected override void SetFields(d.DataInfo objectToDisplay)
        {
            // Name.
            m_NameInputField.text = objectToDisplay.Name;

            // Patient.
            m_Patients = ApplicationState.ProjectLoaded.Patients.ToList();
            m_PatientDropdown.options = (from patient in m_Patients select new Dropdown.OptionData(patient.Name, null)).ToList();
            m_PatientDropdown.value = m_Patients.IndexOf(objectToDisplay.Patient);
            m_PatientDropdown.onValueChanged.RemoveAllListeners();
            m_PatientDropdown.onValueChanged.AddListener((i) => objectToDisplay.Patient = m_Patients[i]);

            // Protocol.
            m_Protocols = ApplicationState.ProjectLoaded.Protocols.ToList();
            m_ProtocolDropdown.options = (from protocol in m_Protocols select new Dropdown.OptionData(protocol.Name, null)).ToList();
            m_ProtocolDropdown.value = m_Protocols.IndexOf(objectToDisplay.Protocol);
            m_ProtocolDropdown.onValueChanged.RemoveAllListeners();
            m_ProtocolDropdown.onValueChanged.AddListener((i) => objectToDisplay.Patient = m_Patients[i]);

            // EEG.
            m_MeasureInputField.text = objectToDisplay.Measure;
            m_MeasureInputField.onValueChanged.RemoveAllListeners();
            m_MeasureInputField.onValueChanged.AddListener((measure) => objectToDisplay.Measure = measure);
            m_EEGFileSelector.File = objectToDisplay.EEG;
            m_EEGFileSelector.onValueChanged.RemoveAllListeners();
            m_EEGFileSelector.onValueChanged.AddListener((eeg) => objectToDisplay.EEG = eeg);

            // POS.
            m_POSFileSelector.File = objectToDisplay.POS;
            m_POSFileSelector.onValueChanged.RemoveAllListeners();
            m_POSFileSelector.onValueChanged.AddListener((pos) => objectToDisplay.POS = pos);
        }
        protected override void SetInteractableFields(bool interactable)
        {
            m_NameInputField.interactable = interactable;
            m_PatientDropdown.interactable = interactable;
            m_ProtocolDropdown.interactable = interactable;
            m_MeasureInputField.interactable = interactable;
            m_EEGFileSelector.interactable = interactable;
            m_POSFileSelector.interactable = interactable;
        }
        protected override void SetWindow()
        {
            Transform general = transform.Find("Content").Find("General");
            m_NameInputField = general.Find("Name").GetComponentInChildren<InputField>();
            m_PatientDropdown = general.Find("Patient").GetComponentInChildren<Dropdown>();
            m_ProtocolDropdown = general.Find("Protocol").GetComponentInChildren<Dropdown>();

            Transform data = transform.Find("Content").Find("Data");
            m_EEGFileSelector = data.Find("EEG").Find("FileSelector").GetComponentInChildren<FileSelector>();
            m_MeasureInputField = data.Find("EEG").Find("Measure").GetComponentInChildren<InputField>();
            m_POSFileSelector = data.Find("POS").GetComponentInChildren<FileSelector>();
        }
        #endregion
    }
}