using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Linq;
using UnityEngine;

namespace HBP.Data
{
    [DisplayName("Enumerable")]
    public class EnumTag : BaseTag
    {
        #region Properties
        [DataMember] public string[] Values { get; set; } = new string[0];
        #endregion

        #region Constructors
        public EnumTag() : this("", new string[0])
        {
        }
        public EnumTag(string name) : this(name, new string[0])
        {
        }
        public EnumTag(string name, string ID) : this(name, new string[0], ID)
        {

        }
        public EnumTag(string name, IEnumerable<string> values) : base(name)
        {
            Values = values.ToArray();
        }
        public EnumTag(string name, IEnumerable<string> values, string ID) : base(name, ID)
        {
            Values = values.ToArray();
        }
        #endregion

        #region Public Methods
        public int Clamp(int value)
        {
            return Mathf.Clamp(value, 0, Values.Length);
        }
        public string Convert(object value)
        {
            if (value != null && value is int)
            {
                int intValue = (int)value;
                if (intValue >= 0 && intValue < Values.Length)
                {
                    return Values[intValue];
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
            return new EnumTag(Name, Values, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is EnumTag enumTag)
            {
                Values = enumTag.Values;
            }
        }
        #endregion
    }
}