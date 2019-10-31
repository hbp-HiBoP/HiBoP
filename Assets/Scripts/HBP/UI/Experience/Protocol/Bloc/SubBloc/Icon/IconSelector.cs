using HBP.UI.Experience.Protocol;
using d = HBP.Data.Experience.Protocol;
using UnityEngine;
using Tools.Unity.Lists;

namespace HBP.UI
{
    public class IconSelector : ObjectSelector<d.Icon>
    {
        #region Properties
        [SerializeField] IconList m_List;
        protected override SelectableList<d.Icon> List => m_List;
        #endregion
    }
}