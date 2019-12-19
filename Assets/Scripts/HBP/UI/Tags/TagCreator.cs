using Tools.Unity.Components;

namespace HBP.UI
{
    public class TagCreator : ObjectCreator<Data.BaseTag>
    {
        protected override void OnSaveCreator(CreatorWindow creatorWindow)
        {
            Data.Enums.CreationType type = creatorWindow.Type;
            Data.BaseTag item = new Data.EmptyTag();
            switch (type)
            {
                case Data.Enums.CreationType.FromScratch:
                    OpenModifier(item);
                    break;
                case Data.Enums.CreationType.FromExistingObject:
                    OpenSelector(ExistingItems);
                    break;
                case Data.Enums.CreationType.FromFile:
                    if (LoadFromFile(out Data.BaseTag[] items))
                    {
                        if (items.Length == 1)
                        {
                            OpenModifier(items[0]);
                        }
                        else
                        {
                            foreach (var i in items)
                            {
                                OnObjectCreated.Invoke(i);
                            }
                        }
                    }
                    break;
            }
        }
    }
}