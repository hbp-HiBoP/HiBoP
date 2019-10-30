using Tools.Unity.Components;
using HBP.Data.Experience.Dataset;

namespace HBP.UI.Experience.Dataset
{
    public class DataInfoCreator : ObjectCreator<DataInfo>
    {
        protected override void OnSaveCreator(CreatorWindow creatorWindow)
        {
            Data.Enums.CreationType type = creatorWindow.Type;
            DataInfo item = new iEEGDataInfo();
            switch (type)
            {
                case Data.Enums.CreationType.FromScratch:
                    OpenModifier(item);
                    break;
                case Data.Enums.CreationType.FromExistingItem:
                    OpenSelector(ExistingItems);
                    break;
                case Data.Enums.CreationType.FromFile:
                    if (LoadFromFile(out item))
                    {
                        OpenModifier(item);
                    }
                    break;
            }
        }
    }
}