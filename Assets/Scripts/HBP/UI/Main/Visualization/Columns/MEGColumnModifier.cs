using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    public class MEGColumnModifier : SubModifier<MEGColumn>
    {
        #region Properties
        [SerializeField] Dropdown m_ProtocolDropdown, m_DatasetDropdown;

        List<Protocol> m_Protocols;
        Protocol m_SelectedProtocol;
        List<Dataset> m_Datasets;

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

        public override MEGColumn Object
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

        Patient[] m_Patients;
        public Patient[] Patients
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
            m_DatasetDropdown.onValueChanged.AddListener(OnChangeDataset);

            base.Initialize();
        }

        // Protocol.
        void SetProtocolDropdown()
        {
            m_Protocols = ApplicationState.ProjectLoaded.Protocols.ToList();
            SetProtocolDropdownInteractable(m_Protocols != null && m_Patients != null && m_Protocols.Count > 0 && m_Patients.Length > 0);
        }
        void OnChangeProtocol(int value)
        {
            if (m_Protocols != null && m_Protocols.Count > value)
            {
                m_SelectedProtocol = m_Protocols[value];
            }
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
                SetDatasetDropdownInteractable(false);
            }
            m_ProtocolDropdown.RefreshShownValue();
        }

        // Dataset.
        void OnChangeDataset(int value)
        {
            if (m_Datasets != null && m_Datasets.Count > value) Object.Dataset = m_Datasets[value];
        }
        void SetDatasetDropdown()
        {
            m_Datasets = ApplicationState.ProjectLoaded.Datasets.Where((d) => d.Protocol == m_SelectedProtocol && d.GetMEGDataInfos().Length > 0 && d.GetMEGDataInfos().Any(dataInfo => dataInfo.IsOk && m_Patients.Any(p => dataInfo.Patient == p))).ToList();
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
            }
            m_DatasetDropdown.RefreshShownValue();
        }
        #endregion
    }
}