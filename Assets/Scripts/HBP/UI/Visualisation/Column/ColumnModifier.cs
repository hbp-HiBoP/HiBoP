using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;
using System.Linq;
using System.Collections.Generic;
using HBP.Data.Experience.Dataset;
using HBP.Data.Experience.Protocol;
using HBP.Data.Visualisation;

namespace HBP.UI.Visualisation
{
    public class ColumnModifier : MonoBehaviour
    {
        #region Properties
        #region UI
        [SerializeField]
        Dropdown m_datasetCB;
        [SerializeField]
        Dropdown m_dataCB;
        [SerializeField]
        Dropdown m_protocolCB;
        [SerializeField]
        Dropdown m_blocCB;
        #endregion
        #region Data
        Data.Patient[] m_patients;
        Data.Patient[] Patients
        {
            get { return m_patients; }
            set { m_patients = value; }
        }
        bool SP;

        Column m_column;
        Column Column
        {
            get
            {
                return m_column;
            }
            set
            {
                m_column = value;           
                SetDatasetComboBox();
                SetDataset(m_column.Dataset);
            }
        }
        #endregion
        #region Others
        List<Dataset> m_datasets = new List<Dataset>();
        List<string> m_dataLabel = new List<string>();
        List<DataInfo> m_dataInfo = new List<DataInfo>();
        List<Protocol> m_protocols = new List<Protocol>();
        List<Bloc> m_blocs = new List<Bloc>();
        #endregion
        #endregion

        #region Public Methods
        #region General
        public void SetTab(Column column,Data.Patient[] patients,bool sp)
        {
            Profiler.BeginSample("SetTab");
            Profiler.BeginSample("SetTab : Patient");
            Patients = patients;
            Profiler.EndSample();
            Profiler.BeginSample("SetTab : SP");
            SP = sp;
            Profiler.EndSample();
            Profiler.BeginSample("SetTab : Column");
            Column = column;
            Profiler.EndSample();
            Profiler.EndSample();
        }
        #endregion
        #region OnChangeEvent()
        public void OnChangeDataset()
        {
            if (m_datasets.Count > m_datasetCB.value)
            {
                Column.Dataset = m_datasets[m_datasetCB.value];
            }
            else
            {
                Column.Dataset = new Dataset();
            }
            SetDataComboBox();
            SetDataInfo(Column.DataLabel);
        }
        public void OnChangeDataComboBox()
        {
            if (m_dataLabel.Count > m_dataCB.value)
            {
                Column.DataLabel = m_dataLabel[m_dataCB.value];
            }
            else
            {
                Column.DataLabel = string.Empty;
            }
            SetProtocolComboBox();
            SetProtocol(Column.Protocol);
        }
        public void OnChangeProtocol()
        {
            if (m_protocols.Count > m_protocolCB.value)
            {
                Column.Protocol = m_protocols[m_protocolCB.value];
            }
            else
            {
                Column.Protocol = new Protocol();
            }
            SetBlocComboBox();
            SetBloc(Column.Bloc);
        }
        public void OnChangeBloc()
        {
            if (m_blocs.Count > m_blocCB.value)
            {
                Column.Bloc = m_blocs[m_blocCB.value];
            }
            else
            {
                Column.Bloc = new Bloc();
            }
        }
        #endregion
        #endregion

