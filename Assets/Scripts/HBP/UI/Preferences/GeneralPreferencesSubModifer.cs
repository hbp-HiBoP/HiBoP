using UnityEngine;
using UnityEngine.UI;
using HBP.Data.Preferences;

namespace HBP.UI.Preferences
{
    public class GeneralPreferencesSubModifer : SubModifier<ProjectSettings>
    {
        #region Properties
        [SerializeField] InputField m_NameInputField;
        [SerializeField] InputField m_LocationInputField;
        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }

            set
            {
                base.Interactable = value;
                m_NameInputField.interactable = value;
                m_LocationInputField.interactable = false;
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();
            m_NameInputField.onValueChanged.AddListener((name) => Object.Name = name);
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(ProjectSettings objectToDisplay)
        {
            base.SetFields(objectToDisplay);
            m_NameInputField.text = objectToDisplay.Name;
            m_LocationInputField.text = ApplicationState.ProjectLoadedLocation;
        }
        #endregion
    }
}