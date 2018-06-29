using UnityEngine;

namespace HBP.UI.Preferences
{
    public class EditMenu : MonoBehaviour
    {
        #region Properties
        [SerializeField]
        GameObject preferencesPrefab;
        [SerializeField]
        GameObject projectPreferences;
        #endregion

        #region Public Methods
        public void OpenPreferences()
        {
            UserPreferences.Open(true);
        }
        public void OpenProjectPreferences()
        {
            ProjectPreferences.Open(true);
        }
        #endregion
    }

}
