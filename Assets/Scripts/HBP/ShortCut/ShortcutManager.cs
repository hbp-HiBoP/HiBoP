using HBP.Module3D;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using HBP.Core.Data.Enums;

namespace HBP.UI
{
    public class ShortcutManager : MonoBehaviour
    {
        #region Properties
        [SerializeField] MainMenu m_MainMenu;

        private bool IsControlPressed
        {
            get
            {
                return Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            }
        }
        private bool IsAltPressed
        {
            get
            {
                return Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            }
        }
        private bool IsShiftPressed
        {
            get
            {
                return Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            }
        }
        private bool IsArrowKeyPressed
        {
            get
            {
                return Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow);
            }
        }
        private bool IsArrowKeyDown
        {
            get
            {
                return Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow);
            }
        }
        private List<KeyCode> m_ChangeColorActions = new List<KeyCode> { KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3, KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6, KeyCode.Alpha7, KeyCode.Alpha8, KeyCode.Alpha9, KeyCode.Alpha0 };
        private List<KeyCode> ChangeSiteStateActions
        {
            get
            {
                return new List<KeyCode>(m_ChangeColorActions) { KeyCode.H, KeyCode.B };
            }
        }
        private bool IsSiteStateActionDown
        {
            get
            {
                return ChangeSiteStateActions.Any(a => Input.GetKeyDown(a));
            }
        }
        private bool NewProjectActionPerformed
        {
            get
            {
                return IsControlPressed && Input.GetKeyDown(KeyCode.N);
            }
        }
        private bool OpenProjectActionPerformed
        {
            get
            {
                return IsControlPressed && Input.GetKeyDown(KeyCode.O);
            }
        }
        private bool SaveActionPerformed
        {
            get
            {
                return IsControlPressed && !IsShiftPressed && Input.GetKeyDown(KeyCode.S);
            }
        }
        private bool SaveAsActionPerformed
        {
            get
            {
                return IsControlPressed && IsShiftPressed && Input.GetKeyDown(KeyCode.S);
            }
        }
        private bool QuitActionPerformed
        {
            get
            {
                return IsControlPressed && Input.GetKeyDown(KeyCode.Q);
            }
        }
        private bool OpenPreferencesActionPerformed
        {
            get
            {
                return IsControlPressed && Input.GetKeyDown(KeyCode.U);
            }
        }
        private bool OpenProjectPreferencesActionPerformed
        {
            get
            {
                return IsControlPressed && Input.GetKeyDown(KeyCode.T);
            }
        }
        private bool OpenPatientsActionPerformed
        {
            get
            {
                return IsControlPressed && Input.GetKeyDown(KeyCode.P);
            }
        }
        private bool OpenGroupsActionPerformed
        {
            get
            {
                return IsControlPressed && Input.GetKeyDown(KeyCode.G);
            }
        }
        private bool OpenProtocolsActionPerformed
        {
            get
            {
                return IsControlPressed && Input.GetKeyDown(KeyCode.E);
            }
        }
        private bool OpenDatasetsActionPerformed
        {
            get
            {
                return IsControlPressed && Input.GetKeyDown(KeyCode.D);
            }
        }
        private bool OpenVisualizationsActionPerformed
        {
            get
            {
                return IsControlPressed && Input.GetKey(KeyCode.Y);
            }
        }
        private const float DELAY = 0.2f;
        private float m_Timer = 0.0f;
        private bool SiteSelectionActionPerformed
        {
            get
            {
                return (IsArrowKeyPressed && m_Timer >= DELAY) || IsArrowKeyDown;
            }
        }
        private bool ChangeSiteStateActionPerformed
        {
            get
            {
                return IsControlPressed && IsSiteStateActionDown;
            }
        }
        private bool IsWritingInInputField
        {
            get
            {
                EventSystem eventSystem = EventSystem.current;
                if (eventSystem)
                {
                    GameObject currentObject = eventSystem.currentSelectedGameObject;
                    if (currentObject)
                    {
                        InputField inputfield = currentObject.GetComponent<InputField>();
                        if (inputfield)
                        {
                            if (inputfield.isFocused)
                            {
                                return true;
                            }
                        }
                    }
                }
                return false;
            }
        }
        #endregion

        #region Private Methods
        void Update()
        {
            m_Timer += Time.deltaTime;
            if (IsWritingInInputField)
            {
                return;
            }
            if (NewProjectActionPerformed)
            {
                NewProject();
            }
            else if (OpenProjectActionPerformed)
            {
                OpenProject();
            }
            else if (SaveAsActionPerformed)
            {
                SaveAs();
            }
            else if (SaveActionPerformed)
            {
                Save();
            }
            else if (QuitActionPerformed)
            {
                Quit();
            }
            else if (OpenPreferencesActionPerformed)
            {
                UserPreferences();
            }
            else if (OpenProjectPreferencesActionPerformed)
            {
                ProjectPreferences();
            }
            else if (OpenPatientsActionPerformed)
            {
                Patients();
            }
            else if (OpenGroupsActionPerformed)
            {
                Groups();
            }
            else if (OpenProtocolsActionPerformed)
            {
                Protocols();
            }
            else if (OpenDatasetsActionPerformed)
            {
                Datasets();
            }
            else if (OpenVisualizationsActionPerformed)
            {
                Visualizations();
            }
            else if (SiteSelectionActionPerformed)
            {
                m_Timer = 0;
                SiteNavigationDirection direction = Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.UpArrow) ? SiteNavigationDirection.Left : SiteNavigationDirection.Right;
                ChangeSiteSelection(direction);
            }
            else if (ChangeSiteStateActionPerformed)
            {
                ChangeSelectedSiteState();
            }
        }
        private void NewProject()
        {
            if (m_MainMenu.FileMenu.NewProjectInteractableConditions.interactable)
                m_MainMenu.FileMenu.OpenNewProject();
        }
        private void OpenProject()
        {
            if (m_MainMenu.FileMenu.OpenProjectInteractableConditions.interactable)
                m_MainMenu.FileMenu.OpenLoadProject();
        }
        private void Save()
        {
            if (m_MainMenu.FileMenu.SaveProjectInteractableConditions.interactable)
                m_MainMenu.FileMenu.Save();
        }
        private void SaveAs()
        {
            if (m_MainMenu.FileMenu.SaveProjectAsInteractableConditions.interactable)
                m_MainMenu.FileMenu.OpenSaveProjectAs();
        }
        private void Quit()
        {
            if (m_MainMenu.FileMenu.QuitInteractableConditions.interactable)
                m_MainMenu.FileMenu.Quit();
        }
        private void UserPreferences()
        {
            if (m_MainMenu.EditMenu.PreferencesInteractableConditions.interactable)
                m_MainMenu.EditMenu.OpenPreferences();
        }
        private void ProjectPreferences()
        {
            if (m_MainMenu.EditMenu.ProjectPreferencesInteractableConditions.interactable)
                m_MainMenu.EditMenu.OpenProjectPreferences();
        }
        private void Patients()
        {
            if (m_MainMenu.PatientMenu.PatientsInteractableConditions.interactable)
                m_MainMenu.PatientMenu.OpenPatientGestion();
        }
        private void Groups()
        {
            if (m_MainMenu.PatientMenu.GroupsInteractableConditions.interactable)
                m_MainMenu.PatientMenu.OpenGroupGestion();
        }
        private void Protocols()
        {
            if (m_MainMenu.ExperienceMenu.ProtocolsInteractableConditions.interactable)
                m_MainMenu.ExperienceMenu.OpenProtocolGestion();
        }
        private void Datasets()
        {
            if (m_MainMenu.ExperienceMenu.DatasetsInteractableConditions.interactable)
                m_MainMenu.ExperienceMenu.OpenDatasetGestion();
        }
        private void Visualizations()
        {
            if (m_MainMenu.VisualizationMenu.InteractableConditions.interactable)
                m_MainMenu.VisualizationMenu.OpenVisualizationGestion();
        }
        private void ChangeSiteSelection(SiteNavigationDirection direction)
        {
            Base3DScene scene = ApplicationState.Module3D.SelectedScene;
            if (scene != null)
            {
                Column3D selectedColumn = scene.SelectedColumn;
                int selectedId = selectedColumn.SelectedSiteID;
                if (selectedId != -1)
                {
                    Core.Object3D.Site site;
                    int id = selectedId;
                    int count = 0;
                    do
                    {
                        switch (direction)
                        {
                            case SiteNavigationDirection.Left:
                                id--;
                                if (id < 0) id = selectedColumn.Sites.Count - 1;
                                break;
                            case SiteNavigationDirection.Right:
                                id++;
                                if (id > selectedColumn.Sites.Count - 1) id = 0;
                                break;
                        }
                        site = selectedColumn.Sites[id];
                    }
                    while ((!site.State.IsFiltered || site.State.IsMasked || (!scene.ShowAllSites && scene.ROIManager.SelectedROI != null && site.State.IsOutOfROI)) && ++count < selectedColumn.Sites.Count);
                    site.IsSelected = true;
                }
            }
        }
        private void ChangeSelectedSiteState()
        {
            Base3DScene scene = ApplicationState.Module3D.SelectedScene;
            if (scene)
            {
                Column3D column = scene.SelectedColumn;
                if (column)
                {
                    Core.Object3D.Site selectedSite = column.SelectedSite;
                    if (selectedSite)
                    {
                        List<Core.Object3D.Site> sites = new List<Core.Object3D.Site>();
                        if (IsShiftPressed)
                        {
                            foreach (Transform siteTransform in selectedSite.transform.parent)
                            {
                                sites.Add(siteTransform.GetComponent<Core.Object3D.Site>());
                            }
                        }
                        else if (IsAltPressed)
                        {
                            foreach (Transform electrode in selectedSite.transform.parent.parent)
                            {
                                foreach (Transform siteTransform in electrode)
                                {
                                    sites.Add(siteTransform.GetComponent<Core.Object3D.Site>());
                                }
                            }
                        }
                        else
                        {
                            sites.Add(selectedSite);
                        }
                        sites = sites.Where(s => s.State.IsFiltered).ToList();
                        KeyCode downAction = ChangeSiteStateActions.FirstOrDefault(a => Input.GetKeyDown(a));
                        switch (downAction)
                        {
                            case KeyCode.H:
                                {
                                    bool allHighlighted = sites.All(s => s.State.IsHighlighted);
                                    foreach (var site in sites) site.State.IsHighlighted = !allHighlighted;
                                }
                                break;
                            case KeyCode.B:
                                {
                                    bool allBlacklisted = sites.All(s => s.State.IsBlackListed);
                                    foreach (var site in sites) site.State.IsBlackListed = !allBlacklisted;
                                }
                                break;
                            default:
                                {
                                    int index = m_ChangeColorActions.IndexOf(downAction);
                                    if (index == -1) break;

                                    Color color = ApplicationState.ColorPicker.GetDefaultColor(index);
                                    foreach (var site in sites) site.State.Color = color;
                                }
                                break;
                        }
                    }
                }
            }
        }
        #endregion
    }
}