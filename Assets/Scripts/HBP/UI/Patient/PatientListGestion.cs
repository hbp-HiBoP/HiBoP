using Tools.Unity.Components;
using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI
{
    public class PatientListGestion : ListGestion<Data.Patient>
    {
        #region Properties
        [SerializeField] PatientList m_List;
        public override ActionableList<Data.Patient> List => m_List;

        [SerializeField] PatientCreator m_ObjectCreator;
        public override ObjectCreator<Data.Patient> ObjectCreator => m_ObjectCreator;
        #endregion
    }
}