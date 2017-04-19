using UnityEngine;

namespace HBP.UI.Settings
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
            RectTransform obj = Instantiate(preferencesPrefab).GetComponent<RectTransform>();
            obj.SetParent(GameObject.Find("Windows").transform);
            obj.localPosition = new Vector3(0, 0, 0);
            obj.GetComponent<Preferences>().Open();
        }
        public void OpenProjectPreferences()
        {
            RectTransform obj = Instantiate(projectPreferences).GetComponent<RectTransform>();
            obj.SetParent(GameObject.Find("Windows").transform);
            obj.localPosition = new Vector3(0, 0, 0);
            obj.GetComponent<ProjectPreferences>().Open();
        }
        #endregion
    }

}
