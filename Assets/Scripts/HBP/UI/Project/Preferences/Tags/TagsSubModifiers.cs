using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    public class TagsSubModifiers : SubModifier<Core.Data.ProjectPreferences>
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
        public override void Initialize()
        {
            base.Initialize();
            m_GeneralSubModifiers.Initialize();
            m_PatientsSubModifiers.Initialize();
            m_SitesSubModifiers.Initialize();
        }
        public override void Save()
        {
            m_GeneralSubModifiers.Save();
            m_PatientsSubModifiers.Save();
            m_SitesSubModifiers.Save();
            base.Save();
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(Core.Data.ProjectPreferences objectToDisplay)
        {
            base.SetFields(objectToDisplay);
            m_GeneralSubModifiers.Object = objectToDisplay;
            m_PatientsSubModifiers.Object = objectToDisplay;
            m_SitesSubModifiers.Object = objectToDisplay;
        }
        #endregion
    }
}