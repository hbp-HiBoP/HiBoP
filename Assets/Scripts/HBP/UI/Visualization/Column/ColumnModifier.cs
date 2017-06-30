using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;
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
        [SerializeField] Dropdown m_DatasetDropdown;
        [SerializeField] Dropdown m_DataLabelDropdown;
        [SerializeField] Dropdown m_ProtocolDropdown;
        [SerializeField] Dropdown m_BlocDropdown;

        Column m_Column;
        Data.Patient[] m_Patients;

        List<Dataset> m_Datasets = new List<Dataset>();
        List<string> m_DataLabels = new List<string>();
        List<DataInfo> m_DataInfos = new List<DataInfo>();
        List<Protocol> m_Protocols = new List<Protocol>();
        #endregion

        #region Public Methods
        public void SetTab(Column column,Data.Patient[] patients)
        {
            m_Patients = patients;
            m_Column = column;
            SetDatasetDropdown();
            SetDataset(m_Column.Dataset);
        }
        public void OnChangeDataset()
        {
            m_Column.Dataset = m_Datasets[m_DatasetDropdown.value];
            SetDataLabelDropdown();
            SetDataLabel(m_Column.DataLabel);
        }
        public void OnChangeDataLabel()
        {
            m_Column.DataLabel = m_DataLabels[m_DatasetDropdown.value];
            SetProtocolDropdown();
            SetProtocol(m_Column.Protocol);
        }
        public void OnChangeProtocol()
        {
            m_Column.Protocol = m_Protocols[m_ProtocolDropdown.value];
            SetBlocDropdown();
            SetBloc(m_Column.Bloc);
        }
        public void OnChangeBloc()
        {
            m_Column.Bloc = m_Protocols[m_ProtocolDropdown.value].Blocs[m_BlocDropdown.value];
        }
        #endregion

        #region Private Methods
        void SetDatasetDropdown()
        {
            m_Datasets = ApplicationState.ProjectLoaded.Datasets.ToList().FindAll((d) => m_Patients.All((p) => d.Data.Exists((i) => i.Patient == p && i.isOk)));
            if (m_Datasets.Count > 0 && m_Patients.Length > 0)
            {
                m_DatasetDropdown.interactable = true;
                m_DatasetDropdown.options = (from data in m_Datasets select new Dropdown.OptionData(data.Name, null)).ToList();
            }
            else DeactivateDatasetDropdown();
        }
        void DeactivateDatasetDropdown()
        {
            m_DatasetDropdown.interactable = false;
            m_DatasetDropdown.options = new List<Dropdown.OptionData>() { new Dropdown.OptionData("", null) };
            m_DatasetDropdown.value = 0;
            DeactivateDataLabelDropdown();
        }
        void SetDataset(Dataset dataset)
        {
            m_DatasetDropdown.value = Mathf.Max(0, m_Datasets.IndexOf(dataset));
            OnChangeDataset();
        }

        void SetDataLabelDropdown()
        {
            m_DataLabels = (from data in m_Datasets[m_DatasetDropdown.value].Data.FindAll((d) => m_Patients.All((p) => m_Datasets[m_DatasetDropdown.value].Data.Exists((i) => i.Patient == p && i.isOk))) select data.Name).Distinct().ToList();
            if (m_DataLabels.Count != 0 && m_DatasetDropdown.interactable)
            {
                m_DataLabelDropdown.interactable = true;
                m_DataLabelDropdown.options = (from label in m_DataLabels select new Dropdown.OptionData(label,null)).ToList();
            }
            else DeactivateDataLabelDropdown();
        }
        void DeactivateDataLabelDropdown()
        {
            m_DataLabelDropdown.interactable = false;
            m_DataLabelDropdown.options = new List<Dropdown.OptionData>() { new Dropdown.OptionData("", null) };
            m_DataLabelDropdown.value = 0;
            DeactivateProtocolDropdown();
        }
        void SetDataLabel(string dataLabel)
        {
            m_DataLabelDropdown.value = Mathf.Max(0,m_DataLabels.IndexOf(dataLabel));
            OnChangeDataLabel();
        }

        void SetProtocolDropdown()
        {
            m_Protocols = (from dataInfo in m_Datasets[m_DatasetDropdown.value].Data.FindAll((d) => d.Name == m_DataLabels[m_DataLabelDropdown.value] && m_Patients.Contains(d.Patient)) select dataInfo.Protocol).Distinct().ToList();
            if(m_Protocols.Count > 0 && m_DataLabelDropdown.interactable)
            {
                m_ProtocolDropdown.interactable = true;
                m_ProtocolDropdown.options = (from protocol in m_Protocols select new Dropdown.OptionData(protocol.Name,null)).ToList();
            }
            else DeactivateProtocolDropdown();
        }
        void DeactivateProtocolDropdown()
        {
            m_ProtocolDropdown.interactable = false;
            m_ProtocolDropdown.options = new List<Dropdown.OptionData>() { new Dropdown.OptionData("", null) };
            m_ProtocolDropdown.value = 0;
            DeactivateBlocDropdown();
        }
        void SetProtocol(Protocol protocol)
        {
            m_ProtocolDropdown.value = Mathf.Max(0, m_Protocols.IndexOf(protocol));
            OnChangeProtocol();
        }

        void SetBlocDropdown()
        {
            if (m_Protocols[m_ProtocolDropdown.value].Blocs.Count > 0 && m_ProtocolDropdown.interactable)
            {
                m_BlocDropdown.interactable = true;
                m_BlocDropdown.options = (from bloc in m_Protocols[m_ProtocolDropdown.value].Blocs select new Dropdown.OptionData(bloc.DisplayInformations.Name,null)).ToList();
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
            m_BlocDropdown.value = Mathf.Max(0,m_Protocols[m_ProtocolDropdown.value].Blocs.IndexOf(bloc));
            OnChangeBloc();
        }

        void ApplyModifications()
        {
            m_Column.Dataset = m_Datasets[m_DatasetDropdown.value];
            m_Column.DataLabel = m_DataLabels[m_DataLabelDropdown.value];
            m_Column.Protocol = m_Protocols[m_ProtocolDropdown.value];
            m_Column.Bloc = m_Column.Protocol.Blocs[m_BlocDropdown.value];
        }
        #endregion
    }
}