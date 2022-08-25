using HBP.UI.Tools;

namespace HBP.UI.Main
{
    /// <summary>
    /// Component to create DataInfo.
    /// </summary>
    public class DataInfoCreator : ObjectCreator<Core.Data.DataInfo>
    {
        /// <summary>
        /// Create a new DataInfo from scratch.
        /// </summary>
        public override void CreateFromScratch()
        {
            OpenModifier(new Core.Data.IEEGDataInfo());
        }
    }
}