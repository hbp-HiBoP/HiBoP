using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Visualization;
using HBP.Data.Experience.Protocol;
using HBP.Data.Experience.Dataset;
using System.Linq;
using Tools.Unity;

namespace HBP.UI
{
    public class IEEGColumnModifier : SubModifier<IEEGColumn>
    {
        #region Properties
        [SerializeField] Dropdown m_ProtocolDropdown, m_BlocDropdown, m_DatasetDropdown, m_DataNameDropdown;

        List<Protocol> m_Protocols;
        Protocol m_SelectedProtocol;
        List<Dataset> m_Datasets;
        List<string> m_DataNames;

        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;
                if (!m_Interactable || !m_Initialized || Object == null) SetProtocolDropdownInteractable(false);
                else SetProtocolDropdown();
            }
        }

        public override IEEGColumn Object
        {
            get
            {
                return base.Object;
            }
            set
            {
                base.Object = value;
                if(value != null && m_Patients != null)
                {
                    SetProtocolDropdown();
                }
                else
                {
                    SetProtocolDropdownInteractable(false);
                }
            }
        }

        Data.Patient[] m_Patients;
        public Data.Patient[] Patients
        {
            get
            {
                return m_Patients;
            }
            set
            {
                m_Patients = value;
                if (Object != null && m_Patients != null)
                {
                    SetProtocolDropdown();
                }
                else
                {
                    SetProtocolDropdownInteractable(false);
                }
            }
        }
        #endregion

        #region Private Methods
        public override void Initialize()
        {
            m_ProtocolDropdown.onValueChanged.AddListener(OnChangeProtocol);
            m_BlocDropdown.onValueChanged.AddListener(OnChangeBloc);
            m_DatasetDropdown.onValueChanged.AddListener(OnChangeDataset);
            m_DataNameDropdown.onValueChanged.AddListener(OnChangeDataName);

            base.Initialize();
        }

        // Protocol.
        void SetProtocolDropdown()
        {
            m_Protocols = ApplicationState.ProjectLoaded.Protocols.Where(p => p.IsVisualizable).ToList();
            SetProtocolDropdownInteractable(m_Protocols != null && m_Patients != null && m_Protocols.Count > 0 && m_Patients.Length > 0);
        }
        void OnChangeProtocol(int value)
        {
            if (m_Protocols != null && m_Protocols.Count > value)
            {
                m_SelectedProtocol = m_Protocols[value];
            }
            SetBlocDropdown();
            SetDatasetDropdown();
        }
        void SetProtocolDropdownInteractable(bool interactable)
        {
            m_ProtocolDropdown.interactable = interactable;
            if (interactable)
            {
                m_ProtocolDropdown.options = m_Protocols.Select(p => new Dropdown.OptionData(p.Name, null)).ToList();
                if (Object.Dataset != null) m_ProtocolDropdown.SetValue(m_Protocols.IndexOf(Object.Dataset.Protocol));
                else m_ProtocolDropdown.SetValue(0);
            }
            else
            {
                m_ProtocolDropdown.ClearOptions();
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
            if(m_SelectedProtocol != null && m_SelectedProtocol.Blocs.Count > value)
            {
                Object.Bloc = m_SelectedProtocol.Blocs[value];
            }
        }
        void SetBlocDropdownInteractable(bool interactable)
        {
            m_BlocDropdown.interactable = interactable;
            if (interactable)
            {
                m_BlocDropdown.options = m_SelectedProtocol.Blocs.Select(b => new Dropdown.OptionData(b.Name, null)).ToList();
                if (Object.Bloc != null) m_BlocDropdown.SetValue(m_SelectedProtocol.Blocs.IndexOf(Object.Bloc));
                else m_BlocDropdown.SetValue(0);
            }
            else
            {
                m_BlocDropdown.ClearOptions();
            }
            m_BlocDropdown.RefreshShownValue();
        }

        // Dataset.
        void OnChangeDataset(int value)
        {
            if (m_Datasets != null && m_Datasets.Count > value) Object.Dataset = m_Datasets[value];
            SetDataNameDropdown();
        }
        void SetDatasetDropdown()
        {
            m_Datasets = ApplicationState.ProjectLoaded.Datasets.Where((d) => d.Protocol == m_SelectedProtocol).ToList();
            SetDatasetDropdownInteractable(m_Datasets != null && m_Patients != null && m_Datasets.Count > 0 && m_ProtocolDropdown.interactable && m_Patients.Length > 0);
        }
        void SetDatasetDropdownInteractable(bool interactable)
        {
            m_DatasetDropdown.interactable = interactable;
            if (interactable)
            {
                m_DatasetDropdown.options = m_Datasets.Select(d => new Dropdown.OptionData(d.Name, null)).ToList();
                if (Object.Dataset != null)
                {
                    m_DatasetDropdown.SetValue(m_Datasets.IndexOf(Object.Dataset));
                }
                else
                {
                    m_DatasetDropdown.SetValue(m_Datasets.FindIndex(d => d.Protocol == m_SelectedProtocol));
                }
            }
            else
            {
                m_DatasetDropdown.ClearOptions();
                SetDataNameDropdownInteractable(false);
            }
            m_DatasetDropdown.RefreshShownValue();
        }

        // Data.
        void OnChangeDataName(int value)
        {
            if(m_DataNames != null && m_DataNames.Count > value)
            {
                Object.DataName = m_DataNames[value];
            }
        }
        void SetDataNameDropdown()
        {
            if(Object.Dataset != null)
            {
                m_DataNames = (from data in Object.Dataset.Data select data.Name).Distinct().Where((name) => m_Patients.All((patient) => Object.Dataset.GetIEEGDataInfos().Any((dataInfo) => dataInfo.IsOk && dataInfo.Name == name && dataInfo.Patient == patient))).ToList();
            }
            else
            {
                m_DataNames = new List<string>();
            }
            SetDataNameDropdownInteractable(m_DataNames != null && m_DataNames.Count > 0 && m_DatasetDropdown.interactable);
        }
        void SetDataNameDropdownInteractable(bool interactable)
        {
            m_DataNameDropdown.interactable = interactable;
            if (interactable)
            {
                m_DataNameDropdown.options = (from label in m_DataNames select new Dropdown.OptionData(label, null)).ToList();
                m_DataNameDropdown.SetValue(Mathf.Clamp(m_DataNames.IndexOf(Object.DataName), 0, m_DataNameDropdown.options.Count - 1));
            }
            else
            {
                m_DataNameDropdown.ClearOptions();
            }
            m_DataNameDropdown.RefreshShownValue();
        }
        #endregion
    }
}