using System;
using System.Collections.Generic;
using Tools.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI
{
    public class TagModifier : ObjectModifier<Data.BaseTag>
    {
        #region Properties
        [SerializeField] InputField m_NameInputField;
        [SerializeField] Dropdown m_TypeDropdown;

        [SerializeField] EmptyTagSubModifier m_EmptyTagSubModifier;
        [SerializeField] BoolTagSubModifier m_BoolTagSubModifier;
        [SerializeField] IntTagSubModifier m_IntTagSubModifier;
        [SerializeField] FloatTagSubModifier m_FloatTagSubModifier;
        [SerializeField] StringTagSubModifier m_StringTagSubModifier;
        [SerializeField] EnumTagSubModifier m_EnumTagSubModifier;

        List<BaseSubModifier> m_SubModifiers;
        List<Data.BaseTag> m_TagsTemp;

        Type[] m_Types;
        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;

                m_NameInputField.interactable = value;
                m_TypeDropdown.interactable = value;

                m_EmptyTagSubModifier.Interactable = value;
                m_BoolTagSubModifier.Interactable = value;
                m_IntTagSubModifier.Interactable = value;
                m_FloatTagSubModifier.Interactable = value;
                m_StringTagSubModifier.Interactable = value;
                m_EnumTagSubModifier.Interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public override void OK()
        {
            item = ItemTemp;
            base.OK();
        }
        #endregion

        #region Protected Methods
        protected override void Initialize()
        {
            base.Initialize();

            m_NameInputField.onValueChanged.AddListener(OnChangeName);

            m_EmptyTagSubModifier.Initialize();
            m_BoolTagSubModifier.Initialize();
            m_IntTagSubModifier.Initialize();
            m_FloatTagSubModifier.Initialize();
            m_StringTagSubModifier.Initialize();
            m_EnumTagSubModifier.Initialize();

            m_TypeDropdown.onValueChanged.AddListener(OnChangeType);
            m_Types = m_TypeDropdown.Set(typeof(Data.BaseTag));

            m_SubModifiers = new List<BaseSubModifier>();
            m_SubModifiers.Add(m_EmptyTagSubModifier);
            m_SubModifiers.Add(m_BoolTagSubModifier);
            m_SubModifiers.Add(m_IntTagSubModifier);
            m_SubModifiers.Add(m_FloatTagSubModifier);
            m_SubModifiers.Add(m_StringTagSubModifier);
            m_SubModifiers.Add(m_EnumTagSubModifier);

            m_TagsTemp = new List<Data.BaseTag>();
            m_TagsTemp.Add(new Data.EmptyTag());
            m_TagsTemp.Add(new Data.BoolTag());
            m_TagsTemp.Add(new Data.IntTag());
            m_TagsTemp.Add(new Data.FloatTag());
            m_TagsTemp.Add(new Data.StringTag());
            m_TagsTemp.Add(new Data.EnumTag());
        }
        protected override void SetFields(Data.BaseTag objectToDisplay)
        {
            int index = m_TagsTemp.FindIndex(t => t.GetType() == ItemTemp.GetType());
            m_TagsTemp[index] = ItemTemp;

            m_NameInputField.text = objectToDisplay.Name;

            m_TypeDropdown.SetValue(Array.IndexOf(m_Types, objectToDisplay.GetType()));
        }
        protected void OnChangeName(string value)
        {
            ItemTemp.Name = value;
        }
        protected void OnChangeType(int value)
        {
            Type type = m_Types[value];

            // Close old subModifier
            m_SubModifiers.Find(s => s.GetType().IsSubclassOf(typeof(SubModifier<>).MakeGenericType(itemTemp.GetType()))).IsActive = false;

            Data.BaseTag tag = m_TagsTemp.Find(t => t.GetType() == type);
            tag.Copy(itemTemp);
            itemTemp = tag;

            // Open new subModifier;
            BaseSubModifier subModifier = m_SubModifiers.Find(s => s.GetType().IsSubclassOf(typeof(SubModifier<>).MakeGenericType(type)));
            subModifier.IsActive = true;
            subModifier.Object = ItemTemp;
        }
        #endregion
    }
}