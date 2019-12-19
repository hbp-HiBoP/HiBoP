using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine;

namespace HBP.UI
{
    public class PatientsTagsSubModifiers : SubModifier<Data.ProjectPreferences>
    {
        #region Properties
        [SerializeField] TagListGestion m_TagListGestion;
        public TagListGestion TagListGestion => m_TagListGestion;
        public ReadOnlyCollection<Data.BaseTag> ModifiedTags => m_TagListGestion.ModifiedTags;
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
            Object.PatientsTags = m_TagListGestion.List.Objects.ToList();
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(Data.ProjectPreferences objectToDisplay)
        {
            base.SetFields(objectToDisplay);
            m_TagListGestion.List.Set(objectToDisplay.PatientsTags);
        }
        #endregion
    }
}