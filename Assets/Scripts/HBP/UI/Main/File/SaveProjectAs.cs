using UnityEngine.UI;
using UnityEngine;
using System.IO;
using HBP.Core.Data;
using HBP.UI.Tools;
using HBP.Core.Tools;

namespace HBP.UI.Main
{
    public class SaveProjectAs : DialogWindow
    {
        #region Properties

        [SerializeField] InputField m_NameInputField;
        [SerializeField] FolderSelector m_LocationFolderSelector;

        public override bool Interactable
        {
            get { return base.Interactable; }

            set
            {
                base.Interactable = value;

                m_NameInputField.interactable = value;
                m_LocationFolderSelector.interactable = value;
            }
        }

        #endregion

        #region Public Methods

        public override async void OK()
        {
            bool overwriteConfirmed = true;
            if (new FileInfo(Path.Combine(m_LocationFolderSelector.Folder, string.Format("{0}.hibop", m_NameInputField.text))).Exists)
            {
                int result = await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Warning, "Project already exists", string.Format("A project named {0} already exists within the selected directory.\n\nWould you like to override this project?", m_NameInputField.text), "OK", "Cancel");
                overwriteConfirmed = result == 0;
            }

            ProjectWorkflowResult workflowResult = await ProjectWorkflowService.Default.SaveProjectAsAsync(m_NameInputField.text, m_LocationFolderSelector.Folder, overwriteConfirmed);

            if (workflowResult.Success)
            {
                base.OK();
            }
        }

        #endregion

        #region Private Methods

        protected override void Initialize()
        {
            m_NameInputField.text = ApplicationState.LoadedProject.Name;
            m_LocationFolderSelector.Folder = ApplicationState.LoadedProjectLocation;
        }

        #endregion
    }
}
