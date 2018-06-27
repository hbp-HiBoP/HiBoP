using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity.Lists
{
    [RequireComponent(typeof(Toggle))]
    public abstract class SelectableItem<T> : Item<T>
    {
        #region Properties
        Toggle m_Toggle;
        bool m_isLock;
        protected Toggle.ToggleEvent m_OnChangeSelected = new Toggle.ToggleEvent();
        public virtual Toggle.ToggleEvent OnChangeSelected
        {
            get { return m_OnChangeSelected; }
        }
        public virtual bool selected
        {
            get { return m_Toggle.isOn; }
        }
        public override bool interactable
        {
            get { return m_Toggle.interactable; }
            set { m_Toggle.interactable = value; }
        }
        #endregion


        #region Public Methods
        public void Select(bool selected, Toggle.ToggleTransition transition = Toggle.ToggleTransition.None)
        {
            //m_isLock = true;
            Toggle.ToggleTransition mode = m_Toggle.toggleTransition;
            m_Toggle.toggleTransition = transition;
            m_Toggle.isOn = selected;
            m_Toggle.toggleTransition = mode;
            //m_isLock = false;
        }
        public void ChangeSelectionState()
        {
            Select(!selected, m_Toggle.toggleTransition);
        }
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_Toggle = GetComponent<Toggle>();
            if (m_Toggle != null)
            {
                m_Toggle.onValueChanged.AddListener((value) =>
                {
                    if (!m_isLock) m_OnChangeSelected.Invoke(value);
                });  
            }
        }
        #endregion
    }
}