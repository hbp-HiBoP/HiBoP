﻿using HBP.UI.Visualization;
using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI
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