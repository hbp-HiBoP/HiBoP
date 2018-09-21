using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Visualization;
using HBP.Data.Experience.Protocol;
using HBP.Data.Experience.Dataset;
using System.Linq;

namespace HBP.UI.Visualization
{
    public class IEEGColumnModifier : MonoBehaviour
    {
        #region Properties
        [SerializeField] Dropdown m_ProtocolDropdown, m_BlocDropdown, m_DatasetDropdown, m_DataNameDropdown;

        IEEGColumn m_Column;
        List<Data.Patient> m_Patients;
        List<Protocol> m_Protocols;
        Protocol m_SelectedProtocol;
        List<Dataset> m_Datasets;
        List<string> m_DataName;

        bool m_Interactable;
        public bool Interactable
        {
            get { return m_Interactable; }
            set
            {
                m_Interactable = value;
                if(m_Interactable == false) SetProtocolDropdownInteractable(false);
                else SetProtocolDropdown();
            }
        }
        #endregion

        #region Public Methods
        public void Set(IEEGColumn column, IEnumerable<Data.Patient> patient)
        {
            m_Column = column;
            m_Patients = patient.ToList();
            SetProtocolDropdown();
        }
        #endregion

        #region Private Methods
        // General
        void Awake()
        {
            m_ProtocolDropdown.onValueChanged.RemoveAllListeners();
            m_ProtocolDropdown.onValueChanged.AddListener(OnChangeProtocol);

            m_BlocDropdown.onValueChanged.RemoveAllListeners();
            m_BlocDropdown.onValueChanged.AddListener(OnChangeBloc);

            m_DatasetDropdown.onValueChanged.RemoveAllListeners();
            m_DatasetDropdown.onValueChanged.AddListener(OnChangeDataset);

            m_DataNameDropdown.onValueChanged.RemoveAllListeners();
            m_DataNameDropdown.onValueChanged.AddListener(OnChangeDataName);
        }

        // Protocol.
        void SetProtocolDropdown()
        {
            m_Protocols = ApplicationState.ProjectLoaded.Protocols.ToList();
            SetProtocolDropdownInteractable(m_Protocols.Count > 0 && m_Patients.Count > 0);
        }
        void OnChangeProtocol(int value)
        {
            m_SelectedProtocol = m_Protocols[value];
            SetBlocDropdown();
            SetDatasetDropdown();
        }
        void SetProtocolDropdownInteractable(bool interactable)
        {
            m_ProtocolDropdown.interactable = interactable;
            if (interactable)
            {
                m_ProtocolDropdown.options = (from protocol in m_Protocols select new Dropdown.OptionData(protocol.Name, null)).ToList();
                if (m_Column.Dataset != null) m_ProtocolDropdown.value = m_Protocols.IndexOf(m_Column.Dataset.Protocol);
                else m_ProtocolDropdown.value = 0;
            }
            else
            {
                m_ProtocolDropdown.options = new List<Dropdown.OptionData>() { new Dropdown.OptionData("", null) };
                m_ProtocolDropdown.value = 0;
                SetBlocDropdownInteractable(false);
                SetDatasetDropdownInteractable(false);
            }
            m_ProtocolDropdown.RefreshShownValue();
        }

        // Bloc.
        void SetBlocDropdown()
        {
            SetBlocDropdownInteractable(m_SelectedProtocol != null && m_SelectedProtocol.Blocs.Count > 0 && m_ProtocolDropdown.interactable);
        }
        void OnChangeBloc(int value)
        {
            m_Column.Bloc = m_SelectedProtocol.Blocs[value];
        }
        void SetBlocDropdownInteractable(bool interactable)
        {
            m_BlocDropdown.interactable = interactable;
            if (interactable)
            {
                m_BlocDropdown.options = (from bloc in m_SelectedProtocol.Blocs select new Dropdown.OptionData(bloc.Name, null)).ToList();
                m_BlocDropdown.value = m_SelectedProtocol.Blocs.IndexOf(m_Column.Bloc);
            }
            else
            {
                m_BlocDropdown.options = new List<Dropdown.OptionData>() { new Dropdown.OptionData("", null) };
                m_BlocDropdown.value = 0;
            }
            m_BlocDropdown.RefreshShownValue();
        }

        // Dataset.
        void OnChangeDataset(int value)
        {
            if (m_Datasets.Count > value) m_Column.Dataset = m_Datasets[value];
            SetDataNameDropdown();
        }
        void SetDatasetDropdown()
        {
            m_Datasets = ApplicationState.ProjectLoaded.Datasets.Where((d) => d.Protocol == m_SelectedProtocol).ToList();
            SetDatasetDropdownInteractable(m_Datasets.Count > 0 && m_ProtocolDropdown.interactable && m_Patients.Count > 0);
          
        }
        void SetDatasetDropdownInteractable(bool interactable)
        {
            m_DatasetDropdown.interactable = interactable;
            if (interactable)
            {
                m_DatasetDropdown.options = (from dataset in m_Datasets select new Dropdown.OptionData(dataset.Name, null)).ToList();
                if (m_Column.Dataset != null)
                {
                    m_DatasetDropdown.value = m_Datasets.IndexOf(m_Column.Dataset);
                }
                else
                {
                    m_DatasetDropdown.value = m_Datasets.FindIndex(d => d.Protocol == m_SelectedProtocol);
                }
            }
            else
            {
                m_DatasetDropdown.options = new List<Dropdown.OptionData>() { new Dropdown.OptionData("", null) };
                m_DatasetDropdown.value = 0;
                SetDataNameDropdownInteractable(false);
            }
            m_DatasetDropdown.RefreshShownValue();
        }

        // Data.
        void OnChangeDataName(int value)
        {
            if(m_DataName.Count > value)
            {
                m_Column.DataName = m_DataName[value];
            }
        }
        void SetDataNameDropdown()
        {
            if(m_Column.Dataset != null)
            {
                m_DataName = (from data in m_Column.Dataset.Data select data.Name).Distinct().Where((l) => m_Patients.All((p) => m_Column.Dataset.Data.Any((d) => d.isOk && d.Name == l && d.Patient == p))).ToList();
            }
            else
            {
                m_DataName = new List<string>();
            }
            SetDataNameDropdownInteractable(m_DataName.Count > 0 && m_DatasetDropdown.interactable);
            m_DataNameDropdown.value = Mathf.Clamp(m_DataName.IndexOf(m_Column.DataName), 0, m_DataNameDropdown.options.Count - 1);
        }
        void SetDataNameDropdownInteractable(bool interactable)
        {
            m_DataNameDropdown.interactable = interactable;
            if (interactable) m_DataNameDropdown.options = (from label in m_DataName select new Dropdown.OptionData(label, null)).ToList();
            else m_DataNameDropdown.options = new List<Dropdown.OptionData>() { new Dropdown.OptionData("", null) };
        }
        #endregion
    }
}