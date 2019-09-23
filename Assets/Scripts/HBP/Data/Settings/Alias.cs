using System.Runtime.Serialization;

namespace HBP.Data.Preferences
{
    [DataContract]
    public class Alias : BaseData
    {
        #region Properties
        [DataMember] public string Key { get; set; }
        [DataMember] public string Value { get; set; }
        #endregion

        #region Constructors
        public Alias() : this("New Key", "New Value")
        {

        }
        public Alias(string key, string value, string ID) : base(ID)
        {
            Key = key;
            Value = value;
        }
        public Alias(string key, string value) : base()
        {
            Key = key;
            Value = value;
        }
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

        #region Operators
        public override object Clone()
        {
            return new Alias(Key, Value, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if(copy is Alias alias)
            {
                Key = alias.Key;
                Value = alias.Value;
            }
        }
        #endregion
    }
}