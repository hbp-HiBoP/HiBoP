using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.UI.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    public class FilterConditionModifier : ObjectModifier<BaseFilterCondition>
    {
        #region Properties
        [SerializeField] Dropdown m_TypeDropdown;
        [SerializeField] Toggle m_NotToggle;

        [SerializeField] NameFilterConditionSubModifier m_NameFilterConditionSubModifier;
        [SerializeField] DateFilterConditionSubModifier m_DateFilterConditionSubModifier;
        [SerializeField] PlaceFilterConditionSubModifier m_PlaceFilterConditionSubModifier;

        List<BaseSubModifier> m_SubModifiers;
        List<BaseFilterCondition> m_FilterConditionsTemp;

        protected List<BaseData> m_FilteringObjects;
        public List<BaseData> FilteringObjects
        {
            get => m_FilteringObjects;
            set
            {
                m_FilteringObjects = value;
                if (value.Count > 0)
                {
                    Type type = value[0].GetType();
                    if (type == typeof(Patient)) FilterConditionAttribute = new Core.Data.PatientFilter();
                    else FilterConditionAttribute = new FilterConditionAttribute();
                }
                else FilterConditionAttribute = new FilterConditionAttribute();

                m_NameFilterConditionSubModifier.FilteringObjects = value;
                m_DateFilterConditionSubModifier.FilteringObjects = value;
                m_PlaceFilterConditionSubModifier.FilteringObjects = value;
            }
        }

        FilterConditionAttribute m_FilterConditionAttribute = new FilterConditionAttribute();
        public FilterConditionAttribute FilterConditionAttribute
        {
            get
            {
                return m_FilterConditionAttribute;
            }
            set
            {
                m_FilterConditionAttribute = value;
                m_Types = m_TypeDropdown.Set(typeof(BaseFilterCondition), m_FilterConditionAttribute);
                m_TypeDropdown.SetValue(Array.IndexOf(m_Types, ObjectTemp.GetType()));
            }
        }

        Type[] m_Types;
        public override bool Interactable
        {
            get => base.Interactable;
            set
            {
                base.Interactable = value;

                m_TypeDropdown.interactable = value;
                m_NotToggle.interactable = value;

                m_NameFilterConditionSubModifier.Interactable = value;
                m_DateFilterConditionSubModifier.Interactable = value;
                m_PlaceFilterConditionSubModifier.Interactable = value;
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
        protected override void Initialize()
        {
            base.Initialize();

            m_TypeDropdown.onValueChanged.AddListener(OnChangeType);
            m_NotToggle.onValueChanged.AddListener(value => ObjectTemp.IsNot = value);

            m_NameFilterConditionSubModifier.Initialize();
            m_DateFilterConditionSubModifier.Initialize();
            m_PlaceFilterConditionSubModifier.Initialize();

            m_SubModifiers = new List<BaseSubModifier>
            {
                m_NameFilterConditionSubModifier,
                m_DateFilterConditionSubModifier,
                m_PlaceFilterConditionSubModifier
            };
            m_FilterConditionsTemp = new List<BaseFilterCondition>
            {
                new NameFilterCondition(),
                new DateFilterCondition(),
                new PlaceFilterCondition()
            };
        }
        protected override void SetFields(BaseFilterCondition objectToDisplay)
        {
            int index = m_FilterConditionsTemp.FindIndex(fc => fc.GetType() == ObjectTemp.GetType());
            m_FilterConditionsTemp[index] = ObjectTemp;

            m_Types = m_TypeDropdown.Set(typeof(BaseFilterCondition), m_FilterConditionAttribute);
            m_TypeDropdown.SetValue(Array.IndexOf(m_Types, ObjectTemp.GetType()));

            m_NotToggle.isOn = objectToDisplay.IsNot;
        }
        protected void OnChangeType(int index)
        {
            Type type = m_Types[index];

            m_SubModifiers.Find(s => s.GetType().IsSubclassOf(typeof(SubModifier<>).MakeGenericType(m_ObjectTemp.GetType()))).IsActive = false;

            BaseFilterCondition filterCondition = m_FilterConditionsTemp.Find(fc => fc.GetType() == type);
            filterCondition.Copy(m_ObjectTemp);
            m_ObjectTemp = filterCondition;

            BaseSubModifier subModifier = m_SubModifiers.Find(s => s.GetType().IsSubclassOf(typeof(SubModifier<>).MakeGenericType(type)));
            subModifier.IsActive = true;
            subModifier.Object = ObjectTemp;
        }
        #endregion
    }
}