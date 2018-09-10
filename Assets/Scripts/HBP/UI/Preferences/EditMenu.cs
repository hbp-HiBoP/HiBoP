using UnityEngine;

namespace HBP.UI.Preferences
{
    public class EditMenu : MonoBehaviour
    {
        #region Public Methods
        public void OpenPreferences()
        {
            ApplicationState.WindowsManager.Open("User preferences window");
        }
        public void OpenProjectPreferences()
        {
            ApplicationState.WindowsManager.Open("Project preferences window");
        }
        #endregion
    }

}
