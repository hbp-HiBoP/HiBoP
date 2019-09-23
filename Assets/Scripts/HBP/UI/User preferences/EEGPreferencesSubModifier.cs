using HBP.Data.Preferences;
using Tools.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.UserPreferences
{
    public class EEGPreferencesSubModifier : SubModifier<EEGPreferences>
    {
        #region Properties
        [SerializeField] Dropdown m_EEGAveragingDropdown;
        [SerializeField] Dropdown m_EEGNormalizationDropdown;

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
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();

            m_EEGNormalizationDropdown.onValueChanged.AddListener(value => Object.Normalization = (Data.Enums.NormalizationType)value);
            m_EEGAveragingDropdown.onValueChanged.AddListener(value => Object.Averaging = (Data.Enums.AveragingType)value);
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(EEGPreferences objectToDisplay)
        {
            base.SetFields(objectToDisplay);

            m_EEGNormalizationDropdown.Set(typeof(Data.Enums.NormalizationType), (int)objectToDisplay.Normalization);
            m_EEGAveragingDropdown.Set(typeof(Data.Enums.AveragingType), (int)objectToDisplay.Averaging);
        }
        #endregion
    }
}