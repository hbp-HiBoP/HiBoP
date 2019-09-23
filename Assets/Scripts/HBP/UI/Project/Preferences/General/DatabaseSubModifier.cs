using HBP.Data.General;
using Tools.Unity;
using UnityEngine;

namespace HBP.UI.General
{
    public class DatabaseSubModifier : SubModifier<ProjectSettings>
    {
        #region Properties
        [SerializeField] FolderSelector m_PatientsDatabaseFolderSelector;
        [SerializeField] FolderSelector m_LocalizerDatabaseFolderSelector;
        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;
                m_PatientsDatabaseFolderSelector.interactable = value;
                m_LocalizerDatabaseFolderSelector.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();
            m_PatientsDatabaseFolderSelector.onValueChanged.AddListener((database) => Object.PatientDatabase = database);
            m_LocalizerDatabaseFolderSelector.onValueChanged.AddListener((database) => Object.LocalizerDatabase = database);
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(ProjectSettings objectToDisplay)
        {
            base.SetFields(objectToDisplay);
            m_PatientsDatabaseFolderSelector.Folder = objectToDisplay.PatientDatabase;
            m_LocalizerDatabaseFolderSelector.Folder = objectToDisplay.LocalizerDatabase;
        }
        #endregion
    }
}