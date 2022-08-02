using HBP.Data.Preferences;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.UserPreferences
{
    public class CutPreferencesSubModifier : SubModifier<Core.Data.Preferences.CutPreferences>
    {
        #region Properties
        [SerializeField] Toggle m_ShowCutLinesToggle;

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }
            set
            {
                base.Interactable = value;

                m_ShowCutLinesToggle.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();

            m_ShowCutLinesToggle.onValueChanged.AddListener(value => Object.ShowCutLines = value);
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(Core.Data.Preferences.CutPreferences objectToDisplay)
        {
            base.SetFields(objectToDisplay);

            m_ShowCutLinesToggle.isOn = objectToDisplay.ShowCutLines;
        }
        #endregion
    }
}