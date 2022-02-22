using HBP.Module3D;
using System.Collections.Generic;
using Tools.Unity;
using UnityEngine;
using UnityEngine.UI;

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
            ChangeLayoutDirection();
            
            ApplicationState.Module3D.OnAddScene.AddListener((scene) =>
            {
                Scene3DWindow sceneWindow = Instantiate(m_SceneWindowPrefab, transform).GetComponent<Scene3DWindow>();
                sceneWindow.Initialize(scene);
                m_SiteInfoDisplayer.transform.SetAsLastSibling();
                sceneWindow.gameObject.SetActive(false);
                Scenes.Add(scene, sceneWindow);
            });
            ApplicationState.UserPreferences.OnSavePreferences.AddListener(ChangeLayoutDirection);
        }
        private void ChangeLayoutDirection()
        {
            DestroyImmediate(gameObject.GetComponent<HorizontalOrVerticalLayoutGroup>());
            HorizontalOrVerticalLayoutGroup layout;
            switch (ApplicationState.UserPreferences.Visualization._3D.VisualizationsLayoutDirection)
            {
                case Data.Enums.LayoutDirection.Horizontal:
                    layout = gameObject.AddComponent<HorizontalLayoutGroup>();
                    break;
                case Data.Enums.LayoutDirection.Vertical:
                    layout = gameObject.AddComponent<VerticalLayoutGroup>();
                    break;
                default:
                    layout = gameObject.AddComponent<VerticalLayoutGroup>();
                    break;
            }
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = true;
            layout.spacing = 1;
        }
        #endregion
    }
}