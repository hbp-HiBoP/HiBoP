using HBP.Data.Experience.Dataset;
using HBP.UI.Experience.Dataset;
using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI
{
    /// <summary>
    /// Window to select datasets.
    /// </summary>
    public class DatasetSelector : ObjectSelector<Dataset>
    {
        #region Properties
        [SerializeField] DatasetList m_List;
        /// <summary>
        /// UI datasets list.
        /// </summary>
        protected override SelectableList<Dataset> List => m_List;
        #endregion
    }
}

