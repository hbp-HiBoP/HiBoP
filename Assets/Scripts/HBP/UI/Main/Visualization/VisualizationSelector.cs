using UnityEngine;
using HBP.UI.Tools.Lists;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    /// <summary>
    /// Window to select visualizations.
    /// </summary>
    public class VisualizationSelector : ObjectSelector<Core.Data.Visualization>
    {
        #region Properties
        [SerializeField] VisualizationList m_List;
        /// <summary>
        /// UI visualizations list.
        /// </summary>
        protected override SelectableList<Core.Data.Visualization> List => m_List;
        #endregion
    }
}