using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class Colormap : Tool
    {
        #region Properties
        [SerializeField]
        private Dropdown m_Dropdown;
        
        /// <summary>
        /// Correspondance between colormap dropdown options indices and color type
        /// </summary>
        private List<Data.Enums.ColorType> m_ColormapIndices = new List<Data.Enums.ColorType>() { Data.Enums.ColorType.Grayscale, Data.Enums.ColorType.Hot, Data.Enums.ColorType.Winter, Data.Enums.ColorType.Warm, Data.Enums.ColorType.Surface, Data.Enums.ColorType.Cool, Data.Enums.ColorType.RedYellow, Data.Enums.ColorType.BlueGreen, Data.Enums.ColorType.ACTC, Data.Enums.ColorType.Bone, Data.Enums.ColorType.GEColor, Data.Enums.ColorType.Gold, Data.Enums.ColorType.XRain, Data.Enums.ColorType.MatLab };
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_Dropdown.onValueChanged.AddListener((value) =>
            {
                Data.Enums.ColorType color = m_ColormapIndices[value];
                SelectedScene.UpdateColormap(color);
            });
        }

        public override void DefaultState()
        {
            m_Dropdown.value = 13;
            m_Dropdown.interactable = false;
        }

        public override void UpdateInteractable()
        {
            m_Dropdown.interactable = true;
        }

        public override void UpdateStatus()
        {
            m_Dropdown.value = m_ColormapIndices.FindIndex((c) => c == SelectedScene.ColumnManager.Colormap);
        }
        #endregion
    }
}