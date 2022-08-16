using UnityEngine;
using HBP.UI.Lists;

namespace HBP.UI
{
    public class PatientSelector : ObjectSelector<Core.Data.Patient>
    {
        #region Properties
        [SerializeField] PatientList m_List;
        protected override SelectableList<Core.Data.Patient> List => m_List;
        #endregion
    }
}