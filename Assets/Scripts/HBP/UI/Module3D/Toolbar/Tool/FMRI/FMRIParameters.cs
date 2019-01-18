using HBP.Module3D;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class FMRIParameters : Tool
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
                    SelectedScene.ColumnManager.FMRICalMin = Mathf.Clamp(floatValue, SelectedScene.ColumnManager.FMRI.Volume.ExtremeValues.ComputedCalMin, SelectedScene.ColumnManager.FMRI.Volume.ExtremeValues.ComputedCalMax);
                    m_CalMinSlider.value = SelectedScene.ColumnManager.FMRICalMinFactor;
                }
                UpdateStatus();
            });
            m_CalMinSlider.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                SelectedScene.ColumnManager.FMRICalMinFactor = value;
                m_CalMinInputField.text = SelectedScene.ColumnManager.FMRICalMin.ToString("N2");
                UpdateStatus();
            });
            m_CalMaxInputField.onEndEdit.AddListener((value) =>
            {
                if (ListenerLock) return;
                
                float floatValue;
                if (global::Tools.Unity.NumberExtension.TryParseFloat(value, out floatValue))
                {
                    SelectedScene.ColumnManager.FMRICalMax = Mathf.Clamp(floatValue, SelectedScene.ColumnManager.FMRI.Volume.ExtremeValues.ComputedCalMin, SelectedScene.ColumnManager.FMRI.Volume.ExtremeValues.ComputedCalMax);
                    m_CalMaxSlider.value = SelectedScene.ColumnManager.FMRICalMax;
                }
                UpdateStatus();
            });
            m_CalMaxSlider.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                SelectedScene.ColumnManager.FMRICalMaxFactor = value;
                m_CalMaxInputField.text = SelectedScene.ColumnManager.FMRICalMax.ToString("N2");
                UpdateStatus();
            });
            m_AlphaSlider.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                SelectedScene.ColumnManager.FMRIAlpha = value;
            });
        }

        public override void DefaultState()
        {
            gameObject.SetActive(false);
        }

        public override void UpdateInteractable()
        {
            bool hasFMRI = SelectedScene.ColumnManager.FMRI != null;

            gameObject.SetActive(hasFMRI);
        }

        public override void UpdateStatus()
        {
            bool hasFMRI = SelectedScene.ColumnManager.FMRI != null;
            if (hasFMRI)
            {
                MRICalValues calValues = SelectedScene.ColumnManager.FMRI.Volume.ExtremeValues;
                m_MinText.text = calValues.ComputedCalMin.ToString("N2");
                m_MaxText.text = calValues.ComputedCalMax.ToString("N2");
                m_CalMinInputField.text = SelectedScene.ColumnManager.FMRICalMin.ToString("N2");
                m_CalMaxInputField.text = SelectedScene.ColumnManager.FMRICalMax.ToString("N2");
                m_CalMinSlider.value = SelectedScene.ColumnManager.FMRICalMinFactor;
                m_CalMaxSlider.value = SelectedScene.ColumnManager.FMRICalMaxFactor;
                m_AlphaSlider.value = SelectedScene.ColumnManager.FMRIAlpha;
            }
        }
        #endregion
    }
}