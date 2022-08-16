using UnityEngine;
using HBP.UI.Lists;

namespace HBP.UI
{
    /// <summary>
    /// Window to select tagValues.
    /// </summary>
    public class TagValueSelector : ObjectSelector<Core.Data.BaseTagValue>
    {
        #region Properties
        [SerializeField] TagValueList m_List;
        /// <summary>
        /// UI tagValue list.
        /// </summary>
        protected override SelectableList<Core.Data.BaseTagValue> List => m_List;
        #endregion
    }
}
