using UnityEngine;

namespace HBP.UI.UserPreferences
{
    public class EditMenu : MonoBehaviour
    {
        #region Public Methods
        public void OpenPreferences()
        {
            ApplicationState.WindowsManager.OpenModifier(ApplicationState.UserPreferences, true);
        }
        public void OpenProjectPreferences()
        {
            var modifier = ApplicationState.WindowsManager.OpenModifier(ApplicationState.ProjectLoaded.Preferences, true);
        }
        #endregion
    }

}
