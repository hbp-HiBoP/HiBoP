using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Tools.Unity.Lists
{
    [RequireComponent(typeof(Toggle))]
    public abstract class SelectableItem<T> : Item<T>
    {
        #region Properties
        Toggle m_Toggle;
        bool m_isLock;

        public virtual GenericEvent<bool> OnChangeSelected { get; } = new GenericEvent<bool>();

        public virtual bool Selected
        {
            get { return m_Toggle.isOn; }
        }
        public override bool Interactable
        {
            get { return m_Toggle.interactable; }
            set { m_Toggle.interactable = value; }
        }
        #endregion

        #region Public Methods
        public void Select(bool selected, Toggle.ToggleTransition transition = Toggle.ToggleTransition.None)
        {
            m_isLock = true;
            Toggle.ToggleTransition mode = m_Toggle.toggleTransition;
            m_Toggle.toggleTransition = transition;
            m_Toggle.isOn = selected;
            m_Toggle.toggleTransition = mode;
            m_isLock = false;
        }
        public void ChangeSelectionState()
        {
            Select(!Selected, m_Toggle.toggleTransition);
        }
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_Toggle = GetComponent<Toggle>();
            if (m_Toggle != null) m_Toggle.onValueChanged.AddListener(OnSelectionValueChanged);
        }
        void OnSelectionValueChanged(bool value)
        {
            if (!m_isLock) OnChangeSelected.Invoke(value);
        }
        #endregion
    }
}