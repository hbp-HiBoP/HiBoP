using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HBP.Data.Visualization;
using HBP.Data;
using HBP.Data.Experience.Dataset;
using HBP.Data.Experience.Protocol;
using HBP.Data.General;
using System.Linq;
using UnityEngine.EventSystems;
using T = Tools.Unity;

namespace HBP.UI.Module3D
{
    public class Module3DUI : MonoBehaviour
    {
        #region Properties
        [SerializeField]
        private SiteInfoDisplayer m_SiteInfoDisplayer;

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
        /// <summary>
        /// DELETEME : DEBUG
        /// </summary>
        private void Update()
        {
            if (Input.GetKey(KeyCode.W))
            {
                Debug.Log(Input.mouseScrollDelta);
            }
        }
        #endregion
    }
}