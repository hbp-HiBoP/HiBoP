using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI
{
    public class PatientSelector : ObjectSelector<Data.Patient>
    {
        #region Properties
        [SerializeField] PatientList m_List;
        protected override SelectableList<Data.Patient> List => m_List;
        #endregion
    }
}