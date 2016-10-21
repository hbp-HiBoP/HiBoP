using UnityEngine.UI;
using Tools.Unity;

namespace HBP.UI
{
    public class SaveProjectAs : Window
    {
        #region Properties
        InputField nameInputField;
        FolderSelector locationFolderSelector;
        #endregion

        #region Public Methods
        public void SaveAs()
        {
            Data.Settings.ProjectSettings l_ps = new Data.Settings.ProjectSettings(nameInputField.text, ApplicationState.ProjectLoaded.Settings.PatientDatabase, ApplicationState.ProjectLoaded.Settings.LocalizerDatabase);
            ApplicationState.ProjectLoaded.Settings = l_ps;
            FindObjectOfType<ProjectLoaderSaver>().Save(locationFolderSelector.Folder);
            base.Close();
        }
        #endregion

        #region Private Methods
        protected override void SetWindow()
        {
            nameInputField = transform.Find("Content").FindChild("Name").FindChild("InputField").GetComponent<InputField>();
            locationFolderSelector = transform.Find("Content").FindChild("Location").FindChild("FolderSelector").GetComponent<FolderSelector>();
            nameInputField.text = ApplicationState.ProjectLoaded.Settings.Name;
            locationFolderSelector.Folder = ApplicationState.ProjectLoadedLocation;
        }
        #endregion
    }
}