using HBP.UI.Tools;

namespace HBP.UI.Main
{
    public class FMRIDataInfoSubModifier : SubModifier<Core.Data.FMRIDataInfo>
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
        protected override void SetFields(Core.Data.FMRIDataInfo objectToDisplay)
        {
        }
        #endregion
    }
}