using UnityEngine.UI;
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
        [SerializeField] InputField m_NameInputField;
        [SerializeField] Dropdown m_PatientDropdown;
        [SerializeField] Dropdown m_NormalizationDropdown;
        [SerializeField] Dropdown m_TypeDropdown;
        [SerializeField] ElanDataInfoGestion m_ElanDataInfoGestion;
        [SerializeField] BrainVisionDataInfoGestion m_BrainVisionDataInfoGestion;
        [SerializeField] EdfDataInfoGestion m_EdfDataInfoGestion;
        [SerializeField] MicromedDataInfoGestion m_MicromedDataInfoGestion;
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
                m_NormalizationDropdown.interactable = value;
                m_TypeDropdown.interactable = value;
                m_ElanDataInfoGestion.interactable = value;
                m_EdfDataInfoGestion.interactable = value;
                m_BrainVisionDataInfoGestion.interactable = value;
                m_MicromedDataInfoGestion.interactable = value;
            }
        }
        #endregion

        #region Private Methods
        public override void Save()
        {
            OnCanSave.Invoke();
            if (CanSave)
            {
                if (m_TypeDropdown.value == (int)Tools.CSharp.EEG.File.FileType.ELAN)
                {
                    d.ElanDataInfo dataInfo = (d.ElanDataInfo)ItemTemp;
                    Item = new d.ElanDataInfo(dataInfo.Name, dataInfo.Patient, dataInfo.Normalization, dataInfo.EEG, dataInfo.POS, dataInfo.Notes, dataInfo.ID);
                }
                else if (m_TypeDropdown.value == (int)Tools.CSharp.EEG.File.FileType.BrainVision)
                {
                    d.BrainVisionDataInfo dataInfo = (d.BrainVisionDataInfo)ItemTemp;
                    Item = new d.BrainVisionDataInfo(dataInfo.Name, dataInfo.Patient, dataInfo.Normalization, dataInfo.Header, dataInfo.ID);
                }
                else if (m_TypeDropdown.value == (int)Tools.CSharp.EEG.File.FileType.EDF)
                {
                    d.EdfDataInfo dataInfo = (d.EdfDataInfo)ItemTemp;
                    Item = new d.EdfDataInfo(dataInfo.Name, dataInfo.Patient, dataInfo.Normalization, dataInfo.EDF, dataInfo.ID);
                }
                else if (m_TypeDropdown.value == (int)Tools.CSharp.EEG.File.FileType.Micromed)
                {
                    d.MicromedDataInfo dataInfo = (d.MicromedDataInfo)ItemTemp;
                    Item = new d.MicromedDataInfo(dataInfo.Name, dataInfo.Patient, dataInfo.Normalization, dataInfo.TRC, dataInfo.ID);
                }
                OnSave.Invoke();
                base.Close();
            }
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

            // Type.
            m_TypeDropdown.Set(typeof(Tools.CSharp.EEG.File.FileType), (int)objectToDisplay.Type);

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

            m_TypeDropdown.onValueChanged.RemoveAllListeners();
            m_TypeDropdown.onValueChanged.AddListener(ChangeDataInfoType);

            m_NormalizationDropdown.RefreshShownValue();
            m_NormalizationDropdown.onValueChanged.RemoveAllListeners();
            m_NormalizationDropdown.onValueChanged.AddListener((value) => ItemTemp.Normalization = (d.DataInfo.NormalizationType)value);

            base.Initialize();
        }
        void ChangeDataInfoType(int i)
        {
            Tools.CSharp.EEG.File.FileType type = (Tools.CSharp.EEG.File.FileType)i;
            if (type == Tools.CSharp.EEG.File.FileType.ELAN)
            {
                if (ItemTemp.Type != Tools.CSharp.EEG.File.FileType.ELAN)
                {
                    itemTemp = new d.ElanDataInfo(ItemTemp.Name, ItemTemp.Patient, ItemTemp.Normalization, string.Empty, string.Empty, string.Empty, ItemTemp.ID);
                }
                m_BrainVisionDataInfoGestion.SetActive(false);
                m_EdfDataInfoGestion.SetActive(false);
                m_MicromedDataInfoGestion.SetActive(false);
                m_ElanDataInfoGestion.Set(ItemTemp as d.ElanDataInfo);
                m_ElanDataInfoGestion.SetActive(true);
            }
            else if (type == Tools.CSharp.EEG.File.FileType.BrainVision)
            {
                if (ItemTemp.Type != Tools.CSharp.EEG.File.FileType.BrainVision)
                {
                    itemTemp = new d.BrainVisionDataInfo(ItemTemp.Name, ItemTemp.Patient, ItemTemp.Normalization, string.Empty, ItemTemp.ID);
                }
                m_ElanDataInfoGestion.SetActive(false);
                m_EdfDataInfoGestion.SetActive(false);
                m_MicromedDataInfoGestion.SetActive(false);
                m_BrainVisionDataInfoGestion.Set(ItemTemp as d.BrainVisionDataInfo);
                m_BrainVisionDataInfoGestion.SetActive(true);
            }
            else if (type == Tools.CSharp.EEG.File.FileType.EDF)
            {
                if (ItemTemp.Type != Tools.CSharp.EEG.File.FileType.EDF)
                {
                    itemTemp = new d.EdfDataInfo(ItemTemp.Name, ItemTemp.Patient, ItemTemp.Normalization, string.Empty, ItemTemp.ID);
                }
                m_ElanDataInfoGestion.SetActive(false);
                m_BrainVisionDataInfoGestion.SetActive(false);
                m_MicromedDataInfoGestion.SetActive(false);
                m_EdfDataInfoGestion.Set(ItemTemp as d.EdfDataInfo);
                m_EdfDataInfoGestion.SetActive(true);
            }
            else if (type == Tools.CSharp.EEG.File.FileType.Micromed)
            {
                if (ItemTemp.Type != Tools.CSharp.EEG.File.FileType.Micromed)
                {
                    itemTemp = new d.MicromedDataInfo(ItemTemp.Name, ItemTemp.Patient, ItemTemp.Normalization, string.Empty, ItemTemp.ID);
                }
                m_ElanDataInfoGestion.SetActive(false);
                m_BrainVisionDataInfoGestion.SetActive(false);
                m_EdfDataInfoGestion.SetActive(false);
                m_MicromedDataInfoGestion.Set(ItemTemp as d.MicromedDataInfo);
                m_MicromedDataInfoGestion.SetActive(true);
            }
        }
        #endregion
    }
}