using HBP.Data;
using System.Collections.Generic;
using Tools.Unity;
using UnityEngine;
using UnityEngine.Events;
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
            ILoadableFromDatabase<Patient> loadable = new Patient() as ILoadableFromDatabase<Patient>;
            GenericEvent<float, float, LoadingText> onChangeProgress = new GenericEvent<float, float, LoadingText>();
            ApplicationState.LoadingManager.Load(loadable.LoadFromDatabase(path, (progress, duration, text) => onChangeProgress.Invoke(progress, duration, text), (result) => FinishedLoadingBIDSDatabase(result)), onChangeProgress);
        }
        private void FinishedLoadingBIDSDatabase(IEnumerable<Patient> patients)
        {
            m_BIDSPatientListGestion.List.Set(patients);
            m_BIDSPatientListPanel.gameObject.SetActive(true);
        }
        #endregion

        #region Public Methods
        public override void ClosePanel()
        {
            base.ClosePanel();
            if (m_BIDS.isOn)
            {
                ApplicationState.ProjectLoaded.SetPatients(m_BIDSPatientListGestion.List.ObjectsSelected);
            }
            else if (m_NotBIDS.isOn)
            {
                ApplicationState.ProjectLoaded.SetPatients(m_NotBIDSPatientListGestion.List.Objects);
            }
        }
        #endregion
    }
}