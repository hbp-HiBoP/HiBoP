using System.Linq;

namespace HBP.UI.Experience.Protocol
{
	/// <summary>
	/// The scripts which manage the event list.
	/// </summary>
	public class EventList : Tools.Unity.Lists.SelectableListWithSave<Data.Experience.Protocol.Event> 
	{
		#region Attributs
        bool m_sortByName = false;
        bool m_sortByCode = false;
        #endregion

        #region Public Methods
        /// <summary>
        /// Sort by name.
        /// </summary>
        public void SortByName()
        {
            if (m_sortByName)
            {
                m_Objects.OrderByDescending(x => x.Name);
            }
            else
            {
                m_Objects.OrderBy(x => x.Name);
            }
            m_sortByName = !m_sortByName;
            ApplySort();
        }

        /// <summary>
        /// Sort by code.
        /// </summary>
		public void SortByCode()
        {
            if (m_sortByCode)
            {
                m_Objects.OrderByDescending(x => x.Codes.Min());
            }
            else
            {
                m_Objects.OrderBy(x => x.Codes.Min());
            }
            ApplySort();
        }
        #endregion
    }
}
