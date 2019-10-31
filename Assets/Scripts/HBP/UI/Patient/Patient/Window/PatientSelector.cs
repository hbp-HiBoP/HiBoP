using HBP.Data;
using HBP.UI.Anatomy;
using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI
{
    public class PatientSelector : ObjectSelector<Patient>
    {
        #region Properties
        [SerializeField] PatientList m_List;
        protected override SelectableList<Patient> List => m_List;
        #endregion
    }
}