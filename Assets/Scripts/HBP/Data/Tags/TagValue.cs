namespace HBP.Data.Tags
{
    public class TagValue
    {
        #region Properties
        public Tag Tag { get; set; }
        public object Value { get; set; }
        #endregion

        #region Constructors
        public TagValue() : this(null, null)
        {

        }
        public TagValue(Tag tag) : this(tag, null)
        {

        }
        public TagValue(Tag tag, object value)
        {
            Tag = tag;
            Value = value;
        }
        #endregion
    }
}