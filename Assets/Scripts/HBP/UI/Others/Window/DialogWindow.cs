using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace HBP.UI
{
    public abstract class DialogWindow : Window
    {
        #region Properties
        [SerializeField] protected Button m_OKButton;
        public UnityEvent OnOk { get; } = new UnityEvent();
        public override bool Interactable
        {
            get
            {
                return base.Interactable;
            }

            set
            {
                base.Interactable = value;
                m_OKButton.interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public virtual void OK()
        {
            WindowsReferencer.SaveAll();
            OnOk.Invoke();
            base.Close();
        }
        #endregion
    }
}

