using HBP.Core.Data;
using HBP.Core.Database;
using HBP.UI.Main;
using HBP.UI.Tools;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Database
{
    public class FunctionalDataExplorer : MonoBehaviour
    {
        #region Properties
        [SerializeField] private GameObject m_ProtocolTabPrefab;
        [SerializeField] private ToggleGroup m_ProtocolTabToggleGroup;
        [SerializeField] private Transform m_ProtocolTabParent;

        private Protocol m_CurrentProtocol;
        private Patient m_CurrentPatient;
        private Dictionary<Patient, List<PatientDataInfo>> m_DataInfosByPatient;

        [SerializeField] private DatabaseDataInfoListGestion m_DatabaseDataInfoListGestion;

        private List<ProtocolTab> m_Tabs = new();
        #endregion

        #region Private Methods
        private void OnSelectProtocol(Protocol protocol)
        {
            m_CurrentProtocol = protocol;
            UpdateList();
        }
        private void UpdateList()
        {
            if (m_CurrentPatient != null && m_CurrentProtocol != null)
            {
                m_DatabaseDataInfoListGestion.List.Set(m_DataInfosByPatient[m_CurrentPatient].Where(pd => pd.Protocol == m_CurrentProtocol));
            }
        }
        #endregion

        #region Public Methods
        public void Initialize(WindowsReferencer windowsReferencer)
        {
            m_DatabaseDataInfoListGestion.WindowsReferencer.OnOpenWindow.AddListener(windowsReferencer.Add);
        }
        public void SetFields()
        {
            foreach (var tab in m_Tabs)
            {
                Destroy(tab.gameObject);
            }
            m_Tabs.Clear();
            foreach (var protocol in DatabaseManager.Database.Protocols)
            {
                var tab = Instantiate(m_ProtocolTabPrefab, m_ProtocolTabParent).GetComponent<ProtocolTab>();
                tab.Initialize(protocol);
                tab.OnSelect.AddListener(OnSelectProtocol);
                m_Tabs.Add(tab);
            }
            foreach (var tab in m_Tabs)
            {
                tab.Toggle.group = m_ProtocolTabToggleGroup;
            }
            m_DataInfosByPatient = new Dictionary<Patient, List<PatientDataInfo>>();
            foreach (var patient in DatabaseManager.Database.Patients)
            {
                m_DataInfosByPatient[patient] = new List<PatientDataInfo>();
            }
            foreach (var dataInfo in DatabaseManager.Database.DataInfos.OfType<PatientDataInfo>())
            {
                m_DataInfosByPatient[dataInfo.Patient].Add(dataInfo);
            }
        }
        public void Set(Patient patient)
        {
            m_CurrentPatient = patient;
            foreach (var tab in m_Tabs)
            {
                tab.HasData = m_DataInfosByPatient[patient].Any(pd => pd.Protocol == tab.Protocol);
            }
            UpdateList();
        }
        #endregion
    }
}