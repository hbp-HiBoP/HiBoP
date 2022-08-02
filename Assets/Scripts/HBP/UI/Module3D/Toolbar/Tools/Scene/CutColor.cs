using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HBP.Core.Data.Enums;

namespace HBP.UI.Module3D.Tools
{
    public class CutColor : Tool
    {
        #region Properties
        /// <summary>
        /// Dropdown to select the colormap used for the cuts
        /// </summary>
        [SerializeField] private Dropdown m_Dropdown;
        /// <summary>
        /// Correspondance between cut color dropdown options indices and color type
        /// </summary>
        private List<ColorType> m_CutColorIndices = new List<ColorType>() { ColorType.Default, ColorType.Grayscale };
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

                SelectedScene.CutColor = m_CutColorIndices[value];
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            m_Dropdown.value = 0;
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
            m_Dropdown.value = m_CutColorIndices.FindIndex((c) => c == SelectedScene.CutColor);
        }
        #endregion
    }
}