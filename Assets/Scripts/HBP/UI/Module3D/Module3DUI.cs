using HBP.Module3D;
using System.Collections.Generic;
using Tools.Unity;
using UnityEngine;

namespace HBP.UI.Module3D
{
    /// <summary>
    /// Base class for the UI of the 3D
    /// </summary>
    public class Module3DUI : MonoBehaviour
    {
        #region Properties
        /// <summary>
        /// Reference to the SiteInfoDisplayer of the software
        /// </summary>
        [SerializeField] private SiteInfoDisplayer m_SiteInfoDisplayer;
        /// <summary>
        /// Reference to the AtlasInfoDisplayer of the software
        /// </summary>
        [SerializeField] private AtlasInfoDisplayer m_AtlasInfoDisplayer;

        [SerializeField] private ColorPicker m_ColorPicker;
        /// <summary>
        /// Reference to the ColorPicker of the software
        /// </summary>
        public ColorPicker ColorPicker
        {
            get
            {
                return m_ColorPicker;
            }
        }

        /// <summary>
        /// Dictionary containing all scene windows by 3D scene
        /// </summary>
        public Dictionary<Base3DScene, Scene3DWindow> Scenes { get; private set; } = new Dictionary<Base3DScene, Scene3DWindow>();

        /// <summary>
        /// Prefab for the Scene3DWindow object
        /// </summary>
        [SerializeField] private GameObject m_SceneWindowPrefab;
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_SiteInfoDisplayer.Initialize();
            m_AtlasInfoDisplayer.Initialize();
            
            ApplicationState.Module3D.OnAddScene.AddListener((scene) =>
            {
                Scene3DWindow sceneWindow = Instantiate(m_SceneWindowPrefab, transform).GetComponent<Scene3DWindow>();
                sceneWindow.Initialize(scene);
                m_SiteInfoDisplayer.transform.SetAsLastSibling();
                sceneWindow.gameObject.SetActive(false);
                Scenes.Add(scene, sceneWindow);
            });
        }
        #endregion
    }
}