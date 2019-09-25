using HBP.Data.General;
using HBP.UI.Tags;
using System.Collections.ObjectModel;
using UnityEngine;

namespace HBP.UI.General
{
    public class PatientsTagsSubModifiers : SubModifier<ProjectSettings>
    {
        #region Properties
        [SerializeField] TagListGestion m_TagListGestion;

        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;
                m_TagListGestion.Interactable = value;
            }
        }

        public ReadOnlyCollection<Data.Tags.Tag> ModifiedTags
        {
            get
            {
                return m_TagListGestion.ModifiedTags;
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();
            m_TagListGestion.Initialize();
        }
        #endregion

        #region Protected Methods

        protected override void SetFields(ProjectSettings objectToDisplay)
        {
            base.SetFields(objectToDisplay);
            m_TagListGestion.Objects = objectToDisplay.PatientsTags;
        }
        #endregion
    }
}