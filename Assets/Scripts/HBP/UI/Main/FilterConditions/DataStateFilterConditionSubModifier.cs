using HBP.Core.Data;
using HBP.Core.Tools;
using HBP.UI.Tools;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.Main
{
    public class DataStateFilterConditionSubModifier : SubModifier<DataStateFilterCondition>
    {
        #region Properties
        [SerializeField] Dropdown m_StateDropdown;

        protected List<object> m_FilteringObjects;
        public List<object> FilteringObjects
        {
            get => m_FilteringObjects;
            set
            {
                m_FilteringObjects = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            base.Initialize();

            m_StateDropdown.onValueChanged.AddListener(OnChangeState);
        }
        protected override void SetFields(DataStateFilterCondition objectToDisplay)
        {
            base.SetFields(objectToDisplay);

            m_StateDropdown.Set(typeof(DataInfo.DataState), (int)objectToDisplay.State);
        }
        #endregion

        #region Private Methods
        private void OnChangeState(int index)
        {
            Object.State = (DataInfo.DataState)index;
        }
        #endregion
    }
}