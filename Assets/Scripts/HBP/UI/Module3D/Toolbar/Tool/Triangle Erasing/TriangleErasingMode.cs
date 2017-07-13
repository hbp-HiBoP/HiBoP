using HBP.Module3D;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class TriangleErasingMode : Tool
    {
        #region Properties
        [SerializeField]
        private Dropdown m_Dropdown;

        [SerializeField]
        private InputField m_InputField;
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_Dropdown.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                TriEraser.Mode mode = (TriEraser.Mode)value;
                ApplicationState.Module3D.SelectedScene.TriangleErasingMode = mode;
                UpdateInteractable();
            });

            m_InputField.onEndEdit.AddListener((value) =>
            {
                if (ListenerLock) return;

                int degrees = 30;
                int.TryParse(value, out degrees);
                ApplicationState.Module3D.SelectedScene.TriangleErasingZoneDegrees = degrees;
                m_InputField.text = degrees.ToString();
            });
        }
        public override void DefaultState()
        {
            m_Dropdown.interactable = false;
            m_Dropdown.value = 0;
            m_InputField.interactable = false;
            m_InputField.text = "30";
        }
        public override void UpdateInteractable()
        {
            bool isZoneModeEnabled = ApplicationState.Module3D.SelectedScene.TriangleErasingMode == TriEraser.Mode.Zone;
            m_InputField.gameObject.SetActive(isZoneModeEnabled);
            switch (ApplicationState.Module3D.SelectedScene.ModesManager.CurrentMode.ID)
            {
                case Mode.ModesId.NoPathDefined:
                    m_Dropdown.interactable = false;
                    m_InputField.interactable = false;
                    break;
                case Mode.ModesId.MinPathDefined:
                    m_Dropdown.interactable = true;
                    m_InputField.interactable = isZoneModeEnabled;
                    break;
                case Mode.ModesId.AllPathDefined:
                    m_Dropdown.interactable = true;
                    m_InputField.interactable = isZoneModeEnabled;
                    break;
                case Mode.ModesId.ComputingAmplitudes:
                    m_Dropdown.interactable = false;
                    m_InputField.interactable = false;
                    break;
                case Mode.ModesId.AmplitudesComputed:
                    m_Dropdown.interactable = true;
                    m_InputField.interactable = isZoneModeEnabled;
                    break;
                case Mode.ModesId.TriErasing:
                    m_Dropdown.interactable = true;
                    m_InputField.interactable = isZoneModeEnabled;
                    break;
                case Mode.ModesId.ROICreation:
                    m_Dropdown.interactable = false;
                    m_InputField.interactable = false;
                    break;
                case Mode.ModesId.AmpNeedUpdate:
                    m_Dropdown.interactable = true;
                    m_InputField.interactable = isZoneModeEnabled;
                    break;
                case Mode.ModesId.Error:
                    m_Dropdown.interactable = false;
                    m_InputField.interactable = false;
                    break;
                default:
                    break;
            }
        }
        public override void UpdateStatus(Toolbar.UpdateToolbarType type)
        {
            if (type == Toolbar.UpdateToolbarType.Scene)
            {
                m_Dropdown.value = (int)ApplicationState.Module3D.SelectedScene.TriangleErasingMode;
                m_InputField.text = ApplicationState.Module3D.SelectedScene.TriangleErasingZoneDegrees.ToString();
                if (ApplicationState.Module3D.SelectedScene.TriangleErasingMode != TriEraser.Mode.Zone)
                {
                    m_InputField.gameObject.SetActive(false);
                }
                else
                {
                    m_InputField.gameObject.SetActive(true);
                }
            }
        }
        #endregion
    }
}