        #region Private Methods
        #region Dataset
        void SetDatasetComboBox()
        {
            Profiler.BeginSample("SetDataset");
            Dataset[] l_dataset = ApplicationState.ProjectLoaded.Datasets.ToArray();
            m_datasets = new List<Dataset>();

            foreach (Dataset dataset in l_dataset)
            {
                bool l_containsAllPatient = true;
                foreach (Data.Patient patient in Patients)
                {
                    bool l_datasetContainsPatient = false;
                    foreach (DataInfo i_dataInfo in dataset.Data)
                    {
                        if (i_dataInfo.Patient.Equals(patient))
                        {
                            l_datasetContainsPatient = true;
                            break;
                        }
                    }
                    if (!l_datasetContainsPatient)
                    {
                        l_containsAllPatient = false;
                        break;
                    }
                }
                if (l_containsAllPatient)
                {
                    m_datasets.Add(dataset);
                }
            }

            if (m_datasets.Count != 0 && Patients.Length != 0)
            {
                m_datasetCB.interactable = true;
                List<Dropdown.OptionData> l_datasetOptions = new List<Dropdown.OptionData>(m_datasets.Count);
                for (int i = 0; i < m_datasets.Count; i++)
                {
                    l_datasetOptions.Add(new Dropdown.OptionData(m_datasets[i].Name, null));
                }
                m_datasetCB.options = l_datasetOptions;
            }
            else
            {
                DeactivateDatasetCB();
            }
            Profiler.EndSample();
        }
        void DeactivateDatasetCB()
        {
            List<Dropdown.OptionData> l_datasetOptions = new List<Dropdown.OptionData>(1);
            l_datasetOptions.Add(new Dropdown.OptionData("", null));
            m_datasetCB.interactable = false;
            m_datasetCB.options = l_datasetOptions;
            m_datasetCB.value = 0;
            DeactivateDataInfoCB();
        }
        void SetDataset(Dataset dataset)
        {
            if(m_datasetCB.interactable)
            {
                bool l_found = false;
                for (int i = 0; i < m_datasets.Count; i++)
                {
                    if (dataset == m_datasets[i])
                    {
                        m_datasetCB.value = i;
                        l_found = true;
                        break;
                    }
                }
                if (!l_found) m_datasetCB.value = 0;
                OnChangeDataset();
            }
        }
        #endregion
        #region ExperienceData
        void SetDataComboBox()
        {
            Profiler.BeginSample("SetDataCB");
            Profiler.BeginSample("Part 1");
            Profiler.BeginSample("1.1");
            Dataset l_dataset = new Dataset();
            if (m_datasets.Count > m_datasetCB.value)
            {
                l_dataset = m_datasets[m_datasetCB.value];
            }
            Profiler.EndSample();
            Profiler.BeginSample("1.2");
            List<string> l_dataInfoLabel = new List<string>();
            List<string> l_dataInfoLabelToUse = new List<string>();
            List<DataInfo> l_dataInfoToUse = new List<DataInfo>();
            foreach (DataInfo i_dataInfo in l_dataset.Data)
            {
                if(SP)
                {
                    if(i_dataInfo.UsableInSinglePatient)
                    {
                        l_dataInfoToUse.Add(i_dataInfo);
                    }
                }
                else
                {
                    if(i_dataInfo.UsableInMultiPatients)
                    {
                        l_dataInfoToUse.Add(i_dataInfo);
                    }
                }
            }
            Profiler.EndSample();
            Profiler.BeginSample("1.3");
            foreach (DataInfo i_dataInfo in l_dataInfoToUse)
            {
                if (!l_dataInfoLabel.Contains(i_dataInfo.Name))
                {
                    l_dataInfoLabel.Add(i_dataInfo.Name);
                }
            }
            Profiler.EndSample();
            Profiler.BeginSample("1.4");
            foreach (string i_dataLabel in l_dataInfoLabel)
            {
                List<DataInfo> l_dataInfoForThisLabel = new List<DataInfo>();
                foreach (DataInfo i_dataInfo in l_dataInfoToUse)
                {
                    if (i_dataInfo.Name == i_dataLabel) l_dataInfoForThisLabel.Add(i_dataInfo);
                }

                bool l_dataLabelContainsAllPatients = true;
                foreach (Data.Patient patient in Patients)
                {
                    bool l_containsThisPatient = false;
                    foreach (DataInfo i_dataInfo in l_dataInfoForThisLabel)
                    {
                        if (i_dataInfo.Patient.Equals(patient))
                        {
                            l_containsThisPatient = true;
                            break;
                        }
                    }
                    if (!l_containsThisPatient)
                    {
                        l_dataLabelContainsAllPatients = false;
                        break;
                    }
                }
                if (l_dataLabelContainsAllPatients)
                {
                    l_dataInfoLabelToUse.Add(i_dataLabel);
                }
            }
            Profiler.EndSample();
            Profiler.EndSample();
            Profiler.BeginSample("Part 2");
            if (l_dataInfoLabelToUse.Count != 0 && m_datasetCB.interactable)
            {
                m_dataCB.interactable = true;
                List<Dropdown.OptionData> l_dataInfoOptions = new List<Dropdown.OptionData>(l_dataInfoLabelToUse.Count);
                for (int i = 0; i < l_dataInfoLabelToUse.Count; i++)
                {
                    l_dataInfoOptions.Add(new Dropdown.OptionData(l_dataInfoLabelToUse[i], null));
                }
                m_dataCB.options = l_dataInfoOptions;
                m_dataLabel = l_dataInfoLabelToUse;
            }
            else
            {
                DeactivateDataInfoCB();
            }
            Profiler.EndSample();
            Profiler.EndSample();
        }

