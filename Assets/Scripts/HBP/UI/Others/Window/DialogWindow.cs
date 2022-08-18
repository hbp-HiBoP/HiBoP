using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace HBP.UI.Tools
{
    /// <summary>
    /// Abstract base class for every dialog window. There is two choices in a dialog window : OK and Close.
    /// </summary>
    public abstract class DialogWindow : Window
    {
        #region Properties
        [SerializeField] protected Button m_OKButton;

        [SerializeField] protected UnityEvent m_OnOk;
        /// <summary>
        /// Callback executed when the window is validate.
        /// </summary>
        public UnityEvent OnOk
        {
            get
            {
                return m_OnOk;
            }
        }

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
        /// <summary>
        /// Valid and close the window and its children.
        /// </summary>
        public virtual void OK()
        {
            WindowsReferencer.SaveAll();
            OnOk.Invoke();
            base.Close();
        }
        #endregion
    }
}

