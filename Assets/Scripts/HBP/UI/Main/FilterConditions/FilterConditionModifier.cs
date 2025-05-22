using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.UI.Tools;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    public class FilterConditionModifier : ObjectModifier<BaseFilterCondition>
    {
        #region Properties
        [SerializeField] Dropdown m_TypeDropdown;
        [SerializeField] Dropdown m_EvaluateDropdown;

        [SerializeField] NameFilterConditionSubModifier m_NameFilterConditionSubModifier;
        [SerializeField] PatientNameFilterConditionSubModifier m_PatientNameFilterConditionSubModifier;
        [SerializeField] DateFilterConditionSubModifier m_DateFilterConditionSubModifier;
        [SerializeField] PlaceFilterConditionSubModifier m_PlaceFilterConditionSubModifier;
        [SerializeField] ProtocolFilterConditionSubModifier m_ProtocolFilterConditionSubModifier;
        [SerializeField] PatientTagFilterConditionSubModifier m_PatientTagFilterConditionSubModifier;
        [SerializeField] SiteTagFilterConditionSubModifier m_SiteTagFilterConditionSubModifier;
        [SerializeField] ActivityFilterConditionSubModifier m_ActivityFilterConditionSubModifier;
        [SerializeField] AllFilterConditionSubModifier m_AllFilterConditionSubModifier;
        [SerializeField] AnyFilterConditionSubModifier m_AnyFilterConditionSubModifier;

        [SerializeField] Text m_ResultText;

        List<BaseSubModifier> m_SubModifiers;
        List<BaseFilterCondition> m_FilterConditionsTemp;

        protected List<object> m_FilteringObjects;
        public List<object> FilteringObjects
        {
            get => m_FilteringObjects;
            set
            {
                m_FilteringObjects = value;
                if (value.Count > 0)
                {
                    FilterConditionAttribute = new FilterConditionAttribute(value[0].GetType());
                }
                else FilterConditionAttribute = new FilterConditionAttribute(null);

                m_NameFilterConditionSubModifier.FilteringObjects = value;
                m_PatientNameFilterConditionSubModifier.FilteringObjects = value;
                m_DateFilterConditionSubModifier.FilteringObjects = value;
                m_PlaceFilterConditionSubModifier.FilteringObjects = value;
                m_ProtocolFilterConditionSubModifier.FilteringObjects = value;
                m_PatientTagFilterConditionSubModifier.FilteringObjects = value;
                m_SiteTagFilterConditionSubModifier.FilteringObjects = value;
                m_ActivityFilterConditionSubModifier.FilteringObjects = value;
                m_AllFilterConditionSubModifier.FilteringObjects = value;
                m_AnyFilterConditionSubModifier.FilteringObjects = value;
            }
        }

        FilterConditionAttribute m_FilterConditionAttribute = new FilterConditionAttribute(null);
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
                m_EvaluateDropdown.interactable = value;

                m_NameFilterConditionSubModifier.Interactable = value;
                m_PatientNameFilterConditionSubModifier.Interactable = value;
                m_DateFilterConditionSubModifier.Interactable = value;
                m_PlaceFilterConditionSubModifier.Interactable = value;
                m_ProtocolFilterConditionSubModifier.Interactable = value;
                m_PatientTagFilterConditionSubModifier.Interactable = value;
                m_SiteTagFilterConditionSubModifier.Interactable = value;
                m_ActivityFilterConditionSubModifier.Interactable = value;
                m_AllFilterConditionSubModifier.Interactable = value;
                m_AnyFilterConditionSubModifier.Interactable = value;
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

            m_TypeDropdown.onValueChanged.AddListener(OnChangeType);
            m_EvaluateDropdown.onValueChanged.AddListener(value => ObjectTemp.IsNot = value != 0);

            m_NameFilterConditionSubModifier.Initialize();
            m_PatientNameFilterConditionSubModifier.Initialize();
            m_DateFilterConditionSubModifier.Initialize();
            m_PlaceFilterConditionSubModifier.Initialize();
            m_ProtocolFilterConditionSubModifier.Initialize();
            m_PatientTagFilterConditionSubModifier.Initialize();
            m_SiteTagFilterConditionSubModifier.Initialize();
            m_ActivityFilterConditionSubModifier.Initialize();
            m_AllFilterConditionSubModifier.Initialize();
            m_AnyFilterConditionSubModifier.Initialize();

            m_SubModifiers = new List<BaseSubModifier>
            {
                m_NameFilterConditionSubModifier,
                m_PatientNameFilterConditionSubModifier,
                m_DateFilterConditionSubModifier,
                m_PlaceFilterConditionSubModifier,
                m_ProtocolFilterConditionSubModifier,
                m_PatientTagFilterConditionSubModifier,
                m_SiteTagFilterConditionSubModifier,
                m_ActivityFilterConditionSubModifier,
                m_AllFilterConditionSubModifier,
                m_AnyFilterConditionSubModifier
            };
            m_FilterConditionsTemp = new List<BaseFilterCondition>
            {
                new NameFilterCondition(),
                new PatientNameFilterCondition(),
                new DateFilterCondition(),
                new PlaceFilterCondition(),
                new ProtocolFilterCondition(),
                new PatientTagFilterCondition(),
                new SiteTagFilterCondition(),
                new ActivityFilterCondition(),
                new AllFilterCondition(),
                new AnyFilterCondition()
            };
        }
        protected override void SetFields(BaseFilterCondition objectToDisplay)
        {
            int index = m_FilterConditionsTemp.FindIndex(fc => fc.GetType() == ObjectTemp.GetType());
            m_FilterConditionsTemp[index] = ObjectTemp;

            m_Types = m_TypeDropdown.Set(typeof(BaseFilterCondition), m_FilterConditionAttribute);
            m_TypeDropdown.SetValue(Array.IndexOf(m_Types, ObjectTemp.GetType()));

            m_EvaluateDropdown.SetValue(objectToDisplay.IsNot ? 1 : 0);
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