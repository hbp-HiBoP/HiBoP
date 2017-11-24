using System.Linq;
using HBP.Data.Anatomy;
using UnityEngine;
using Tools.Unity.Lists;

namespace HBP.UI.Anatomy
{
    public class ImplantationGestion : AnatomyGestion<Implantation>
    {
        #region Properties
        [SerializeField] ImplantationList m_List;
        protected override SelectableListWithItemAction<Implantation> List { get { return m_List; } }
        #endregion

        #region Public Methods
        public override void Set(Data.Patient patient)
        {
            base.Set(patient);
            List.Objects = m_Patient.Brain.Implantations.ToArray();
            m_List.SortByName(ImplantationList.Sorting.Descending);
        }
        public override void SetActive(bool active)
        {
            base.SetActive(active);
            m_List.SortByName(ImplantationList.Sorting.Descending);
        }
        public override void RemoveItem()
        {
            base.RemoveItem();
            m_Patient.Brain.Implantations = List.Objects.ToList();
        }
        public override void Save()
        {
            m_Patient.Brain.Implantations = List.Objects.ToList();
        }
        #endregion

        #region Private Methods
        protected override void OnSaveModifier(ItemModifier<Implantation> modifier)
        {
            base.OnSaveModifier(modifier);
            m_List.SortByNone();
        }
        #endregion
    }
}