using UnityEngine;

namespace HBP.UI.Preferences
{
    public class ThemePreferencesModifier : MonoBehaviour
    {
        #region Properties
        protected bool m_Interactable;
        public virtual bool Interactable
        {
            get
            {
                return m_Interactable;
            }
            set
            {
                m_Interactable = value;
            }
        }
        #endregion

        #region Public Methods
        public void SetFields()
        {

        }
        public void Save()
        {
        }
        #endregion
    }
}