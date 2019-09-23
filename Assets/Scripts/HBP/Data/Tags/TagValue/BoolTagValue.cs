namespace HBP.Data.Tags
{
    public class BoolTagValue : TagValue<BoolTag, bool>
    {
        #region Constructors
        public BoolTagValue(BoolTag tag, bool value, string ID) : base(tag, value, ID)
        {
        }
        public BoolTagValue(BoolTag tag, bool value) : base(tag, value)
        {
        }
        public BoolTagValue() : this(null, default)
        {
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new BoolTagValue(Tag, Value, ID);
        }
        #endregion
    }
}