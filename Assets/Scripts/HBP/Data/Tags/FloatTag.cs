using System;

namespace HBP.Data.Tags
{
    public class FloatTag : Tag
    {
        #region Properties
        public bool Limited { get; set; }
        public float Min { get; set; }
        public float Max { get; set; }
        #endregion

        #region Constructors
        public FloatTag() : this("", false, 0, 0)
        {
        }
        public FloatTag(string name) : this(name, false, 0, 0)
        {
        }
        public FloatTag(string name, bool limited, float min, float max) : base(name)
        {
            Limited = limited;
            Min = min;
            Max = max;
        }
        #endregion

        #region Public Methods
        public float Convert(object value)
        {
            if (value != null && value is float)
            {
                return (float)value;
            }
            else
            {
                throw new Exception("Wrong value type");
            }
        }
        public override object Clone()
        {
            return new FloatTag(Name.Clone() as string, Limited, Min, Max);
        }
        #endregion
    }
}
