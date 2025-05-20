using HBP.Data.Preferences;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Linq;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn), DisplayName("Tag"), SortingOrder(5), FilterCondition(typeof(Patient))]
    public class PatientTagFilterCondition : BaseFilterCondition
    {
        #region Properties
        public override string Description
        {
            get
            {
                if (Tag != null)
                {
                    string result = "";
                    if (Target == TargetType.Patient)
                    {
                        result = $"The patient has the tag \"{Tag.Name}\"{Value.GetDescription(IsNot)}";
                    }
                    else if (Target == TargetType.Sites)
                    {
                        result = $"The patient has a site with the tag \"{Tag.Name}\"{Value.GetDescription(IsNot)}";
                    }

                    if (Tag is EnumTag enumTag)
                    {
                        result += $"{enumTag.Values[Value is EnumTagFilterValue enumValue ? enumValue.Value : 0]}";
                    }

                    return result;
                }
                return "Filter not supported";
            }
        }

        public enum TargetType { Patient, Sites }
        [JsonProperty("Target")] public TargetType Target { get; set; }

        [JsonProperty("Tag")] private string m_TagID = "";
        public BaseTag Tag { get; set; }

        [JsonProperty("Value")] public TagFilterValue Value { get; set; }
        #endregion

        #region Constructors
        public PatientTagFilterCondition() : this(TargetType.Patient, null, new EmptyTagFilterValue(), false)
        {
        }
        public PatientTagFilterCondition(TargetType target, BaseTag tag, TagFilterValue value, bool isNot) : base(isNot)
        {
            Target = target;
            Tag = tag;
            Value = value;
        }
        public PatientTagFilterCondition(TargetType target, BaseTag tag, TagFilterValue value, bool isNot, string ID) : base(isNot, ID)
        {
            Target = target;
            Tag = tag;
            Value = value;
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new PatientTagFilterCondition(Target, Tag, Value.Clone() as TagFilterValue, IsNot, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is PatientTagFilterCondition tagFilterCondition)
            {
                Target = tagFilterCondition.Target;
                Tag = tagFilterCondition.Tag;
                Value = tagFilterCondition.Value;
            }
        }
        #endregion

        #region Public Methods
        public override bool Check(object obj)
        {
            if (obj is Patient patient)
            {
                if (Target == TargetType.Patient)
                {
                    var tagValue = patient.Tags.FirstOrDefault(t => t.Tag == Tag);
                    if (tagValue != null)
                    {
                        return Value.Compare(tagValue.Value) != IsNot;
                    }
                    else
                    {
                        return IsNot;
                    }
                }
                else if (Target == TargetType.Sites)
                {
                    var tagValues = patient.Sites.SelectMany(s => s.Tags).Where(t => t.Tag == Tag);
                    if (tagValues.Count() > 0)
                    {
                        return tagValues.Any(t => Value.Compare(t.Value)) != IsNot;
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

        #region Serialization
        protected override void OnDeserialized()
        {
            base.OnDeserialized();
            Tag = PersistentDataManager.Tags.AllTags.FirstOrDefault(t => t.ID == m_TagID);
        }
        protected override void OnSerializing()
        {
            base.OnSerializing();
            m_TagID = Tag.ID;
        }
        #endregion
    }
}