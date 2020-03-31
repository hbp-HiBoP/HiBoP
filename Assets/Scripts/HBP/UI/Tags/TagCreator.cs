using HBP.Data;
using Tools.Unity.Components;

namespace HBP.UI
{
    /// <summary>
    /// Component to create tags.
    /// </summary>
    public class TagCreator : ObjectCreator<BaseTag>
    {
        #region Public Methods
        /// <summary>
        /// Create a new Tag from scratch.
        /// </summary>
        public override void CreateFromScratch()
        {
            OpenModifier(new EmptyTag());
        }
        #endregion
    }
}