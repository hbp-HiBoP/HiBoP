
/**
 * \file    UIManager.cs
 * \author  Lance Florian
 * \date    29/04/2016
 * \brief   Define UIManager
 */

// system
using System;
using System.Collections;
using System.Collections.Generic;

// unity
using UnityEngine;
using UnityEngine.UI;

namespace HBP.VISU3D
{
    /// <summary>
    /// UI manager of the 3D module
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        #region members          
        
        private Camera m_backgroundCamera = null; /**< camera used with the canvas */
        public Camera BackgroundCamera
        {
            get { return m_backgroundCamera; }
        }

        private UIOverlayManager m_UIOverlayManager = null; /**< manager of the screen space overlay canvas */
        public UIOverlayManager UIOverlayManager
        {
            get { return m_UIOverlayManager; }
        }

        private UICameraManager m_UICameraManager = null; /**< manager of the screen space camera canvas */
        public UICameraManager UICameraManager
        {
            get { return m_UICameraManager; }
        }

        #endregion members

        #region mono_behaviour

        /// <summary>
        /// This function is always called before any Start functions and also just after a prefab is instantiated. (If a GameObject is inactive during start up Awake is not called until it is made active.)
        /// </summary>
        void Awake()
        {
            // retrieve
            //  camera
            m_backgroundCamera = transform.Find("background camera").GetComponent<Camera>();
            //  managers
            m_UIOverlayManager = transform.Find("managers").Find("overlay").GetComponent<UIOverlayManager>();
            m_UICameraManager = transform.Find("managers").Find("camera").GetComponent<UICameraManager>();
        }

        #endregion mono_behaviour   

        #region functions

        /// <summary>
        /// Init the UI managers
        /// </summary>
        /// <param name="scenesManager"></param>
        public void init(ScenesManager scenesManager)
        {
            m_UIOverlayManager.init(scenesManager, transform.Find("canvas").Find("overlay").GetComponent<Canvas>());
            m_UICameraManager.init(scenesManager, transform.Find("canvas").Find("camera").GetComponent<Canvas>());

            scenesManager.SPScene.DefineSelectedColumn.AddListener((idColumn) =>
            {
                updateFocusedSceneAndColumn(true, idColumn);
            });
            scenesManager.MPScene.DefineSelectedColumn.AddListener((idColumn) =>
            {
                updateFocusedSceneAndColumn(false, idColumn);
            });
        }

        /// <summary>
        /// Update the focused scene and column for the canvas managers
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="idColumn"></param>
        public void updateFocusedSceneAndColumn(bool spScene, int idColumn)
        {
            m_UIOverlayManager.updatFocusedSceneAndColumn(spScene, idColumn);
            m_UICameraManager.updateFocusedSceneAndColumn(spScene, idColumn);
        }

        /// <summary>
        /// Add an IRMF column in the UI
        /// </summary>
        /// <param name="spScene"></param>
        public void addIRMFColumn(bool spScene)
        {
            UIOverlayManager.addIRMFColumn(spScene);
            UICameraManager.addIRMFColumn(spScene);
        }

        /// <summary>
        /// Remove the last IRMF column from the UI
        /// </summary>
        /// <param name="spScene"></param>
        public void removeLastIRMFColumn(bool spScene)
        {
            UIOverlayManager.removeLastIRMFColumn(spScene);
            UICameraManager.removeLastIRMFColumn(spScene);
        }

        /// <summary>
        /// Init the UI with iEEG columns
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="iEEGColumns"></param>
        public void setIEEGColumns(bool spScene, List<HBP.Data.Visualisation.ColumnData> iEEGColumns)
        {
            UIOverlayManager.setIEEGColumnsNb(spScene, iEEGColumns);
            UICameraManager.setIEEGColumnsNb(spScene, iEEGColumns.Count);
        }

        #endregion functions

    }
}