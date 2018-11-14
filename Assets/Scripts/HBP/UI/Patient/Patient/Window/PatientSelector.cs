using HBP.Data;
using HBP.UI.Anatomy;
using UnityEngine;

namespace HBP.UI
{
    public class PatientSelector : ObjectSelector<Patient>
    {
        #region Properties
        [SerializeField] PatientList m_PatientList;
        #endregion

        #region Public Methods
        protected override void Initialize()
        {
            m_List = m_PatientList;
            base.Initialize();
        }
        #endregion
    }
}