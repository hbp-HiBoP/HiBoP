using HBP.UI.Tools;

namespace HBP.UI.Main
{
    public class MEGvDataInfoSubModifier : SubModifier<Core.Data.MEGvDataInfo>
    {
        #region Properties     
        public override bool Interactable
        {
            get
            {
                return m_Interactable;
            }
            set
            {
                base.Interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();
        }
        #endregion

        #region Protected Methods
        protected override void SetFields(Core.Data.MEGvDataInfo objectToDisplay)
        {
        }
        #endregion
    }
}