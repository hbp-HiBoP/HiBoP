using UnityEngine.UI;
using d = HBP.Data.Experience.Dataset;
using Tools.Unity;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

namespace HBP.UI.Experience.Dataset
{
    public class DataInfoModifier : ItemModifier<d.DataInfo>
    {
        #region Properties
        InputField m_NameInputField, m_MeasureInputField;
        Dropdown m_PatientDropdown;
        FileSelector m_EEGFileSelector, m_POSFileSelector;
        List<Data.Patient> m_Patients;
        #endregion

        #region Private Methods
        protected override void SetFields(d.DataInfo objectToDisplay)
        {
            // Name.
            m_NameInputField.text = objectToDisplay.Name;
            m_NameInputField.onValueChanged.RemoveAllListeners();
            m_NameInputField.onValueChanged.AddListener((name) => objectToDisplay.Name = name);

            // Patient.
            m_Patients = ApplicationState.ProjectLoaded.Patients.ToList();
            m_PatientDropdown.options = (from patient in m_Patients select new Dropdown.OptionData(patient.Name, null)).ToList();
            m_PatientDropdown.value = m_Patients.IndexOf(objectToDisplay.Patient);
            m_PatientDropdown.onValueChanged.RemoveAllListeners();
            m_PatientDropdown.onValueChanged.AddListener((i) => objectToDisplay.Patient = m_Patients[i]);

            // EEG.
            m_MeasureInputField.text = objectToDisplay.Measure;
            m_MeasureInputField.onValueChanged.RemoveAllListeners();
            m_MeasureInputField.onValueChanged.AddListener((measure) => objectToDisplay.Measure = measure);
            m_EEGFileSelector.DefaultDirectory = ApplicationState.ProjectLoaded.Settings.LocalizerDatabase;
            m_EEGFileSelector.File = objectToDisplay.EEG;
            m_EEGFileSelector.onValueChanged.RemoveAllListeners();
            m_EEGFileSelector.onValueChanged.AddListener((eeg) => objectToDisplay.EEG = eeg);
            m_EEGFileSelector.onValueChanged.AddListener((eeg) => SetPosFile());

            // POS.
            SetPosFile();
            m_POSFileSelector.File = objectToDisplay.POS;
            m_POSFileSelector.onValueChanged.RemoveAllListeners();
            m_POSFileSelector.onValueChanged.AddListener((pos) => objectToDisplay.POS = pos);
        }
        protected override void SetInteractableFields(bool interactable)
        {
            m_NameInputField.interactable = interactable;
            m_PatientDropdown.interactable = interactable;
            m_MeasureInputField.interactable = interactable;
            m_EEGFileSelector.interactable = interactable;
            m_POSFileSelector.interactable = interactable;
        }
        protected override void SetWindow()
        {
            Transform general = transform.Find("Content").Find("General");
            m_NameInputField = general.Find("Name").GetComponentInChildren<InputField>();
            m_PatientDropdown = general.Find("Patient").GetComponentInChildren<Dropdown>();

            Transform data = transform.Find("Content").Find("Data");
            m_EEGFileSelector = data.Find("EEG").Find("FileSelector").GetComponentInChildren<FileSelector>();
            m_MeasureInputField = data.Find("EEG").Find("Measure").GetComponentInChildren<InputField>();
            m_POSFileSelector = data.Find("POS").GetComponentInChildren<FileSelector>();
        }
        void SetPosFile()
        {
            if(!ItemTemp.Errors.Contains(d.DataInfo.ErrorType.EEGEmpty) && !ItemTemp.Errors.Contains(d.DataInfo.ErrorType.EEGFileNotExist) && !ItemTemp.Errors.Contains(d.DataInfo.ErrorType.EEGFileNotAGoodFile))
            {
                m_POSFileSelector.DefaultDirectory = ItemTemp.EEG;
            }
            else m_POSFileSelector.DefaultDirectory = ApplicationState.ProjectLoaded.Settings.LocalizerDatabase;
        }
        #endregion
    }
}