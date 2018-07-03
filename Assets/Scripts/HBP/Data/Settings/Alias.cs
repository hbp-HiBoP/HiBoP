using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.Data.Preferences
{
    public class Alias
    {
        #region Properties
        public string Key { get; set; }
        public string Value { get; set; }
        #endregion

        #region Public Methods
        public void ConvertKeyToValue(ref string s)
        {
            s = s.Replace(Key, Value);
        }
        public void ConvertValueToKey(ref string s)
        {
            s = s.Replace(Value, Key);
        }
        #endregion
    }
}