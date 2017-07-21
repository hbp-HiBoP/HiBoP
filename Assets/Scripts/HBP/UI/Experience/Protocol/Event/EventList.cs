using System.Linq;

namespace HBP.UI.Experience.Protocol
{
	/// <summary>
	/// The scripts which manage the event list.
	/// </summary>
	public class EventList : Tools.Unity.Lists.SelectableListWithSave<Data.Experience.Protocol.Event> 
	{
        #region Proterties
        enum OrderBy { None, Name, DescendingName, Code, DescendingCode }
        OrderBy m_OrderBy = OrderBy.None;
        #endregion

        #region Public Methods
        /// <summary>
        /// Sort by name.
        /// </summary>
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

        /// <summary>
        /// Sort by code.
        /// </summary>
		public void SortByCode()
        {
            switch (m_OrderBy)
            {
                case OrderBy.Code:
                    m_ObjectsToItems = m_ObjectsToItems.OrderByDescending((elt) => elt.Key.Codes.Min()).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.DescendingCode;
                    break;
                default:
                    m_ObjectsToItems = m_ObjectsToItems.OrderBy((elt) => elt.Key.Codes.Min()).ToDictionary(k => k.Key, v => v.Value);
                    m_OrderBy = OrderBy.Code;
                    break;
            }
            foreach (var item in m_ObjectsToItems.Values) item.transform.SetAsLastSibling();
        }
        #endregion
    }
}
