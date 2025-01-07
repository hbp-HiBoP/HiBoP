using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using HBP.UI.Tools;
using HBP.Core.Tools;
using UnityEngine.Events;
using HBP.Data.Preferences;

namespace HBP.UI.Main
{
    public class TagCollectionModifier : ObjectModifier<Core.Data.TagCollection>
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

        public ReadOnlyCollection<Core.Data.BaseTag> ModifiedTags
        {
            get
            {
                List<Core.Data.BaseTag> tags = new List<Core.Data.BaseTag>();
                tags.AddRange(m_GeneralSubModifiers.ModifiedTags);
                tags.AddRange(m_PatientsSubModifiers.ModifiedTags);
                tags.AddRange(m_SitesSubModifiers.ModifiedTags);
                return new ReadOnlyCollection<Core.Data.BaseTag>(tags);
            }
        }
        #endregion

        #region Public Methods
        public override void OK()
        {
            m_GeneralSubModifiers.Save();
            m_PatientsSubModifiers.Save();
            m_SitesSubModifiers.Save();
            base.OK();
            PersistentDataManager.Tags.Save();
            if (ApplicationState.ProjectLoaded != null)
            {
                GenericEvent<float, float, LoadingText> onChangeProgress = new GenericEvent<float, float, LoadingText>();
                LoadingManager.Load(ApplicationState.ProjectLoaded.c_CheckPatientTagValues(ModifiedTags, (progress, duration, text) => onChangeProgress.Invoke(progress, duration, text)), onChangeProgress);
            }
        }
        #endregion

        #region Protected Methods
        protected override void Initialize()
        {
            base.Initialize();
            m_GeneralSubModifiers.Initialize();
            m_PatientsSubModifiers.Initialize();
            m_SitesSubModifiers.Initialize();
        }
        protected override void SetFields(Core.Data.TagCollection objectToDisplay)
        {
            m_GeneralSubModifiers.Object = objectToDisplay;
            m_PatientsSubModifiers.Object = objectToDisplay;
            m_SitesSubModifiers.Object = objectToDisplay;
        }
        #endregion
    }
}