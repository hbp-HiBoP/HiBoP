using HBP.UI.Tools;
using UnityEngine;

namespace HBP.UI.Main
{
    public class HelpMenu : Menu
    {
        #region Properties
        [SerializeField] private MenuButton m_OpenVersionWindowButton;
        public MenuButton OpenVersionWindowButton { get { return m_OpenVersionWindowButton; } }

        [SerializeField] private MenuButton m_OpenBugReporterButton;
        public MenuButton OpenBugReporterButton { get { return m_OpenBugReporterButton; } }

        [SerializeField] private MenuButton m_OpenAboutWindowButton;
        public MenuButton OpenAboutWindowButton { get { return m_OpenAboutWindowButton; } }
        #endregion

        #region Private Methods
        override protected void Awake()
        {
            base.Awake();
            m_OpenVersionWindowButton.Initialize(this, OpenVersionWindow);
            m_OpenBugReporterButton.Initialize(this, OpenBugReporter);
            m_OpenAboutWindowButton.Initialize(this, OpenAboutWindow);
        }
        #endregion

        #region Public Methods
        public void OpenVersionWindow()
        {
            WindowsManager.Open("Version Window");
        }
        public void OpenBugReporter()
        {
            WindowsManager.Open("Bug Reporter window");
        }
        public void OpenAboutWindow()
        {
            WindowsManager.Open("About window");
        }
        #endregion
    }
}