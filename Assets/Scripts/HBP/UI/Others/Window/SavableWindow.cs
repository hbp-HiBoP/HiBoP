using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using System.Linq;

namespace HBP.UI
{
    public abstract class SavableWindow : Window, ISavable
    {
        #region Properties
        public UnityEvent OnSave { get; set; }
        [SerializeField] protected Button m_SaveButton;
        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }

            set
            {
                base.Interactable = value;
                m_SaveButton.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public virtual void Save()
        {
            foreach (var savableSubWindow in m_SubWindows.OfType<SavableWindow>()) savableSubWindow.Save();
            OnSave.Invoke();
            base.Close();
        }
        #endregion

        #region Protected Methods
        protected override void Initialize()
        {
            OnSave = new UnityEvent();
            m_SaveButton.onClick.AddListener(Save);
            base.Initialize();
        }
        #endregion
    }
}

