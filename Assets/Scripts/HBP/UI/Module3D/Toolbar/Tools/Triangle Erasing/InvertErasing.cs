﻿using UnityEngine;
using UnityEngine.UI;
using HBP.Core.Data.Enums;

namespace HBP.UI.Module3D.Tools
{
    public class InvertErasing : Tool
    {
        #region Properties
        /// <summary>
        /// Invert the erased area
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
                if (ListenerLock) return;

                SelectedScene.TriangleEraser.CurrentMode = TriEraserMode.Invert;
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
            m_Button.interactable = true;
        }
        #endregion
    }
}