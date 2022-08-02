namespace HBP.UI
{
    public class MeshCreator : ObjectCreator<Core.Data.BaseMesh>
    {
        /// <summary>
        /// Create a new Mesh from scratch.
        /// </summary>
        public override void CreateFromScratch()
        {
            OpenModifier(new Core.Data.LeftRightMesh());
        }
    }
}