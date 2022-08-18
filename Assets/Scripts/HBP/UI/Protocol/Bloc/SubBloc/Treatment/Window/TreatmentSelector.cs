using HBP.UI.Tools.Lists;
using UnityEngine;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    /// <summary>
    /// Window to select treatments.
    /// </summary>
    public class TreatmentSelector : ObjectSelector<Core.Data.Treatment>
    {
        #region Properties
        [SerializeField] TreatmentList m_List;
        /// <summary>
        /// UI treatments list.
        /// </summary>
        protected override SelectableList<Core.Data.Treatment> List => m_List;
        #endregion
    }
}