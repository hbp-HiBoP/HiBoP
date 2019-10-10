using HBP.Data.Tags;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Tags
{
    public class TagValueModifier : ItemModifier<BaseTagValue>
    {
        #region Properties
        [SerializeField] Dropdown m_TagDropdown;

        [SerializeField] EmptyTagValueSubModifier m_EmptyTagValueSubModifier;
        [SerializeField] BoolTagValueSubModifier m_BoolTagValueSubModifier;
        [SerializeField] FloatTagValueSubModifier m_FloatTagValueSubModifier;
        [SerializeField] IntTagValueSubModifier m_IntTagValueSubModifier;
        [SerializeField] EnumTagValueSubModifier m_EnumTagValueSubModifier;
        [SerializeField] StringTagValueSubModifier m_StringTagValueSubModifier;

        List<BaseSubModifier> m_SubModifiers;
        List<BaseTagValue> m_TagValuesTemp;

        Tag[] m_Tags = new Tag[0];
        public Tag[] Tags
        {
            get
            {
                return m_Tags;
            }
            set
            {
                m_Tags = value;
                if(m_Tags.Length == 0)
                {
                    m_TagDropdown.options = new List<Dropdown.OptionData> { new Dropdown.OptionData("None") };
                    m_TagDropdown.interactable = false;
                    m_SaveButton.interactable = false;
                }
                else
                {
                    m_TagDropdown.options = Tags.Select(t => new Dropdown.OptionData(t.Name)).ToList();
                    m_TagDropdown.interactable = Interactable;
                    m_SaveButton.interactable = Interactable;
                }
                SetFields(ItemTemp);
            }
        }

        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }
            set
            {
                base.Interactable = value;

                m_EmptyTagValueSubModifier.Interactable = value;
                m_BoolTagValueSubModifier.Interactable = value;
                m_FloatTagValueSubModifier.Interactable = value;
                m_IntTagValueSubModifier.Interactable = value;
                m_EnumTagValueSubModifier.Interactable = value;
                m_StringTagValueSubModifier.Interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Save()
        {
            item = ItemTemp;
            base.Save();
        }
        #endregion

        #region Protected Methods
        protected override void Initialize()
        {
            base.Initialize();

            m_TagDropdown.onValueChanged.AddListener(OnChangeTag);
            m_TagDropdown.options = Tags.Select(t => new Dropdown.OptionData(t.Name)).ToList();

            m_EmptyTagValueSubModifier.Initialize();
            m_BoolTagValueSubModifier.Initialize();
            m_FloatTagValueSubModifier.Initialize();
            m_IntTagValueSubModifier.Initialize();
            m_EnumTagValueSubModifier.Initialize();
            m_StringTagValueSubModifier.Initialize();

            m_SubModifiers = new List<BaseSubModifier>();
            m_SubModifiers.Add(m_EmptyTagValueSubModifier);
            m_SubModifiers.Add(m_BoolTagValueSubModifier);
            m_SubModifiers.Add(m_FloatTagValueSubModifier);
            m_SubModifiers.Add(m_IntTagValueSubModifier);
            m_SubModifiers.Add(m_EnumTagValueSubModifier);
            m_SubModifiers.Add(m_StringTagValueSubModifier);

            m_TagValuesTemp = new List<BaseTagValue>();
            m_TagValuesTemp.Add(new EmptyTagValue());
            m_TagValuesTemp.Add(new BoolTagValue());
            m_TagValuesTemp.Add(new FloatTagValue());
            m_TagValuesTemp.Add(new IntTagValue());
            m_TagValuesTemp.Add(new EnumTagValue());
            m_TagValuesTemp.Add(new StringTagValue());
        }
        protected override void SetFields(BaseTagValue objectToDisplay)
        {
            int index = m_TagValuesTemp.FindIndex(t => t.GetType() == objectToDisplay.GetType());
            m_TagValuesTemp[index] = objectToDisplay;

            m_TagDropdown.SetValue(Array.IndexOf(Tags, objectToDisplay.Tag));
        }
        protected void OnChangeTag(int value)
        {
            // Close old subModifier
            m_SubModifiers.Find(subModifier => subModifier.GetType().IsSubclassOf(typeof(SubModifier<>).MakeGenericType(itemTemp.GetType()))).IsActive = false;

            if (value >= 0 && value < Tags.Length)
            {
                Tag tag = Tags[value];


                BaseTagValue tagValue = null;
                if (tag is EmptyTag emptyTag)
                {
                    tagValue = m_TagValuesTemp.Find(t => t.GetType() == typeof(EmptyTagValue));
                    tagValue.Copy(itemTemp);
                    (tagValue as EmptyTagValue).Tag = emptyTag;
                }
                else if (tag is BoolTag boolTag)
                {
                    tagValue = m_TagValuesTemp.Find(t => t.GetType() == typeof(BoolTagValue));
                    tagValue.Copy(itemTemp);
                    (tagValue as BoolTagValue).Tag = boolTag;
                }
                else if (tag is EnumTag enumTag)
                {
                    tagValue = m_TagValuesTemp.Find(t => t.GetType() == typeof(EnumTagValue));
                    tagValue.Copy(itemTemp);
                    (tagValue as EnumTagValue).Tag = enumTag;
                }
                else if (tag is FloatTag floatTag)
                {
                    tagValue = m_TagValuesTemp.Find(t => t.GetType() == typeof(FloatTagValue));
                    tagValue.Copy(itemTemp);
                    (tagValue as FloatTagValue).Tag = floatTag;
                }
                else if (tag is IntTag intTag)
                {
                    tagValue = m_TagValuesTemp.Find(t => t.GetType() == typeof(IntTagValue));
                    tagValue.Copy(itemTemp);
                    (tagValue as IntTagValue).Tag = intTag;
                }
                else if (tag is StringTag stringTag)
                {
                    tagValue = m_TagValuesTemp.Find(t => t.GetType() == typeof(StringTagValue));
                    tagValue.Copy(itemTemp);
                    (tagValue as StringTagValue).Tag = stringTag;
                }

                itemTemp = tagValue;

                // Open new subModifier;
                BaseSubModifier newSubModifier = m_SubModifiers.Find(subModifier => subModifier.GetType().IsSubclassOf(typeof(SubModifier<>).MakeGenericType(tagValue.GetType())));
                newSubModifier.IsActive = true;
                newSubModifier.Object = itemTemp;
            }
        }
        #endregion
    }
}