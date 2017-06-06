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
        public GameObject SceneUIPrefab;
        public GameObject CutUIPrefab;
        //public GameObject GraphsUIPrefab;
        #endregion

        #region Private Methods

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
            //grid.AddColumn();
            grid.VerticalHandlers[0].MagneticPosition = 0.9f;
            grid.VerticalHandlers[0].Position = 0.9f;
        }
        #endregion
    }
}