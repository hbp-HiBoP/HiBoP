using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI
{
    /// <summary>
    /// Window to select tagValues.
    /// </summary>
    public class TagValueSelector : ObjectSelector<Data.BaseTagValue>
    {
        #region Properties
        [SerializeField] TagValueList m_List;
        /// <summary>
        /// UI tagValue list.
        /// </summary>
        protected override SelectableList<Data.BaseTagValue> List => m_List;
        #endregion
    }
}
