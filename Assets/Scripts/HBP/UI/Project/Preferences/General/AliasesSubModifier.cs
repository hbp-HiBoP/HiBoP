using HBP.Data.General;
using HBP.UI.Alias;
using UnityEngine;

namespace HBP.UI.General
{
    public class AliasesSubModifier : SubModifier<ProjectSettings>
    {
        #region Properties
        [SerializeField] AliasListGestion m_AliasListGestion;

        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;
                m_AliasListGestion.Interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();
            m_AliasListGestion.Initialize();
        }
        #endregion

        #region Protected Methods

        protected override void SetFields(ProjectSettings objectToDisplay)
        {
            base.SetFields(objectToDisplay);
            m_AliasListGestion.Objects = objectToDisplay.Aliases;
        }
        #endregion
    }
}