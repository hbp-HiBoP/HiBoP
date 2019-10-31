using UnityEngine;
using HBP.Data.General;
using UnityEngine.Events;

namespace HBP.UI.General
{
    public class ProjectPreferencesModifier : ObjectModifier<ProjectSettings>
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

        #region Public Methods
        public override void Save()
        {
            base.Save();
            GenericEvent<float, float, LoadingText> onChangeProgress = new GenericEvent<float, float, LoadingText>();
            ApplicationState.LoadingManager.Load(ApplicationState.ProjectLoaded.c_CheckPatientTagValues(m_TagsSubModifier.ModifiedTags, (progress, duration, text) => onChangeProgress.Invoke(progress, duration, text)), onChangeProgress);
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
