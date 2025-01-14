using HBP.UI.Tools;
using UnityEngine;

namespace HBP.UI.Main
{
    public class DatabaseMenu : Menu
    {
        #region Properties
        [SerializeField] private MenuButton m_OpenProtocolGestionButton;
        public MenuButton OpenProtocolGestionButton { get { return m_OpenProtocolGestionButton; } }

        [SerializeField] private MenuButton m_OpenDatabaseGestionButton;
        public MenuButton OpenDatabaseGestionButton { get { return m_OpenDatabaseGestionButton; } }
        #endregion

        #region Private Methods
        protected override void Awake()
        {
            base.Awake();
            m_OpenProtocolGestionButton.Initialize(this, OpenProtocolGestion);
            m_OpenDatabaseGestionButton.Initialize(this, OpenDatabaseGestion);
        }
        #endregion

        #region Public Methods
        public void OpenProtocolGestion()
        {
            WindowsManager.Open("Protocol gestion window");
        }
        public void OpenDatabaseGestion()
        {
            WindowsManager.Open("Database Reference gestion window");
        }
        #endregion
    }
}