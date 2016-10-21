using UnityEngine;

namespace HBP.UI
{
    public class ProjectMenu : MonoBehaviour
    {
        #region Properties
        [SerializeField]
        GameObject newProjectPrefab;
        [SerializeField]
        GameObject loadProjectPrefab;
        [SerializeField]
        GameObject saveProjectAsPrefab;
        #endregion

        #region Public Methods
        public void OpenNewProject()
        {
            RectTransform obj = Instantiate(newProjectPrefab).GetComponent<RectTransform>();
            obj.SetParent(GameObject.Find("Windows").transform);
            obj.localPosition = new Vector3(0, 0, 0);
            obj.GetComponent<HBP.UI.NewProject>().Open();
        }
        public void OpenLoadProject()
        {
            RectTransform obj = Instantiate(loadProjectPrefab).GetComponent<RectTransform>();
            obj.SetParent(GameObject.Find("Windows").transform);
            obj.localPosition = new Vector3(0, 0, 0);
            obj.GetComponent<HBP.UI.OpenProject>().Open();
        }
        public void OpenSaveProjectAs()
        {
            RectTransform obj = Instantiate(saveProjectAsPrefab).GetComponent<RectTransform>();
            obj.SetParent(GameObject.Find("Windows").transform);
            obj.localPosition = new Vector3(0, 0, 0);
            obj.GetComponent<HBP.UI.SaveProjectAs>().Open();
        }
        #endregion
    }
}