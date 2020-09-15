using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace Tools.Unity.Graph
{
    public class GraphsGridContainer : MonoBehaviour
    {
        #region Properties
        [SerializeField] private GameObject m_Content;
        public GameObject Content
        {
            get
            {
                return m_Content;
            }
            set
            {
                m_Content = value;
                value.transform.SetParent(transform);
            }
        }
        
        #endregion
    }
}