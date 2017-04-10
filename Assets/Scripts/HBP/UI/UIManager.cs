/**
 * \file    UIManager.cs
 * \author  Lance Florian
 * \date    29/04/2016
 * \brief   Define UIManager
 */

using UnityEngine;
using System.Collections.Generic;

namespace HBP.VISU3D
{
    /// <summary>
    /// UI manager of the 3D module
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        #region Properties            
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
        #endregion

        #region mono_behaviour

        /// <summary>
        /// This function is always called before any Start functions and also just after a prefab is instantiated. (If a GameObject is inactive during start up Awake is not called until it is made active.)
        /// </summary>
        void Awake()
        {
            // retrieve
            //  camera
            m_backgroundCamera = transform.Find("Background camera").GetComponent<Camera>();

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
            m_UICameraManager.init(scenesManager);

            scenesManager.SPScene.DefineSelectedColumn.AddListener((idColumn) =>
            {
                update_focused_scene_and_column("", true, idColumn);
            });
            scenesManager.MPScene.DefineSelectedColumn.AddListener((idColumn) =>
            {
                update_focused_scene_and_column("MNI", false, idColumn);
            });
        }

        /// <summary>
        /// Update the focused scene and column for the canvas managers
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="idColumn"></param>
        public void update_focused_scene_and_column(string nameScene, bool spScene, int idColumn)
        {
            m_UIOverlayManager.update_focused_scene_and_column(spScene, idColumn);
            m_UICameraManager.UpdateFocusedSceneAndColumn(nameScene, spScene, idColumn);
        }

        /// <summary>
        /// Add a FMRI column in the UI
        /// </summary>
        /// <param name="spScene"></param>
        public void add_fMRI_column(bool spScene, string label)
        {
            UIOverlayManager.add_fMRI_column(spScene, label);
            UICameraManager.add_fMRI_column(spScene, label);
        }

        /// <summary>
        /// Remove the last IRMF column from the UI
        /// </summary>
        /// <param name="spScene"></param>
        public void remove_last_fMRI_column(bool spScene)
        {
            UIOverlayManager.remove_last_fMRI_column(spScene);
            UICameraManager.remove_last_fMRI_column(spScene);
        }

        /// <summary>
        /// Init the UI with iEEG columns
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="iEEGColumns"></param>
        public void set_iEEG_columns(bool spScene, List<HBP.Data.Visualisation.ColumnData> iEEGColumns)
        {
            UIOverlayManager.set_iEEG_columns_nb(spScene, iEEGColumns);
            UICameraManager.set_iEEG_columns_nb(spScene, iEEGColumns);
        }

        #endregion functions

    }
}