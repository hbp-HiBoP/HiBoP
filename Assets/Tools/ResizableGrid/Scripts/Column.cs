using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Tools.Unity.ResizableGrid
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

        private GameObject m_ViewPrefab;
        #endregion

        #region Private Methods
        void Awake()
        {
            m_ViewPrefab = GetComponentInParent<ResizableGrid>().ViewPrefab;
            AddView();
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Add a view to this column
        /// </summary>
        public void AddView()
        {
            m_Views.Add(Instantiate(m_ViewPrefab, transform).GetComponent<View>());
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