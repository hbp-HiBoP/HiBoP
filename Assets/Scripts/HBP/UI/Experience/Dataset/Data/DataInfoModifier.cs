using UnityEngine.UI;
using d = HBP.Data.Experience.Dataset;
using Tools.Unity;
using UnityEngine;
using System.Linq;
using UnityEngine.Events;

namespace HBP.UI.Experience.Dataset
{
    public class DataInfoModifier : ItemModifier<d.DataInfo>
    {
        #region Properties
        enum DataType
        {
            iEEG
        }
        public UnityEvent OnCanSave { get; set; }
        public bool CanSave { get; set; }

        d.iEEGDataInfo m_IEEGDataInfoTemp;
        public new d.DataInfo ItemTemp { get { return itemTemp; } }

        [SerializeField] InputField m_NameInputField;
        [SerializeField] PatientDataInfoGestion m_PatientDataInfoGestion;
        [SerializeField] Dropdown m_TypeDropdown;
        [SerializeField] DataContainerGestion m_DataContainerGestion;
        [SerializeField] iEEGDataInfoGestion m_iEEGDataInfoGestion;

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
                m_TypeDropdown.interactable = value;
                m_PatientDataInfoGestion.Interactable = value;
                m_iEEGDataInfoGestion.Interactable = value;
                m_DataContainerGestion.Interactable = value;
            }
        }
        #endregion

        #region Private Methods

        public override void Save()
        {
            OnCanSave.Invoke();
            if (CanSave)
            {
                Item = ItemTemp;
                OnSave.Invoke();
                base.Close();
            }
            else ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Warning, "Data already exists", "A data for this patient with the same name already exists.");
        }
        protected override void SetFields(d.DataInfo objectToDisplay)
        {
            if (objectToDisplay is d.iEEGDataInfo iEEGDataInfo)
            {
                m_IEEGDataInfoTemp = iEEGDataInfo;
            }
            else if (objectToDisplay is d.PatientDataInfo patientDataInfo)
            {
                m_IEEGDataInfoTemp = new d.iEEGDataInfo(patientDataInfo.Name, new d.ElanDataContainer(), patientDataInfo.Patient, d.iEEGDataInfo.NormalizationType.Auto, patientDataInfo.ID);
            }
            else
            {
                m_IEEGDataInfoTemp = new d.iEEGDataInfo(objectToDisplay.Name, new d.ElanDataContainer(), ApplicationState.ProjectLoaded.Patients.FirstOrDefault(), d.iEEGDataInfo.NormalizationType.Auto, objectToDisplay.ID);
            }

            // Name.
            m_NameInputField.text = objectToDisplay.Name;

            // Type.
            int value;
            if(objectToDisplay is d.iEEGDataInfo)
            {
                value = (int) DataType.iEEG;
            }
            else
            {
                value = (int)DataType.iEEG;
            }
            m_TypeDropdown.Set(typeof(DataType), value);
        }
        protected override void Initialize()
        {
            OnCanSave = new UnityEvent();

            m_NameInputField.onValueChanged.RemoveAllListeners();
            m_NameInputField.onValueChanged.AddListener((name) => ItemTemp.Name = name);

            m_TypeDropdown.onValueChanged.RemoveAllListeners();
            m_TypeDropdown.onValueChanged.AddListener(ChangeDataInfoType);

            m_DataContainerGestion.OnChangeDataType.RemoveAllListeners();
            m_DataContainerGestion.OnChangeDataType.AddListener(OnChangeDataContainerType);
            base.Initialize();
        }
        void ChangeDataInfoType(int i)
        {
            DataType type = (DataType)i;
            switch (type)
            {
                case DataType.iEEG:
                    m_PatientDataInfoGestion.Set(m_IEEGDataInfoTemp);
                    m_PatientDataInfoGestion.SetActive(true);
                    m_iEEGDataInfoGestion.Set(m_IEEGDataInfoTemp);
                    m_iEEGDataInfoGestion.SetActive(true);
                    m_DataContainerGestion.Set(m_IEEGDataInfoTemp.DataContainer);

                    itemTemp = m_IEEGDataInfoTemp;
                    break;
                default:
                    break;
            }
        }
        void OnChangeDataContainerType()
        {
            ItemTemp.DataContainer = m_DataContainerGestion.DataContainer;
        }
        #endregion
    }
}