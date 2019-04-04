using System;

namespace HBP.Data.Tags
{
    public class EnumTag : Tag
    {
        #region Properties
        public string[] PossibleValues { get; set; }
        #endregion

        #region Constructors
        public EnumTag() : this("", new string[0])
        {
        }
        public EnumTag(string name) : this(name, new string[0])
        {
        }
        public EnumTag(string name, string[] possibleValues) : base(name)
        {
            PossibleValues = possibleValues;
        }
        #endregion

        #region Public Methods
        public string Convert(object value)
        {
            if (value != null && value is int)
            {
                int intValue = (int)value;
                if (intValue >= 0 && intValue < PossibleValues.Length)
                {
                    return PossibleValues[intValue];
                }
                else
                {
                    throw new Exception("Wrong value range");
                }
            }
            else
            {
                throw new Exception("Wrong value type");
            }
        }
        public override object Clone()
        {
            return new EnumTag(Name.Clone() as string, PossibleValues.Clone() as string[]);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            PossibleValues = (copy as EnumTag).PossibleValues;
        }
        #endregion
    }
}