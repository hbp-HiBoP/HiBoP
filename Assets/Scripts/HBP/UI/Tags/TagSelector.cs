using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI
{
    public class TagSelector : ObjectSelector<Data.BaseTag>
    {
        #region Properties
        [SerializeField] TagList m_List;
        protected override SelectableList<Data.BaseTag> List => m_List;
        #endregion
    }
}