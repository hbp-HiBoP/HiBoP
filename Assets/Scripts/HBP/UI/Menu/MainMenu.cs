using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using HBP.UI.Tools;

namespace HBP.UI.Main
{
    public class MainMenu : MonoBehaviour
    {
        #region Properties
        [SerializeField] FileMenu m_FileMenu;
        public FileMenu FileMenu { get { return m_FileMenu; } }
        [SerializeField] EditMenu m_EditMenu;
        public EditMenu EditMenu { get { return m_EditMenu; } }
        [SerializeField] PatientMenu m_PatientMenu;
        public PatientMenu PatientMenu { get { return m_PatientMenu; } }
        [SerializeField] ExperienceMenu m_ExperienceMenu;
        public ExperienceMenu ExperienceMenu { get { return m_ExperienceMenu; } }
        [SerializeField] VisualizationMenu m_VisualizationMenu;
        public VisualizationMenu VisualizationMenu { get { return m_VisualizationMenu; } }
        [SerializeField] VersionMenu m_VersionMenu;
        public VersionMenu VersionMenu { get { return m_VersionMenu; } }
        [SerializeField] BugReporterMenu m_BugReporterMenu;
        public BugReporterMenu BugReporterMenu { get { return m_BugReporterMenu; } }
        bool IsOneMenuOpen
        {
            get
            {
                return m_FileMenu.IsOpen || m_EditMenu.IsOpen || m_PatientMenu.IsOpen || m_ExperienceMenu.IsOpen || m_VisualizationMenu.IsOpen || m_VersionMenu.IsOpen || m_BugReporterMenu.IsOpen;
            }
        }
        #endregion

        #region Public Methods
        public void CloseAll()
        {
            m_FileMenu.Close();
            m_EditMenu.Close();
            m_PatientMenu.Close();
            m_ExperienceMenu.Close();
            m_VisualizationMenu.Close();
            m_VersionMenu.Close();
            m_BugReporterMenu.Close();
        }
        #endregion

        #region Private Methods
        void Set(Menu menu)
        {
            if (menu != m_FileMenu) m_FileMenu.Close();
            if (menu != m_EditMenu) m_EditMenu.Close();
            if (menu != m_PatientMenu) m_PatientMenu.Close();
            if (menu != m_ExperienceMenu) m_ExperienceMenu.Close();
            if (menu != m_VisualizationMenu) m_VisualizationMenu.Close();
            if (menu != m_VersionMenu) m_VersionMenu.Close();
            if (menu != m_BugReporterMenu) m_BugReporterMenu.Close();
        }
        #endregion

        #region Unity Region
        private void Awake()
        {
            m_FileMenu.OnChangeOpenState.AddListener((isOpen) =>
            {
                if (isOpen)
                    Set(m_FileMenu);
            });
            m_FileMenu.OnHover.AddListener((isHovered) =>
            {
                if (isHovered && IsOneMenuOpen)
                    m_FileMenu.Open();
            });
            m_EditMenu.OnChangeOpenState.AddListener((isOpen) =>
            {
                if (isOpen)
                    Set(m_EditMenu);
            });
            m_EditMenu.OnHover.AddListener((isHovered) =>
            {
                if (isHovered && IsOneMenuOpen)
                    m_EditMenu.Open();
            });
            m_PatientMenu.OnChangeOpenState.AddListener((isOpen) =>
            {
                if (isOpen)
                    Set(m_PatientMenu);
            });
            m_PatientMenu.OnHover.AddListener((isHovered) =>
            {
                if (isHovered && IsOneMenuOpen)
                    m_PatientMenu.Open();
            });
            m_ExperienceMenu.OnChangeOpenState.AddListener((isOpen) =>
            {
                if (isOpen)
                    Set(m_ExperienceMenu);
            });
            m_ExperienceMenu.OnHover.AddListener((isHovered) =>
            {
                if (isHovered && IsOneMenuOpen)
                    m_ExperienceMenu.Open();
            });
            m_VisualizationMenu.OnChangeOpenState.AddListener((isOpen) =>
            {
                if (isOpen)
                    Set(m_VisualizationMenu);
            });
            m_VisualizationMenu.OnHover.AddListener((isHovered) =>
            {
                if (isHovered && IsOneMenuOpen)
                    m_VisualizationMenu.Open();
            });
            m_VersionMenu.OnChangeOpenState.AddListener((isOpen) =>
            {
                if (isOpen)
                    Set(m_VersionMenu);
            });
            m_VersionMenu.OnHover.AddListener((isHovered) =>
            {
                if (isHovered && IsOneMenuOpen)
                    m_VersionMenu.Open();
            });
            m_BugReporterMenu.OnChangeOpenState.AddListener((isOpen) =>
            {
                if (isOpen)
                    Set(m_BugReporterMenu);
            });
            m_BugReporterMenu.OnHover.AddListener((isHovered) =>
            {
                if (isHovered && IsOneMenuOpen)
                    m_BugReporterMenu.Open();
            });
        }
        void Update()
        {
            if(Input.GetMouseButtonUp(0))
            {
                PointerEventData pointer = new PointerEventData(EventSystem.current);
                // convert to a 2D position
                pointer.position = Input.mousePosition;
                List<RaycastResult> raycastResults = new List<RaycastResult>();
                EventSystem.current.RaycastAll(pointer, raycastResults);
                if (raycastResults.Count > 0)
                {
                    if(raycastResults[0].gameObject.layer != 23)
                    {
                        //Close All
                        CloseAll();
                    }
                }
            }
        }
        #endregion
    }
}