using HBP.Data;
using Tools.Unity.Components;
using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI.Anatomy
{
    public class PatientListGestion : ListGestion<Patient>
    {
        #region Properties
        [SerializeField] PatientList m_List;
        public override SelectableListWithItemAction<Patient> List
        {
            get
            {
                return m_List;
            }
        }

        [SerializeField] PatientCreator m_ObjectCreator;
        public override ObjectCreator<Patient> ObjectCreator
        {
            get
            {
                return m_ObjectCreator;
            }
        }
        #endregion
    }
}