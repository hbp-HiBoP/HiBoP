using System;
using System.ComponentModel;

namespace HBP.Core.Data
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
            if (value is not null and bool)
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
        public override BaseTagValue CreateValue(string value)
        {
            if (bool.TryParse(value, out bool result))
            {
                return new BoolTagValue(this, result);
            }
            return null;
        }
        #endregion
    }
}