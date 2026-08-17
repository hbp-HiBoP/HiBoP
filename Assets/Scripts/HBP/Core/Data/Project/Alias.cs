using HBP.Core.Interfaces;
using HBP.Core.Tools;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Scripting;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn), Preserve]
    public class Alias : BaseData, INameable
    {
        #region Properties

        [JsonProperty] public string Key { get; set; }
        [JsonProperty("Value")] private string m_Value;

        public string Value
        {
            get => m_Value.StandardizeToEnvironement();
            set => m_Value = value.StandardizeToEnvironement();
        }

        string INameable.Name
        {
            get => Key;
            set => Key = value;
        }

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
