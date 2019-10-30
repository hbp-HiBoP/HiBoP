using HBP.Data.Tags;
using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI.Tags
{
    public class TagSelector : ObjectSelector<Tag>
    {
        #region Properties
        [SerializeField] TagList m_List;
        protected override SelectableList<Tag> List => m_List;
        #endregion
    }
}