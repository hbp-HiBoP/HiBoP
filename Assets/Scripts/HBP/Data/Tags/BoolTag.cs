using System;

namespace HBP.Data.Tags
{
    public class BoolTag : Tag
    {
        #region Properties
        #endregion

        #region Constructors
        public BoolTag() : base()
        {
        }
        public BoolTag(string name) : base(name)
        {
        }
        #endregion

        #region Public Methods
        public bool Convert(object value)
        {
            if (value != null && value is bool)
            {
                return (bool)value;
            }
            else
            {
                throw new Exception("Wrong value type");
            }
        }
        public override object Clone()
        {
            return new BoolTag(Name.Clone() as string);
        }
        #endregion
    }
}