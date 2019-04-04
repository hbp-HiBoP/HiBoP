﻿using System;

namespace HBP.Data.Tags
{
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
            return new StringTag(Name.Clone() as string);
        }
        #endregion
    }
}