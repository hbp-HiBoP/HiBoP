using HBP.Data.Tags;
using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI.Tags
{
    public class TagValueSelector : ObjectSelector<BaseTagValue>
    {
        #region Properties
        [SerializeField] TagValueList m_List;
        protected override SelectableList<BaseTagValue> List => m_List;
        #endregion
    }
}
