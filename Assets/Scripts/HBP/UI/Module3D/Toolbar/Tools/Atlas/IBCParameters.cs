using HBP.Module3D;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Module3D.Tools
{
    public class IBCParameters : Tool
    {
        #region Properties
        /// <summary>
        /// Text to display the minimum value of the contrast
        /// </summary>
        [SerializeField] private Text m_MinText;
        /// <summary>
        /// Text to display the maximum value of the contrast
        /// </summary>
        [SerializeField] private Text m_MaxText;
        /// <summary>
        /// Inputfield to set the minimum calibration value
        /// </summary>
        [SerializeField] private InputField m_CalMinInputField;
        /// <summary>
        /// Slider to set the minimum calibration value
        /// </summary>
        [SerializeField] private Slider m_CalMinSlider;
        /// <summary>
        /// Inputfield to set the maximum calibration value
        /// </summary>
        [SerializeField] private InputField m_CalMaxInputField;
        /// <summary>
        /// Slider to set the maximum calibration value
        /// </summary>
        [SerializeField] private Slider m_CalMaxSlider;
        /// <summary>
        /// Slider to set the alpha of the contrast
        /// </summary>
        [SerializeField] private Slider m_AlphaSlider;
        #endregion

        #region Public Methods
        /// <summary>
        /// Initialize the toolbar
        /// </summary>
        public override void Initialize()
        {
            m_CalMinInputField.onEndEdit.AddListener((value) =>
            {
                if (ListenerLock) return;

                float floatValue;
                if (global::Tools.CSharp.NumberExtension.TryParseFloat(value, out floatValue))
                {
                    SelectedScene.FMRIManager.FMRINegativeCalMin = floatValue;
                }
                ListenerLock = true;
                UpdateStatus();
                ListenerLock = false;
            });
            m_CalMinSlider.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                SelectedScene.FMRIManager.FMRINegativeCalMinFactor = value;
                ListenerLock = true;
                UpdateStatus();
                ListenerLock = false;
            });
            m_CalMaxInputField.onEndEdit.AddListener((value) =>
            {
                if (ListenerLock) return;

                float floatValue;
                if (global::Tools.CSharp.NumberExtension.TryParseFloat(value, out floatValue))
                {
                    SelectedScene.FMRIManager.FMRIPositiveCalMin = floatValue;
                }
                ListenerLock = true;
                UpdateStatus();
                ListenerLock = false;
            });
            m_CalMaxSlider.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                SelectedScene.FMRIManager.FMRIPositiveCalMinFactor = value;
                ListenerLock = true;
                UpdateStatus();
                ListenerLock = false;
            });
            m_AlphaSlider.onValueChanged.AddListener((value) =>
            {
                if (ListenerLock) return;

                SelectedScene.FMRIManager.FMRIAlpha = value;
                ListenerLock = true;
                UpdateStatus();
                ListenerLock = false;
            });
        }
        /// <summary>
        /// Set the default state of this tool
        /// </summary>
        public override void DefaultState()
        {
            gameObject.SetActive(false);
        }
        /// <summary>
        /// Update the interactable state of the tool
        /// </summary>
        public override void UpdateInteractable()
        {
            bool isIBC = SelectedScene.FMRIManager.DisplayIBCContrasts;

            gameObject.SetActive(isIBC);
        }
        /// <summary>
        /// Update the status of the tool
        /// </summary>
        public override void UpdateStatus()
        {
            bool hasIBC = SelectedScene.FMRIManager.SelectedIBCContrast != null;
            if (hasIBC)
            {
                MRICalValues calValues = SelectedScene.FMRIManager.SelectedIBCContrast.Volume.ExtremeValues;
                m_MinText.text = calValues.Min.ToString("N2");
                m_MaxText.text = calValues.Max.ToString("N2");
                m_CalMinInputField.text = SelectedScene.FMRIManager.FMRINegativeCalMin.ToString("N2");
                m_CalMaxInputField.text = SelectedScene.FMRIManager.FMRIPositiveCalMin.ToString("N2");
                m_CalMinSlider.value = SelectedScene.FMRIManager.FMRINegativeCalMinFactor;
                m_CalMaxSlider.value = SelectedScene.FMRIManager.FMRIPositiveCalMinFactor;
                m_AlphaSlider.value = SelectedScene.FMRIManager.FMRIAlpha;
            }
        }
        #endregion
    }
}