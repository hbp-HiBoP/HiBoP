using UnityEngine;
using HBP.Core.Interfaces;
using Newtonsoft.Json;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn)]
    public class Alias : BaseData, INameable
    {
        #region Properties
        [JsonProperty] public string Key { get; set; }
        [JsonProperty] public string Value { get; set; }
        string INameable.Name { get => Key; set => Key = value; }
        #endregion

        #region Constructors
        public Alias() : this("New Key", "New Value")
        {

        }
        public Alias(string key, string value, string ID) : base(ID)
        {
            Key = key;
            Value = string.IsNullOrEmpty(value) ? Application.dataPath : value;
        }
        public Alias(string key, string value) : base()
        {
            Key = key;
            Value = string.IsNullOrEmpty(value) ? Application.dataPath : value;
        }
        #endregion

        #region Public Methods
        public void ConvertKeyToValue(ref string s)
        {
            if (string.IsNullOrEmpty(Value)) return;
            s = s.Replace(Key, Value);
        }
        public void ConvertValueToKey(ref string s)
        {
            if (string.IsNullOrEmpty(Value)) return;
            s = s.Replace(Value, Key);
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new Alias(Key, Value, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is Alias alias)
            {
                Key = alias.Key;
                Value = alias.Value;
            }
        }
        #endregion
    }
}