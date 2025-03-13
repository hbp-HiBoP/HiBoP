using Newtonsoft.Json;
using System.ComponentModel;
using System.Linq;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn), DisplayName("Tag"), SortingOrder(4), PatientFilter]
    public class TagFilterCondition : BaseFilterCondition
    {
        #region Properties
        public override string Description
        {
            get
            {
                if (Tag != null)
                {
                    if (Target == TargetType.Patient)
                    {
                        return $"The patient has the tag \"{Tag.Name}\" with value {(ExactMatch ? (IsNot ? "not equal to" : "equal to") : (IsNot ? "not containing" : "containing"))} \"{Value}\" (case {(CaseSensitive ? "sensitive" : "insensitive")})";
                    }
                    else if (Target == TargetType.Site)
                    {
                        return $"The patient has a site with the tag \"{Tag.Name}\" with value {(ExactMatch ? (IsNot ? "not equal to" : "equal to") : (IsNot ? "not containing" : "containing"))} \"{Value}\" (case {(CaseSensitive ? "sensitive" : "insensitive")})";
                    }
                }
                return "Filter not supported";
            }
        }

        public enum TargetType { Patient, Site }
        public TargetType Target { get; set; }

        public BaseTag Tag { get; set; }
        public string Value { get; set; }
        public bool ExactMatch { get; set; }
        public bool CaseSensitive { get; set; }
        #endregion

        #region Constructors
        public TagFilterCondition() : this(TargetType.Patient, null, "", false, false, false)
        {
        }
        public TagFilterCondition(TargetType target, BaseTag tag, string value, bool exactMatch, bool caseSensitive, bool isNot) : base(isNot)
        {
            Target = target;
            Tag = tag;
            Value = value;
            ExactMatch = exactMatch;
            CaseSensitive = caseSensitive;
        }
        public TagFilterCondition(TargetType target, BaseTag tag, string value, bool exactMatch, bool caseSensitive, bool isNot, string ID) : base(isNot, ID)
        {
            Target = target;
            Tag = tag;
            Value = value;
            ExactMatch = exactMatch;
            CaseSensitive = caseSensitive;
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new TagFilterCondition(Target, Tag, Value, ExactMatch, CaseSensitive, IsNot, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is TagFilterCondition tagFilterCondition)
            {
                Target = tagFilterCondition.Target;
                Tag = tagFilterCondition.Tag;
                Value = tagFilterCondition.Value;
                ExactMatch = tagFilterCondition.ExactMatch;
                CaseSensitive = tagFilterCondition.CaseSensitive;
            }
        }
        #endregion

        #region Public Methods
        public override bool Check(BaseData obj)
        {
            bool compareValue(string value, string valueToCompare)
            {
                if (!CaseSensitive)
                {
                    value = value.ToLower();
                    valueToCompare = valueToCompare.ToLower();
                }
                if (ExactMatch)
                {
                    return value == valueToCompare;
                }
                else
                {
                    return value.Contains(valueToCompare);
                }
            }
            if (obj is Patient patient)
            {
                if (Target == TargetType.Patient)
                {
                    var tagValue = patient.Tags.FirstOrDefault(t => t.Tag == Tag);
                    if (tagValue != null)
                    {
                        return compareValue(tagValue.DisplayableValue, Value) != IsNot;
                    }
                    else
                    {
                        return IsNot;
                    }
                }
                else if (Target == TargetType.Site)
                {
                    var tagValues = patient.Sites.SelectMany(s => s.Tags).Where(t => t.Tag == Tag);
                    if (tagValues.Count() > 0)
                    {
                        return tagValues.Any(t => compareValue(t.DisplayableValue, Value)) != IsNot;
                    }
                    else
                    {
                        return IsNot;
                    }
                }
            }
            return false;
        }
        #endregion
    }
}