using UnityEngine;
using HBP.UI.Tools.Lists;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    /// <summary>
    /// Window to select datasets.
    /// </summary>
    public class DatasetSelector : ObjectSelector<Core.Data.Dataset>
    {
        #region Properties
        [SerializeField] DatasetList m_List;
        /// <summary>
        /// UI datasets list.
        /// </summary>
        protected override SelectableList<Core.Data.Dataset> List => m_List;
        #endregion
    }
}
