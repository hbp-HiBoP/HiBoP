using UnityEngine.UI;
using Tools.Unity;
using UnityEngine;

namespace HBP.UI
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
            var preferences = ApplicationState.ProjectLoaded.Preferences.Clone() as Data.ProjectPreferences;
            preferences.Name = m_NameInputField.text;
            ApplicationState.ProjectLoaded.Preferences = preferences;
            FindObjectOfType<ProjectLoaderSaver>().Save(m_LocationFolderSelector.Folder);
            base.OK();
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