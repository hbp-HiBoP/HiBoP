using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HBP.Core.Enums;
using HBP.Data.Module3D;
using HBP.Data.Preferences;

namespace HBP.UI.Module3D
{
    /// <summary>
    /// Base class for the UI of the 3D
    /// </summary>
    public class Module3DUI : MonoBehaviour
    {
        #region Properties
        private static Module3DUI m_Instance;

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
        private Dictionary<Base3DScene, Scene3DWindow> m_Scenes = new Dictionary<Base3DScene, Scene3DWindow>();
        public static Dictionary<Base3DScene, Scene3DWindow> Scenes { get { return m_Instance.m_Scenes; } }

        /// <summary>
        /// Prefab for the Scene3DWindow object
        /// </summary>
        [SerializeField] private GameObject m_SceneWindowPrefab;
        #endregion

        #region Private Methods
        private void Awake()
        {
            if (m_Instance == null)
            {
                m_Instance = this;
            }
            else
            {
                Destroy(this);
            }

            m_SiteInfoDisplayer.Initialize();
            m_AtlasInfoDisplayer.Initialize();
            ChangeLayoutDirection();
            
            Module3DMain.OnAddScene.AddListener((scene) =>
            {
                Scene3DWindow sceneWindow = Instantiate(m_SceneWindowPrefab, transform).GetComponent<Scene3DWindow>();
                sceneWindow.Initialize(scene);
                m_SiteInfoDisplayer.transform.SetAsLastSibling();
                sceneWindow.gameObject.SetActive(false);
                m_Scenes.Add(scene, sceneWindow);
            });
            PreferencesManager.UserPreferences.OnSavePreferences.AddListener(ChangeLayoutDirection);
        }
        private void ChangeLayoutDirection()
        {
            DestroyImmediate(gameObject.GetComponent<HorizontalOrVerticalLayoutGroup>());
            HorizontalOrVerticalLayoutGroup layout;
            switch (PreferencesManager.UserPreferences.Visualization._3D.VisualizationsLayoutDirection)
            {
                case LayoutDirection.Horizontal:
                    layout = gameObject.AddComponent<HorizontalLayoutGroup>();
                    break;
                case LayoutDirection.Vertical:
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