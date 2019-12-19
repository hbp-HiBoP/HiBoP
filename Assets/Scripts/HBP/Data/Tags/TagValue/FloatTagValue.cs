namespace HBP.Data
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
                base.Tag.OnNeedToRecalculateValue.AddListener(RecalculateValue);
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

        #region Public Methods
        public override object Clone()
        {
            return new FloatTagValue(Tag, Value, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is BaseTagValue baseTagValue)
            {
                if (baseTagValue.Value is int intValue)
                {
                    Value = intValue;
                }
                if (baseTagValue.Value is string stringValue)
                {
                    if (float.TryParse(stringValue, out float floatValue))
                    {
                        Value = floatValue;
                    }
                }
            }
        }
        #endregion

        #region Private Methods
        void RecalculateValue()
        {
            Value = Value;
        }
        #endregion
    }
}