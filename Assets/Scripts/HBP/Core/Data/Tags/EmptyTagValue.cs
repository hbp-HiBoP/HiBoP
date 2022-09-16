namespace HBP.Core.Data
{
    /// <summary>
    /// A class which contains all the data about a EmptyTag.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader>
    /// <term>Data</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term><b>Tag</b></term>
    /// <description>Empty.</description>
    /// </item>
    /// </list>
    /// </remarks>
    public class EmptyTagValue : TagValue<EmptyTag, object>
    {
        #region Properties
        /// <summary>
        /// Return null because there is no value associated with EmptyTag.
        /// </summary>
        public override object Value => null;
        /// <summary>
        /// Return "Empty" because there is no value associated with EmptyTag.
        /// </summary>
        public override string DisplayableValue => "Empty";
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new instance of EmptyTagValue.
        /// </summary>
        /// <param name="tag">Tag associated with the value</param>
        /// <param name="ID">Unique identifier</param>
        public EmptyTagValue(EmptyTag tag, string ID) : base(tag, null, ID)
        {
        }
        /// <summary>
        /// Create a new instance of EmptyTagValue.
        /// </summary>
        /// <param name="tag">Tag associated with the value</param>
        public EmptyTagValue(EmptyTag tag) : base(tag, null)
        {
        }
        /// <summary>
        /// Create a new instance of EmptyTagValue.
        /// </summary>
        public EmptyTagValue() : this(null, default)
        {
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new EmptyTagValue(Tag, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            Value = null;
        }
        #endregion
    }
}