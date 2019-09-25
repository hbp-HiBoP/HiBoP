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