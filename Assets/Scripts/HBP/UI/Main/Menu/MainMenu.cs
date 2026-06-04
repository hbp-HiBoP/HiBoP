using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;

namespace HBP.UI.Main
{
    public class MainMenu : MonoBehaviour
    {
        #region Properties
        [SerializeField] FileMenu m_FileMenu;
        public FileMenu FileMenu { get { return m_FileMenu; } }

        [SerializeField] EditMenu m_EditMenu;
        public EditMenu EditMenu { get { return m_EditMenu; } }

        [SerializeField] ProjectMenu m_ProjectMenu;
        public ProjectMenu ProjectMenu { get { return m_ProjectMenu; } }

        [SerializeField] DatabaseMenu m_DatabaseMenu;
        public DatabaseMenu DatabaseMenu { get { return m_DatabaseMenu; } }

        [SerializeField] HelpMenu m_HelpMenu;
        public HelpMenu HelpMenu { get { return m_HelpMenu; } }

        bool IsOneMenuOpen
        {
            get
            {
                return m_FileMenu.IsOpen || m_EditMenu.IsOpen || m_ProjectMenu.IsOpen || m_DatabaseMenu.IsOpen || m_HelpMenu.IsOpen;
            }
        }
        #endregion

        #region Private Methods
        private void Awake()
        {
            SetupMenu(m_FileMenu);
            SetupMenu(m_EditMenu);
            SetupMenu(m_ProjectMenu);
            SetupMenu(m_DatabaseMenu);
            SetupMenu(m_HelpMenu);
        }
        void Update()
        {
            if (Input.GetMouseButtonUp(0))
            {
                PointerEventData pointer = new(EventSystem.current)
                {
                    position = Input.mousePosition
                };
                List<RaycastResult> raycastResults = new();
                EventSystem.current.RaycastAll(pointer, raycastResults);
                if (raycastResults.Count > 0)
                {
                    if (raycastResults[0].gameObject.layer != LayerMask.NameToLayer("UI_Menu"))
                    {
                        CloseAll();
                    }
                }
            }
        }
        private void SetupMenu(Menu menu)
        {
            menu.OnChangeOpenState.AddListener((isOpen) =>
            {
                if (isOpen)
                    CloseAllBut(menu);
            });
            menu.OnHover.AddListener((isHovered) =>
            {
                if (isHovered && IsOneMenuOpen)
                    menu.Open();
            });
        }
        private void CloseAllBut(Menu menu)
        {
            if (menu != m_FileMenu) m_FileMenu.Close();
            if (menu != m_EditMenu) m_EditMenu.Close();
            if (menu != m_ProjectMenu) m_ProjectMenu.Close();
            if (menu != m_DatabaseMenu) m_DatabaseMenu.Close();
            if (menu != m_HelpMenu) m_HelpMenu.Close();
        }
        private void CloseAll()
        {
            m_FileMenu.Close();
            m_EditMenu.Close();
            m_ProjectMenu.Close();
            m_DatabaseMenu.Close();
            m_HelpMenu.Close();
        }
        #endregion
    }
}