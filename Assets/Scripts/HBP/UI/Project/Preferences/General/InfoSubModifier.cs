using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI
{
    public class InfoSubModifier : SubModifier<Core.Data.ProjectPreferences>
    {
        #region Properties
        [SerializeField] InputField m_NameInputField;
        [SerializeField] InputField m_LocationInputField;
        [SerializeField] InputField m_IDInputField;

        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;
                m_NameInputField.interactable = value;
                m_LocationInputField.interactable = false;
                m_IDInputField.interactable = false;
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
        protected override void SetFields(Core.Data.ProjectPreferences objectToDisplay)
        {
            base.SetFields(objectToDisplay);
            m_NameInputField.text = objectToDisplay.Name;
            m_LocationInputField.text = ApplicationState.ProjectLoadedLocation;
            m_IDInputField.text = objectToDisplay.ID;
        }
        #endregion
    }
}