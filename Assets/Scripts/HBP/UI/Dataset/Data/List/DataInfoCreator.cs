using Tools.Unity.Components;
using HBP.Data.Experience.Dataset;

namespace HBP.UI.Experience.Dataset
{
    public class DataInfoCreator : ObjectCreator<DataInfo>
    {
        public override void CreateFromScratch()
        {
            OpenModifier(new IEEGDataInfo());
        }
    }
}