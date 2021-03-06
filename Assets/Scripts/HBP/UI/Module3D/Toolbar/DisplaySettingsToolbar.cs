﻿using HBP.UI.Module3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using UnityEngine.UI;

namespace HBP.UI.Module3D
{
    public class DisplaySettingsToolbar : Toolbar
    {
        #region Properties
        /// <summary>
        /// Handle automatic rotation
        /// </summary>
        [SerializeField] private Tools.AutoRotate m_AutoRotate;
        /// <summary>
        /// Add / remove views from the selected scene
        /// </summary>
        [SerializeField] private Tools.Views m_Views;
        /// <summary>
        /// Set the scene to the standard views
        /// </summary>
        [SerializeField] private Tools.StandardViews m_StandardViews;
        /// <summary>
        /// Set the scene to the standard views
        /// </summary>
        [SerializeField] private Tools.DefaultView m_DefaultView;
        /// <summary>
        /// Set the scene to the standard views
        /// </summary>
        [SerializeField] private Tools.ResetViews m_ResetViews;
        /// <summary>
        /// Set the camera control type
        /// </summary>
        [SerializeField] private Tools.CameraTypes m_CameraTypes;
        /// <summary>
        /// Take a screenshot of the selected scene
        /// </summary>
        [SerializeField] private Tools.Screenshot m_Screenshot;
        #endregion

        #region Private Methods
        /// <summary>
        /// Link elements to the toolbar
        /// </summary>
        protected override void AddTools()
        {
            m_Tools.Add(m_AutoRotate);
            m_Tools.Add(m_Views);
            m_Tools.Add(m_StandardViews);
            m_Tools.Add(m_DefaultView);
            m_Tools.Add(m_ResetViews);
            m_Tools.Add(m_CameraTypes);
            m_Tools.Add(m_Screenshot);
        }
        /// <summary>
        /// Add the listeners to the elements of the toolbar
        /// </summary>
        protected override void AddListeners()
        {
            base.AddListeners();

            m_Views.OnClick.AddListener(() =>
            {
                ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
            });

            m_StandardViews.OnClick.AddListener(() =>
            {
                ApplicationState.Module3D.OnRequestUpdateInToolbar.Invoke();
            });
        }
        #endregion
    }
}