﻿using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI
{
    /// <summary>
    /// Window to select coordinate.
    /// </summary>
    public class CoordinateSelector : ObjectSelector<Data.Coordinate>
    {
        #region Properties
        [SerializeField] CoordinateList m_List;
        /// <summary>
        /// UI coordinates list.
        /// </summary>
        protected override SelectableList<Data.Coordinate> List => m_List;
        #endregion
    }
}