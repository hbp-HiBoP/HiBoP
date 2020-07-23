using HBP.UI.Experience.Protocol;
using d = HBP.Data.Experience.Protocol;
using UnityEngine;
using Tools.Unity.Lists;

namespace HBP.UI
{
    /// <summary>
    /// Window to select treatments.
    /// </summary>
    public class TreatmentSelector : ObjectSelector<d.Treatment>
    {
        #region Properties
        [SerializeField] TreatmentList m_List;
        /// <summary>
        /// UI treatments list.
        /// </summary>
        protected override SelectableList<d.Treatment> List => m_List;
        #endregion
    }
}