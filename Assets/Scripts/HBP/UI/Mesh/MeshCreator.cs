using Tools.Unity.Components;

namespace HBP.UI
{
    public class MeshCreator : ObjectCreator<Data.BaseMesh>
    {
        protected override void OnSaveCreator(CreatorWindow creatorWindow)
        {
            Data.Enums.CreationType type = creatorWindow.Type;
            Data.BaseMesh item = new Data.LeftRightMesh();
            switch (type)
            {
                case Data.Enums.CreationType.FromScratch:
                    OpenModifier(item);
                    break;
                case Data.Enums.CreationType.FromExistingObject:
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