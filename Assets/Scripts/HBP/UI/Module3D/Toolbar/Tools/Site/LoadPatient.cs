﻿using UnityEngine;
using UnityEngine.UI;
using HBP.Core.Enums;
using HBP.Display.Module3D;

namespace HBP.UI.Toolbar
{
    public class LoadPatient : Tool
    {
        #region Properties
        /// <summary>
        /// Button to load a single patient visualization from the selected site
        /// </summary>
        [SerializeField] private Button m_Button;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_Button.onClick.AddListener(() =>
            {
                Module3DMain.LoadSinglePatientSceneFromMultiPatientScene(SelectedScene.Visualization, SelectedColumn.SelectedSite.Information.Patient);
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_Button.interactable = false;
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            bool isInteractable = (SelectedColumn.SelectedSite != null) && (SelectedScene.Type == SceneType.MultiPatients);

            m_Button.interactable = isInteractable;
        }
        #endregion
    }
}