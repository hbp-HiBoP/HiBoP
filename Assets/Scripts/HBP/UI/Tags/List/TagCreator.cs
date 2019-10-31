using HBP.Data.Tags;
using Tools.Unity.Components;

namespace HBP.UI.Tags
{
    public class TagCreator : ObjectCreator<Tag>
    {
        protected override void OnSaveCreator(CreatorWindow creatorWindow)
        {
            Data.Enums.CreationType type = creatorWindow.Type;
            Tag item = new EmptyTag();
            switch (type)
            {
                case Data.Enums.CreationType.FromScratch:
                    OpenModifier(item);
                    break;
                case Data.Enums.CreationType.FromExistingItem:
                    OpenSelector(ExistingItems);
                    break;
                case Data.Enums.CreationType.FromFile:
                    if (LoadFromFile(out item)) OpenModifier(item);
                    break;
            }
        }
    }
}