﻿using HBP.Module3D;
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
        private RectTransform m_InputFieldParent;
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
            m_InputFieldParent.gameObject.SetActive(false);
        }
        public override void UpdateInteractable()
        {
            bool isZoneModeEnabled = ApplicationState.Module3D.SelectedScene.TriangleErasingMode == TriEraser.Mode.Zone;

            m_InputFieldParent.gameObject.SetActive(isZoneModeEnabled);
            m_InputField.gameObject.SetActive(isZoneModeEnabled);
            m_Dropdown.interactable = true;
            m_InputField.interactable = isZoneModeEnabled;
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
                    m_InputFieldParent.gameObject.SetActive(false);
                }
                else
                {
                    m_InputField.gameObject.SetActive(true);
                    m_InputFieldParent.gameObject.SetActive(true);
                }
            }
        }
        #endregion
    }
}