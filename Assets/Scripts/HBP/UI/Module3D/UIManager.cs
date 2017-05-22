/**
 * \file    UIManager.cs
 * \author  Lance Florian - Adrien Gannerie 
 * \date    29/04/2016 - 2017
 * \brief   Define UIManager
 */
using UnityEngine;
using System.Collections.Generic;
using HBP.Module3D;

namespace HBP.UI.Module3D
{
    /// <summary>
    /// UI manager of the 3D module
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        #region Properties            
        private UIOverlayManager m_OverlayManager = null; /**< manager of the screen space overlay canvas */
        public UIOverlayManager OverlayManager
        {
            get { return m_OverlayManager; }
        }

        private MenuManager m_MenuManager = null;
        public MenuManager MenuManager
        {
            get { return m_MenuManager; }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Init the UI managers
        /// </summary>
        /// <param name="scenesManager"></param>
        public void Initialize(ScenesManager scenesManager)
        {
            //m_OverlayManager.Initialize(scenesManager);
            m_MenuManager.Initialize(scenesManager);
            foreach (Base3DScene scene in scenesManager.Scenes)
            {
                scene.OnSelectColumn.AddListener((column) =>
                {
                    scenesManager.SelectedScene = scene;
                    UpdateFocusedSceneAndColumn(scene, column);
                });
            }
        }
        /// <summary>
        /// Update the focused scene and column for the canvas managers
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="column"></param>
        public void UpdateFocusedSceneAndColumn(Base3DScene scene, int column)
        {
            m_OverlayManager.UpdateFocusedSceneAndColumn(scene, column);
            m_MenuManager.UpdateFocusedSceneAndColumn(scene, column);
        }
        /// <summary>
        /// Add a FMRI column in the UI
        /// </summary>
        /// <param name="spScene"></param>
        public void AddfMRIColumn(SceneType type, string label)
        {
            OverlayManager.AddfMRIColumn(type, label);
            MenuManager.AddfMRIColumn(type, label);
        }
        /// <summary>
        /// Remove the last IRMF column from the UI
        /// </summary>
        /// <param name="spScene"></param>
        public void RemoveLastfMRIColumn(SceneType type)
        {
            OverlayManager.RemoveLastfMRIColumn(type);
            MenuManager.RemoveLastfMRIColumn(type);
        }
        /// <summary>
        /// Init the UI with iEEG columns
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="iEEGColumns"></param>
        public void SetiEEGColumns(SceneType type, List<Data.Visualization.Column> iEEGColumns)
        {
            OverlayManager.SetiEEGColumnsNb(type, iEEGColumns);
            MenuManager.SetiEEGColumnsNb(type, iEEGColumns);
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            m_OverlayManager = transform.GetComponentInChildren<UIOverlayManager>(false);
            m_MenuManager = transform.GetComponentInChildren<MenuManager>(false);
            Initialize(ApplicationState.Module3D.ScenesManager);
        }
        #endregion
    }
}