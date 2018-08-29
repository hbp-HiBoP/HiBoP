using UnityEngine;

namespace HBP.UI.Preferences
{
    public class EditMenu : MonoBehaviour
    {
        #region Public Methods
        public void OpenPreferences()
        {
            ApplicationState.WindowsManager.Open("User Preferences Window");
        }
        public void OpenProjectPreferences()
        {
            ApplicationState.WindowsManager.Open("Project Preferences Window");
        }
        #endregion
    }

}
