using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using HBP.Core.Interfaces;
using HBP.UI.Tools.Lists;
using System.Linq;

namespace HBP.UI.Tools
{
    public class ListSelectionCounter : MonoBehaviour
    {
        #region Properties
        public Text DisplayText;
        public BaseList List;
        public bool DisplayFilteredCount = true;
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

                string selectedText = m_SelectionCountable.CanSelectMultipleObjects ? $"Selected: {numberOfSelectedObjects}" : "";
                string filteredText = DisplayFilteredCount && numberOfFilteredObjects < numberOfObjects ? $"Filtered: {numberOfFilteredObjects}" : "";
                string totalText = $"Total: {numberOfObjects}";
                DisplayText.text = string.Join(" - ", new[] { selectedText, filteredText, totalText }.Where(s => !string.IsNullOrEmpty(s)));
            }
        }
        #endregion
    }
}