using HBP.UI.Tools;
using UnityEngine;

namespace HBP.UI.Main
{
    public class DatabaseMenu : Menu
    {
        #region Properties
        [SerializeField] private MenuButton m_OpenProtocolGestionButton;
        public MenuButton OpenProtocolGestionButton { get { return m_OpenProtocolGestionButton; } }
        #endregion

        #region Private Methods
        protected override void Awake()
        {
            base.Awake();
            m_OpenProtocolGestionButton.Initialize(this, OpenProtocolGestion);
        }
        #endregion

        #region Public Methods
        public void OpenProtocolGestion()
        {
            WindowsManager.Open("Protocol gestion window");
        }
        #endregion
    }
}