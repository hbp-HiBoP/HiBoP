using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using HBP.Core.Interfaces;
using HBP.Core.Tools;
using HBP.Core.Data;

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
            ILoadableFromDatabase<Patient> loadable = new Patient();
            GenericEvent<float, float, LoadingText> onChangeProgress = new GenericEvent<float, float, LoadingText>();
            LoadingManager.Load(loadable.LoadFromDatabase(path, (progress, duration, text) => onChangeProgress.Invoke(progress, duration, text), (result) => FinishedLoadingBIDSDatabase(result)), onChangeProgress);
        }
        private void FinishedLoadingBIDSDatabase(IEnumerable<Patient> patients)
        {
            m_BIDSPatientListGestion.List.Set(patients);
            m_BIDSPatientListPanel.gameObject.SetActive(true);
        }
        #endregion

        #region Public Methods
        public override bool OpenNextPanel()
        {
            if (m_BIDS.isOn)
            {
                var patients = m_BIDSPatientListGestion.List.ObjectsSelected;
                if (patients.Length > 0)
                    ApplicationState.ProjectLoaded.SetPatients(patients);
                else
                {
                    DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "No patient have been selected", "You need to select at least one patient in order to continue.");
                    return false;
                }
            }
            else if (m_NotBIDS.isOn)
            {
                var patients = m_NotBIDSPatientListGestion.List.Objects;
                if (patients.Count > 0)
                    ApplicationState.ProjectLoaded.SetPatients(patients);
                else
                {
                    DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "No patient have been added", "You need to add at least one patient to the list in order to continue.");
                    return false;
                }
            }
            else
            {
                DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "No option selected", "You need to select an option in order to continue.");
                return false;
            }
            return base.OpenNextPanel();
        }
        public override bool OpenPreviousPanel()
        {
            ApplicationState.ProjectLoaded.SetPatients(new Patient[0]);
            return base.OpenPreviousPanel();
        }
        #endregion
    }
}