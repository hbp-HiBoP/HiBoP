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
        public override container.Elan Object
        {
            get => base.Object;
            set
            {
                base.Object = value;
                m_EEGFileSelector.DefaultDirectory = !string.IsNullOrEmpty(value.EEG) ? value.EEG : ApplicationState.ProjectLoaded.Settings.LocalizerDatabase;
                m_EEGFileSelector.File = value.SavedEEG;

                m_POSFileSelector.DefaultDirectory = !string.IsNullOrEmpty(value.POS) ? value.POS : ApplicationState.ProjectLoaded.Settings.LocalizerDatabase;
                m_POSFileSelector.File = value.SavedPOS;

                m_NotesFileSelector.DefaultDirectory = !string.IsNullOrEmpty(value.Notes) ? value.Notes : ApplicationState.ProjectLoaded.Settings.LocalizerDatabase;
                m_NotesFileSelector.File = value.SavedNotes;
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();

            m_EEGFileSelector.onValueChanged.AddListener((eeg) => { m_Object.EEG = eeg; SetDefaultDirectories(m_Object, eeg); });
            m_POSFileSelector.onValueChanged.AddListener((pos) => { m_Object.POS = pos; SetDefaultDirectories(m_Object, pos); });
            m_NotesFileSelector.onValueChanged.AddListener((notes) => { m_Object.Notes = notes; SetDefaultDirectories(m_Object, notes); });
        }
        void SetDefaultDirectories(container.Elan dataInfo, string path)
        {
            m_EEGFileSelector.DefaultDirectory = path;
            m_POSFileSelector.DefaultDirectory = path;
            m_NotesFileSelector.DefaultDirectory = path;
        }
        #endregion
    }
}