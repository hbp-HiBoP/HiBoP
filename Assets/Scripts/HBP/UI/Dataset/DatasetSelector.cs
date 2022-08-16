using HBP.UI.Experience.Dataset;
using UnityEngine;
using HBP.UI.Lists;

namespace HBP.UI
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