        void DeactivateDataInfoCB()
        {
            List<Dropdown.OptionData> l_dataInfoOptions = new List<Dropdown.OptionData>(1);
            m_dataLabel = new List<string>();
            l_dataInfoOptions.Add(new Dropdown.OptionData("", null));
            m_dataCB.interactable = false;
            m_dataCB.options = l_dataInfoOptions;
            m_dataCB.value = 0;
            DeactivateProtocolCB();
        }

        void SetDataInfo(string dataInfo)
        {
            if(m_dataCB.interactable)
            {
                bool l_found = false;
                int length = m_dataLabel.Count;
                for (int i = 0; i < length; i++)
                {
                    if (dataInfo == m_dataLabel[i])
                    {
                        m_dataCB.value = i;
                        l_found = true;
                        break;
                    }
                }
                if (!l_found) m_dataCB.value = 0;
                OnChangeDataComboBox();
            }
        }

        #endregion
        #region Protocol
        void SetProtocolComboBox()
        {
            Profiler.BeginSample("SetProtocolCB");
            Dataset l_dataset = new Dataset();
            if (m_datasets.Count > m_datasetCB.value)
            {
                l_dataset = m_datasets[m_datasetCB.value];
            }
            string l_labelData = m_dataLabel[m_dataCB.value];
            DataInfo[] l_dataInfo = l_dataset.Data.ToArray();

            List<DataInfo> l_dataInfoToUse = new List<DataInfo>();
            List<Protocol> l_possibleProtocol = new List<Protocol>();
            List<Protocol> l_possibleProtocolToUse = new List<Protocol>();
            foreach (DataInfo i_dataInfo in l_dataInfo)
            {
                if (i_dataInfo.Name == l_labelData)
                {
                    l_dataInfoToUse.Add(i_dataInfo);
                    if (!l_possibleProtocol.Contains(i_dataInfo.Protocol))
                    {
                        l_possibleProtocol.Add(i_dataInfo.Protocol);
                    }
                }
            }
            m_dataInfo = l_dataInfoToUse;

            foreach (Protocol protocol in l_possibleProtocol)
            {
                List<DataInfo> l_dataInfoWithThisProtocol = new List<DataInfo>();
                foreach (DataInfo i_dataInfo in m_dataInfo)
                {
                    if (i_dataInfo.Protocol == protocol) l_dataInfoWithThisProtocol.Add(i_dataInfo);
                }

                bool l_protocolContainsAllPatients = true;
                foreach (Data.Patient patient in Patients)
                {
                    bool l_containsThisPatient = false;
                    foreach (DataInfo i_dataInfo in l_dataInfoWithThisProtocol)
                    {
                        if (i_dataInfo.Patient.Equals(patient))
                        {
                            l_containsThisPatient = true;
                            break;
                        }
                    }
                    if (!l_containsThisPatient)
                    {
                        l_protocolContainsAllPatients = false;
                        break;
                    }
                }
                if (l_protocolContainsAllPatients)
                {
                    l_possibleProtocolToUse.Add(protocol);
                }
            }

            int l_nbProtocols = l_possibleProtocolToUse.Count;
            if (l_nbProtocols != 0 && m_dataCB.interactable)
            {
                m_protocolCB.interactable = true;
                List<Dropdown.OptionData> l_protocolsOptions = new List<Dropdown.OptionData>(l_nbProtocols);
                for (int i = 0; i < l_nbProtocols; i++)
                {
                    l_protocolsOptions.Add(new Dropdown.OptionData(l_possibleProtocolToUse[i].Name, null));
                }
                m_protocolCB.options = l_protocolsOptions;
                m_protocols = l_possibleProtocolToUse;
            }
            else
            {
                DeactivateProtocolCB();
            }
            Profiler.EndSample();
        }

