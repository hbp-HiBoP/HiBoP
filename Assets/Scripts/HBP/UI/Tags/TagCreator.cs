namespace HBP.UI
{
    /// <summary>
    /// Component to create tags.
    /// </summary>
    public class TagCreator : ObjectCreator<Core.Data.BaseTag>
    {
        #region Public Methods
        /// <summary>
        /// Create a new Tag from scratch.
        /// </summary>
        public override void CreateFromScratch()
        {
            OpenModifier(new Core.Data.EmptyTag());
        }
        #endregion
    }
}