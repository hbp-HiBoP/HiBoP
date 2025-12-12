using HBP.Core.Data;
using HBP.UI.Tools;
using HBP.UI.Tools.Lists;
using UnityEngine;

namespace HBP.UI.Main
{
    public class FilterConditionSelector : ObjectSelector<BaseFilterCondition>
    {
        #region Properties
        [SerializeField] FilterConditionList m_List;
        protected override SelectableList<BaseFilterCondition> List => m_List;
        #endregion
    }
}