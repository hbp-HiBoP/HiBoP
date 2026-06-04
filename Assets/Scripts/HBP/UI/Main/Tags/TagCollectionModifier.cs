using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;
using HBP.UI.Tools;
using HBP.Core.Tools;
using HBP.Data.Preferences;
using HBP.Data.Database;

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
                List<Core.Data.BaseTag> tags = new();
                tags.AddRange(m_GeneralSubModifiers.ModifiedTags);
                tags.AddRange(m_PatientsSubModifiers.ModifiedTags);
                tags.AddRange(m_SitesSubModifiers.ModifiedTags);
                return new ReadOnlyCollection<Core.Data.BaseTag>(tags);
            }
        }
        #endregion

        #region Public Methods
        public override async void OK()
        {
            if (m_GeneralSubModifiers.TagListGestion.HasBeenModified || m_PatientsSubModifiers.TagListGestion.HasBeenModified || m_SitesSubModifiers.TagListGestion.HasBeenModified)
            {
                int result = await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Informational, "Tags Modified", "Some tags have been added, deleted or modified. Patients and sites will be checked if you proceed.", "OK", "Cancel");
                if (result == 0)
                {
                    m_GeneralSubModifiers.Save();
                    m_PatientsSubModifiers.Save();
                    m_SitesSubModifiers.Save();
                    base.OK();
                    PersistentDataManager.Tags.Save();
                    await PersistentDataManager.Tags.CheckTagsAsync(ModifiedTags);
                    if (DatabaseManager.Database.IsLoaded) DatabaseManager.Database.SaveDatabase().Forget();
                }
            }
            else
            {
                base.OK();
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