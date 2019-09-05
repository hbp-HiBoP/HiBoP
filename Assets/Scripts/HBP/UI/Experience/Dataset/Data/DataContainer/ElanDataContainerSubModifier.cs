using Tools.Unity;
using UnityEngine;
using container = HBP.Data.Container;

namespace HBP.UI.Experience.Dataset
{
    public class ElanDataContainerSubModifier : SubModifier<container.Elan>
    {
        #region Properties
        [SerializeField] FileSelector m_EEGFileSelector, m_POSFileSelector, m_NotesFileSelector;

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }
            set
            {
                base.Interactable = value;
                m_EEGFileSelector.interactable = value;
                m_POSFileSelector.interactable = value;
                m_NotesFileSelector.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();

            m_EEGFileSelector.onValueChanged.AddListener((eeg) => { Object.EEG = eeg; SetDefaultDirectories(Object, eeg); });
            m_POSFileSelector.onValueChanged.AddListener((pos) => { Object.POS = pos; SetDefaultDirectories(Object, pos); });
            m_NotesFileSelector.onValueChanged.AddListener((notes) => { Object.Notes = notes; SetDefaultDirectories(Object, notes); });
        }
        void SetDefaultDirectories(container.Elan dataInfo, string path)
        {
            m_EEGFileSelector.DefaultDirectory = path;
            m_POSFileSelector.DefaultDirectory = path;
            m_NotesFileSelector.DefaultDirectory = path;
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(container.Elan objectToDisplay)
        {
            m_EEGFileSelector.DefaultDirectory = !string.IsNullOrEmpty(objectToDisplay.EEG) ? objectToDisplay.EEG : ApplicationState.ProjectLoaded.Settings.LocalizerDatabase;
            m_EEGFileSelector.File = objectToDisplay.SavedEEG;

            m_POSFileSelector.DefaultDirectory = !string.IsNullOrEmpty(objectToDisplay.POS) ? objectToDisplay.POS : ApplicationState.ProjectLoaded.Settings.LocalizerDatabase;
            m_POSFileSelector.File = objectToDisplay.SavedPOS;

            m_NotesFileSelector.DefaultDirectory = !string.IsNullOrEmpty(objectToDisplay.Notes) ? objectToDisplay.Notes : ApplicationState.ProjectLoaded.Settings.LocalizerDatabase;
            m_NotesFileSelector.File = objectToDisplay.SavedNotes;
        }
        #endregion
    }
}