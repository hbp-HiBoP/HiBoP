using System.Linq;
using HBP.Data.Anatomy;
using UnityEngine;
using Tools.Unity.Lists;
using HBP.Data;

namespace HBP.UI.Anatomy
{
    public class ConnectivityGestion : Gestion<Connectivity, Patient>
    {
        #region Properties
        [SerializeField] ConnectivityList m_List;
        protected override SelectableListWithItemAction<Connectivity> List { get { return m_List; } }
        #endregion

        #region Public Methods
        public override void Set(Data.Patient patient)
        {
            base.Set(patient);
            List.Objects = m_ParentObject.Brain.Connectivities.ToArray();
            m_List.SortByName(ConnectivityList.Sorting.Descending);
        }
        public override void SetActive(bool active)
        {
            base.SetActive(active);
            m_List.SortByName(ConnectivityList.Sorting.Descending);
        }
        public override void RemoveItem()
        {
            base.RemoveItem();
            m_ParentObject.Brain.Connectivities = List.Objects.ToList();
        }
        public override void Save()
        {
            m_ParentObject.Brain.Connectivities = List.Objects.ToList();
        }
        #endregion

        #region Private Methods
        protected override void OnSaveModifier(ItemModifier<Connectivity> modifier)
        {
            base.OnSaveModifier(modifier);
            m_List.SortByNone();
        }
        #endregion
    }
}