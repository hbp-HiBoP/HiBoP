using HBP.Data.Preferences;
using HBP.UI.Tools;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    public class MiscPreferencesSubModifier : SubModifier<MiscPreferences>
    {
        #region Properties
        [SerializeField] Toggle m_AdvancedFeaturesToggle;

        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;
                m_AdvancedFeaturesToggle.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();

            m_AdvancedFeaturesToggle.onValueChanged.AddListener(value => Object.AdvancedFeatures = value);
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(MiscPreferences objectToDisplay)
        {
            base.SetFields(objectToDisplay);

            m_AdvancedFeaturesToggle.isOn = objectToDisplay.AdvancedFeatures;
        }
        #endregion
    }
}