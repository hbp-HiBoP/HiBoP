using HBP.Core.Tools;
using HBP.UI.Tools;
using UnityEngine;

namespace HBP.UI.Main
{
    public class ProjectMenu : Menu
    {
        #region Properties

        [SerializeField] private MenuButton m_OpenProjectPreferencesButton;

        public MenuButton OpenProjectPreferencesButton
        {
            get { return m_OpenProjectPreferencesButton; }
        }

        [SerializeField] private MenuButton m_OpenPatientGestionButton;

        public MenuButton OpenPatientGestionButton
        {
            get { return m_OpenPatientGestionButton; }
        }

        [SerializeField] private MenuButton m_OpenGroupGestionButton;

        public MenuButton OpenGroupGestionButton
        {
            get { return m_OpenGroupGestionButton; }
        }

        [SerializeField] private MenuButton m_OpenDatasetGestionButton;

        public MenuButton OpenDatasetGestionButton
        {
            get { return m_OpenDatasetGestionButton; }
        }

        [SerializeField] private MenuButton m_OpenVisualizationGestionButton;

        public MenuButton OpenVisualizationGestionButton
        {
            get { return m_OpenVisualizationGestionButton; }
        }

        #endregion

        #region Private Methods

        protected override void Awake()
        {
            base.Awake();
            m_OpenProjectPreferencesButton.Initialize(this, OpenProjectPreferences);
            m_OpenPatientGestionButton.Initialize(this, OpenPatientGestion);
            m_OpenGroupGestionButton.Initialize(this, OpenGroupGestion);
            m_OpenDatasetGestionButton.Initialize(this, OpenDatasetGestion);
            m_OpenVisualizationGestionButton.Initialize(this, OpenVisualizationGestion);
        }

        #endregion

        #region Public Methods

        public void OpenProjectPreferences()
        {
            WindowsManager.OpenModifier(ApplicationState.LoadedProject.Preferences, null);
        }

        public void OpenPatientGestion()
        {
            WindowsManager.Open("Patient gestion window", null);
        }

        public void OpenGroupGestion()
        {
            WindowsManager.Open("Group gestion window", null);
        }

        public void OpenDatasetGestion()
        {
            WindowsManager.Open("Dataset gestion window", null);
        }

        public void OpenVisualizationGestion()
        {
            WindowsManager.Open("Visualization gestion window", null);
        }

        #endregion
    }
}
