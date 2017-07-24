using HBP.Data.Experience.Protocol;
using System.Linq;

namespace HBP.UI.Experience.Protocol
{
    public class IconList : Tools.Unity.Lists.SelectableListWithSave<Icon>
    {
        #region Properties
        enum OrderBy { None, Name, DescendingName, Path, DescendingPath, Start, DescendingStart, End, DescendingEnd}
        OrderBy m_OrderBy = OrderBy.None;
        #endregion

        #region Public Methods
        public void SortByName()
        {
            switch (m_OrderBy)
            {
                case OrderBy.Name:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Name).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingName;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Name).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Name;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
        }
		public void SortByPath()
        {
            switch (m_OrderBy)
            {
                case OrderBy.Path:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.IllustrationPath).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingPath;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.IllustrationPath).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Path;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
        }
        public void SortByStart()
        {
            switch (m_OrderBy)
            {
                case OrderBy.Start:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Window.Start).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingStart;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Window.Start).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Start;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
        }
        public void SortByEnd()
        {
            switch (m_OrderBy)
            {
                case OrderBy.End:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Window.End).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingEnd;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Window.End).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.End;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
        }
        #endregion
    }
}
