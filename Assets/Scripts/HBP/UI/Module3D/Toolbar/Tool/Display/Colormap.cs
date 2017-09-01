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
        private List<ColorType> m_ColormapIndices = new List<ColorType>() { ColorType.Grayscale, ColorType.Hot, ColorType.Winter, ColorType.Warm, ColorType.Surface, ColorType.Cool, ColorType.RedYellow, ColorType.BlueGreen, ColorType.ACTC, ColorType.Bone, ColorType.GEColor, ColorType.Gold, ColorType.XRain, ColorType.MatLab };

        public GenericEvent<ColorType> OnChangeValue = new GenericEvent<ColorType>();
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_Dropdown.onValueChanged.AddListener((value) =>
            {
                ColorType color = m_ColormapIndices[value];
                ApplicationState.Module3D.SelectedScene.UpdateColormap(color);
                OnChangeValue.Invoke(color);
            });
        }

        public override void DefaultState()
        {
            m_Dropdown.value = 13;
            m_Dropdown.interactable = false;
        }

        public override void UpdateInteractable()
        {
            switch (ApplicationState.Module3D.SelectedScene.ModesManager.CurrentModeID)
            {
                case HBP.Module3D.Mode.ModesId.NoPathDefined:
                    m_Dropdown.interactable = false;
                    break;
                case HBP.Module3D.Mode.ModesId.MinPathDefined:
                    m_Dropdown.interactable = true;
                    break;
                case HBP.Module3D.Mode.ModesId.AllPathDefined:
                    m_Dropdown.interactable = true;
                    break;
                case HBP.Module3D.Mode.ModesId.ComputingAmplitudes:
                    m_Dropdown.interactable = false;
                    break;
                case HBP.Module3D.Mode.ModesId.AmplitudesComputed:
                    m_Dropdown.interactable = true;
                    break;
                case HBP.Module3D.Mode.ModesId.TriErasing:
                    m_Dropdown.interactable = false;
                    break;
                case HBP.Module3D.Mode.ModesId.ROICreation:
                    m_Dropdown.interactable = false;
                    break;
                case HBP.Module3D.Mode.ModesId.AmpNeedUpdate:
                    m_Dropdown.interactable = true;
                    break;
                case HBP.Module3D.Mode.ModesId.Error:
                    m_Dropdown.interactable = false;
                    break;
                default:
                    break;
            }
        }

        public override void UpdateStatus(Toolbar.UpdateToolbarType type)
        {
            if (type == Toolbar.UpdateToolbarType.Scene)
            {
                m_Dropdown.value = m_ColormapIndices.FindIndex((c) => c == ApplicationState.Module3D.SelectedScene.ColumnManager.Colormap);
            }
        }
        #endregion
    }
}