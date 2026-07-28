using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.UI.Tools;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    public class DataTypeFilterConditionSubModifier : SubModifier<DataTypeFilterCondition>
    {
        #region Properties

        [SerializeField] Dropdown m_TypeDropdown;

        Type[] m_Types;

        protected List<object> m_FilteringObjects;

        public List<object> FilteringObjects
        {
            get => m_FilteringObjects;
            set { m_FilteringObjects = value; }
        }

        #endregion

        #region Public Methods

        public override void Initialize()
        {
            base.Initialize();

            m_TypeDropdown.onValueChanged.AddListener(OnChangeType);
            m_Types = m_TypeDropdown.Set(typeof(DataInfo));
        }

        protected override void SetFields(DataTypeFilterCondition objectToDisplay)
        {
            base.SetFields(objectToDisplay);

            m_TypeDropdown.SetValue(Array.IndexOf(m_Types, objectToDisplay.Type));
        }

        #endregion

        #region Private Methods

        private void OnChangeType(int index)
        {
            Object.Type = m_Types[index];
        }

        #endregion
    }
}
