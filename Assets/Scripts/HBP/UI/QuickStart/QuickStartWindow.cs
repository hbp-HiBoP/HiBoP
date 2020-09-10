using CielaSpike;
using HBP.Data;
using UnityEngine;
using UnityEngine.Events;
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
                m_CurrentPanel = value;
                if (m_CurrentPanel != null)
                {
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
        }

        private Project m_CurrentlyOpenedProject;
        private string m_CurrentlyOpenedProjectLocation;
        #endregion

        #region Private Methods
        protected override void Initialize()
        {
            base.Initialize();
            CurrentPanel = m_IntroductionPanel;
            m_IntroductionPanel.Open();
            m_Back.onClick.AddListener(() =>
            {
                if (CurrentPanel.OpenPreviousPanel())
                    CurrentPanel = CurrentPanel.PreviousPanel;
            });
            m_Next.onClick.AddListener(() =>
            {
                if (CurrentPanel.OpenNextPanel())
                    CurrentPanel = CurrentPanel.NextPanel;
                if (CurrentPanel == null)
                    Finish();
            });
            m_CurrentlyOpenedProject = ApplicationState.ProjectLoaded;
            m_CurrentlyOpenedProjectLocation = ApplicationState.ProjectLoadedLocation;
            ApplicationState.ProjectLoaded = new Project();
            ApplicationState.ProjectLoadedLocation = Application.dataPath;
        }
        private void Finish()
        {
            base.Close();
            GenericEvent<float, float, LoadingText> onChangeProgress = new GenericEvent<float, float, LoadingText>();
            ApplicationState.LoadingManager.Load(ApplicationState.ProjectLoaded.c_Save(ApplicationState.ProjectLoadedLocation, (progress, duration, text) => onChangeProgress.Invoke(progress, duration, text)), onChangeProgress, (state) =>
            {
                if (state == TaskState.Done)
                {
                    FindObjectOfType<MenuButtonState>().SetInteractables();
                    ApplicationState.Module3D.LoadScenes(ApplicationState.ProjectLoaded.Visualizations);
                }
            });
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