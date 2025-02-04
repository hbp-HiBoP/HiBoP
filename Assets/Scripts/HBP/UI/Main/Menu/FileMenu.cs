using UnityEngine;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    public class FileMenu : Menu
    {
        #region Properties
        [SerializeField] private MenuButton m_NewProjectButton;
        public MenuButton NewProjectButton { get { return m_NewProjectButton; } }

        [SerializeField] private MenuButton m_OpenProjectButton;
        public MenuButton OpenProjectButton { get { return m_OpenProjectButton; } }

        [SerializeField] private MenuButton m_SaveButton;
        public MenuButton SaveButton { get { return m_SaveButton; } }

        [SerializeField] private MenuButton m_SaveAsButton;
        public MenuButton SaveAsButton { get { return m_SaveAsButton; } }

        [SerializeField] private MenuButton m_QuickStartButton;
        public MenuButton QuickStartButton { get { return m_QuickStartButton; } }

        [SerializeField] private MenuButton m_QuitButton;
        public MenuButton QuitButton { get { return m_QuitButton; } }
        #endregion

        #region Private Methods
        protected override void Awake()
        {
            base.Awake();
            m_NewProjectButton.Initialize(this, OpenNewProject);
            m_OpenProjectButton.Initialize(this, OpenLoadProject);
            m_SaveButton.Initialize(this, Save);
            m_SaveAsButton.Initialize(this, OpenSaveProjectAs);
            m_QuickStartButton.Initialize(this, QuickStart);
            m_QuitButton.Initialize(this, Quit);
        }
        #endregion

        #region Public Methods
        public void OpenNewProject()
        {
            WindowsManager.Open("New project window");
        }
        public void OpenLoadProject()
        {
            WindowsManager.Open("Open project window");
        }
        public void Save()
        {
            ProjectLoaderSaver.Save();
        }
        public void OpenSaveProjectAs()
        {
            WindowsManager.Open("Save project as window");
        }
        public void QuickStart()
        {
            WindowsManager.Open("Quick start window");
        }
        public async void Quit()
        {
            int result = await DialogBoxManager.OpenAsync(Core.Enums.DialogBoxType.Informational, "Quit HiBoP?", "Are you sure you want to quit HiBoP? Make sure all your data is saved.", "Quit", "Cancel");
            if (result == 0) Application.Quit();
        }
        #endregion
    }
}