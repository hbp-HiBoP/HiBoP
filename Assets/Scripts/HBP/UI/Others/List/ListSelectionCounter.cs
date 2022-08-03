using Tools.Unity.Lists;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using HBP.Core.Interfaces;

namespace Tools.Unity.Components
{
    [RequireComponent(typeof(SelectableList<>))]
    public class ListSelectionCounter : MonoBehaviour
    {
        #region Properties
        public Text Counter;
        public BaseList List;
        UnityAction m_Action;
        ISelectionCountable m_SelectionCountable;
        #endregion

        #region Private Methods
        void OnEnable()
        {
            m_Action = UpdateCounter;
            if (List is ISelectionCountable selectionCountable)
            {
                m_SelectionCountable = selectionCountable;
                m_SelectionCountable.OnSelectionChanged.AddListener(m_Action);
            }
            else
            {
                List = null;
            }
        }
        void OnDisable()
        {
            if(m_SelectionCountable != null) m_SelectionCountable.OnSelectionChanged.RemoveListener(m_Action);
        }
        void UpdateCounter()
        {
            if(m_SelectionCountable != null)
            {
                Counter.text = m_SelectionCountable.NumberOfItemSelected.ToString();
            }
        }
        #endregion
    }
}