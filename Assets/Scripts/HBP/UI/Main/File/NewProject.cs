using UnityEngine;
using UnityEngine.UI;
using System.Linq;
using System.IO;
using HBP.Core.Data;
using HBP.Data.Module3D;
using HBP.UI.Tools;
using HBP.Core.Tools;
using HBP.Core.Preferences;
using Cysharp.Threading.Tasks;

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
                        await CreateNewProjectAsync();
                    }
                }
                else
                {
                    await CreateNewProjectAsync();
                }
            }
            else
            {
                await CreateNewProjectAsync();
            }
        }
        #endregion

        #region Private Methods
        protected override void SetFields()
		{
            Core.Preferences.ProjectPreferences preferences = PersistentDataManager.UserPreferences.General.Project;

            m_NameInputField.text = preferences.DefaultName;
            m_ProjectLocationFolderSelector.Folder = preferences.DefaultLocation;
        }
        async UniTask CreateNewProjectAsync()
        {
            bool overwriteConfirmed = true;
            if (new FileInfo(Path.Combine(m_ProjectLocationFolderSelector.Folder, string.Format("{0}.hibop", m_NameInputField.text))).Exists)
            {
                int result = await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Warning, "Project already exists", string.Format("A project named {0} already exists within the selected directory.\n\nWould you like to override this project?", m_NameInputField.text), "OK", "Cancel");
                overwriteConfirmed = result == 0;
            }

            ProjectWorkflowResult workflowResult = await ProjectWorkflowService.Default.CreateNewProjectAsync(
                m_NameInputField.text,
                m_ProjectLocationFolderSelector.Folder,
                overwriteConfirmed);

            if (workflowResult.Success)
            {
                base.OK();
            }
        }
        #endregion
    }
}
