using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HBP.Core.Data.Enums;

namespace HBP.UI.Module3D.Tools
{
    public class Colormap : Tool
    {
        #region Properties
        /// <summary>
        /// Dropdown to select the colormap
        /// </summary>
        [SerializeField] private Dropdown m_Dropdown;
        /// <summary>
        /// Correspondance between colormap dropdown options indices and color type
        /// </summary>
        private List<ColorType> m_ColormapIndices = new List<ColorType>() { ColorType.Grayscale, ColorType.Hot, ColorType.Winter, ColorType.Warm, ColorType.Surface, ColorType.Cool, ColorType.RedYellow, ColorType.BlueGreen, ColorType.ACTC, ColorType.Bone, ColorType.GEColor, ColorType.Gold, ColorType.XRain, ColorType.MatLab };
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_Dropdown.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                SelectedScene.Colormap = m_ColormapIndices[value];
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_Dropdown.value = 13;
            m_Dropdown.interactable = false;
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            m_Dropdown.interactable = true;
        }
        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public override void UpdateStatus()
        {
            m_Dropdown.value = m_ColormapIndices.FindIndex((c) => c == SelectedScene.Colormap);
        }
        #endregion
    }
}