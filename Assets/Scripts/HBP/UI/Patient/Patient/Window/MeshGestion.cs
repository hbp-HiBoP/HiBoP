using System.Linq;
using HBP.Data.Anatomy;
using UnityEngine;
using Tools.Unity.Lists;

namespace HBP.UI.Anatomy
{
    public class MeshGestion : Gestion<Data.Anatomy.Mesh, Data.Patient>
    {
        #region Properties
        [SerializeField] MeshList m_List;
        protected override SelectableListWithItemAction<Data.Anatomy.Mesh> List { get { return m_List; } }
        #endregion

        #region Public Methods
        public override void Set(Data.Patient patient)
        {
            base.Set(patient);
            List.Objects = m_ParentObject.Brain.Meshes.ToArray();
            m_List.SortByName(MeshList.Sorting.Descending);
        }
        public override void SetActive(bool active)
        {
            base.SetActive(active);
            m_List.SortByName(MeshList.Sorting.Descending);
        }
        public override void AddItem()
        {
            OpenModifier(new LeftRightMesh(), interactable);
        }
        public override void RemoveItem()
        {
            base.RemoveItem();
            m_ParentObject.Brain.Meshes = List.Objects.ToList();
        }
        public override void Save()
        {
            m_ParentObject.Brain.Meshes = List.Objects.ToList();
        }
        #endregion

        #region Private Methods
        protected override void OnSaveModifier(ItemModifier<Data.Anatomy.Mesh> modifier)
        {
            base.OnSaveModifier(modifier);
            m_List.SortByNone();
        }
        #endregion
    }
}