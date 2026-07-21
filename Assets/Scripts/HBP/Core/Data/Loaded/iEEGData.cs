namespace HBP.Core.Data
{
    public class IEEGData : EpochedData
    {
        #region Constructors
        public IEEGData(IEEGDataInfo dataInfo) : base(dataInfo)
        {
        }
        internal IEEGData(IEEGDataInfo dataInfo, DynamicData rawData) : base(dataInfo, rawData)
        {
        }
        #endregion
    }
}
