using System.Linq;
using HBP.Data.Anatomy;
using UnityEngine;
using Tools.Unity.Lists;

namespace HBP.UI.Anatomy
{
    public class MRIGestion : Gestion<MRI,Data.Patient>
    {
        #region Properties
        [SerializeField] MRIList m_List;
        protected override SelectableListWithItemAction<MRI> List { get { return m_List; } }
        #endregion

        #region Public Methods
        public override void Set(Data.Patient patient)
        {
            base.Set(patient);
            List.Objects = m_ParentObject.Brain.MRIs.ToArray();
            m_List.SortByName(MRIList.Sorting.Descending);
        }
        public override void RemoveItem()
        {
            base.RemoveItem();
            m_ParentObject.Brain.MRIs = List.Objects.ToList();
        }
        public override void Save()
        {
            m_ParentObject.Brain.MRIs = List.Objects.ToList();
        }
        #endregion

        #region Private Methods
        protected override void OnSaveModifier(ItemModifier<MRI> modifier)
        {
            base.OnSaveModifier(modifier);
            m_List.SortByNone();
        }
        #endregion
    }
}