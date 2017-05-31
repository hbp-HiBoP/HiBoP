using HBP.UI.Module3D;
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
        /// Change IEEG colormap
        /// </summary>
        public Dropdown Colormap { get; set; }
        /// <summary>
        /// Change brain surface color
        /// </summary>
        public Dropdown BrainColor { get; set; }
        /// <summary>
        /// Change brain cut color
        /// </summary>
        public Dropdown CutColor { get; set; }
        /// <summary>
        /// Show / hide edges
        /// </summary>
        public Toggle EdgeMode { get; set; }
        /// <summary>
        /// Start / stop automatic rotation
        /// </summary>
        public Toggle AutoRotate { get; set; }
        /// <summary>
        /// Automatic rotation speed slider
        /// </summary>
        public Slider AutoRotateSpeed { get; set; }

        /// <summary>
        /// Correspondance between colormap dropdown options indices and color type
        /// </summary>
        private List<ColorType> m_ColormapIndices = new List<ColorType>() { ColorType.Grayscale, ColorType.Hot, ColorType.Winter, ColorType.Warm, ColorType.Surface, ColorType.Cool, ColorType.RedYellow, ColorType.BlueGreen, ColorType.ACTC, ColorType.Bone, ColorType.GEColor, ColorType.Gold, ColorType.XRain, ColorType.MatLab };
        /// <summary>
        /// Correspondance between brain color dropdown options indices and color type
        /// </summary>
        private List<ColorType> m_BrainColorIndices = new List<ColorType>() { ColorType.BrainColor, ColorType.Default, ColorType.White, ColorType.Grayscale, ColorType.SoftGrayscale };
        /// <summary>
        /// Correspondance between cut color dropdown options indices and color type
        /// </summary>
        private List<ColorType> m_CutColorIndices = new List<ColorType>() { ColorType.BrainColor, ColorType.Grayscale };
        #endregion

        #region Private Methods
        private void Awake()
        {
            Initialize();
        }
        /// <summary>
        /// Link elements to the toolbar
        /// </summary>
        /// <param name="parent">Transform of the toolbar</param>
        protected override void FindButtons()
        {
            Transform colormapParent = transform.Find("Colormap");
            Colormap = colormapParent.Find("Dropdown").GetComponent<Dropdown>();

            Transform brainColorparent = transform.Find("Brain Color");
            BrainColor = brainColorparent.Find("Dropdown").GetComponent<Dropdown>();

            Transform cutColorParent = transform.Find("Cut Color");
            CutColor = cutColorParent.Find("Dropdown").GetComponent<Dropdown>();

            EdgeMode = transform.Find("Edge Mode").GetComponent<Toggle>();

            Transform autoRotateParent = transform.Find("Auto Rotate");
            AutoRotate = autoRotateParent.Find("Toggle").GetComponent<Toggle>();
            AutoRotateSpeed = autoRotateParent.Find("Slider").GetComponent<Slider>();
        }
        /// <summary>
        /// Add the listeners to the elements of the toolbar
        /// </summary>
        protected override void AddListeners()
        {
            base.AddListeners();

            Colormap.onValueChanged.AddListener((value) =>
            {
                // TODO : update colormap controller
                ApplicationState.Module3D.SelectedScene.UpdateColormap(m_ColormapIndices[value]);
            });

            BrainColor.onValueChanged.AddListener((value) =>
            {
                ApplicationState.Module3D.SelectedScene.UpdateBrainSurfaceColor(m_BrainColorIndices[value]);
            });

            CutColor.onValueChanged.AddListener((value) =>
            {
                ApplicationState.Module3D.SelectedScene.UpdateBrainCutColor(m_CutColorIndices[value]);
            });

            EdgeMode.onValueChanged.AddListener((isOn) =>
            {
                ApplicationState.Module3D.SelectedScene.EdgeMode = isOn;
            });

            AutoRotate.onValueChanged.AddListener((isOn) =>
            {
                ApplicationState.Module3D.SelectedScene.AutomaticRotation = isOn;
            });

            AutoRotateSpeed.onValueChanged.AddListener((value) =>
            {
                ApplicationState.Module3D.SelectedScene.AutomaticRotationSpeed = value;
            });
        }
        /// <summary>
        /// Set the toolbar elements to their default state
        /// </summary>
        protected override void DefaultState()
        {
            Colormap.value = 13;
            BrainColor.value = 0;
            CutColor.value = 0;
            EdgeMode.isOn = false;
            AutoRotate.isOn = false;
            AutoRotateSpeed.value = 30.0f;

            Colormap.interactable = false;
            BrainColor.interactable = false;
            CutColor.interactable = false;
            EdgeMode.interactable = false;
            AutoRotate.interactable = false;
            AutoRotateSpeed.interactable = false;
        }
        /// <summary>
        /// Update the interactable buttons of the toolbar
        /// </summary>
        protected override void UpdateInteractableButtons()
        {
            switch (ApplicationState.Module3D.SelectedScene.ModesManager.CurrentModeID)
            {
                case HBP.Module3D.Mode.ModesId.NoPathDefined:
                    Colormap.interactable = false;
                    BrainColor.interactable = false;
                    CutColor.interactable = false;
                    EdgeMode.interactable = false;
                    AutoRotate.interactable = false;
                    AutoRotateSpeed.interactable = false;
                    break;

                case HBP.Module3D.Mode.ModesId.MinPathDefined:
                    Colormap.interactable = true;
                    BrainColor.interactable = true;
                    CutColor.interactable = true;
                    EdgeMode.interactable = true;
                    AutoRotate.interactable = true;
                    AutoRotateSpeed.interactable = true;
                    break;

                case HBP.Module3D.Mode.ModesId.AllPathDefined:
                    Colormap.interactable = true;
                    BrainColor.interactable = true;
                    CutColor.interactable = true;
                    EdgeMode.interactable = true;
                    AutoRotate.interactable = true;
                    AutoRotateSpeed.interactable = true;
                    break;

                case HBP.Module3D.Mode.ModesId.ComputingAmplitudes:
                    Colormap.interactable = false;
                    BrainColor.interactable = false;
                    CutColor.interactable = false;
                    EdgeMode.interactable = true;
                    AutoRotate.interactable = true;
                    AutoRotateSpeed.interactable = true;
                    break;

                case HBP.Module3D.Mode.ModesId.AmplitudesComputed:
                    Colormap.interactable = true;
                    BrainColor.interactable = true;
                    CutColor.interactable = true;
                    EdgeMode.interactable = true;
                    AutoRotate.interactable = true;
                    AutoRotateSpeed.interactable = true;
                    break;

                case HBP.Module3D.Mode.ModesId.AmpNeedUpdate:
                    Colormap.interactable = true;
                    BrainColor.interactable = true;
                    CutColor.interactable = true;
                    EdgeMode.interactable = true;
                    AutoRotate.interactable = true;
                    AutoRotateSpeed.interactable = true;
                    break;

                case HBP.Module3D.Mode.ModesId.Error:
                    Colormap.interactable = false;
                    BrainColor.interactable = false;
                    CutColor.interactable = false;
                    EdgeMode.interactable = false;
                    AutoRotate.interactable = false;
                    AutoRotateSpeed.interactable = false;
                    break;

                default:
                    Debug.LogError("Error : setUIActivity :" + ApplicationState.Module3D.SelectedScene.ModesManager.CurrentMode.ID.ToString());
                    break;
            }
        }
        /// <summary>
        /// Change the status of the toolbar elements according to the selected scene parameters
        /// </summary>
        protected override void UpdateButtonsStatus(UpdateToolbarType type)
        {
            if (type == UpdateToolbarType.Scene)
            {
                Colormap.value = m_ColormapIndices.FindIndex((c) => c == ApplicationState.Module3D.SelectedScene.ColumnManager.Colormap);
                BrainColor.value = m_BrainColorIndices.FindIndex((c) => c == ApplicationState.Module3D.SelectedScene.ColumnManager.BrainColor);
                CutColor.value = m_CutColorIndices.FindIndex((c) => c == ApplicationState.Module3D.SelectedScene.ColumnManager.BrainCutColor);
                EdgeMode.isOn = ApplicationState.Module3D.SelectedScene.EdgeMode;
                AutoRotate.isOn = ApplicationState.Module3D.SelectedScene.AutomaticRotation;
                AutoRotateSpeed.value = ApplicationState.Module3D.SelectedScene.AutomaticRotationSpeed;
            }
        }
        #endregion
    }
}