using HBP.UI.Experience.Protocol;
using d = HBP.Data.Experience.Protocol;
using UnityEngine;
using Tools.Unity.Lists;

namespace HBP.UI
{
    public class TreatmentSelector : ObjectSelector<d.Treatment>
    {
        #region Properties
        [SerializeField] TreatmentList m_List;
        protected override SelectableList<d.Treatment> List => m_List;
        #endregion
    }
}