using HBP.Data;
using UnityEngine;
using UnityEngine.UI;

namespace HBP.UI.QuickStart
{
    public class QuickStartWindow : Window
    {
        #region Properties
        [SerializeField] private Button m_Back;
        [SerializeField] private Button m_Next;
        [SerializeField] private IntroductionPanel m_IntroductionPanel;
        [SerializeField] private DataTypePanel m_DataTypePanel;
        [SerializeField] private AnatomicalPanel m_AnatomicalPanel;
        [SerializeField] private EpochingPanel m_EpochingPanel;
        [SerializeField] private FunctionalPanel m_FunctionalDataPanel;
        [SerializeField] private FinalizationPanel m_FinalizationPanel;
        private QuickStartPanel m_CurrentPanel;
        public QuickStartPanel CurrentPanel
        {
            get
            {
                return m_CurrentPanel;
            }
            set
            {
                m_CurrentPanel?.ClosePanel();
                m_CurrentPanel = value;
                m_CurrentPanel.OpenPanel();
                m_Back.gameObject.SetActive(m_CurrentPanel.PreviousPanel != null);
                m_Next.GetComponentInChildren<Text>().text = m_CurrentPanel.NextPanel != null ? "Next >" : "Finish";
                // Special case because we need to skip some panels
                if (m_CurrentPanel == m_AnatomicalPanel)
                {
                    if (m_DataTypePanel.OnlyAnatomical)
                    {
                        m_AnatomicalPanel.NextPanel = m_FinalizationPanel;
                        m_FinalizationPanel.PreviousPanel = m_AnatomicalPanel;
                    }
                    else
                    {
                        m_AnatomicalPanel.NextPanel = m_EpochingPanel;
                        m_FinalizationPanel.PreviousPanel = m_FunctionalDataPanel;
                    }
                }
            }
        }

        private Project m_CurrentlyOpenedProject;
        private string m_CurrentlyOpenedProjectLocation;
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            base.Initialize();
            CurrentPanel = m_IntroductionPanel;
            m_Back.onClick.AddListener(() => CurrentPanel = CurrentPanel.PreviousPanel);
            m_Next.onClick.AddListener(() =>
            {
                if (CurrentPanel.NextPanel == null)
                {
                    CurrentPanel.ClosePanel();
                    Finish();
                }
                else
                {
                    CurrentPanel = CurrentPanel.NextPanel;
                }
            });
            m_CurrentlyOpenedProject = ApplicationState.ProjectLoaded;
            m_CurrentlyOpenedProjectLocation = ApplicationState.ProjectLoadedLocation;
            ApplicationState.ProjectLoaded = new Project();
            ApplicationState.ProjectLoadedLocation = Application.dataPath;
        }
        private void Finish()
        {
            base.Close();
        }
        #endregion

        #region Public Methods
        public override void Close()
        {
            base.Close();
            ApplicationState.ProjectLoaded = m_CurrentlyOpenedProject;
            ApplicationState.ProjectLoadedLocation = m_CurrentlyOpenedProjectLocation;
        }
        #endregion
    }
}