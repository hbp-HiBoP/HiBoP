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
            NewProject.Open(true);
        }
        public void OpenLoadProject()
        {
            OpenProject.Open(true);
        }
        public void OpenSaveProjectAs()
        {
            SaveProjectAs.Open(true);
        }
        #endregion
    }
}