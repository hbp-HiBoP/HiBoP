using HBP.Core.Data;
using HBP.Data.Database;
using HBP.UI.Main;
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

        [SerializeField] private DataInfoListGestion m_DataInfoListGestion;
        #endregion

        #region Private Methods
        private void Awake()
        {
            System.Collections.Generic.List<ProtocolTab> tabs = new();
            foreach (var protocol in DatabaseManager.Database.Protocols)
            {
                var tab = Instantiate(m_ProtocolTabPrefab, m_ProtocolTabParent).GetComponent<ProtocolTab>();
                tab.Initialize(protocol);
                tab.OnSelect.AddListener(OnSelectProtocol);
                tabs.Add(tab);
            }
            foreach (var tab in tabs)
            {
                tab.Toggle.group = m_ProtocolTabToggleGroup;
            }
        }
        private void OnSelectProtocol(Protocol protocol)
        {
            m_CurrentProtocol = protocol;
            UpdateList();
        }
        private void UpdateList()
        {
            if (m_CurrentPatient != null && m_CurrentProtocol != null)
            {
                m_DataInfoListGestion.List.Set(DatabaseManager.Database.DataInfos.OfType<PatientDataInfo>().Where(pd => pd.Protocol == m_CurrentProtocol && pd.Patient == m_CurrentPatient));
            }
        }
        #endregion

        #region Public Methods
        public void Set(Patient patient)
        {
            m_CurrentPatient = patient;
            UpdateList();
        }
        #endregion
    }
}