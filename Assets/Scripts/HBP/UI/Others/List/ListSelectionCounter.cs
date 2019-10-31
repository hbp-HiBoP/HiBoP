using Tools.Unity.Lists;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Tools.Unity.Components
{
    [RequireComponent(typeof(Lists.SelectableList<object>))]
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
            m_SelectionCountable.OnSelectionChanged.RemoveListener(m_Action);
        }
        void OnValidate()
        {
            if (!(List is ISelectionCountable)) List = null;
        }
        void UpdateCounter()
        {
            Counter.text = m_SelectionCountable.NumberOfItemSelected.ToString();
        }
        #endregion
    }
}