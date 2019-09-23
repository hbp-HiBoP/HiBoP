namespace HBP.Data.Tags
{
    public class FloatTagValue : TagValue<FloatTag, float>
    {
        #region Properties
        public override FloatTag Tag
        {
            get => base.Tag;
            set
            {
                base.Tag = value;
                base.Tag.OnNeedToRecalculateValue.AddListener(() => Value = Value);
            }
        }
        public override float Value
        {
            get => base.Value;
            set
            {
                if (Tag != null) base.Value = Tag.Clamp(value);
            }
        }
        #endregion

        #region Constructors
        public FloatTagValue(FloatTag tag, float value, string ID) : base(tag, value, ID)
        {

        }
        public FloatTagValue(FloatTag tag, float value) : base(tag, value)
        {

        }
        public FloatTagValue() : this(null, default)
        {
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new FloatTagValue(Tag, Value, ID);
        }
        #endregion
    }
}