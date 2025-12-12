using HBP.Core.Data;
using HBP.UI.Tools.Lists;
using HBP.UI.Tools;
using UnityEngine;

namespace HBP.UI.Main
{
    public class SingleTagFilterSelector : ObjectSelector<SingleTagFilter>
    {
        #region Properties
        [SerializeField] SingleTagFilterList m_List;
        protected override SelectableList<SingleTagFilter> List => m_List;
        #endregion
    }
}