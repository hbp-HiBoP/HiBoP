using System.Linq;
using HBP.Data.Anatomy;
using UnityEngine;
using Tools.Unity.Lists;

namespace HBP.UI.Anatomy
{
    public class MRIGestion : AnatomyGestion<MRI>
    {
        #region Properties
        [SerializeField] MRIList m_List;
        protected override SelectableListWithItemAction<MRI> List { get { return m_List; } }
        #endregion

        #region Public Methods
        public override void Set(Data.Patient patient)
        {
            base.Set(patient);
            List.Objects = m_Patient.Brain.MRIs.ToArray();
            m_List.SortByName(MRIList.Sorting.Descending);
        }
        public override void SetActive(bool active)
        {
            base.SetActive(active);
            m_List.SortByName(MRIList.Sorting.Descending);
        }
        public override void RemoveItem()
        {
            base.RemoveItem();
            m_Patient.Brain.MRIs = List.Objects.ToList();
        }
        public override void Save()
        {
            m_Patient.Brain.MRIs = List.Objects.ToList();
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