using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.IO;
using HBP.Core.Data;
using HBP.Data.Module3D;
using HBP.UI.Tools;
using HBP.Core.Tools;
using HBP.Data.Preferences;

namespace HBP.UI.Main
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
        public override async void OK()
        {
            if (string.IsNullOrEmpty(m_ProjectLocationFolderSelector.Folder) || !Directory.Exists(m_ProjectLocationFolderSelector.Folder))
            {
                DialogBoxManager.Open(Core.Enums.DialogBoxType.Error, "Directory not found", "Please select a valid directory to save your project file.").Forget();
                return;
            }
            if (ApplicationState.LoadedProject != null)
            {
                if (ApplicationState.LoadedProject.Visualizations.Any(v => Module3DMain.Visualizations.Contains(v)))
                {
                    int result = await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Warning, "Opened visualizations", "Some visualizations of the currently loaded project are opened. Loading another project will close any opened visualization.\n\nWould you like to load another project ?", "Load project", "Cancel");
                    if (result == 0)
                    {
                        Module3DMain.RemoveAllScenes();
                        CreateNewProject();
                    }
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
            Data.Preferences.ProjectPreferences preferences = PersistentDataManager.UserPreferences.General.Project;

            m_NameInputField.text = preferences.DefaultName;
            m_ProjectLocationFolderSelector.Folder = preferences.DefaultLocation;
        }
        async void CreateNewProject()
        {
            if (new FileInfo(Path.Combine(m_ProjectLocationFolderSelector.Folder, string.Format("{0}.hibop", m_NameInputField.text))).Exists)
            {
                int result = await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Warning, "Project already exists", string.Format("A project named {0} already exists within the selected directory.\n\nWould you like to override this project?", m_NameInputField.text), "OK", "Cancel");
                if (result == 0)
                {
                    Core.Data.ProjectPreferences preferences = new Core.Data.ProjectPreferences();
                    ApplicationState.LoadedProject = new Project(m_NameInputField.text, preferences);
                    ApplicationState.LoadedProjectLocation = m_ProjectLocationFolderSelector.Folder;
                    ProjectLoaderSaver.SaveAndReload().Forget();
                    base.OK();
                }
            }
            else
            {
                Core.Data.ProjectPreferences preferences = new Core.Data.ProjectPreferences();
                ApplicationState.LoadedProject = new Project(m_NameInputField.text, preferences);
                ApplicationState.LoadedProjectLocation = m_ProjectLocationFolderSelector.Folder;
                ProjectLoaderSaver.SaveAndReload().Forget();
                base.OK();
            }
        }
        #endregion
    }
}
