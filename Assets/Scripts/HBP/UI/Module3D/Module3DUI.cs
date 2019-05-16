using Tools.Unity;
using UnityEngine;

namespace HBP.UI.Module3D
{
    public class Module3DUI : MonoBehaviour
    {
        #region Properties
        [SerializeField] private SiteInfoDisplayer m_SiteInfoDisplayer;
        public ColorPicker ColorPicker;

        public GameObject SceneWindowPrefab;
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_SiteInfoDisplayer.Initialize();
            
            ApplicationState.Module3D.OnAddScene.AddListener((scene) =>
            {
                Scene3DWindow sceneWindow = Instantiate(SceneWindowPrefab, transform).GetComponentInChildren<Scene3DWindow>();
                sceneWindow.Initialize(scene);
                m_SiteInfoDisplayer.transform.SetAsLastSibling();
                sceneWindow.gameObject.SetActive(false);
            });
        }
        #endregion
    }
}