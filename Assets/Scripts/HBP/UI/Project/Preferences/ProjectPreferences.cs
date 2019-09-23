using UnityEngine;
using HBP.Data.General;

namespace HBP.UI.General
{
    public class ProjectPreferences : ItemModifier<ProjectSettings>
    {
        #region Properties
        [SerializeField] GeneralSubModifer m_GeneralSubModifier;
        [SerializeField] TagsSubModifiers m_TagsSubModifier;

        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;
                m_GeneralSubModifier.Interactable = value;
                m_TagsSubModifier.Interactable = value;
            }
        }
        #endregion

        #region Protected Methods
        protected override void Initialize()
        {
            base.Initialize();
            m_GeneralSubModifier.Initialize();
            m_TagsSubModifier.Initialize();
        }
        protected override void SetFields(ProjectSettings objectToDisplay)
        {
            m_GeneralSubModifier.Object = objectToDisplay;
            m_TagsSubModifier.Object = objectToDisplay;
        }
        #endregion
    }
}
