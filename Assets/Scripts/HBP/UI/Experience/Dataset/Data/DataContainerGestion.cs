using Tools.Unity;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using d = HBP.Data.Experience.Dataset;

namespace HBP.UI.Experience.Dataset
{

    public class DataContainerGestion : MonoBehaviour
    {
        #region Properties
        [SerializeField] Dropdown m_ContainerTypeDropdown;
        [SerializeField] ElanDataContainerGestion m_ElanDataContainerGestion;
        [SerializeField] BrainVisionDataContainerGestion m_BrainVisionDataContainerGestion;
        [SerializeField] EdfDataContainerGestion m_EdfDataContainerGestion;
        [SerializeField] MicromedDataContainerGestion m_MicromedDataContainerGestion;

        d.ElanDataContainer m_ElanDataContainerTemp;
        d.EdfDataContainer m_EDFDataContainerTemp;
        d.MicromedDataContainer m_MicromedDataContainerTemp;
        d.BrainVisionDataContainer m_BrainVisionDataContainerTemp;
        public d.DataContainer DataContainer { get; private set; }

        public UnityEvent OnChangeDataType { get; } = new UnityEvent();

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
                m_ContainerTypeDropdown.interactable = value;

                m_ElanDataContainerGestion.interactable = value;
                m_EdfDataContainerGestion.interactable = value;
                m_BrainVisionDataContainerGestion.interactable = value;
                m_MicromedDataContainerGestion.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }
        public void Set(d.DataContainer dataContainer)
        {
            DataContainer = dataContainer;
            m_ElanDataContainerTemp = new d.ElanDataContainer("","","", dataContainer.ID);
            m_EDFDataContainerTemp = new d.EdfDataContainer("", dataContainer.ID);
            m_BrainVisionDataContainerTemp = new d.BrainVisionDataContainer("", dataContainer.ID);
            m_MicromedDataContainerTemp = new d.MicromedDataContainer("", dataContainer.ID);
            switch (DataContainer.Type)
            {
                case Tools.CSharp.EEG.File.FileType.ELAN:
                    m_ElanDataContainerTemp = DataContainer as d.ElanDataContainer;
                    break;
                case Tools.CSharp.EEG.File.FileType.EDF:
                    m_EDFDataContainerTemp = DataContainer as d.EdfDataContainer;
                    break;
                case Tools.CSharp.EEG.File.FileType.Micromed:
                    m_MicromedDataContainerTemp = DataContainer as d.MicromedDataContainer;
                    break;
                case Tools.CSharp.EEG.File.FileType.BrainVision:
                    m_BrainVisionDataContainerTemp = DataContainer as d.BrainVisionDataContainer;
                    break;
                default:
                    break;
            }

            // Type
            m_ContainerTypeDropdown.onValueChanged.RemoveAllListeners();
            m_ContainerTypeDropdown.onValueChanged.AddListener(ChangeDataInfoType);
            m_ContainerTypeDropdown.Set(typeof(Tools.CSharp.EEG.File.FileType), (int)dataContainer.Type);
        }
        #endregion

        #region Private Methods
        void ChangeDataInfoType(int i)
        {
            Tools.CSharp.EEG.File.FileType type = (Tools.CSharp.EEG.File.FileType)i;
            if (type == Tools.CSharp.EEG.File.FileType.ELAN)
            {
                m_BrainVisionDataContainerGestion.SetActive(false);
                m_EdfDataContainerGestion.SetActive(false);
                m_MicromedDataContainerGestion.SetActive(false);

                m_ElanDataContainerGestion.Set(m_ElanDataContainerTemp);
                m_ElanDataContainerGestion.SetActive(true);
                DataContainer = m_ElanDataContainerTemp;
            }
            else if (type == Tools.CSharp.EEG.File.FileType.BrainVision)
            {
                m_ElanDataContainerGestion.SetActive(false);
                m_EdfDataContainerGestion.SetActive(false);
                m_MicromedDataContainerGestion.SetActive(false);

                m_BrainVisionDataContainerGestion.Set(m_BrainVisionDataContainerTemp);
                m_BrainVisionDataContainerGestion.SetActive(true);
                DataContainer = m_BrainVisionDataContainerTemp;
            }
            else if (type == Tools.CSharp.EEG.File.FileType.EDF)
            {
                m_ElanDataContainerGestion.SetActive(false);
                m_BrainVisionDataContainerGestion.SetActive(false);
                m_MicromedDataContainerGestion.SetActive(false);

                m_EdfDataContainerGestion.Set(m_EDFDataContainerTemp);
                m_EdfDataContainerGestion.SetActive(true);
                DataContainer = m_EDFDataContainerTemp;
            }
            else if (type == Tools.CSharp.EEG.File.FileType.Micromed)
            {
                m_ElanDataContainerGestion.SetActive(false);
                m_BrainVisionDataContainerGestion.SetActive(false);
                m_EdfDataContainerGestion.SetActive(false);

                m_MicromedDataContainerGestion.Set(m_MicromedDataContainerTemp);
                m_MicromedDataContainerGestion.SetActive(true);
                DataContainer = m_MicromedDataContainerTemp;
            }
            OnChangeDataType.Invoke();
        }
        #endregion
    }
}