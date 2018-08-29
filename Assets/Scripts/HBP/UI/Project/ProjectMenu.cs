using UnityEngine;

namespace HBP.UI
{
    public class ProjectMenu : MonoBehaviour
    {
        #region Public Methods
        public void OpenNewProject()
        {
            ApplicationState.WindowsManager.Open("New Project Window");
        }
        public void OpenLoadProject()
        {
            ApplicationState.WindowsManager.Open("Open Project Window");
        }
        public void OpenSaveProjectAs()
        {
            ApplicationState.WindowsManager.Open("Save Project As Window");
        }
        #endregion
    }
}