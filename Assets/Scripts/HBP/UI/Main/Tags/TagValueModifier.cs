using HBP.Core.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    /// <summary>
    /// Window to modify a tagValue.
    /// </summary>
    public class TagValueModifier : ObjectModifier<Core.Data.BaseTagValue>
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
        List<Core.Data.BaseTagValue> m_TagValuesTemp;

        Core.Data.BaseTag[] m_Tags = new Core.Data.BaseTag[0];
        /// <summary>
        /// Possible tags.
        /// </summary>
        public Core.Data.BaseTag[] Tags
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
                    m_OKButton.interactable = false;
                }
                else
                {
                    m_TagDropdown.options = Tags.Select(t => new Dropdown.OptionData(t.Name)).ToList();
                    m_TagDropdown.interactable = Interactable;
                    m_OKButton.interactable = Interactable;
                }
                SetFields(ObjectTemp);
            }
        }

        /// <summary>
        /// True if interactable, False otherwise.
        /// </summary>
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
        /// <summary>
        /// Save the modifications.
        /// </summary>
        public override void OK()
        {
            m_Object = ObjectTemp;
            base.OK();
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Initialize the window.
        /// </summary>
        protected override void Initialize()
        {
            base.Initialize();

            m_TagDropdown.onValueChanged.AddListener(ChangeTag);
            m_TagDropdown.options = Tags.Select(t => new Dropdown.OptionData(t.Name)).ToList();

            m_EmptyTagValueSubModifier.Initialize();
            m_BoolTagValueSubModifier.Initialize();
            m_FloatTagValueSubModifier.Initialize();
            m_IntTagValueSubModifier.Initialize();
            m_EnumTagValueSubModifier.Initialize();
            m_StringTagValueSubModifier.Initialize();

            m_SubModifiers = new List<BaseSubModifier>
            {
                m_EmptyTagValueSubModifier,
                m_BoolTagValueSubModifier,
                m_FloatTagValueSubModifier,
                m_IntTagValueSubModifier,
                m_EnumTagValueSubModifier,
                m_StringTagValueSubModifier
            };

            m_TagValuesTemp = new List<Core.Data.BaseTagValue>
            {
                new Core.Data.EmptyTagValue(),
                new Core.Data.BoolTagValue(),
                new Core.Data.FloatTagValue(),
                new Core.Data.IntTagValue(),
                new Core.Data.EnumTagValue(),
                new Core.Data.StringTagValue()
            };
        }
        /// <summary>
        /// Set the fields.
        /// </summary>
        /// <param name="objectToModify">TagValue to modify</param>
        protected override void SetFields(Core.Data.BaseTagValue objectToModify)
        {
            int index = m_TagValuesTemp.FindIndex(t => t.GetType() == objectToModify.GetType());
            m_TagValuesTemp[index] = objectToModify;

            m_TagDropdown.SetValue(Array.IndexOf(Tags, objectToModify.Tag));
        }
        /// <summary>
        /// Change the tag.
        /// </summary>
        /// <param name="index">Index of the tag</param>
        protected void ChangeTag(int index)
        {
            // Close old subModifier
            m_SubModifiers.Find(subModifier => subModifier.GetType().IsSubclassOf(typeof(SubModifier<>).MakeGenericType(m_ObjectTemp.GetType()))).IsActive = false;

            if (index >= 0 && index < Tags.Length)
            {
                Core.Data.BaseTag tag = Tags[index];


                Core.Data.BaseTagValue tagValue = null;
                if (tag is Core.Data.EmptyTag emptyTag)
                {
                    tagValue = m_TagValuesTemp.Find(t => t.GetType() == typeof(Core.Data.EmptyTagValue));
                    tagValue.Copy(m_ObjectTemp);
                    (tagValue as Core.Data.EmptyTagValue).Tag = emptyTag;
                }
                else if (tag is Core.Data.BoolTag boolTag)
                {
                    tagValue = m_TagValuesTemp.Find(t => t.GetType() == typeof(Core.Data.BoolTagValue));
                    tagValue.Copy(m_ObjectTemp);
                    (tagValue as Core.Data.BoolTagValue).Tag = boolTag;
                }
                else if (tag is Core.Data.EnumTag enumTag)
                {
                    tagValue = m_TagValuesTemp.Find(t => t.GetType() == typeof(Core.Data.EnumTagValue));
                    tagValue.Copy(m_ObjectTemp);
                    (tagValue as Core.Data.EnumTagValue).Tag = enumTag;
                }
                else if (tag is Core.Data.FloatTag floatTag)
                {
                    tagValue = m_TagValuesTemp.Find(t => t.GetType() == typeof(Core.Data.FloatTagValue));
                    tagValue.Copy(m_ObjectTemp);
                    (tagValue as Core.Data.FloatTagValue).Tag = floatTag;
                }
                else if (tag is Core.Data.IntTag intTag)
                {
                    tagValue = m_TagValuesTemp.Find(t => t.GetType() == typeof(Core.Data.IntTagValue));
                    tagValue.Copy(m_ObjectTemp);
                    (tagValue as Core.Data.IntTagValue).Tag = intTag;
                }
                else if (tag is Core.Data.StringTag stringTag)
                {
                    tagValue = m_TagValuesTemp.Find(t => t.GetType() == typeof(Core.Data.StringTagValue));
                    tagValue.Copy(m_ObjectTemp);
                    (tagValue as Core.Data.StringTagValue).Tag = stringTag;
                }

                m_ObjectTemp = tagValue;

                // Open new subModifier;
                BaseSubModifier newSubModifier = m_SubModifiers.Find(subModifier => subModifier.GetType().IsSubclassOf(typeof(SubModifier<>).MakeGenericType(tagValue.GetType())));
                newSubModifier.IsActive = true;
                newSubModifier.Object = m_ObjectTemp;
            }
        }
        #endregion
    }
}