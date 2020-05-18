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

        [SerializeField] PatientListFilter m_ListFilter;
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            base.Initialize();
            m_ListFilter.Initialize(m_List);
            m_ListFilter.OnApplyFilters.AddListener(mask =>
            {
                m_List.MaskList(mask);
                m_List.SortByNone();
            });
        }
        #endregion
    }
}