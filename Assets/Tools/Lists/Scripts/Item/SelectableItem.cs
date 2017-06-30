using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Tools.Unity.Lists
{
    [RequireComponent(typeof(Toggle))]
    public abstract class SelectableItem<T> : Item<T>
    {
        #region Properties
        UnityEvent m_OnChangeSelected = new UnityEvent();
        public UnityEvent OnChangeSelected
        {
            get { return m_OnChangeSelected; }
        }
        Toggle m_Toggle;
        public bool Selected
        {
            get { return m_Toggle.isOn; }
            set
            {
                if (Interactable)
                {
                    m_Toggle.isOn = value;
                    OnChangeSelected.Invoke();
                }
            }
        }
        public bool Interactable
        {
            get { return m_Toggle.interactable; }
            set { if (!value) Selected = false; m_Toggle.interactable = value; }
        }
        #endregion

        #region Private Methods
        private void Awake()
        {
            m_Toggle = GetComponent<Toggle>();
            m_Toggle.onValueChanged.AddListener((value) => OnChangeSelected.Invoke());
        }
        #endregion
    }
}