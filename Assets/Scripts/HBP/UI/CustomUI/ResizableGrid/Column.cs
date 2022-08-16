using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace HBP.UI.ResizableGrid
{
    public class Column : MonoBehaviour
    {
        #region Properties
        List<View> m_Views = new List<View>();
        /// <summary>
        /// Views of this column
        /// </summary>
        public ReadOnlyCollection<View> Views
        {
            get
            {
                return new ReadOnlyCollection<View>(m_Views);
            }
        }

        /// <summary>
        /// Number of views in this column
        /// </summary>
        public int ViewNumber
        {
            get
            {
                return m_Views.Count;
            }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Add a view to this column
        /// </summary>
        public void AddView(GameObject customPrefab)
        {
            View view = Instantiate(customPrefab, transform).GetComponent<View>();
            m_Views.Add(view);
            view.transform.SetAsFirstSibling();
        }
        /// <summary>
        /// Remove a view from this column
        /// </summary>
        /// <param name="view">View to be removed</param>
        public void RemoveView(int lineID)
        {
            if (ViewNumber > 1 && lineID < ViewNumber)
            {
                Destroy(m_Views[lineID].gameObject);
                m_Views.RemoveAt(lineID);
            }
        }
        #endregion
    }
}