using System;
using System.Collections.Generic;
using Tools.Unity;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI
{
    /// <summary>
    /// Window to modify a tag.
    /// </summary>
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
        /// <summary>
        /// True if interactable, False otherwise.
        /// </summary>
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

            m_NameInputField.onValueChanged.AddListener(ChangeName);

            m_EmptyTagSubModifier.Initialize();
            m_BoolTagSubModifier.Initialize();
            m_IntTagSubModifier.Initialize();
            m_FloatTagSubModifier.Initialize();
            m_StringTagSubModifier.Initialize();
            m_EnumTagSubModifier.Initialize();

            m_TypeDropdown.onValueChanged.AddListener(ChangeType);
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
        /// <summary>
        /// Set the fields
        /// </summary>
        /// <param name="objectToDisplay"></param>
        protected override void SetFields(Data.BaseTag objectToDisplay)
        {
            int index = m_TagsTemp.FindIndex(t => t.GetType() == ObjectTemp.GetType());
            m_TagsTemp[index] = ObjectTemp;

            m_NameInputField.text = objectToDisplay.Name;

            m_TypeDropdown.SetValue(Array.IndexOf(m_Types, objectToDisplay.GetType()));
        }
        /// <summary>
        /// Change the tag name.
        /// </summary>
        /// <param name="name">Name of the tag</param>
        protected void ChangeName(string name)
        {
            ObjectTemp.Name = name;
        }
        /// <summary>
        /// Change the type of the tag.
        /// </summary>
        /// <param name="index">Index of the type</param>
        protected void ChangeType(int index)
        {
            Type type = m_Types[index];

            // Close old subModifier
            m_SubModifiers.Find(s => s.GetType().IsSubclassOf(typeof(SubModifier<>).MakeGenericType(m_ObjectTemp.GetType()))).IsActive = false;

            Data.BaseTag tag = m_TagsTemp.Find(t => t.GetType() == type);
            tag.Copy(m_ObjectTemp);
            m_ObjectTemp = tag;

            // Open new subModifier;
            BaseSubModifier subModifier = m_SubModifiers.Find(s => s.GetType().IsSubclassOf(typeof(SubModifier<>).MakeGenericType(type)));
            subModifier.IsActive = true;
            subModifier.Object = ObjectTemp;
        }
        #endregion
    }
}