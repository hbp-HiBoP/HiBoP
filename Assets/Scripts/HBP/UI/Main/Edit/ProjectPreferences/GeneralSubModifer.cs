using UnityEngine;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    public class GeneralSubModifer : SubModifier<Core.Data.ProjectPreferences>
    {
        #region Properties
        [SerializeField] InfoSubModifier m_InfoSubModifier;
        [SerializeField] DatabaseSubModifier m_DatabaseSubModifier;

        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;
                m_InfoSubModifier.Interactable = value;
                m_DatabaseSubModifier.Interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Save()
        {
            m_InfoSubModifier.Save();
            m_DatabaseSubModifier.Save();
            base.Save();
        }
        public override void Initialize()
        {
            base.Initialize();
            m_InfoSubModifier.Initialize();
            m_DatabaseSubModifier.Initialize();
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(Core.Data.ProjectPreferences objectToDisplay)
        {
            base.SetFields(objectToDisplay);
            m_InfoSubModifier.Object = objectToDisplay;
            m_DatabaseSubModifier.Object = objectToDisplay;
        }
        #endregion
    }
}