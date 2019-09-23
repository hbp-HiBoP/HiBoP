using HBP.Data.General;
using UnityEngine;

namespace HBP.UI.General
{
    public class TagsSubModifiers : SubModifier<ProjectSettings>
    {
        #region Properties
        [SerializeField] GeneralTagsSubModifiers m_GeneralSubModifiers;
        [SerializeField] PatientsTagsSubModifiers m_PatientsSubModifiers;
        [SerializeField] SitesTagsSubModifiers m_SitesSubModifiers;

        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;
                m_GeneralSubModifiers.Interactable = value;
                m_PatientsSubModifiers.Interactable = value;
                m_SitesSubModifiers.Interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();
            m_GeneralSubModifiers.Initialize();
            m_PatientsSubModifiers.Initialize();
            m_SitesSubModifiers.Initialize();
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(ProjectSettings objectToDisplay)
        {
            base.SetFields(objectToDisplay);
            m_GeneralSubModifiers.Object = objectToDisplay;
            m_PatientsSubModifiers.Object = objectToDisplay;
            m_SitesSubModifiers.Object = objectToDisplay;
        }
        #endregion
    }
}