        void DeactivateProtocolCB()
        {
            List<Dropdown.OptionData> l_protocolOptions = new List<Dropdown.OptionData>(1);
            l_protocolOptions.Add(new Dropdown.OptionData("", null));
            m_protocolCB.interactable = false;
            m_protocolCB.options = l_protocolOptions;
            m_protocolCB.value = 0;
            DeactivateBlocCB();
        }

        void SetProtocol(Protocol protocol)
        {
            if(m_protocolCB.interactable)
            {
                int length = m_protocols.Count;
                bool l_found = false;
                for (int i = 0; i < length; i++)
                {
                    if (m_protocols[i] == protocol)
                    {
                        m_protocolCB.value = i;
                        l_found = true;
                        break;
                    }
                }
                if (!l_found) m_protocolCB.value = 0; OnChangeProtocol();
            }
        }
        #endregion
        #region Bloc
        void SetBlocComboBox()
        {
            Profiler.BeginSample("SetBlocCB");
            Protocol l_protocol = m_protocols[m_protocolCB.value];
            int l_nbBlocs = l_protocol.Blocs.Count;
            if (l_nbBlocs != 0 && m_protocolCB.interactable)
            {
                m_blocCB.interactable = true;
                List<Dropdown.OptionData> l_blocsOptions = new List<Dropdown.OptionData>(l_nbBlocs);
                for (int i = 0; i < l_nbBlocs; i++)
                {
                    l_blocsOptions.Add(new Dropdown.OptionData(l_protocol.Blocs[i].DisplayInformations.Name, null));
                }
                m_blocCB.options = l_blocsOptions;
                m_blocs = l_protocol.Blocs.ToList();
            }
            else
            {
                DeactivateBlocCB();
            }
            Profiler.EndSample();
        }

        void DeactivateBlocCB()
        {
            List<Dropdown.OptionData> l_blocOptions = new List<Dropdown.OptionData>(1);
            l_blocOptions.Add(new Dropdown.OptionData("", null));
            m_blocCB.interactable = false;
            m_blocCB.options = l_blocOptions;
            m_blocCB.value = 0;
        }

        void SetBloc(Bloc bloc)
        {
            if(m_blocCB.interactable)
            {
                int length = m_blocs.Count;
                bool l_found = false;
                for (int i = 0; i < length; i++)
                {
                    if (m_blocs[i].Equals(bloc))
                    {
                        m_blocCB.value = i;
                        l_found = true;
                        break;
                    }
                }
                if (!l_found) m_blocCB.value = 0; OnChangeBloc();
            }
        }

        void ApplyModification()
        {
            if(m_datasets.Count > m_datasetCB.value)
            {
                Column.Dataset = m_datasets[m_datasetCB.value];
            }
            else
            {
                Column.Dataset = new Dataset();
            }
            if(m_dataLabel.Count > m_dataCB.value)
            {
                Column.DataLabel = m_dataLabel[m_dataCB.value];
            }
            else
            {
                Column.DataLabel = string.Empty;
            }
            if(m_protocols.Count > m_protocolCB.value)
            {
                Column.Protocol = m_protocols[m_protocolCB.value];
            }
            else
            {
                Column.Protocol = new Protocol();
            }
            if(m_blocs.Count > m_blocCB.value)
            {
                Column.Bloc = m_blocs[m_blocCB.value];
            }
            else
            {
                Column.Bloc = new Bloc();
            }
        }
        #endregion
        #endregion
    }
}