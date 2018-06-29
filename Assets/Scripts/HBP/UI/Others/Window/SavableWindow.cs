using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace HBP.UI
{
    public abstract class SavableWindow : Window, ISavable
    {
        #region Properties
        public UnityEvent OnSave { get; set; }
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

