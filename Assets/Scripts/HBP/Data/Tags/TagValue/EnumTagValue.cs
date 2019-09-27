using System;
using UnityEngine;

namespace HBP.Data.Tags
{
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
        public override string DisplayableValue
        {
            get
            {
                if(Tag == null || Value < 0 || Value >= Tag.Values.Length)
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
        public EnumTagValue() : this(null, default)
        {
        }
        public EnumTagValue(EnumTag tag, int value) : base(tag, value)
        {
        }
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
            if(copy is FloatTagValue floatTagValue)
            {
                Value = Mathf.RoundToInt(floatTagValue.Value);
            }
            if(copy is StringTagValue stringTagValue)
            {
                Value = Array.FindIndex(Tag.Values, t => t == stringTagValue.Value);
            }
        }
        #endregion
    }
}