using HBP.Core.Preferences;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Linq;
using UnityEngine.Scripting;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn), Preserve, DisplayName("Tag"), SortingOrder(5), FilterCondition(typeof(Object3D.Site))]
    public class SiteTagFilterCondition : BaseFilterCondition
    {
        #region Properties
        public override string Description
        {
            get
            {
                if (Tag != null)
                {
                    string result = "";
                    if (Target == TargetType.Site)
                    {
                        result = $"The site has the tag \"{Tag.Name}\"{Value.GetDescription(IsNot)}";
                    }
                    else if (Target == TargetType.Patient)
                    {
                        result = $"The patient of the site has the tag \"{Tag.Name}\"{Value.GetDescription(IsNot)}";
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

        public enum TargetType { Site, Patient }
        [JsonProperty("Target")] public TargetType Target { get; set; }

        [JsonProperty("Tag")] private string m_TagID = "";
        public BaseTag Tag { get; set; }

        [JsonProperty("Value")] public TagFilterValue Value { get; set; }
        #endregion

        #region Constructors
        public SiteTagFilterCondition() : this(TargetType.Site, null, new EmptyTagFilterValue(), false)
        {
        }
        public SiteTagFilterCondition(TargetType target, BaseTag tag, TagFilterValue value, bool isNot) : base(isNot)
        {
            Target = target;
            Tag = tag;
            Value = value;
        }
        public SiteTagFilterCondition(TargetType target, BaseTag tag, TagFilterValue value, bool isNot, string ID) : base(isNot, ID)
        {
            Target = target;
            Tag = tag;
            Value = value;
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new SiteTagFilterCondition(Target, Tag, Value.Clone() as TagFilterValue, IsNot, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is SiteTagFilterCondition tagFilterCondition)
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
            if (obj is Object3D.Site site)
            {
                if (Target == TargetType.Site)
                {
                    var tagValue = site.Information.SiteData.Tags.FirstOrDefault(t => t.Tag == Tag);
                    if (tagValue != null)
                    {
                        return Value.Compare(tagValue.Value) != IsNot;
                    }
                    else
                    {
                        return IsNot;
                    }
                }
                else if (Target == TargetType.Patient)
                {
                    var tagValue = site.Information.Patient.Tags.FirstOrDefault(t => t.Tag == Tag);
                    if (tagValue != null)
                    {
                        return Value.Compare(tagValue.Value) != IsNot;
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
            PersistentDataManager.Tags.TryGetTag(m_TagID, out BaseTag tag);
            Tag = tag;
        }
        protected override void OnSerializing()
        {
            base.OnSerializing();
            m_TagID = Tag?.ID ?? "";
        }
        #endregion
    }
}
