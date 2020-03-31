using Tools.Unity.Components;
using HBP.Data.Experience.Dataset;

namespace HBP.UI.Experience.Dataset
{
    /// <summary>
    /// Component to create DataInfo.
    /// </summary>
    public class DataInfoCreator : ObjectCreator<DataInfo>
    {
        /// <summary>
        /// Create a new DataInfo from scratch.
        /// </summary>
        public override void CreateFromScratch()
        {
            OpenModifier(new IEEGDataInfo());
        }
    }
}