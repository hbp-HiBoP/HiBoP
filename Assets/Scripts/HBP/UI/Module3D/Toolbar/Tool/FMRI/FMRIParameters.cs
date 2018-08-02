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

                Base3DScene scene = ApplicationState.Module3D.SelectedScene;
                float floatValue;
                if (float.TryParse(value, out floatValue))
                {
                    scene.ColumnManager.FMRICalMin = Mathf.Clamp(floatValue, scene.ColumnManager.FMRI.Volume.ExtremeValues.ComputedCalMin, scene.ColumnManager.FMRI.Volume.ExtremeValues.ComputedCalMax); ;
                    m_CalMinSlider.value = scene.ColumnManager.FMRICalMinFactor;
                }
                UpdateStatus();
            });
            m_CalMinSlider.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                Base3DScene scene = ApplicationState.Module3D.SelectedScene;
                scene.ColumnManager.FMRICalMinFactor = value;
                m_CalMinInputField.text = scene.ColumnManager.FMRICalMin.ToString("N2");
                UpdateStatus();
            });
            m_CalMaxInputField.onEndEdit.AddListener((value) =>
            {
                if (ListenerLock) return;

                Base3DScene scene = ApplicationState.Module3D.SelectedScene;
                float floatValue;
                if (float.TryParse(value, out floatValue))
                {
                    scene.ColumnManager.FMRICalMax = Mathf.Clamp(floatValue, scene.ColumnManager.FMRI.Volume.ExtremeValues.ComputedCalMin, scene.ColumnManager.FMRI.Volume.ExtremeValues.ComputedCalMax);
                    m_CalMaxSlider.value = scene.ColumnManager.FMRICalMax;
                }
                UpdateStatus();
            });
            m_CalMaxSlider.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                Base3DScene scene = ApplicationState.Module3D.SelectedScene;
                scene.ColumnManager.FMRICalMaxFactor = value;
                m_CalMaxInputField.text = scene.ColumnManager.FMRICalMax.ToString("N2");
                UpdateStatus();
            });
            m_AlphaSlider.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                ApplicationState.Module3D.SelectedScene.ColumnManager.FMRIAlpha = value;
            });
        }

        public override void DefaultState()
        {
            gameObject.SetActive(false);
        }

        public override void UpdateInteractable()
        {
            bool hasFMRI = false;
            Base3DScene scene = ApplicationState.Module3D.SelectedScene;
            if (scene)
            {
                hasFMRI = scene.ColumnManager.FMRI != null;
            }

            gameObject.SetActive(hasFMRI);
        }

        public override void UpdateStatus()
        {
            bool hasFMRI = false;
            Base3DScene scene = ApplicationState.Module3D.SelectedScene;
            if (scene)
            {
                hasFMRI = scene.ColumnManager.FMRI != null;
            }
            if (hasFMRI)
            {
                MRICalValues calValues = scene.ColumnManager.FMRI.Volume.ExtremeValues;
                m_MinText.text = calValues.ComputedCalMin.ToString("N2");
                m_MaxText.text = calValues.ComputedCalMax.ToString("N2");
                m_CalMinInputField.text = scene.ColumnManager.FMRICalMin.ToString("N2");
                m_CalMaxInputField.text = scene.ColumnManager.FMRICalMax.ToString("N2");
                m_CalMinSlider.value = scene.ColumnManager.FMRICalMinFactor;
                m_CalMaxSlider.value = scene.ColumnManager.FMRICalMaxFactor;
                m_AlphaSlider.value = scene.ColumnManager.FMRIAlpha;
            }
        }
        #endregion
    }
}