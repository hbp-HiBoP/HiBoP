using System;
using UnityEngine;

namespace HBP.Data
{
    /// <summary>
    /// Class which contains all the data about a enum value and its associated EnumTag.
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
    /// <description>Enum value associated with the EnumTag.</description>
    /// </item>
    /// </list>
    /// </remarks>
    public class EnumTagValue : TagValue<EnumTag, int>
    {
        #region Properties
        public override int Value
        {
            get
            {
                return base.Value;
            }
            set
            {
                if (Tag != null) base.Value = Tag.Clamp(value);
            }
        }
        /// <summary>
        /// String value associated with the tag.
        /// </summary>
        public string StringValue
        {
            set
            {
                if (Tag != null) Value = Array.IndexOf(Tag.Values, value);
            }
        }
        public override string DisplayableValue
        {
            get
            {
                if (Tag == null || Value < 0 || Value >= Tag.Values.Length)
                {
                    return "None";
                }
                else
                {
                    return Tag.Values[Value];
                }
            }
        }
        #endregion

        #region Constructors
        /// <summary>
        /// Create a new instance of EnumTagValue.
        /// </summary>
        public EnumTagValue() : this(null, default(int))
        {
        }
        /// <summary>
        /// Create a new instance of EnumTagValue.
        /// </summary>
        /// <param name="tag">Tag associated with the value</param>
        /// <param name="value">Integer value associated with the tag</param>
        public EnumTagValue(EnumTag tag, int value) : base(tag, value)
        {
        }
        /// <summary>
        /// Create a new instance of EnumTagValue.
        /// </summary>
        /// <param name="tag">Tag associated with the value</param>
        /// <param name="value">String value associated with the tag</param>
        public EnumTagValue(EnumTag tag, string value) : base(tag, 0)
        {
            StringValue = value;
        }
        /// <summary>
        /// Create a new instance of EnumTagValue.
        /// </summary>
        /// <param name="tag">Tag associated with the value</param>
        /// <param name="value">Value associated with the tag</param>
        /// <param name="ID">Unique identifier</param>
        public EnumTagValue(EnumTag tag, int value, string ID) : base(tag, value, ID)
        {
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new EnumTagValue(Tag, Value, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is FloatTagValue floatTagValue)
            {
                Value = Mathf.RoundToInt(floatTagValue.Value);
            }
            if (copy is StringTagValue stringTagValue)
            {
                Value = Array.FindIndex(Tag.Values, t => t == stringTagValue.Value);
            }
        }
        #endregion
    }
}