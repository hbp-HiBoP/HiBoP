namespace HBP.Data
{
    /// <summary>
    /// A base abstract generic class which contains all the data about a value and its associated tag.
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
    /// <description>Value associated with the tag.</description>
    /// </item>
    /// </list>
    /// </remarks>
    public abstract class TagValue<T, I> : BaseTagValue where T : BaseTag
    {
        #region Properties
        /// <summary>
        /// Tag associated with the value.
        /// </summary>
        public virtual new T Tag
        {
            get
            {
                return (T)base.Tag;
            }
            set
            {
                base.Tag = value;
            }
        }
        /// <summary>
        /// Value associated with the tag.
        /// </summary>
        public virtual new I Value
        {
            get
            {
                dynamic value = base.Value;
                return (I)value;
            }
            set
            {
                if (Tag != null)
                {
                    base.Value = value;
                }
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new instance of TagValue.
        /// </summary>
        /// <param name="tag">Tag associated with the value</param>
        /// <param name="value">Value associated with the tag</param>
        /// <param name="ID">Unique identifier</param>
        public TagValue(T tag, I value, string ID) : base(tag, value, ID)
        {
            Value = value;
        }
        /// <summary>
        /// Create a new instance of TagValue.
        /// </summary>
        /// <param name="tag">Tag associated with the value</param>
        /// <param name="value">Value associated with the tag</param>
        public TagValue(T tag, I value) : base(tag, value)
        {
            Value = value;
        }
        /// <summary>
        /// Create a new instance of TagValue.
        /// </summary>
        public TagValue() : this(null, default)
        {
        }
        #endregion

        #region Public Methods
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is BaseTagValue baseTagValue)
            {
                if (baseTagValue.Tag is T tag)
                {
                    Tag = tag;
                }
                if (baseTagValue.Value is I value)
                {
                    Value = value;
                }
            }
        }
        #endregion
    }
}