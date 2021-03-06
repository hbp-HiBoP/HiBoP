﻿using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI
{
    /// <summary>
    /// Window to select tags.
    /// </summary>
    public class TagSelector : ObjectSelector<Data.BaseTag>
    {
        #region Properties
        [SerializeField] TagList m_List;
        /// <summary>
        /// UI tags list.
        /// </summary>
        protected override SelectableList<Data.BaseTag> List => m_List;
        #endregion
    }
}