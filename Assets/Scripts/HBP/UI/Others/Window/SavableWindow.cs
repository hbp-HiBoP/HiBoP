using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI
{
    public abstract class SavableWindow : Window, ISavable
    {
        #region Properties
        UnityEvent m_OnSave = new UnityEvent();
        public UnityEvent OnSave
        {
            get { return m_OnSave; }
            set { m_OnSave = value; }
        }
        [SerializeField] protected Button m_SaveButton;
        #endregion

        #region Public Methods
        public virtual void Save()
        {
            OnSave.Invoke();
            base.Close();
        }
        #endregion

        #region Protected Methods
        protected override void Initialize()
        {
            base.Initialize();
            m_SaveButton.onClick.AddListener(Save);
        }
        protected override void SetInteractable(bool interactable)
        {
            m_SaveButton.interactable = interactable;
        }
        #endregion
    }
}

