using HBP.Module3D;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class IBCParameters : Tool
    {
        #region Properties
        [SerializeField]
        private Text m_MinText;

        [SerializeField]
        private Text m_MaxText;

        [SerializeField]
        private InputField m_CalMinInputField;

        [SerializeField]
        private Slider m_CalMinSlider;

        [SerializeField]
        private InputField m_CalMaxInputField;

        [SerializeField]
        private Slider m_CalMaxSlider;

        [SerializeField]
        private Slider m_AlphaSlider;
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_CalMinInputField.onEndEdit.AddListener((value) =>
            {
                if (ListenerLock) return;

                float floatValue;
                if (global::Tools.Unity.NumberExtension.TryParseFloat(value, out floatValue))
                {
                    SelectedScene.ColumnManager.IBCCalMin = Mathf.Clamp(floatValue, SelectedScene.ColumnManager.SelectedIBCContrast.Volume.ExtremeValues.ComputedCalMin, SelectedScene.ColumnManager.SelectedIBCContrast.Volume.ExtremeValues.ComputedCalMax);
                    m_CalMinSlider.value = SelectedScene.ColumnManager.IBCCalMin;
                }
                UpdateStatus();
            });
            m_CalMinSlider.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                SelectedScene.ColumnManager.IBCCalMinFactor = value;
                m_CalMinInputField.text = SelectedScene.ColumnManager.IBCCalMin.ToString("N2");
                UpdateStatus();
            });
            m_CalMaxInputField.onEndEdit.AddListener((value) =>
            {
                if (ListenerLock) return;

                float floatValue;
                if (global::Tools.Unity.NumberExtension.TryParseFloat(value, out floatValue))
                {
                    SelectedScene.ColumnManager.IBCCalMax = Mathf.Clamp(floatValue, SelectedScene.ColumnManager.SelectedIBCContrast.Volume.ExtremeValues.ComputedCalMin, SelectedScene.ColumnManager.SelectedIBCContrast.Volume.ExtremeValues.ComputedCalMax);
                    m_CalMaxSlider.value = SelectedScene.ColumnManager.IBCCalMax;
                }
                UpdateStatus();
            });
            m_CalMaxSlider.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                SelectedScene.ColumnManager.IBCCalMaxFactor = value;
                m_CalMaxInputField.text = SelectedScene.ColumnManager.IBCCalMax.ToString("N2");
                UpdateStatus();
            });
            m_AlphaSlider.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                SelectedScene.ColumnManager.IBCAlpha = value;
            });
        }

        public override void DefaultState()
        {
            gameObject.SetActive(false);
        }

        public override void UpdateInteractable()
        {
            bool isIBC = SelectedScene.ColumnManager.DisplayIBCContrasts;

            gameObject.SetActive(isIBC);
        }

        public override void UpdateStatus()
        {
            bool hasIBC = SelectedScene.ColumnManager.SelectedIBCContrast != null;
            if (hasIBC)
            {
                MRICalValues calValues = SelectedScene.ColumnManager.SelectedIBCContrast.Volume.ExtremeValues;
                m_MinText.text = calValues.ComputedCalMin.ToString("N2");
                m_MaxText.text = calValues.ComputedCalMax.ToString("N2");
                m_CalMinInputField.text = SelectedScene.ColumnManager.IBCCalMin.ToString("N2");
                m_CalMaxInputField.text = SelectedScene.ColumnManager.IBCCalMax.ToString("N2");
                m_CalMinSlider.value = SelectedScene.ColumnManager.IBCCalMinFactor;
                m_CalMaxSlider.value = SelectedScene.ColumnManager.IBCCalMaxFactor;
                m_AlphaSlider.value = SelectedScene.ColumnManager.IBCAlpha;
            }
        }
        #endregion
    }
}