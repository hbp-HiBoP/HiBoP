using HBP.Core.Tools;
using HBP.Data.Preferences;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

namespace HBP.Core.Data
{
    [JsonObject(MemberSerialization.OptIn), DisplayName("Multiple Site Tags"), SortingOrder(6), FilterCondition(typeof(Patient))]
    public class MultipleSiteTagsFilterCondition : BaseFilterCondition
    {
        #region Properties
        public override string Description
        {
            get
            {
                if (TagFilters != null && TagFilters.Count > 0)
                {
                    var tagDescriptions = TagFilters.Where(tf => tf.Tag != null).Select(tf => 
                    {
                        string tagDescription = $"\"{tf.Tag.Name}\"{tf.Value.GetDescription(false)}";
                        
                        // Handle EnumTag special case like in PatientTagFilterCondition
                        if (tf.Tag is EnumTag enumTag && tf.Value is EnumTagFilterValue enumValue)
                        {
                            tagDescription += $" {enumTag.Values[enumValue.Value]}";
                        }
                        
                        return tagDescription;
                    });
                    
                    if (tagDescriptions.Count() > 0)
                    {
                        return $"The patient has {(IsNot ? "no" : "a")} site with the following tags: {string.Join(", ", tagDescriptions)}";
                    }
                }
                return "Filter not configured";
            }
        }

        [JsonProperty("TagFilters")] public List<SingleTagFilter> TagFilters { get; set; }
        #endregion

        #region Constructors
        public MultipleSiteTagsFilterCondition() : this(new List<SingleTagFilter>(), false)
        {
        }
        public MultipleSiteTagsFilterCondition(IEnumerable<SingleTagFilter> tagFilters, bool isNot) : base(isNot)
        {
            TagFilters = tagFilters.ToList();
        }
        public MultipleSiteTagsFilterCondition(IEnumerable<SingleTagFilter> tagFilters, bool isNot, string ID) : base(isNot, ID)
        {
            TagFilters = tagFilters.ToList();
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new MultipleSiteTagsFilterCondition(TagFilters.DeepClone(), IsNot, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is MultipleSiteTagsFilterCondition multipleTagsFilterCondition)
            {
                TagFilters = multipleTagsFilterCondition.TagFilters;
            }
        }
        #endregion

        #region Public Methods
        public override bool Check(object obj)
        {
            if (TagFilters == null || TagFilters.Count == 0)
                return false;

            if (obj is Patient patient)
            {
                foreach (var site in patient.Sites)
                {
                    bool siteMatchesAllTags = true;
                    
                    foreach (var tagFilter in TagFilters)
                    {
                        if (tagFilter.Tag == null)
                        {
                            siteMatchesAllTags = false;
                            break;
                        }

                        var tagValue = site.Tags.FirstOrDefault(t => t.Tag == tagFilter.Tag);
                        
                        if (tagValue == null)
                        {
                            siteMatchesAllTags = false;
                            break;
                        }

                        if (!tagFilter.Value.Compare(tagValue.Value))
                        {
                            siteMatchesAllTags = false;
                            break;
                        }
                    }

                    if (siteMatchesAllTags)
                    {
                        return !IsNot;
                    }
                }

                return IsNot;
            }
            return false;
        }
        #endregion

        #region Serialization
        protected override void OnDeserialized()
        {
            base.OnDeserialized();
            if (TagFilters != null)
            {
                foreach (var tagFilter in TagFilters)
                {
                    tagFilter.ResolveTag();
                }
            }
        }
        protected override void OnSerializing()
        {
            base.OnSerializing();
            if (TagFilters != null)
            {
                foreach (var tagFilter in TagFilters)
                {
                    tagFilter.PrepareForSerialization();
                }
            }
        }
        #endregion
    }

    [JsonObject(MemberSerialization.OptIn)]
    public class SingleTagFilter : BaseData
    {
        #region Properties
        [JsonProperty("Tag")] private string m_TagID = "";
        public BaseTag Tag { get; set; }

        [JsonProperty("Value")] public TagFilterValue Value { get; set; }

        public string Description
        {
            get
            {
                if (Tag != null && Value != null)
                {
                    return $"{Tag.Name}{Value.GetDescription(false)}";
                }
                return "No tag selected";
            }
        }
        #endregion

        #region Constructors
        public SingleTagFilter() : this(null, new EmptyTagFilterValue())
        {
        }
        public SingleTagFilter(BaseTag tag, TagFilterValue value) : base()
        {
            Tag = tag;
            Value = value;
        }
        public SingleTagFilter(BaseTag tag, TagFilterValue value, string ID) : base(ID)
        {
            Tag = tag;
            Value = value;
        }
        #endregion

        #region Operators
        public override object Clone()
        {
            return new SingleTagFilter(Tag, Value.Clone() as TagFilterValue, ID);
        }
        public override void Copy(object copy)
        {
            base.Copy(copy);
            if (copy is SingleTagFilter singleTagFilter)
            {
                Tag = singleTagFilter.Tag;
                Value = singleTagFilter.Value;
            }
        }
        #endregion

        #region Serialization
        public void ResolveTag()
        {
            if (!string.IsNullOrEmpty(m_TagID))
            {
                Tag = PersistentDataManager.Tags.AllTags.FirstOrDefault(t => t.ID == m_TagID);
            }
        }
        public void PrepareForSerialization()
        {
            m_TagID = Tag?.ID ?? "";
        }
        #endregion
    }
}