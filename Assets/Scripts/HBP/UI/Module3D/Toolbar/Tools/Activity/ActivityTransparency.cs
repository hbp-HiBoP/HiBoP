using HBP.Display.Module3D;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Toolbar
{
    public class ActivityTransparency : Tool
    {
        #region Properties
        /// <summary>
        /// Slider to control the alpha of the iEEG on the brain
        /// </summary>
        [SerializeField] private Slider m_Slider;
        /// <summary>
        /// Are the changes applied to all columns ?
        /// </summary>
        public bool IsGlobal { get; set; }
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_Slider.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                List<Column3D> columns = IsGlobal ? SelectedScene.Columns : new List<Column3D>() { SelectedColumn };
                foreach (var column in columns)
                {
                    column.ActivityAlpha = value;
                }
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_Slider.value = 0.8f;
            m_Slider.interactable = false;
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            m_Slider.interactable = true;
        }
        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public override void UpdateStatus()
        {
            m_Slider.value = SelectedColumn.ActivityAlpha;
        }
        #endregion
    }
}