using HBP.Core.Data;
using HBP.UI.Tools;
using System.Collections.Generic;
using UnityEngine;

namespace HBP.UI.Main
{
    public class AllFilterConditionSubModifier : SubModifier<AllFilterCondition>
    {
        #region Properties
        [SerializeField] FilterConditionListGestion m_FilterConditionsListGestion;

        protected List<object> m_FilteringObjects;
        public List<object> FilteringObjects
        {
            get => m_FilteringObjects;
            set
            {
                m_FilteringObjects = value;
                m_FilterConditionsListGestion.FilteringObjects = value;
            }
        }
        #endregion

        #region Public Methods
        public override void Initialize()
        {
            m_FilterConditionsListGestion.WindowsReferencer.OnOpenWindow.AddListener(WindowsReferencer.Add);
            m_FilterConditionsListGestion.List.OnAddObject.AddListener(AddCondition);
            m_FilterConditionsListGestion.List.OnRemoveObject.AddListener(RemoveCondition);
            m_FilterConditionsListGestion.List.OnUpdateObject.AddListener(UpdateCondition);
        }
        #endregion

        #region Private Methods
        protected override void SetFields(AllFilterCondition condition)
        {
            m_FilterConditionsListGestion.List.Set(condition.Conditions);
        }

        private void AddCondition(BaseFilterCondition condition)
        {
            if (!Object.Conditions.Contains(condition))
            {
                Object.Conditions.Add(condition);
            }
        }
        private void RemoveCondition(BaseFilterCondition condition)
        {
            if (Object.Conditions.Contains(condition))
            {
                Object.Conditions.Remove(condition);
            }
        }
        private void UpdateCondition(BaseFilterCondition condition)
        {
            int index = Object.Conditions.IndexOf(condition);
            if (index != -1)
            {
                Object.Conditions[index] = condition;
            }
        }
        #endregion
    }
}