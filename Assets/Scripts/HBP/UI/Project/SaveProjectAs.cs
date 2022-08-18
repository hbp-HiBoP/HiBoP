using UnityEngine.UI;
using UnityEngine;
using System.IO;
using HBP.Core.Data;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    public class SaveProjectAs : DialogWindow
    {
        #region Properties
        [SerializeField] InputField m_NameInputField;
        [SerializeField] FolderSelector m_LocationFolderSelector;

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
                m_LocationFolderSelector.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void OK()
        {
            if (new FileInfo(Path.Combine(m_LocationFolderSelector.Folder, string.Format("{0}.hibop", m_NameInputField.text))).Exists)
            {
                DialogBoxManager.Open(DialogBoxManager.AlertType.WarningMultiOptions, "Project already exists", string.Format("A project named {0} already exists within the selected directory.\n\nWould you like to override this project?", m_NameInputField.text), () =>
                {
                    var preferences = ApplicationState.ProjectLoaded.Preferences.Clone() as ProjectPreferences;
                    preferences.Name = m_NameInputField.text;
                    ApplicationState.ProjectLoaded.Preferences = preferences;
                    FindObjectOfType<ProjectLoaderSaver>().Save(m_LocationFolderSelector.Folder);
                    base.OK();
                },
                "OK");
            }
            else
            {
                var preferences = ApplicationState.ProjectLoaded.Preferences.Clone() as ProjectPreferences;
                preferences.Name = m_NameInputField.text;
                ApplicationState.ProjectLoaded.Preferences = preferences;
                FindObjectOfType<ProjectLoaderSaver>().Save(m_LocationFolderSelector.Folder);
                base.OK();
            }
        }
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            m_NameInputField.text = ApplicationState.ProjectLoaded.Preferences.Name;
            m_LocationFolderSelector.Folder = ApplicationState.ProjectLoadedLocation;
        }
        #endregion
    }
}