using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.Data.Preferences;
using HBP.UI.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    public class TagFilterConditionSubModifier : SubModifier<TagFilterCondition>
    {
        #region Properties
        [SerializeField] Dropdown m_TargetDropdown;
        [SerializeField] Dropdown m_TagDropdown;

        protected List<BaseData> m_FilteringObjects;
        public List<BaseData> FilteringObjects
        {
            get => m_FilteringObjects;
            set
            {
                m_FilteringObjects = value;
            }
        }

        [SerializeField] EmptyTagFilterValueSubModifier m_EmptyTagFilterValueSubModifier;
        [SerializeField] BoolTagFilterValueSubModifier m_BoolTagFilterValueSubModifier;
        [SerializeField] StringTagFilterValueSubModifier m_StringTagFilterValueSubModifier;
        [SerializeField] NumberTagFilterValueSubModifier m_NumberTagFilterValueSubModifier;
        [SerializeField] EnumTagFilterValueSubModifier m_EnumTagFilterValueSubModifier;

        private List<BaseTag> m_Tags = new();

        Dictionary<Type, BaseSubModifier> m_SubModifiers;
        Dictionary<Type, TagFilterValue> m_TagFilterValuesTemp;
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();

            m_TargetDropdown.onValueChanged.AddListener(OnChangeTarget);
            m_TagDropdown.onValueChanged.AddListener(OnChangeTag);

            m_EmptyTagFilterValueSubModifier.Initialize();
            m_BoolTagFilterValueSubModifier.Initialize();
            m_StringTagFilterValueSubModifier.Initialize();
            m_NumberTagFilterValueSubModifier.Initialize();
            m_EnumTagFilterValueSubModifier.Initialize();

            m_SubModifiers = new Dictionary<Type, BaseSubModifier>
            {
                { typeof(EmptyTagFilterValue), m_EmptyTagFilterValueSubModifier },
                { typeof(BoolTagFilterValue), m_BoolTagFilterValueSubModifier },
                { typeof(StringTagFilterValue), m_StringTagFilterValueSubModifier },
                { typeof(NumberTagFilterValue), m_NumberTagFilterValueSubModifier },
                { typeof(EnumTagFilterValue), m_EnumTagFilterValueSubModifier }
            };
            m_TagFilterValuesTemp = new Dictionary<Type, TagFilterValue>
            {
                { typeof(EmptyTag), new EmptyTagFilterValue() },
                { typeof(BoolTag), new BoolTagFilterValue() },
                { typeof(StringTag), new StringTagFilterValue() },
                { typeof(IntTag), new NumberTagFilterValue() },
                { typeof(FloatTag), new NumberTagFilterValue() },
                { typeof(EnumTag), new EnumTagFilterValue() }
            };
        }
        #endregion

        #region Private Methods
        protected override void SetFields(TagFilterCondition objectToDisplay)
        {
            base.SetFields(objectToDisplay);

            m_TargetDropdown.SetValue((int)objectToDisplay.Target);

            var currentTag = m_Tags.FirstOrDefault(t => t == objectToDisplay.Tag);
            m_TagDropdown.SetValue(currentTag != null ? m_Tags.IndexOf(currentTag) : 0);
        }
        void OnChangeTarget(int value)
        {
            Object.Target = (TagFilterCondition.TargetType)value;
            m_Tags = Object.Target switch
            {
                TagFilterCondition.TargetType.Patient => PersistentDataManager.Tags.PatientsTags.Concat(PersistentDataManager.Tags.GeneralTags).ToList(),
                TagFilterCondition.TargetType.Site => PersistentDataManager.Tags.SitesTags.Concat(PersistentDataManager.Tags.GeneralTags).ToList(),
                _ => PersistentDataManager.Tags.GeneralTags.ToList(),
            };
            m_TagDropdown.options = m_Tags.Select(t => new Dropdown.OptionData(t.Name)).ToList();
            m_TagDropdown.SetValue(0);
        }
        void OnChangeTag(int value)
        {
            foreach (var sm in m_SubModifiers.Values)
                sm.IsActive = false;

            if (m_Tags.Count == 0)
            {
                Object.Tag = null;
                Object.Value = new EmptyTagFilterValue();
                return;
            }

            BaseTag tag = m_Tags[value];
            Object.Tag = tag;

            TagFilterValue tagFilterValue = m_TagFilterValuesTemp[Object.Tag.GetType()];
            tagFilterValue.Copy(Object.Value);
            Object.Value = tagFilterValue;

            BaseSubModifier subModifier = m_SubModifiers[Object.Value.GetType()];
            if (subModifier is EnumTagFilterValueSubModifier enumTagFilterValueSubModifier) enumTagFilterValueSubModifier.Tag = tag as EnumTag;
            subModifier.IsActive = true;
            subModifier.Object = Object.Value;
        }
        #endregion
    }
}