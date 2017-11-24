using System.Linq;
using HBP.Data.Anatomy;
using Tools.Unity.Lists;

namespace HBP.UI.Anatomy
{
    public class MeshList : Tools.Unity.Lists.SelectableListWithItemAction<Mesh>
    {
        #region Properties
        enum OrderBy { None, Name, DescendingName, Mesh, DescendingMesh, MarsAtlas, DescendingMarsAtlas, Transformation, DescendingTransformation }
        OrderBy m_OrderBy = OrderBy.None;
        public enum Sorting { Ascending, Descending }
        public SortingDisplayer m_NameSortingDisplayer;
        public SortingDisplayer m_MeshSortingDisplayer;
        public SortingDisplayer m_MarsAtlasSortingDisplayer;
        public SortingDisplayer m_TransformationSortingDisplayer;
        #endregion

        #region Public Methods
        public override bool Add(Mesh objectToAdd)
        {
            if (base.Add(objectToAdd))
            {
                SortByNone();
                return true;
            }
            else return false;
        }
        public override bool UpdateObject(Mesh objectToUpdate)
        {

            int index = m_Objects.FindIndex(obj => obj == objectToUpdate);
            if (index != -1)
            {
                m_Objects[index] = objectToUpdate;
                Item<Mesh> item;
                if (m_ItemByObject.TryGetValue(objectToUpdate, out item))
                {
                    ActionnableItem<Mesh> actionnableItem = item as ActionnableItem<Mesh>;
                    actionnableItem.Object = objectToUpdate;
                    actionnableItem.OnChangeSelected.RemoveAllListeners();
                    actionnableItem.Select(m_SelectedStateByObject[objectToUpdate]);
                    actionnableItem.OnChangeSelected.AddListener((selected) => OnSelection(objectToUpdate, selected));
                    actionnableItem.OnAction.RemoveAllListeners();
                    actionnableItem.OnAction.AddListener((actionID) => m_OnAction.Invoke(objectToUpdate, actionID));
                    return true;
                }
            }
            return false;
        }
        #endregion

        #region SortingMethods
        public void SortByName()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingName: SortByName(Sorting.Ascending); break;
                default: SortByName(Sorting.Descending); break;
            }
        }
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

        public void SortByMesh()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingMesh: SortByMesh(Sorting.Ascending); break;
                default: SortByMesh(Sorting.Descending); break;
            }
        }
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

        public void SortByMarsAtlas()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingMarsAtlas: SortByMarsAtlas(Sorting.Ascending); break;
                default: SortByMarsAtlas(Sorting.Descending); break;
            }
        }
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

        public void SortByTransformation()
        {
            switch (m_OrderBy)
            {
                case OrderBy.DescendingTransformation: SortByTransformation(Sorting.Ascending); break;
                default: SortByTransformation(Sorting.Descending); break;
            }
        }
        public void SortByTransformation(Sorting sorting)
        {
            switch (sorting)
            {
                case Sorting.Ascending:
                    m_Objects = m_Objects.OrderBy((elt) => elt.Transformation).ToList();
                    m_OrderBy = OrderBy.Transformation;
                    m_TransformationSortingDisplayer.Sorting = SortingDisplayer.SortingType.Ascending;
                    break;
                case Sorting.Descending:
                    m_Objects = m_Objects.OrderByDescending((elt) => elt.Transformation).ToList();
                    m_OrderBy = OrderBy.DescendingTransformation;
                    m_TransformationSortingDisplayer.Sorting = SortingDisplayer.SortingType.Descending;
                    break;
            }
            Refresh();
            m_NameSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_MeshSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
            m_MarsAtlasSortingDisplayer.Sorting = SortingDisplayer.SortingType.None;
        }

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