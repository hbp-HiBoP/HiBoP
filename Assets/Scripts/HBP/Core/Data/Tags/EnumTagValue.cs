using System;
using UnityEngine;
using Newtonsoft.Json;

namespace HBP.Core.Data
{
    public readonly struct EnumValueReference
    {
        public int Index { get; }
        public string Value { get; }

        public EnumValueReference(int index, string value)
        {
            Index = index;
            Value = value;
        }
    }

    /// <summary>
    /// Class which contains all the data about a enum value and its associated EnumTag.
    /// </summary>
    /// <remarks>
    /// <list type="table">
    /// <listheader>
    /// <term>Data</term>
    /// <description>Description</description>
    /// </listheader>
    /// <item>
    /// <term><b>Tag</b></term>
    /// <description>Tag associated with the value.</description>
    /// </item>
    /// <item>
    /// <term><b>Value</b></term>
    /// <description>Enum value associated with the EnumTag.</description>
    /// </item>
    /// </list>
    /// </remarks>
    public class EnumTagValue : TagValue<EnumTag, int>
    {
        #region Properties

        public override EnumTag Tag
        {
            get => base.Tag;
            set
            {
                if (value == null)
                {
                    base.Tag = null;
                }
                else
                {
                    BindTag(value);
                }
            }
        }

        public override int Value
        {
            get { return base.Value; }
            set
            {
                if (Tag != null)
                {
                    int index = Tag.Clamp(value);
                    base.Value = index;
                    m_StringValue = Tag.Values[index];
                }
            }
        }

        [JsonProperty("StringValue")] private string m_StringValue;

        /// <summary>
        /// String value associated with the tag.
        /// </summary>
        public string StringValue
        {
            get => m_StringValue;
            set
            {
                if (Tag == null)
                {
                    m_StringValue = value;
                    return;
                }

                int index = Array.IndexOf(Tag.Values, value);
                if (index < 0)
                {
                    throw new ArgumentException($"'{value}' is not a value of enum tag '{Tag.Name}'.", nameof(value));
                }

                Value = index;
            }
        }

        public EnumValueReference Reference => new(Value, StringValue);

        public override string DisplayableValue
        {
            get
            {
                if (Tag == null || Value < 0 || Value >= Tag.Values.Length)
                {
                    return $"Invalid enum value ({Value})";
                }
                else
                {
                    return Tag.Values[Value];
                }
            }
        }

        #endregion

        #region Constructors

        /// <summary>
        /// Create a new instance of EnumTagValue.
        /// </summary>
        public EnumTagValue() : this(null, default(int))
        {
        }

        /// <summary>
        /// Create a new instance of EnumTagValue.
        /// </summary>
        /// <param name="tag">Tag associated with the value</param>
        /// <param name="value">Integer value associated with the tag</param>
        public EnumTagValue(EnumTag tag, int value) : base(tag, default)
        {
            if (tag != null)
            {
                Value = value;
            }
        }

        /// <summary>
        /// Create a new instance of EnumTagValue.
        /// </summary>
        /// <param name="tag">Tag associated with the value</param>
        /// <param name="value">String value associated with the tag</param>
        public EnumTagValue(EnumTag tag, string value) : base(tag, 0)
        {
            StringValue = value;
        }

        /// <summary>
        /// Create a new instance of EnumTagValue.
        /// </summary>
        /// <param name="tag">Tag associated with the value</param>
        /// <param name="value">Value associated with the tag</param>
        /// <param name="ID">Unique identifier</param>
        public EnumTagValue(EnumTag tag, int value, string ID) : base(tag, default, ID)
        {
            if (tag != null)
            {
                Value = value;
            }
        }

        #endregion

        #region Operators

        public override object Clone()
        {
            EnumTagValue clone = new(Tag, Value, ID);
            if (Tag == null)
            {
                clone.m_Value = Value;
                clone.m_TagID = m_TagID;
            }

            clone.m_StringValue = m_StringValue;
            return clone;
        }

        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is EnumTagValue enumTagValue)
            {
                m_StringValue = enumTagValue.m_StringValue;
                if (Tag != null && m_StringValue != null)
                {
                    StringValue = m_StringValue;
                }
            }

            if (copy is FloatTagValue floatTagValue)
            {
                Value = Mathf.RoundToInt(floatTagValue.Value);
            }

            if (copy is StringTagValue stringTagValue)
            {
                Value = Array.FindIndex(Tag.Values, t => t == stringTagValue.Value);
            }
        }

        internal override void BindTag(BaseTag tag)
        {
            if (tag is not EnumTag enumTag)
            {
                throw new InvalidOperationException($"Enum tag value '{ID}' cannot be bound to tag type '{tag?.GetType().Name ?? "null"}'.");
            }

            int index;
            if (m_StringValue != null)
            {
                if (!enumTag.TryGetValueIndex(m_StringValue, out index))
                {
                    throw new InvalidOperationException($"Enum value '{m_StringValue}' from tag value '{ID}' does not exist in enum tag '{enumTag.Name}' ({enumTag.ID}).");
                }
            }
            else
            {
                index = enumTag.Clamp(Value);
            }

            base.Tag = enumTag;
            Value = index;
        }

        internal override void ResolveReferences(LoadingContext context)
        {
            bool isLegacy = m_StringValue == null;
            base.ResolveReferences(context);
            if (isLegacy && Tag != null)
            {
                context.ReportLegacyEnumReference(Tag, false);
            }
        }

        protected override void OnSerializing()
        {
            if (Tag != null && m_StringValue == null)
            {
                Value = Value;
            }

            base.OnSerializing();
        }

        #endregion
    }
}
