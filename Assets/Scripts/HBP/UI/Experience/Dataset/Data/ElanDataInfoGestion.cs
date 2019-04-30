using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tools.Unity;
using UnityEngine;

namespace HBP.UI.Experience.Dataset
{
    public class ElanDataInfoGestion : MonoBehaviour
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
        public void Set(Data.Experience.Dataset.ElanDataInfo dataInfo)
        {
            // EEG.
            m_EEGFileSelector.DefaultDirectory = !string.IsNullOrEmpty(dataInfo.EEG) ? dataInfo.EEG : ApplicationState.ProjectLoaded.Settings.LocalizerDatabase;
            m_EEGFileSelector.File = dataInfo.SavedEEG;
            m_EEGFileSelector.onValueChanged.RemoveAllListeners();
            m_EEGFileSelector.onValueChanged.AddListener((eeg) =>
            {
                dataInfo.EEG = eeg;
                SetDefaultDirectories(dataInfo, eeg);
            });

            // POS.
            m_POSFileSelector.DefaultDirectory = !string.IsNullOrEmpty(dataInfo.POS) ? dataInfo.POS : ApplicationState.ProjectLoaded.Settings.LocalizerDatabase;
            m_POSFileSelector.File = dataInfo.SavedPOS;
            m_POSFileSelector.onValueChanged.RemoveAllListeners();
            m_POSFileSelector.onValueChanged.AddListener((pos) =>
            {
                dataInfo.POS = pos;
                SetDefaultDirectories(dataInfo, pos);
            });

            // Notes.
            m_NotesFileSelector.DefaultDirectory = !string.IsNullOrEmpty(dataInfo.Notes) ? dataInfo.Notes : ApplicationState.ProjectLoaded.Settings.LocalizerDatabase;
            m_NotesFileSelector.File = dataInfo.SavedNotes;
            m_NotesFileSelector.onValueChanged.RemoveAllListeners();
            m_NotesFileSelector.onValueChanged.AddListener((notes) =>
            {
                dataInfo.Notes = notes;
                SetDefaultDirectories(dataInfo, notes);
            });
        }
        void SetDefaultDirectories(Data.Experience.Dataset.ElanDataInfo dataInfo, string path)
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