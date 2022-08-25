namespace HBP.Core.Data
{
    public class CCEPData : EpochedData
    {
        #region Properties
        public virtual string StimulatedChannel { get; set; }
        #endregion

        #region Constructors
        public CCEPData(CCEPDataInfo dataInfo) : base (dataInfo)
        {
            StimulatedChannel = dataInfo.StimulatedChannel;
        }
        #endregion

        #region Public Methods
        public override void Clear()
        {
            base.Clear();
            StimulatedChannel = "";
        }
        #endregion
    }
}