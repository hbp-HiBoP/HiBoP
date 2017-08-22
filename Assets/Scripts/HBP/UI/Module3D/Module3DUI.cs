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
                Scene3DWindow sceneWindow = Instantiate(SceneWindowPrefab, transform).GetComponent<Scene3DWindow>();
                sceneWindow.Initialize(scene);
                m_SiteInfoDisplayer.transform.SetAsLastSibling();
            });
        }
        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                ApplicationState.Module3D.SelectedScene.SaveConfiguration();
            }
            if (Input.GetKeyDown(KeyCode.B))
            {
                ApplicationState.Module3D.SelectedScene.ResetConfiguration();
            }
            if (Input.GetKeyDown(KeyCode.C))
            {
                ApplicationState.Module3D.SelectedScene.LoadConfiguration();
            }
            if (Input.GetKeyDown(KeyCode.D))
            {
                ApplicationState.Module3D.SelectedScene.AddCutPlane();
            }
        }
        #endregion
    }
}