using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;
using System.Collections.Generic;
using HBP.Data.Experience.Dataset;
using HBP.Data.Experience.Protocol;
using HBP.Data.Visualization;

namespace HBP.UI.Visualization
{
    public class ColumnModifier : MonoBehaviour
    {
        #region Properties
        [SerializeField] Dropdown m_ProtocolDropdown;
        [SerializeField] Dropdown m_BlocDropdown;
        [SerializeField] Dropdown m_DatasetDropdown;
        [SerializeField] Dropdown m_DataDropdown;

        Column m_Column;
        Data.Patient[] m_Patients;

        Dataset[] m_Datasets;
        string[] m_Data;
        Protocol[] m_Protocols;
        #endregion

        #region Public Methods
        public void SetTab(Column column,Data.Patient[] patients)
        {
            m_Patients = patients;
            m_Column = column;
            SetProtocolDropdown();
        }
        #endregion

        #region Private Methods
        void OnChangeProtocol()
        {
            m_Column.Protocol = m_Protocols[m_ProtocolDropdown.value];
            SetBlocDropdown();
            SetDatasetDropdown();
        }
        void SetProtocolDropdown()
        {
            m_ProtocolDropdown.onValueChanged.RemoveAllListeners();
            m_ProtocolDropdown.onValueChanged.AddListener((index) => OnChangeProtocol());

            m_Protocols = ApplicationState.ProjectLoaded.Protocols.ToArray();
            if (m_Protocols.Length > 0)
            {
                m_ProtocolDropdown.interactable = true;
                m_ProtocolDropdown.options = (from protocol in m_Protocols select new Dropdown.OptionData(protocol.Name, null)).ToList();
                SetProtocol(m_Column.Protocol);
            }
            else DeactivateProtocolDropdown();
        }
        void DeactivateProtocolDropdown()
        {
            m_ProtocolDropdown.interactable = false;
            m_ProtocolDropdown.options = new List<Dropdown.OptionData>() { new Dropdown.OptionData("", null) };
            m_ProtocolDropdown.value = 0;
            DeactivateBlocDropdown();
            DeactivateDatasetDropdown();
        }
        void SetProtocol(Protocol protocol)
        {
            m_ProtocolDropdown.value = Mathf.Max(0, Array.IndexOf(m_Protocols,protocol));
            OnChangeProtocol();
        }

        void OnChangeBloc()
        {
            m_Column.Bloc = m_Column.Protocol.Blocs[m_BlocDropdown.value];
        }
        void SetBlocDropdown()
        {
            m_BlocDropdown.onValueChanged.RemoveAllListeners();
            m_BlocDropdown.onValueChanged.AddListener((index) => OnChangeBloc());

            if (m_Column.Protocol.Blocs.Count > 0 && m_ProtocolDropdown.interactable)
            {
                m_BlocDropdown.interactable = true;
                m_BlocDropdown.options = (from bloc in m_Column.Protocol.Blocs select new Dropdown.OptionData(bloc.DisplayInformations.Name, null)).ToList();
                SetBloc(m_Column.Bloc);
            }
            else DeactivateBlocDropdown();
        }
        void DeactivateBlocDropdown()
        {
            m_BlocDropdown.interactable = false;
            m_BlocDropdown.options = new List<Dropdown.OptionData>() { new Dropdown.OptionData("", null) }; ;
            m_BlocDropdown.value = 0;
        }
        void SetBloc(Bloc bloc)
        {
            m_BlocDropdown.value = Mathf.Max(0, m_Column.Protocol.Blocs.IndexOf(bloc));
            OnChangeBloc();
        }

        void OnChangeDataset()
        {
            m_Column.Dataset = m_Datasets[m_DatasetDropdown.value];
            SetDataDropdown();
        }
        void SetDatasetDropdown()
        {
            m_DatasetDropdown.onValueChanged.RemoveAllListeners();
            m_DatasetDropdown.onValueChanged.AddListener((index) => OnChangeDataset());

            m_Datasets = ApplicationState.ProjectLoaded.Datasets.Where((d) => d.Protocol == m_Column.Protocol).ToArray();
            if (m_Datasets.Length > 0)
            {
                m_DatasetDropdown.interactable = true;
                m_DatasetDropdown.options = (from dataset in m_Datasets select new Dropdown.OptionData(dataset.Name, null)).ToList();
                SetDataset(m_Column.Dataset);
            }
            else DeactivateDatasetDropdown();
        }
        void DeactivateDatasetDropdown()
        {
            m_DatasetDropdown.interactable = false;
            m_DatasetDropdown.options = new List<Dropdown.OptionData>() { new Dropdown.OptionData("", null) };
            m_DatasetDropdown.value = 0;
            DeactivateDataDropdown();
        }
        void SetDataset(Dataset dataset)
        {
            m_DatasetDropdown.value = Mathf.Max(0, Array.IndexOf(m_Datasets,dataset));
            OnChangeDataset();
        }

        void OnChangeData()
        {
            m_Column.Data = m_Data[m_DataDropdown.value];
        }
        void SetDataDropdown()
        {
            m_DataDropdown.onValueChanged.RemoveAllListeners();
            m_DataDropdown.onValueChanged.AddListener((index) => OnChangeData());

            m_Data = (from data in m_Column.Dataset.Data select data.Name).Distinct().Where((l) => m_Patients.All((p) => m_Column.Dataset.Data.Any((d) => d.isOk && d.Name == l && d.Patient == p))).ToArray();
            if(m_Data.Length > 0 && m_DatasetDropdown.interactable)
            {
                m_DataDropdown.interactable = true;
                m_DataDropdown.options = (from label in m_Data select new Dropdown.OptionData(label,null)).ToList();
                SetData(m_Column.Data);
            }
            else DeactivateDataDropdown();
        }
        void DeactivateDataDropdown()
        {
            m_DataDropdown.interactable = false;
            m_DataDropdown.options = new List<Dropdown.OptionData>() { new Dropdown.OptionData("", null) };
            m_DataDropdown.value = 0;
        }
        void SetData(string data)
        {
            m_DataDropdown.value = Mathf.Max(0, Array.IndexOf(m_Data,data));
            OnChangeData();
        }
        #endregion
    }
}