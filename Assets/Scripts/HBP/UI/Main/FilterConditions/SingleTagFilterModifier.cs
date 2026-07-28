using HBP.Core.Data;
using HBP.Core.Preferences;
using HBP.UI.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using HBP.Core.Tools;

namespace HBP.UI.Main
{
    public class SingleTagFilterModifier : ObjectModifier<SingleTagFilter>
    {
        #region Properties

        [SerializeField] Dropdown m_TagDropdown;

        [SerializeField] EmptyTagFilterValueSubModifier m_EmptyTagFilterValueSubModifier;
        [SerializeField] BoolTagFilterValueSubModifier m_BoolTagFilterValueSubModifier;
        [SerializeField] StringTagFilterValueSubModifier m_StringTagFilterValueSubModifier;
        [SerializeField] NumberTagFilterValueSubModifier m_NumberTagFilterValueSubModifier;
        [SerializeField] EnumTagFilterValueSubModifier m_EnumTagFilterValueSubModifier;

        [SerializeField] Text m_ResultText;

        private List<BaseTag> m_Tags = new();

        Dictionary<Type, BaseSubModifier> m_SubModifiers;
        Dictionary<Type, TagFilterValue> m_TagFilterValuesTemp;

        protected List<object> m_FilteringObjects;

        public List<object> FilteringObjects
        {
            get => m_FilteringObjects;
            set
            {
                m_FilteringObjects = value;
                UpdateAvailableTags();
            }
        }

        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;

                m_TagDropdown.interactable = value;

                m_EmptyTagFilterValueSubModifier.Interactable = value;
                m_BoolTagFilterValueSubModifier.Interactable = value;
                m_StringTagFilterValueSubModifier.Interactable = value;
                m_NumberTagFilterValueSubModifier.Interactable = value;
                m_EnumTagFilterValueSubModifier.Interactable = value;
            }
        }

        #endregion

        #region Public Methods

        public override void OK()
        {
            m_Object = ObjectTemp;
            base.OK();
        }

        #endregion

        #region Protected Methods

        private void Update()
        {
            m_ResultText.text = ObjectTemp.Description;
        }

        protected override void Initialize()
        {
            base.Initialize();

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

            UpdateAvailableTags();
        }

        protected override void SetFields(SingleTagFilter objectToDisplay)
        {
            var currentTag = m_Tags.FirstOrDefault(t => t == objectToDisplay.Tag);
            m_TagDropdown.SetValue(currentTag != null ? m_Tags.IndexOf(currentTag) : 0);
        }

        void UpdateAvailableTags()
        {
            m_Tags = PersistentDataManager.Tags.SitesTags.Concat(PersistentDataManager.Tags.GeneralTags).ToList();
            m_TagDropdown.options = m_Tags.Select(t => new Dropdown.OptionData(t.Name)).ToList();
        }

        void OnChangeTag(int value)
        {
            foreach (var sm in m_SubModifiers.Values)
                sm.IsActive = false;

            if (m_Tags.Count == 0)
            {
                ObjectTemp.Tag = null;
                ObjectTemp.Value = new EmptyTagFilterValue();
                return;
            }

            BaseTag tag = m_Tags[value];
            ObjectTemp.Tag = tag;

            TagFilterValue tagFilterValue = m_TagFilterValuesTemp[ObjectTemp.Tag.GetType()];
            tagFilterValue.Copy(ObjectTemp.Value);
            ObjectTemp.Value = tagFilterValue;

            BaseSubModifier subModifier = m_SubModifiers[ObjectTemp.Value.GetType()];
            if (subModifier is EnumTagFilterValueSubModifier enumTagFilterValueSubModifier) enumTagFilterValueSubModifier.Tag = tag as EnumTag;
            subModifier.IsActive = true;
            subModifier.Object = ObjectTemp.Value;
        }

        #endregion
    }
}
