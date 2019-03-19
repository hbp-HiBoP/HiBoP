using UnityEngine;

namespace HBP.UI
{
    public class ProjectMenu : MonoBehaviour
    {
        #region Public Methods
        public void OpenNewProject()
        {
            ApplicationState.WindowsManager.Open("New project window");
        }
        public void OpenLoadProject()
        {
            ApplicationState.WindowsManager.Open("Open project window");
        }
        public void OpenSaveProjectAs()
        {
            ApplicationState.WindowsManager.Open("Save project as window");
        }
        public void Quit()
        {
            Application.Quit();
        }
        #endregion
    }
}