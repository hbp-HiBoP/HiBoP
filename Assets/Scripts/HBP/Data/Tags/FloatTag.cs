using System;
using System.ComponentModel;
using System.Runtime.Serialization;

namespace HBP.Data.Tags
{
    [DisplayName("Decimal")]
    public class FloatTag : Tag
    {
        #region Properties
        [DataMember] public bool Limited { get; set; }
        [DataMember] public float Min { get; set; }
        [DataMember] public float Max { get; set; }
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
        public FloatTag(string name, bool limited, float min, float max, string ID) : base(name,ID)
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
            return new FloatTag(Name.Clone() as string, Limited, Min, Max, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if(copy is FloatTag floatTag)
            {
                Limited = floatTag.Limited;
                Min = floatTag.Min;
                Max = floatTag.Max;
            }
        }
        #endregion
    }
}
