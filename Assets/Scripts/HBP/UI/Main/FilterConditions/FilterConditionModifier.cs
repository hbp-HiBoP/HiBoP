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
        [SerializeField] ProtocolFilterConditionSubModifier m_ProtocolFilterConditionSubModifier;
        [SerializeField] AttributesFilterConditionSubModifier m_AttributesFilterConditionSubModifier;
        [SerializeField] PatientTagFilterConditionSubModifier m_PatientTagFilterConditionSubModifier;
        [SerializeField] SiteTagFilterConditionSubModifier m_SiteTagFilterConditionSubModifier;
        [SerializeField] SpecificSiteLocationFilterConditionSubModifier m_SpecificSiteLocationFilterConditionSubModifier;
        [SerializeField] RawSitePositionFilterConditionSubModifier m_RawSitePositionFilterConditionSubModifier;
        [SerializeField] ActivityFilterConditionSubModifier m_ActivityFilterConditionSubModifier;
        [SerializeField] DataTypeFilterConditionSubModifier m_DataTypeFilterConditionSubModifier;
        [SerializeField] DataStateFilterConditionSubModifier m_DataStateFilterConditionSubModifier;
        [SerializeField] AllFilterConditionSubModifier m_AllFilterConditionSubModifier;
        [SerializeField] AnyFilterConditionSubModifier m_AnyFilterConditionSubModifier;
        [SerializeField] MultipleSiteTagsFilterConditionSubModifier m_MultipleSiteTagsFilterConditionSubModifier;
        [SerializeField] GroupFilterConditionSubModifier m_GroupFilterConditionSubModifier;

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
                m_ProtocolFilterConditionSubModifier.FilteringObjects = value;
                m_AttributesFilterConditionSubModifier.FilteringObjects = value;
                m_PatientTagFilterConditionSubModifier.FilteringObjects = value;
                m_SiteTagFilterConditionSubModifier.FilteringObjects = value;
                m_SpecificSiteLocationFilterConditionSubModifier.FilteringObjects = value;
                m_RawSitePositionFilterConditionSubModifier.FilteringObjects = value;
                m_ActivityFilterConditionSubModifier.FilteringObjects = value;
                m_DataTypeFilterConditionSubModifier.FilteringObjects = value;
                m_DataStateFilterConditionSubModifier.FilteringObjects = value;
                m_AllFilterConditionSubModifier.FilteringObjects = value;
                m_AnyFilterConditionSubModifier.FilteringObjects = value;
                m_MultipleSiteTagsFilterConditionSubModifier.FilteringObjects = value;
                m_GroupFilterConditionSubModifier.FilteringObjects = value;
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
                m_ProtocolFilterConditionSubModifier.Interactable = value;
                m_AttributesFilterConditionSubModifier.Interactable = value;
                m_PatientTagFilterConditionSubModifier.Interactable = value;
                m_SiteTagFilterConditionSubModifier.Interactable = value;
                m_SpecificSiteLocationFilterConditionSubModifier.Interactable = value;
                m_RawSitePositionFilterConditionSubModifier.Interactable = value;
                m_ActivityFilterConditionSubModifier.Interactable = value;
                m_DataTypeFilterConditionSubModifier.Interactable = value;
                m_DataStateFilterConditionSubModifier.Interactable = value;
                m_AllFilterConditionSubModifier.Interactable = value;
                m_AnyFilterConditionSubModifier.Interactable = value;
                m_MultipleSiteTagsFilterConditionSubModifier.Interactable = value;
                m_GroupFilterConditionSubModifier.Interactable = value;
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
            m_ProtocolFilterConditionSubModifier.Initialize();
            m_AttributesFilterConditionSubModifier.Initialize();
            m_PatientTagFilterConditionSubModifier.Initialize();
            m_SiteTagFilterConditionSubModifier.Initialize();
            m_SpecificSiteLocationFilterConditionSubModifier.Initialize();
            m_RawSitePositionFilterConditionSubModifier.Initialize();
            m_ActivityFilterConditionSubModifier.Initialize();
            m_DataTypeFilterConditionSubModifier.Initialize();
            m_DataStateFilterConditionSubModifier.Initialize();
            m_AllFilterConditionSubModifier.Initialize();
            m_AnyFilterConditionSubModifier.Initialize();
            m_MultipleSiteTagsFilterConditionSubModifier.Initialize();
            m_GroupFilterConditionSubModifier.Initialize();

            m_SubModifiers = new List<BaseSubModifier>
            {
                m_NameFilterConditionSubModifier,
                m_PatientNameFilterConditionSubModifier,
                m_ProtocolFilterConditionSubModifier,
                m_AttributesFilterConditionSubModifier,
                m_PatientTagFilterConditionSubModifier,
                m_SiteTagFilterConditionSubModifier,
                m_SpecificSiteLocationFilterConditionSubModifier,
                m_RawSitePositionFilterConditionSubModifier,
                m_ActivityFilterConditionSubModifier,
                m_DataTypeFilterConditionSubModifier,
                m_DataStateFilterConditionSubModifier,
                m_AllFilterConditionSubModifier,
                m_AnyFilterConditionSubModifier,
                m_MultipleSiteTagsFilterConditionSubModifier,
                m_GroupFilterConditionSubModifier
            };
            m_FilterConditionsTemp = new List<BaseFilterCondition>
            {
                new NameFilterCondition(),
                new PatientNameFilterCondition(),
                new ProtocolFilterCondition(),
                new AttributesFilterCondition(),
                new PatientTagFilterCondition(),
                new SiteTagFilterCondition(),
                new SpecificSiteLocationFilterCondition(),
                new RawSitePositionFilterCondition(),
                new ActivityFilterCondition(),
                new DataTypeFilterCondition(),
                new DataStateFilterCondition(),
                new AllFilterCondition(),
                new AnyFilterCondition(),
                new MultipleSiteTagsFilterCondition(),
                new GroupFilterCondition()
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