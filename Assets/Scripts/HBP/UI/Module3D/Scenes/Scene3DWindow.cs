using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tools.Unity.ResizableGrid;
using UnityEngine;

namespace HBP.UI.Module3D
{
    public class Scene3DWindow : MonoBehaviour
    {
        #region Properties
        private Base3DScene m_Scene;
        public GameObject SceneUIPrefab;
        public GameObject CutUIPrefab;
        public GameObject GraphsUIPrefab;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the scene window
        /// </summary>
        /// <param name="scene">Associated Base3DScene</param>
        public void Initialize(Base3DScene scene)
        {
            ResizableGrid grid = GetComponent<ResizableGrid>();
            grid.AddColumn();
            grid.AddViewLine(SceneUIPrefab);
            grid.Columns.Last().Views.Last().GetComponent<Scene3DUI>().Initialize(scene);
            grid.AddColumn(null, CutUIPrefab);
            grid.Columns.Last().Views.Last().GetComponent<CutController>().Initialize(scene);
            grid.AddColumn(null, GraphsUIPrefab);
            grid.Columns.Last().Views.Last().GetComponent<Graph.GraphsGestion>().Scene = scene;
            grid.VerticalHandlers[0].MagneticPosition = 0.9f;
            grid.VerticalHandlers[0].Position = 0.9f;
            grid.VerticalHandlers[1].MagneticPosition = 0.5f;
            grid.VerticalHandlers[1].Position = 1.0f;

            ApplicationState.Module3D.OnRemoveScene.AddListener((s) =>
            {
                if (s == scene)
                {
                    Destroy(gameObject);
                }
            });
            scene.OnChangeVisibleState.AddListener((value) =>
            {
                gameObject.SetActive(value);
            });
        }
        #endregion
    }
}