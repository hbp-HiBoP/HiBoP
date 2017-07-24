using UnityEngine;
using UnityEngine.UI;

namespace Tools.Unity.Lists
{
    [RequireComponent(typeof(Toggle))]
    public abstract class SelectableItem<T> : Item<T>
    {
        #region Properties
        protected Toggle m_Toggle;
        public virtual Toggle.ToggleEvent OnChangeSelected { get { return m_Toggle.onValueChanged; } }
        public virtual bool selected
        {
            get { return m_Toggle.isOn; }
            set { m_Toggle.isOn = value; }
        }
        public override bool interactable
        {
            get { return m_Toggle.interactable; }
            set { m_Toggle.interactable = value; }
        }
        #endregion

        #region Private Methods
        void Awake()
        {
            m_Toggle = GetComponent<Toggle>();
        }
        #endregion
    }
}