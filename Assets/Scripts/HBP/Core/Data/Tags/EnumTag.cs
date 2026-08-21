using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine.Scripting;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn), Preserve, DisplayName("Enumerable")]
    public class EnumTag : BaseTag
    {
        #region Properties

        private string[] m_Values = Array.Empty<string>();
        private readonly object m_ValuesLock = new();

        [JsonProperty] public string[] Values
        {
            get => m_Values;
            set
            {
                lock (m_ValuesLock)
                {
                    string[] values = value ?? Array.Empty<string>();
                    if (values.Any(item => item == null))
                    {
                        throw new ArgumentException("Enum values cannot contain null.", nameof(value));
                    }

                    if (values.Distinct(StringComparer.Ordinal).Count() != values.Length)
                    {
                        throw new ArgumentException("Enum values must be unique.", nameof(value));
                    }

                    m_Values = values.ToArray();
                }
            }
        }

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
            if (value < 0 || value >= Values.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, $"Enum tag '{Name}' contains {Values.Length} values.");
            }

            return value;
        }

        public bool TryGetValueIndex(string value, out int index)
        {
            index = Array.IndexOf(Values, value);
            return index >= 0;
        }

        public int GetOrAddValue(string value)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            lock (m_ValuesLock)
            {
                if (TryGetValueIndex(value, out int index))
                {
                    return index;
                }

                Values = Values.Append(value).ToArray();
                return Values.Length - 1;
            }
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
                Values = enumTag.Values.ToArray();
            }
        }

        public override BaseTagValue CreateValue(string value)
        {
            return string.IsNullOrEmpty(value) ? null : new EnumTagValue(this, GetOrAddValue(value));
        }

        #endregion
    }
}
