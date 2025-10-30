using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    public class PatientsTagsSubModifiers : SubModifier<Core.Data.TagCollection>
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
                m_TagListGestion.Modifiable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Save()
        {
            base.Save();
            Object.SetPatientTags(m_TagListGestion.List.Objects.ToList(), false);
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(Core.Data.TagCollection objectToDisplay)
        {
            base.SetFields(objectToDisplay);
            m_TagListGestion.List.Set(objectToDisplay.PatientsTags);
        }
        #endregion
    }
}