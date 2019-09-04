using HBP.UI.Experience.Protocol;
using d = HBP.Data.Experience.Protocol;
using UnityEngine;

namespace HBP.UI
{
    public class TreatmentSelector : ObjectSelector<d.Treatment>
    {
        #region Properties
        [SerializeField] TreatmentList m_TreatmentList;
        #endregion

        #region Public Methods
        protected override void Initialize()
        {
            m_List = m_TreatmentList;
            base.Initialize();
        }
        #endregion
    }
}