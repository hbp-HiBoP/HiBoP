using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using HBP.Core.Interfaces;
using HBP.UI.Tools.Lists;

namespace HBP.UI.Tools
{
    public class ListSelectionCounter : MonoBehaviour
    {
        #region Properties
        public Text DisplayText;
        public BaseList List;
        ISelectionCountable m_SelectionCountable;
        #endregion

        #region Private Methods
        void OnEnable()
        {
            if (List is ISelectionCountable selectionCountable)
            {
                m_SelectionCountable = selectionCountable;
                m_SelectionCountable.OnSelectionChanged.AddListener(UpdateCounter);
                UpdateCounter();
            }
        }
        void OnDisable()
        {
            m_SelectionCountable?.OnSelectionChanged.RemoveListener(UpdateCounter);
        }
        void UpdateCounter()
        {
            if (m_SelectionCountable != null)
            {
                int numberOfSelectedObjects = m_SelectionCountable.NumberOfSelectedObjects;
                int numberOfObjects = m_SelectionCountable.NumberOfObjects;
                int numberOfFilteredObjects = m_SelectionCountable.NumberOfFilteredObjects;
                DisplayText.text = $"Selected: {numberOfSelectedObjects}" + (numberOfFilteredObjects < numberOfObjects ? $" - Filtered: {numberOfFilteredObjects}" : "") + $" - Total: {numberOfObjects}";
            }
        }
        #endregion
    }
}