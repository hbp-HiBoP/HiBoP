using HBP.UI.Tools.Lists;
using UnityEngine;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    public class PatientListGestion : ListGestion<Core.Data.Patient>
    {
        #region Properties
        [SerializeField] PatientList m_List;
        public override ActionableList<Core.Data.Patient> List => m_List;

        [SerializeField] PatientCreator m_ObjectCreator;
        public override ObjectCreator<Core.Data.Patient> ObjectCreator => m_ObjectCreator;
        #endregion
    }
}