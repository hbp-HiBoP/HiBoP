using System;

namespace HBP.Data
{
    /// <summary>
    /// A class which contains all the data about a boolean value and its associated BoolTag.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader>
    /// <term>Data</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term><b>Tag</b></term>
    /// <description>BoolTag associated with the value.</description>
    /// </item>
    /// <item>
    /// <term><b>Value</b></term>
    /// <description>Boolean value associated with the BoolTag.</description>
    /// </item>
    /// </list>
    /// </remarks>
    public class BoolTagValue : TagValue<BoolTag, bool>
    {
        #region Constructors
        /// <summary>
        /// Create a new instance of BoolTagValue.
        /// </summary>
        /// <param name="tag">Tag associated with the value</param>
        /// <param name="value">Value associated with the tag</param>
        /// <param name="ID">Unique identifier</param>
        public BoolTagValue(BoolTag tag, bool value, string ID) : base(tag, value, ID)
        {
        }
        /// <summary>
        /// Create a new instance of BoolTagValue.
        /// </summary>
        /// <param name="tag">Tag associated with the value</param>
        /// <param name="value">Value associated with the tag</param>
        public BoolTagValue(BoolTag tag, bool value) : base(tag, value)
        {
        }
        /// <summary>
        /// Create a new instance of BoolTagValue.
        /// </summary>
        public BoolTagValue() : this(null, default)
        {
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new BoolTagValue(Tag, Value, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is IntTagValue intTagValue)
            {
                Value = Convert.ToBoolean(intTagValue.Value);
            }
            if (copy is FloatTagValue floatTagValue)
            {
                Value = Convert.ToBoolean(floatTagValue.Value);
            }
        }
        #endregion
    }
}