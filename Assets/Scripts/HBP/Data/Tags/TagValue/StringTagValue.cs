namespace HBP.Data
{
    /// <summary>
    /// Class which contains all the data about a string value and its associated StringTag.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader>
    /// <term>Data</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term><b>Tag</b></term>
    /// <description>Tag associated with the value.</description>
    /// </item>
    /// <item>
    /// <term><b>Value</b></term>
    /// <description>String value associated with the StringTag.</description>
    /// </item>
    /// </list>
    /// </remarks>
    public class StringTagValue : TagValue<StringTag, string>
    {
        #region Constructors
        /// <summary>
        /// Create a new instance of StringTagValue.
        /// </summary>
        /// <param name="tag">Tag associated with the value</param>
        /// <param name="value">Value associated with the tag</param>
        /// <param name="ID">Unique identifier</param>
        public StringTagValue(StringTag tag, string value, string ID) : base(tag, value, ID)
        {
        }
        /// <summary>
        /// Create a new instance of StringTagValue.
        /// </summary>
        /// <param name="tag">Tag associated with the value</param>
        /// <param name="value">Value associated with the tag</param>
        public StringTagValue(StringTag tag, string value) : base(tag, value)
        {
        }
        /// <summary>
        /// Create a new instance of StringTagValue.
        /// </summary>
        public StringTagValue() : this(null, default)
        {
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new StringTagValue(Tag, Value, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if(copy is BaseTagValue baseTagValue)
            {
                Value = baseTagValue.DisplayableValue;
            }
        }
        #endregion
    }
}