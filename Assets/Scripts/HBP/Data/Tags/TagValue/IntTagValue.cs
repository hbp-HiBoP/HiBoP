using System;
using UnityEngine;

namespace HBP.Data.Tags
{
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
        public IntTagValue(IntTag tag, int value, string ID) : base(tag, value, ID)
        {
        }
        public IntTagValue(IntTag tag, int value) : base(tag, value)
        {
        }
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