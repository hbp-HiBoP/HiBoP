namespace HBP.Data.Tags
{
    public class StringTagValue : TagValue<StringTag, string>
    {
        #region Constructors
        public StringTagValue(StringTag tag, string value, string ID) : base(tag, value, ID)
        {
        }
        public StringTagValue(StringTag tag, string value) : base(tag, value)
        {
        }
        public StringTagValue() : this(null, default)
        {
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new StringTagValue(Tag, Value, ID);
        }
        #endregion
    }
}