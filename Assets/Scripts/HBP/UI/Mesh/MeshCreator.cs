using HBP.Data;
using Tools.Unity.Components;

namespace HBP.UI
{
    public class MeshCreator : ObjectCreator<Data.BaseMesh>
    {
        public override void CreateFromScratch()
        {
            OpenModifier(new LeftRightMesh());
        }
    }
}