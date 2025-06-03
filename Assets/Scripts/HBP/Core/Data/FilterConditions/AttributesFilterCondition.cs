using HBP.Core.Object3D;
using HBP.Core.Tools;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml.Linq;
using UnityEngine;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn), DisplayName("Attributes"), SortingOrder(2), FilterCondition(typeof(Object3D.Site))]
    public class AttributesFilterCondition : BaseFilterCondition
    {
        #region Enums
        public enum AttributeType { Highlighted, Blacklisted, Label, Color }
        #endregion

        #region Properties
        [JsonProperty("Type")] public AttributeType Type { get; set; }
        [JsonProperty("LabelValue")] public string LabelValue { get; set; }
        [JsonProperty("ExactMatch")] public bool ExactMatch { get; set; }
        [JsonProperty("CaseSensitive")] public bool CaseSensitive { get; set; }
        [JsonProperty("Color")] private SerializableColor m_Color;
        public Color Color
        {
            get => m_Color.ToColor();
            set => m_Color = new SerializableColor(value);
        }

        public override string Description
        {
            get
            {
                return Type switch
                {
                    AttributeType.Highlighted => $"The site is{(IsNot ? " not" : "")} highlighted",
                    AttributeType.Blacklisted => $"The site is{(IsNot ? " not" : "")} blacklisted",
                    AttributeType.Label => $"The site {(IsNot ? "does not have" : "has")} a label which {(ExactMatch ? "is exactly" : "contains")} \"{LabelValue}\" (case {(CaseSensitive ? "sensitive" : "insensitive")})",
                    AttributeType.Color => $"The site's color is{(IsNot ? " not" : "")} <color={Color.ToHexString()}>{Color.ToHexString()}</color>",
                    _ => "Invalid condition",
                };
            }
        }
        #endregion

        #region Constructeurs
        public AttributesFilterCondition() : this(AttributeType.Highlighted, "", false, false, SiteState.DefaultColor, false) { }
        public AttributesFilterCondition(AttributeType type, string labelValue, bool exactMatch, bool caseSensitive, Color color, bool isNot) : base(isNot)
        {
            Type = type;
            LabelValue = labelValue;
            ExactMatch = exactMatch;
            CaseSensitive = caseSensitive;
            Color = color;
        }
        public AttributesFilterCondition(AttributeType type, string labelValue, bool exactMatch, bool caseSensitive, Color color, bool isNot, string ID) : base(isNot, ID)
        {
            Type = type;
            LabelValue = labelValue;
            ExactMatch = exactMatch;
            CaseSensitive = caseSensitive;
            Color = color;
        }
        #endregion

        #region Opérateurs
        public override object Clone()
        {
            return new AttributesFilterCondition(Type, LabelValue, ExactMatch, CaseSensitive, Color, IsNot, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is AttributesFilterCondition other)
            {
                Type = other.Type;
                LabelValue = other.LabelValue;
                ExactMatch = other.ExactMatch;
                CaseSensitive = other.CaseSensitive;
                Color = other.Color;
            }
        }
        #endregion

        #region Méthodes publiques
        public override bool Check(object obj)
        {
            if (obj is Object3D.Site site)
            {
                bool result = false;
                switch (Type)
                {
                    case AttributeType.Highlighted:
                        result = site.State.IsHighlighted;
                        break;
                    case AttributeType.Blacklisted:
                        result = site.State.IsBlackListed;
                        break;
                    case AttributeType.Label:
                        List<string> labels = site.State.Labels;
                        string labelToCompare = LabelValue;
                        if (labels.All(l => string.IsNullOrEmpty(l)) && string.IsNullOrEmpty(labelToCompare))
                            return false;

                        if (!CaseSensitive)
                        {
                            labels = labels.Select(l => l.ToLower()).ToList();
                            labelToCompare = labelToCompare.ToLower();
                        }
                        if (ExactMatch)
                        {
                            result = labels.Contains(labelToCompare);
                        }
                        else
                        {
                            result = labels.Any(l => l.Contains(labelToCompare));
                        }
                        break;
                    case AttributeType.Color:
                        result = site.State.Color.Equals(Color);
                        break;
                }
                return result != IsNot;
            }
            return false;
        }
        #endregion
    }
}