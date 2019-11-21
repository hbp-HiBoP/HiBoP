using System.Linq;
using Tools.Unity.Lists;
using UnityEngine;

namespace HBP.UI
{
    public class MeshList : ActionableList<Data.BaseMesh>
    {
        #region Properties
        enum OrderBy { None, Name, DescendingName, Mesh, DescendingMesh, MarsAtlas, DescendingMarsAtlas, Transformation, DescendingTransformation }
        OrderBy m_OrderBy = OrderBy.None;

        [SerializeField] SortingDisplayer m_NameSortingDisplayer;
        [SerializeField] SortingDisplayer m_MeshSortingDisplayer;
        [SerializeField] SortingDisplayer m_MarsAtlasSortingDisplayer;
        [SerializeField] SortingDisplayer m_TransformationSortingDisplayer;

        public UnityEngine.Events.GenericEvent<Data.BaseMesh, int> OnUpdateObject = new UnityEngine.Events.GenericEvent<Data.BaseMesh, int>();
        #endregion

        #region Public Methods
        public override bool Add(Data.BaseMesh objectToAdd)
        {
            if(base.Add(objectToAdd))
            {
                SortByNone();
                return true;
            }
            else return false;
        }
        public override bool UpdateObject(Data.BaseMesh objectToUpdate)
        {
            int index = m_Objects.FindIndex(obj => obj == objectToUpdate);
            if (index != -1)
            {
                m_Objects[index] = objectToUpdate;
                OnUpdateObject.Invoke(objectToUpdate, index);
                if (GetItemFromObject(objectToUpdate, out Item<Data.BaseMesh> item))
                {
                    ActionnableItem<Data.BaseMesh> actionnableItem = item as ActionnableItem<Data.BaseMesh>;
                    actionnableItem.Object = objectToUpdate;
                    actionnableItem.OnChangeSelected.RemoveAllListeners();
                    actionnableItem.Select(m_SelectedStateByObject[objectToUpdate]);
                    actionnableItem.OnChangeSelected.AddListener((selected) => OnChangeSelectionState(objectToUpdate, selected));
                    actionnableItem.OnAction.RemoveAllListeners();
                    actionnableItem.OnAction.AddListener((actionID) => m_OnAction.Invoke(objectToUpdate, actionID));
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region SortingMethods
        /// <summary>
        /// Sort by name.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByName(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.Name).ToList();
                    m_OrderBy = OrderBy.Name;
                    m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderBy((elt) => elt.Name).ToList();
                    m_OrderBy = OrderBy.DescendingName;
                    m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_MeshSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_MarsAtlasSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_TransformationSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by name.
        /// </summary>
        public void SortByName()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingName: SortByName(Sorting.Ascending); break;
                default: SortByName(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by mesh.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByMesh(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_Objects = m_Objects.OrderBy((elt) => elt.HasMesh).ToList();
                    m_OrderBy = OrderBy.Mesh;
                    m_MeshSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.HasMesh).ToList();
                    m_OrderBy = OrderBy.DescendingMesh;
                    m_MeshSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_MarsAtlasSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_TransformationSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by mesh.
        /// </summary>
        public void SortByMesh()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingMesh: SortByMesh(Sorting.Ascending); break;
                default: SortByMesh(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by Mars atlas.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByMarsAtlas(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_Objects = m_Objects.OrderBy((elt) => elt.HasMarsAtlas).ToList();
                    m_OrderBy = OrderBy.MarsAtlas;
                    m_MarsAtlasSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.HasMarsAtlas).ToList();
                    m_OrderBy = OrderBy.DescendingMarsAtlas;
                    m_MarsAtlasSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_MeshSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_TransformationSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by Mars atlas.
        /// </summary>
        public void SortByMarsAtlas()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingMarsAtlas: SortByMarsAtlas(Sorting.Ascending); break;
                default: SortByMarsAtlas(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort ny transformation.
        /// </summary>
        /// <param name="sorting">Sorting</param>
        public void SortByTransformation(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_Objects = m_Objects.OrderBy((elt) => elt.HasTransformation).ToList();
                    m_OrderBy = OrderBy.Transformation;
                    m_TransformationSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.HasTransformation).ToList();
                    m_OrderBy = OrderBy.DescendingTransformation;
                    m_TransformationSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_MeshSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_MarsAtlasSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        /// <summary>
        /// Sort by transformation.
        /// </summary>
        public void SortByTransformation()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingTransformation: SortByTransformation(Sorting.Ascending); break;
                default: SortByTransformation(Sorting.Descending); break;
            }
        }

        /// <summary>
        /// Sort by none.
        /// </summary>
        public void SortByNone()
        {
            m_OrderBy = OrderBy.None;
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_MeshSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_MarsAtlasSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_TransformationSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }
        #endregion
    }
}