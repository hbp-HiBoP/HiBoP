using System;
using System.ComponentModel;

namespace HBP.Data
{
    [DisplayName("Boolean")]
    public class BoolTag : BaseTag
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
        public BoolTag(string name, string ID) : base(name, ID)
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
            return new BoolTag(Name, ID);
        }
        #endregion
    }
}