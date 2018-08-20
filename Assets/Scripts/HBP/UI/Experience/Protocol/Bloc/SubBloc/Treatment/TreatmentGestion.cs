using HBP.Data.Experience.Protocol;
using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI.Experience.Protocol
{
    public class TreatmentGestion : Gestion<Treatment, SubBloc>
    {
        #region Properties
        [SerializeField] TreatmentList m_List;
        protected override SelectableListWithItemAction<Treatment> List { get { return m_List; } }
        #endregion

        #region Public Methods
        public override void Set(SubBloc subBloc)
        {
            base.Set(subBloc);
            List.Objects = m_ParentObject.Treatments.ToArray();
            //m_List.SortByName(EventList.Sorting.Descending);
        }
        public override void Save()
        {
            //m_ParentObject.Events = List.Objects.ToList();
        }
        #endregion

        #region Private Methods
        protected override void OnSaveModifier(ItemModifier<Treatment> modifier)
        {
            base.OnSaveModifier(modifier);
            //m_List.SortByNone();
        }
        #endregion
    }
}