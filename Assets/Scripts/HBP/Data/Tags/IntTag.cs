using System;
using System.Runtime.Serialization;

namespace HBP.Data.Tags
{
    public class IntTag : Tag
    {
        #region Properties
        [DataMember] public bool Limited { get; set; }
        [DataMember] public int Min { get; set; }
        [DataMember] public int Max { get; set; }
        #endregion

        #region Constructors
        public IntTag() : this("", false, 0, 0)
        {
        }
        public IntTag(string name) : this (name, false, 0, 0)
        {

        }
        public IntTag(string name, bool limited, int min, int max) : base(name)
        {
            Limited = limited;
            Min = min;
            Max = max;
        }
        #endregion

        #region Public Methods
        public int Convert(object value)
        {
            if (value != null && value is int)
            {
                return (int)value;
            }
            else
            {
                throw new Exception("Wrong value type");
            }
        }
        public override object Clone()
        {
            return new IntTag(Name.Clone() as string, Limited, Min, Max);
        }
        #endregion
    }
}