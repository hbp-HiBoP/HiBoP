using HBP.UI.Tools.Lists;
using UnityEngine;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    /// <summary>
    /// Window to select MRIs.
    /// </summary>
    public class MRISelector : ObjectSelector<Core.Data.MRI>
    {
        #region Properties
        [SerializeField] MRIList m_List;
        /// <summary>
        ///  UI MRIs list.
        /// </summary>
        protected override SelectableList<Core.Data.MRI> List => m_List;
        #endregion
    }
}