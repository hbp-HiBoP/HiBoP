using System.Linq;
using Tools.Unity;
using UnityEngine;

namespace HBP.UI.Experience.Dataset
{
    public class ElanDataContainerGestion : MonoBehaviour
    {
        #region Properties
        [SerializeField] FileSelector m_EEGFileSelector, m_POSFileSelector, m_NotesFileSelector;

        bool m_interactable;
        public bool interactable
        {
            get { return m_interactable; }
            set
            {
                m_interactable = value;
                m_EEGFileSelector.interactable = value;
                m_POSFileSelector.interactable = value;
                m_NotesFileSelector.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public void SetActive(bool active)
        {
            gameObject.SetActive(active);
        }
        public void Set(Data.Experience.Dataset.ElanDataContainer dataContainer)
        {
            // EEG.
            m_EEGFileSelector.DefaultDirectory = !string.IsNullOrEmpty(dataContainer.EEG) ? dataContainer.EEG : ApplicationState.ProjectLoaded.Settings.LocalizerDatabase;
            m_EEGFileSelector.File = dataContainer.SavedEEG;
            m_EEGFileSelector.onValueChanged.RemoveAllListeners();
            m_EEGFileSelector.onValueChanged.AddListener((eeg) =>
            {
                dataContainer.EEG = eeg;
                SetDefaultDirectories(dataContainer, eeg);
            });

            // POS.
            m_POSFileSelector.DefaultDirectory = !string.IsNullOrEmpty(dataContainer.POS) ? dataContainer.POS : ApplicationState.ProjectLoaded.Settings.LocalizerDatabase;
            m_POSFileSelector.File = dataContainer.SavedPOS;
            m_POSFileSelector.onValueChanged.RemoveAllListeners();
            m_POSFileSelector.onValueChanged.AddListener((pos) =>
            {
                dataContainer.POS = pos;
                SetDefaultDirectories(dataContainer, pos);
            });

            // Notes.
            m_NotesFileSelector.DefaultDirectory = !string.IsNullOrEmpty(dataContainer.Notes) ? dataContainer.Notes : ApplicationState.ProjectLoaded.Settings.LocalizerDatabase;
            m_NotesFileSelector.File = dataContainer.SavedNotes;
            m_NotesFileSelector.onValueChanged.RemoveAllListeners();
            m_NotesFileSelector.onValueChanged.AddListener((notes) =>
            {
                dataContainer.Notes = notes;
                SetDefaultDirectories(dataContainer, notes);
            });
        }
        void SetDefaultDirectories(Data.Experience.Dataset.ElanDataContainer dataInfo, string path)
        {
            if (!dataInfo.Errors.Contains(Data.Experience.Dataset.DataInfo.ErrorType.RequiredFieldEmpty) &&
                !dataInfo.Errors.Contains(Data.Experience.Dataset.DataInfo.ErrorType.FileDoesNotExist) &&
                !dataInfo.Errors.Contains(Data.Experience.Dataset.DataInfo.ErrorType.WrongExtension))
            {
                m_EEGFileSelector.DefaultDirectory = path;
                m_POSFileSelector.DefaultDirectory = path;
                m_NotesFileSelector.DefaultDirectory = path;
            }
        }
        #endregion
    }
}