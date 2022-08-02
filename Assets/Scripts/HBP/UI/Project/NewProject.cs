using UnityEngine;
using UnityEngine.UI;
using Tools.Unity;
using System.Linq;
using System.IO;

namespace HBP.UI
{
    /// <summary>
    /// Manage the New Project window.
    /// </summary>
    public class NewProject : DialogWindow
	{
        #region Properties
		[SerializeField] InputField m_NameInputField;
        [SerializeField] FolderSelector m_ProjectLocationFolderSelector;
        [SerializeField] FolderSelector m_PatientsDatabaseLocationFolderSelector;
        [SerializeField] FolderSelector m_LocalizerDatabaseLocationFolderSelector;

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }

            set
            {
                base.Interactable = value;

                m_NameInputField.interactable = value;
                m_ProjectLocationFolderSelector.interactable = value;
                m_PatientsDatabaseLocationFolderSelector.interactable = value;
                m_LocalizerDatabaseLocationFolderSelector.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void OK()
        {
            if (string.IsNullOrEmpty(m_ProjectLocationFolderSelector.Folder) || !Directory.Exists(m_ProjectLocationFolderSelector.Folder))
            {
                ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.Error, "Directory not found", "Please select a valid directory to save your project file.");
                return;
            }
            if (ApplicationState.ProjectLoaded != null)
            {
                if (ApplicationState.ProjectLoaded.Visualizations.Any(v => v.IsOpen))
                {
                    ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.WarningMultiOptions, "Opened visualizations", "Some visualizations of the currently loaded project are opened. Loading another project will close any opened visualization.\n\nWould you like to load another project ?", () =>
                    {
                        ApplicationState.Module3D.RemoveAllScenes();
                        CreateNewProject();
                    },
                    "Load project");
                }
                else
                {
                    CreateNewProject();
                }
            }
            else
            {
                CreateNewProject();
            }
        }
        #endregion

        #region Private Methods
        protected override void SetFields()
		{
            Core.Data.Preferences.ProjectPreferences preferences = ApplicationState.UserPreferences.General.Project;

            m_NameInputField.text = preferences.DefaultName;
            m_ProjectLocationFolderSelector.Folder = preferences.DefaultLocation;
            m_PatientsDatabaseLocationFolderSelector.Folder = preferences.DefaultPatientDatabase;
            m_LocalizerDatabaseLocationFolderSelector.Folder = preferences.DefaultLocalizerDatabase;
        }
        void CreateNewProject()
        {
            if (new FileInfo(Path.Combine(m_ProjectLocationFolderSelector.Folder, string.Format("{0}.hibop", m_NameInputField.text))).Exists)
            {
                ApplicationState.DialogBoxManager.Open(DialogBoxManager.AlertType.WarningMultiOptions, "Project already exists", string.Format("A project named {0} already exists within the selected directory.\n\nWould you like to override this project?", m_NameInputField.text), () =>
                {
                    Core.Data.ProjectPreferences preferences = new Core.Data.ProjectPreferences(m_NameInputField.text, m_PatientsDatabaseLocationFolderSelector.Folder, m_LocalizerDatabaseLocationFolderSelector.Folder);
                    ApplicationState.ProjectLoaded = new Core.Data.Project(preferences);
                    ApplicationState.ProjectLoadedLocation = m_ProjectLocationFolderSelector.Folder;
                    FindObjectOfType<ProjectLoaderSaver>().SaveAndReload();
                    base.OK();
                },
                "OK");
            }
            else
            {
                Core.Data.ProjectPreferences preferences = new Core.Data.ProjectPreferences(m_NameInputField.text, m_PatientsDatabaseLocationFolderSelector.Folder, m_LocalizerDatabaseLocationFolderSelector.Folder);
                ApplicationState.ProjectLoaded = new Core.Data.Project(preferences);
                ApplicationState.ProjectLoadedLocation = m_ProjectLocationFolderSelector.Folder;
                FindObjectOfType<ProjectLoaderSaver>().SaveAndReload();
                base.OK();
            }
        }
        #endregion
    }
}
