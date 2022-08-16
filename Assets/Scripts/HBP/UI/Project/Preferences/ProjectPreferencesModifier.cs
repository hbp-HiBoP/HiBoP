using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using HBP.Core.Tools;
using HBP.Core.Data;

namespace HBP.UI
{
    /// <summary>
    /// Window to modify the project preferences.
    /// </summary>
    public class ProjectPreferencesModifier : ObjectModifier<ProjectPreferences>
    {
        #region Properties
        [SerializeField] GeneralSubModifer m_GeneralSubModifier;
        [SerializeField] TagsSubModifiers m_TagsSubModifier;

        /// <summary>
        /// True if interactable, False otherwise.
        /// </summary>
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
        /// <summary>
        /// Save the modifications.
        /// </summary>
        public override void OK()
        {
            m_GeneralSubModifier.Save();
            m_TagsSubModifier.Save();
            base.OK();
            GenericEvent<float, float, LoadingText> onChangeProgress = new GenericEvent<float, float, LoadingText>();
            LoadingManager.Load(c_CheckProject(onChangeProgress), onChangeProgress);
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Initialize the window.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();
            m_GeneralSubModifier.Initialize();
            m_TagsSubModifier.Initialize();
        }
        /// <summary>
        /// Set the fields.
        /// </summary>
        /// <param name="objectToDisplay">project preferences to display</param>
        protected override void SetFields(ProjectPreferences objectToDisplay)
        {
            m_GeneralSubModifier.Object = objectToDisplay;
            m_TagsSubModifier.Object = objectToDisplay;
        }
        #endregion

        #region Coroutines
        private IEnumerator c_CheckProject(GenericEvent<float, float, LoadingText> onChangeProgress)
        {
            yield return ApplicationState.ProjectLoaded.c_CheckPatientTagValues(m_TagsSubModifier.ModifiedTags, (progress, duration, text) => onChangeProgress.Invoke(progress * 0.5f, duration, text));
            yield return ApplicationState.ProjectLoaded.c_CheckDatasets(ApplicationState.ProjectLoaded.Protocols, (progress, duration, text) => onChangeProgress.Invoke(progress * 0.5f + 0.5f, duration, text));
        }
        #endregion
    }
}
