﻿using UnityEngine;

namespace HBP.UI
{
    public class FileMenu : Menu
    {
        #region Properties
        [SerializeField] ProjectLoaderSaver m_ProjectLoaderSaver;
        [SerializeField] InteractableConditions m_NewProjectInteractableConditions;
        public InteractableConditions NewProjectInteractableConditions { get { return m_NewProjectInteractableConditions; } }
        [SerializeField] InteractableConditions m_OpenProjectInteractableConditions;
        public InteractableConditions OpenProjectInteractableConditions { get { return m_OpenProjectInteractableConditions; } }
        [SerializeField] InteractableConditions m_SaveProjectInteractableConditions;
        public InteractableConditions SaveProjectInteractableConditions { get { return m_SaveProjectInteractableConditions; } }
        [SerializeField] InteractableConditions m_SaveProjectAsInteractableConditions;
        public InteractableConditions SaveProjectAsInteractableConditions { get { return m_SaveProjectAsInteractableConditions; } }
        [SerializeField] InteractableConditions m_QuickStartInteractableConditions;
        public InteractableConditions QuickStartInteractableConditions { get { return m_QuickStartInteractableConditions; } }
        [SerializeField] InteractableConditions m_QuitInteractableConditions;
        public InteractableConditions QuitInteractableConditions { get { return m_QuitInteractableConditions; } }
        #endregion

        #region Public Methods
        public void OpenNewProject()
        {
            ApplicationState.WindowsManager.Open("New project window");
        }
        public void OpenLoadProject()
        {
            ApplicationState.WindowsManager.Open("Open project window");
        }
        public void Save()
        {
            m_ProjectLoaderSaver.Save();
        }
        public void OpenSaveProjectAs()
        {
            ApplicationState.WindowsManager.Open("Save project as window");
        }
        public void QuickStart()
        {
            ApplicationState.WindowsManager.Open("Quick start window");
        }
        public void Quit()
        {
            ApplicationState.DialogBoxManager.Open(Tools.Unity.DialogBoxManager.AlertType.WarningMultiOptions, "Quit HiBoP?", "Are you sure you want to quit HiBoP? Make sure all your data is saved.", () => { Application.Quit(); }, "Quit", () => { }, "Cancel");
        }
        #endregion
    }
}