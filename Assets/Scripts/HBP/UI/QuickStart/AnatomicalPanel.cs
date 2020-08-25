using Tools.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.QuickStart
{
    public class AnatomicalPanel : QuickStartPanel
    {
        #region Properties
        [SerializeField] private Toggle m_BIDS;
        [SerializeField] private Toggle m_NotBIDS;

        [SerializeField] private RectTransform m_BIDSPanel;
        [SerializeField] private FolderSelector m_BIDSFolderSelector;
        [SerializeField] private RectTransform m_BIDSPatientListPanel;
        [SerializeField] private PatientListGestion m_BIDSPatientListGestion;

        [SerializeField] private RectTransform m_NotBIDSPanel;
        [SerializeField] private PatientListGestion m_NotBIDSPatientListGestion;
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            base.Initialize();
            m_BIDS.onValueChanged.AddListener(m_BIDSPanel.gameObject.SetActive);
            m_NotBIDS.onValueChanged.AddListener(m_NotBIDSPanel.gameObject.SetActive);
            m_BIDSFolderSelector.onEndEdit.AddListener(LoadBIDSDatabase);
        }
        private void LoadBIDSDatabase(string path)
        {
            m_BIDSPatientListPanel.gameObject.SetActive(true);
        }
        #endregion
    }
}