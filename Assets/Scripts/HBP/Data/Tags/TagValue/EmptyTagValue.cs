namespace HBP.Data.Tags
{
    public class EmptyTagValue : TagValue<EmptyTag, object>
    {
        #region Properties
        public override object Value
        {
            get
            {
                return null;
            }
        }

        public override string DisplayableValue => "Empty";
        #endregion

        #region Constructors
        public EmptyTagValue(EmptyTag tag, string ID) : base(tag, null, ID)
        {
        }
        public EmptyTagValue(EmptyTag tag) : base(tag, null)
        {
        }
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