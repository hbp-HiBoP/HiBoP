using HBP.Data.General;
using UnityEngine;

namespace HBP.UI.General
{
    public class GeneralSubModifer : SubModifier<ProjectSettings>
    {
        #region Properties
        [SerializeField] InfoSubModifier m_InfoSubModifier;
        [SerializeField] DatabaseSubModifier m_DatabaseSubModifier;
        [SerializeField] AliasesSubModifier m_AliasesSubModifier;

        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;
                m_InfoSubModifier.Interactable = value;
                m_DatabaseSubModifier.Interactable = value;
                m_AliasesSubModifier.Interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();
            m_InfoSubModifier.Initialize();
            m_DatabaseSubModifier.Initialize();
            m_AliasesSubModifier.Initialize();
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(ProjectSettings objectToDisplay)
        {
            base.SetFields(objectToDisplay);
            m_InfoSubModifier.Object = objectToDisplay;
            m_DatabaseSubModifier.Object = objectToDisplay;
            m_AliasesSubModifier.Object = objectToDisplay;
        }
        #endregion
    }
}