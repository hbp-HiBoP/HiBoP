using UnityEngine;
using HBP.UI.Tools.Lists;
using HBP.UI.Tools;

namespace HBP.UI.Main
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
