using System;
using System.ComponentModel;

namespace HBP.Data.Tags
{
    [DisplayName("String")]
    public class StringTag : Tag
    {
        #region Properties
        #endregion

        #region Constructors
        public StringTag() : this("")
        {
        }
        public StringTag(string name) : base(name)
        {
        }
        public StringTag(string name, string ID) : base(name, ID)
        {

        }
        #endregion

        #region Public Methods
        public string Convert(object value)
        {
            if (value != null && value is string)
            {
                return (string)value;
            }
            else
            {
                throw new Exception("Wrong value type");
            }
        }
        public override object Clone()
        {
            return new StringTag(Name, ID);
        }
        #endregion
    }
}