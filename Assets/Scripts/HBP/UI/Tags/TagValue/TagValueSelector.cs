using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI
{
    public class TagValueSelector : ObjectSelector<Data.BaseTagValue>
    {
        #region Properties
        [SerializeField] TagValueList m_List;
        protected override SelectableList<Data.BaseTagValue> List => m_List;
        #endregion
    }
}
