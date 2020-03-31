using HBP.Data;
using Tools.Unity.Components;

namespace HBP.UI
{
    public class MeshCreator : ObjectCreator<BaseMesh>
    {
        /// <summary>
        /// Create a new Mesh from scratch.
        /// </summary>
        public override void CreateFromScratch()
        {
            OpenModifier(new LeftRightMesh());
        }
    }
}