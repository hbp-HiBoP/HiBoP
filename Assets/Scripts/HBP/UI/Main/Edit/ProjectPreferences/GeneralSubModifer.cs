using UnityEngine;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    public class GeneralSubModifer : SubModifier<Core.Data.ProjectPreferences>
    {
        #region Properties

        [SerializeField] InfoSubModifier m_InfoSubModifier;

        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;
                m_InfoSubModifier.Interactable = value;
            }
        }

        #endregion

        #region Public Methods

        public override void Save()
        {
            m_InfoSubModifier.Save();
            base.Save();
        }

        public override void Initialize()
        {
            base.Initialize();
            m_InfoSubModifier.Initialize();
        }

        #endregion

        #region Protected Methods

        protected override void SetFields(Core.Data.ProjectPreferences objectToDisplay)
        {
            base.SetFields(objectToDisplay);
            m_InfoSubModifier.Object = objectToDisplay;
        }

        #endregion
    }
}
