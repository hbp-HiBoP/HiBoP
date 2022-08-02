using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI
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