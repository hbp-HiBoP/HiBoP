﻿using UnityEngine.UI;
using d = HBP.Data.Experience.Dataset;
using Tools.Unity;
using UnityEngine;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Events;

namespace HBP.UI.Experience.Dataset
{
    public class DataInfoModifier : ItemModifier<d.DataInfo>
    {
        #region Properties
        public UnityEvent OnCanSave { get; set; }
        public bool CanSave { get; set; }
        public new d.DataInfo ItemTemp { get { return itemTemp; } }
        [SerializeField] InputField m_NameInputField, m_MeasureInputField;
        [SerializeField] Dropdown m_PatientDropdown;
        [SerializeField] Dropdown m_NormalizationDropdown;
        [SerializeField] FileSelector m_EEGFileSelector, m_POSFileSelector;
        List<Data.Patient> m_Patients;

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }

            set
            {
                base.Interactable = value;
                m_NameInputField.interactable = value;
                m_PatientDropdown.interactable = value;
                m_MeasureInputField.interactable = value;
                m_EEGFileSelector.interactable = value;
                m_POSFileSelector.interactable = value;
                m_NormalizationDropdown.interactable = value;
            }
        }
        #endregion

        #region Private Methods
        public override void Save()
        {
            OnCanSave.Invoke();
            if (CanSave) base.Save();
            else ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Warning, "Data already exists", "A data for this patient with the same name already exists.");
        }
        protected override void SetFields(d.DataInfo objectToDisplay)
        {
            // Name.
            m_NameInputField.text = objectToDisplay.Name;

            // Patient.
            m_Patients = ApplicationState.ProjectLoaded.Patients.ToList();
            m_PatientDropdown.options = (from patient in m_Patients select new Dropdown.OptionData(patient.CompleteName, null)).ToList();
            m_PatientDropdown.value = m_Patients.IndexOf(objectToDisplay.Patient);


            // EEG.
            m_MeasureInputField.text = objectToDisplay.Measure;
            m_EEGFileSelector.DefaultDirectory = ApplicationState.ProjectLoaded.Settings.LocalizerDatabase;
            m_EEGFileSelector.File = objectToDisplay.SavedEEG;

            // POS.
            SetPosFile();
            m_POSFileSelector.File = objectToDisplay.SavedPOS;

            // Normalization.
            m_NormalizationDropdown.options = (from name in System.Enum.GetNames(typeof(d.DataInfo.NormalizationType)) select new Dropdown.OptionData(name, null)).ToList();
            m_NormalizationDropdown.value = (int) objectToDisplay.Normalization;

        }
        protected override void Initialize()
        {
            OnCanSave = new UnityEvent();

            m_NameInputField.onValueChanged.RemoveAllListeners();
            m_NameInputField.onValueChanged.AddListener((name) => ItemTemp.Name = name);

            m_PatientDropdown.onValueChanged.RemoveAllListeners();
            m_PatientDropdown.onValueChanged.AddListener((i) => ItemTemp.Patient = m_Patients[i]);

            m_MeasureInputField.onValueChanged.RemoveAllListeners();
            m_MeasureInputField.onValueChanged.AddListener((measure) => ItemTemp.Measure = measure);

            m_EEGFileSelector.onValueChanged.RemoveAllListeners();
            m_EEGFileSelector.onValueChanged.AddListener((eeg) => ItemTemp.EEG = eeg);
            m_EEGFileSelector.onValueChanged.AddListener((eeg) => SetPosFile());

            m_POSFileSelector.onValueChanged.RemoveAllListeners();
            m_POSFileSelector.onValueChanged.AddListener((pos) => ItemTemp.POS = pos);

            m_NormalizationDropdown.RefreshShownValue();
            m_NormalizationDropdown.onValueChanged.RemoveAllListeners();
            m_NormalizationDropdown.onValueChanged.AddListener((value) => ItemTemp.Normalization = (d.DataInfo.NormalizationType)value);

            base.Initialize();
        }
        void SetPosFile()
        {
            if(!itemTemp.Errors.Contains(d.DataInfo.ErrorType.EEGEmpty) && !itemTemp.Errors.Contains(d.DataInfo.ErrorType.EEGFileNotExist) && !itemTemp.Errors.Contains(d.DataInfo.ErrorType.EEGFileNotAGoodFile))
            {
                m_POSFileSelector.DefaultDirectory = ItemTemp.EEG;
            }
            else m_POSFileSelector.DefaultDirectory = ApplicationState.ProjectLoaded.Settings.LocalizerDatabase;
        }
        #endregion
    }
}