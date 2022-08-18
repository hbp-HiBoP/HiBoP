using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    public class SitesTagsSubModifiers : SubModifier<Core.Data.ProjectPreferences>
    {
        #region Properties
        [SerializeField] TagListGestion m_TagListGestion;
        public TagListGestion TagListGestion => m_TagListGestion;
        public ReadOnlyCollection<Core.Data.BaseTag> ModifiedTags => m_TagListGestion.ModifiedTags;
        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;
                m_TagListGestion.Interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Save()
        {
            base.Save();
            Object.SitesTags = m_TagListGestion.List.Objects.ToList();
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(Core.Data.ProjectPreferences objectToDisplay)
        {
            base.SetFields(objectToDisplay);
            m_TagListGestion.List.Set(objectToDisplay.SitesTags);
        }
        #endregion
    }
}
