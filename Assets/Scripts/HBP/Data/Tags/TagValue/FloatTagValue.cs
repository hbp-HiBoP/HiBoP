namespace HBP.Core.Data
{
    /// <summary>
    /// Class which contains all the data about a float value and its associated FloatTag.
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
    /// <description>Float value associated with the FloatTag.</description>
    /// </item>
    /// </list>
    /// </remarks>
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
        /// <summary>
        /// Create a new instance of FloatTagValue.
        /// </summary>
        /// <param name="tag">Tag associated with the value</param>
        /// <param name="value">Value associated with the tag</param>
        /// <param name="ID">Unique identifier</param>
        public FloatTagValue(FloatTag tag, float value, string ID) : base(tag, value, ID)
        {

        }
        /// <summary>
        /// Create a new instance of FloatTagValue.
        /// </summary>
        /// <param name="tag">Tag associated with the value</param>
        /// <param name="value">Value associated with the tag</param>
        public FloatTagValue(FloatTag tag, float value) : base(tag, value)
        {

        }
        /// <summary>
        /// Create a new instance of FloatTagValue.
        /// </summary>
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
        /// <summary>
        /// Recalculate the value.
        /// </summary>
        void RecalculateValue()
        {
            Value = Value;
        }
        #endregion
    }
}