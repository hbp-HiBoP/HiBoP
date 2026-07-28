using HBP.Core.Data;
using HBP.UI.Tools;
using HBP.UI.Tools.Lists;
using UnityEngine;

namespace HBP.UI.Main
{
    public class FilterConditionListGestion : ListGestion<BaseFilterCondition>
    {
        #region Properties

        [SerializeField] FilterConditionList m_List;
        public override ActionableList<BaseFilterCondition> List => m_List;

        [SerializeField] FilterConditionCreator m_ObjectCreator;
        public override ObjectCreator<BaseFilterCondition> ObjectCreator => m_ObjectCreator;

        protected System.Collections.Generic.List<object> m_FilteringObjects;

        public System.Collections.Generic.List<object> FilteringObjects
        {
            get => m_FilteringObjects;
            set
            {
                m_FilteringObjects = value;
                m_ObjectCreator.FilteringObjects = value;
            }
        }

        #endregion

        #region Public Methods

        protected override ObjectModifier<BaseFilterCondition> OpenModifier(BaseFilterCondition obj)
        {
            var modifier = base.OpenModifier(obj) as FilterConditionModifier;
            modifier.FilteringObjects = m_FilteringObjects;
            return modifier;
        }

        #endregion
    }
}
