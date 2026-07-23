using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using HBP.Core.Enums;
using HBP.Core.Preferences;
using HBP.Core.Tools;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    public class EEGPreferencesSubModifier : SubModifier<EEGPreferences>
    {
        #region Properties
        [SerializeField] Dropdown m_EEGAveragingDropdown;
        [SerializeField] Dropdown m_EEGNormalizationDropdown;
        [SerializeField] Dropdown m_TemporalSamplingDropdown;
        [SerializeField] InputField m_CorrelationAlphaInputField;
        [SerializeField] Toggle m_BonferroniCorrectionToggle;
        private readonly NormalizationType[] m_UserNormalizationTypes = ((NormalizationType[])Enum.GetValues(typeof(NormalizationType))).Where(value => value != NormalizationType.Auto).ToArray();

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }
            set
            {
                base.Interactable = value;

                m_EEGAveragingDropdown.interactable = value;
                m_EEGNormalizationDropdown.interactable = value;
                m_TemporalSamplingDropdown.interactable = value;
                m_CorrelationAlphaInputField.interactable = value;
                m_BonferroniCorrectionToggle.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();

            m_EEGNormalizationDropdown.onValueChanged.AddListener(value => Object.Normalization = m_UserNormalizationTypes[value]);
            m_EEGAveragingDropdown.onValueChanged.AddListener(value => Object.Averaging = (AveragingType)value);
            m_TemporalSamplingDropdown.onValueChanged.AddListener(value => Object.TemporalSampling = (TemporalSamplingPolicy)value);
            m_CorrelationAlphaInputField.onEndEdit.AddListener((value) =>
            {
                if (NumberExtension.TryParseFloat(value, out float result))
                {
                    Object.CorrelationAlpha = result;
                }
                else
                {
                    m_CorrelationAlphaInputField.text = Object.CorrelationAlpha.ToString();
                }
            });
            m_BonferroniCorrectionToggle.onValueChanged.AddListener((value) => Object.BonferroniCorrection = value);
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(EEGPreferences objectToDisplay)
        {
            base.SetFields(objectToDisplay);

            m_EEGNormalizationDropdown.options = m_UserNormalizationTypes.Select(value => new Dropdown.OptionData(value.ToString().CamelCaseToWords())).ToList();
            m_EEGNormalizationDropdown.SetValue(Array.IndexOf(m_UserNormalizationTypes, objectToDisplay.Normalization));
            m_EEGNormalizationDropdown.RefreshShownValue();
            m_EEGAveragingDropdown.Set(typeof(AveragingType), (int)objectToDisplay.Averaging);
            m_TemporalSamplingDropdown.Set(typeof(TemporalSamplingPolicy), (int)objectToDisplay.TemporalSampling);
            m_CorrelationAlphaInputField.text = Object.CorrelationAlpha.ToString();
            m_BonferroniCorrectionToggle.isOn = Object.BonferroniCorrection;
        }
        #endregion
    }
}
