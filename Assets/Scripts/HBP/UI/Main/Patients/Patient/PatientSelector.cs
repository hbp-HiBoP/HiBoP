using UnityEngine;
using HBP.UI.Tools.Lists;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    public class PatientSelector : ObjectSelector<Core.Data.Patient>
    {
        #region Properties
        [SerializeField] PatientList m_List;
        protected override SelectableList<Core.Data.Patient> List => m_List;
        #endregion
    }
}