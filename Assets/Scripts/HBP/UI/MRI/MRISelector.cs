using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI
{
    /// <summary>
    /// Window to select MRIs.
    /// </summary>
    public class MRISelector : ObjectSelector<Data.MRI>
    {
        #region Properties
        [SerializeField] MRIList m_List;
        /// <summary>
        ///  UI MRIs list.
        /// </summary>
        protected override SelectableList<Data.MRI> List => m_List;
        #endregion
    }
}