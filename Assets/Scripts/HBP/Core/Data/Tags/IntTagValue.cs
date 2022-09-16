using System;
using UnityEngine;

namespace HBP.Core.Data
{
    /// <summary>
    /// Class which contains all the data about a int value and its associated IntTag.
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
    /// <description>Integer value associated with the FloatTag.</description>
    /// </item>
    /// </list>
    /// </remarks>
    public class IntTagValue : TagValue<IntTag, int>
    {
        #region Properties
        public override IntTag Tag
        {
            get => base.Tag;
            set
            {
                base.Tag = value;
                base.Tag.OnNeedToRecalculateValue.AddListener(() => Value = Value);
            }
        }
        public override int Value
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
        /// Create a new instance of IntTagValue.
        /// </summary>
        /// <param name="tag">Tag associated with the value</param>
        /// <param name="value">Value associated with the tag</param>
        /// <param name="ID">Unique identifier</param>
        public IntTagValue(IntTag tag, int value, string ID) : base(tag, value, ID)
        {
        }
        /// <summary>
        /// Create a new instance of IntTagValue.
        /// </summary>
        /// <param name="tag">Tag associated with the value</param>
        /// <param name="value">Value associated with the tag</param>
        public IntTagValue(IntTag tag, int value) : base(tag, value)
        {
        }
        /// <summary>
        /// Create a new instance of IntTagValue.
        /// </summary>
        public IntTagValue() : this(null, default)
        {
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new IntTagValue(Tag, Value, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is FloatTagValue floatTagValue)
            {
                Value = Mathf.RoundToInt(floatTagValue.Value);
            }
            if (copy is BoolTagValue boolTagValue)
            {
                Value = Convert.ToInt32(boolTagValue.Value);
            }
        }
        #endregion
    }
}