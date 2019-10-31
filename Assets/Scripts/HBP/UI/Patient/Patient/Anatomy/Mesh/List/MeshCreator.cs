using Tools.Unity.Components;
using d = HBP.Data.Anatomy;

namespace HBP.UI.Anatomy
{
    public class MeshCreator : ObjectCreator<d.Mesh>
    {
        protected override void OnSaveCreator(CreatorWindow creatorWindow)
        {
            Data.Enums.CreationType type = creatorWindow.Type;
            d.Mesh item = new d.LeftRightMesh();
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