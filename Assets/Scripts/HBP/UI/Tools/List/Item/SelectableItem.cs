using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI.Tools.Lists
{
    /// <summary>
    /// Abstract component to display selectable item in a list.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    [RequireComponent(typeof(Toggle))]
    public abstract class SelectableItem<T> : Item<T>
    {
        #region Properties
        Toggle m_Toggle;
        bool m_isLock;

        /// <summary>
        /// Event called when the item is selected or deselected.
        /// </summary>
        public virtual GenericEvent<bool> OnChangeSelected { get; } = new GenericEvent<bool>();
        /// <summary>
        /// True if selected, False otherwise.
        /// </summary>
        public virtual bool Selected
        {
            get { return m_Toggle.isOn; }
        }
        /// <summary>
        /// True if interactable, False otherwise.
        /// </summary>
        public override bool Interactable
        {
            get { return m_Toggle.interactable; }
            set { m_Toggle.interactable = value; }
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Select the item with a specified transition.
        /// </summary>
        /// <param name="transition">Transition</param>
        public void Select(Toggle.ToggleTransition transition = Toggle.ToggleTransition.None)
        {
            ChangeSelectionValue(true, transition);
        }
        /// <summary>
        /// Deselect the item with a specified transition.
        /// </summary>
        /// <param name="transition">Transition</param>
        public void Deselect(Toggle.ToggleTransition transition = Toggle.ToggleTransition.None)
        {
            ChangeSelectionValue(false, transition);
        }
        /// <summary>
        /// Inverse the item selection.
        /// </summary>
        public void InverseSelection()
        {
            ChangeSelectionValue(!Selected, m_Toggle.toggleTransition);
        }
        /// <summary>
        /// Change the item selection value.
        /// </summary>
        /// <param name="selected">Value of the selection</param>
        /// <param name="transition">Transition</param>
        public void ChangeSelectionValue(bool selected, Toggle.ToggleTransition transition = Toggle.ToggleTransition.None)
        {
            m_isLock = true;
            Toggle.ToggleTransition mode = m_Toggle.toggleTransition;
            m_Toggle.toggleTransition = transition;
            m_Toggle.isOn = selected;
            m_Toggle.toggleTransition = mode;
            m_isLock = false;
        }
        #endregion

        #region Private Methods
        protected virtual void Awake()
        {
            m_Toggle = GetComponent<Toggle>();
            if (m_Toggle != null) m_Toggle.onValueChanged.AddListener((value) => { if (!m_isLock) OnChangeSelected.Invoke(value); });
        }
        #endregion
    }
}