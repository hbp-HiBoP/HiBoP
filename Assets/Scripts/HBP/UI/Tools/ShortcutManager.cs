using HBP.Input;
using UnityEngine.InputSystem;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using HBP.Core.Enums;
using HBP.Data.Module3D;
using HBP.UI.Main;
using HBP.UI.Module3D;
using HBP.Core.Object3D;
using HBP.Core.Tools;

namespace HBP.UI.Tools
{
    public class ShortcutManager : Manager<ShortcutManager>
    {
        #region Properties

        private MainMenu m_MainMenu;

        private MainMenu MainMenu
        {
            get
            {
                if (m_MainMenu == null)
                    m_MainMenu = FindAnyObjectByType<MainMenu>();
                return m_MainMenu;
            }
        }

        private bool IsControlPressed
        {
            get { return DesktopInput.IsControlPressed; }
        }

        private bool IsAltPressed
        {
            get { return DesktopInput.IsAltPressed; }
        }

        private bool IsShiftPressed
        {
            get { return DesktopInput.IsShiftPressed; }
        }

        private bool IsModPressed
        {
            get { return IsControlPressed || IsAltPressed || IsShiftPressed; }
        }

        private bool IsArrowKeyPressed
        {
            get { return DesktopInput.IsPressed(Key.LeftArrow) || DesktopInput.IsPressed(Key.RightArrow) || DesktopInput.IsPressed(Key.UpArrow) || DesktopInput.IsPressed(Key.DownArrow); }
        }

        private bool IsArrowKeyDown
        {
            get { return DesktopInput.WasPressedThisFrame(Key.LeftArrow) || DesktopInput.WasPressedThisFrame(Key.RightArrow) || DesktopInput.WasPressedThisFrame(Key.UpArrow) || DesktopInput.WasPressedThisFrame(Key.DownArrow); }
        }

        private List<Key> m_ChangeColorActions = new() { Key.Digit1, Key.Digit2, Key.Digit3, Key.Digit4, Key.Digit5, Key.Digit6, Key.Digit7, Key.Digit8, Key.Digit9, Key.Digit0 };

        private List<Key> ChangeSiteStateActions
        {
            get { return new List<Key>(m_ChangeColorActions) { Key.H, Key.B }; }
        }

        private bool IsSiteStateActionDown
        {
            get { return ChangeSiteStateActions.Any(DesktopInput.WasPressedThisFrame); }
        }

        private bool NewProjectActionPerformed
        {
            get { return IsControlPressed && DesktopInput.WasPressedThisFrame(Key.N); }
        }

        private bool OpenProjectActionPerformed
        {
            get { return IsControlPressed && DesktopInput.WasPressedThisFrame(Key.O); }
        }

        private bool SaveActionPerformed
        {
            get { return IsControlPressed && !IsShiftPressed && DesktopInput.WasPressedThisFrame(Key.S); }
        }

        private bool SaveAsActionPerformed
        {
            get { return IsControlPressed && IsShiftPressed && DesktopInput.WasPressedThisFrame(Key.S); }
        }

        private bool QuitActionPerformed
        {
            get { return IsControlPressed && DesktopInput.WasPressedThisFrame(Key.Q); }
        }

        private bool OpenPreferencesActionPerformed
        {
            get { return IsControlPressed && DesktopInput.WasPressedThisFrame(Key.U); }
        }

        private bool OpenProjectPreferencesActionPerformed
        {
            get { return IsControlPressed && DesktopInput.WasPressedThisFrame(Key.T); }
        }

        private bool OpenPatientsActionPerformed
        {
            get { return IsControlPressed && DesktopInput.WasPressedThisFrame(Key.P); }
        }

        private bool OpenGroupsActionPerformed
        {
            get { return IsControlPressed && DesktopInput.WasPressedThisFrame(Key.G); }
        }

        private bool OpenProtocolsActionPerformed
        {
            get { return IsControlPressed && DesktopInput.WasPressedThisFrame(Key.E); }
        }

        private bool OpenDatasetsActionPerformed
        {
            get { return IsControlPressed && DesktopInput.WasPressedThisFrame(Key.D); }
        }

        private bool OpenVisualizationsActionPerformed
        {
            get { return IsControlPressed && DesktopInput.IsPressed(Key.Y); }
        }

        private const float SITE_SELECTION_DELAY = 0.2f;
        private const float CUT_ACTION_DELAY = 0.02f;
        private float m_Timer = 0.0f;

        private bool SiteSelectionActionPerformed
        {
            get { return ((IsArrowKeyPressed && m_Timer >= SITE_SELECTION_DELAY) || IsArrowKeyDown) && !IsModPressed && !IsWindowSelected; }
        }

        private bool CutModificationActionPerformed
        {
            get { return ((IsArrowKeyPressed && m_Timer >= CUT_ACTION_DELAY) || IsArrowKeyDown || DesktopInput.WasPressedThisFrame(Key.F) || DesktopInput.WasPressedThisFrame(Key.C) || DesktopInput.WasPressedThisFrame(Key.A) || DesktopInput.WasPressedThisFrame(Key.Tab)) && IsControlPressed && !IsWindowSelected; }
        }

        private bool ChangeSiteStateActionPerformed
        {
            get { return IsControlPressed && IsSiteStateActionDown && !IsWindowSelected; }
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

                        TMPro.TMP_InputField tmpInputField = currentObject.GetComponent<TMPro.TMP_InputField>();
                        if (tmpInputField && tmpInputField.isFocused) return true;
                    }
                }

                return false;
            }
        }

        private bool IsWindowSelected => SelectionManager.IsAnySelected;

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
                SiteNavigationDirection direction = DesktopInput.IsPressed(Key.LeftArrow) || DesktopInput.IsPressed(Key.UpArrow) ? SiteNavigationDirection.Left : SiteNavigationDirection.Right;
                ChangeSiteSelection(direction);
            }
            else if (ChangeSiteStateActionPerformed)
            {
                ChangeSelectedSiteState();
            }
            else if (CutModificationActionPerformed)
            {
                m_Timer = 0;
                PerformActionOnCut();
            }
        }

        private void NewProject()
        {
            MainMenu.FileMenu.NewProjectButton.Action();
        }

        private void OpenProject()
        {
            MainMenu.FileMenu.OpenProjectButton.Action();
        }

        private void Save()
        {
            MainMenu.FileMenu.SaveButton.Action();
        }

        private void SaveAs()
        {
            MainMenu.FileMenu.SaveAsButton.Action();
        }

        private void Quit()
        {
            MainMenu.FileMenu.QuitButton.Action();
        }

        private void UserPreferences()
        {
            MainMenu.EditMenu.OpenPreferencesButton.Action();
        }

        private void TagsManager()
        {
            MainMenu.EditMenu.OpenTagsManagerButton.Action();
        }

        private void ProjectPreferences()
        {
            MainMenu.ProjectMenu.OpenProjectPreferencesButton.Action();
        }

        private void Patients()
        {
            MainMenu.ProjectMenu.OpenPatientGestionButton.Action();
        }

        private void Groups()
        {
            MainMenu.ProjectMenu.OpenGroupGestionButton.Action();
        }

        private void Datasets()
        {
            MainMenu.ProjectMenu.OpenDatasetGestionButton.Action();
        }

        private void Visualizations()
        {
            MainMenu.ProjectMenu.OpenVisualizationGestionButton.Action();
        }

        private void Protocols()
        {
            MainMenu.DatabaseMenu.OpenProtocolGestionButton.Action();
        }

        private void ChangeSiteSelection(SiteNavigationDirection direction)
        {
            Base3DScene scene = Module3DMain.SelectedScene;
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
                    } while ((!site.State.IsFiltered || site.State.IsMasked || (!scene.ShowAllSites && scene.ROIManager.SelectedROI != null && site.State.IsOutOfROI)) && ++count < selectedColumn.Sites.Count);

                    site.IsSelected = true;
                }
            }
        }

        private void ChangeSelectedSiteState()
        {
            Base3DScene scene = Module3DMain.SelectedScene;
            if (scene)
            {
                Column3D column = scene.SelectedColumn;
                if (column)
                {
                    Site selectedSite = column.SelectedSite;
                    if (selectedSite)
                    {
                        List<Core.Object3D.Site> sites = new();
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
                        Key downAction = ChangeSiteStateActions.FirstOrDefault(DesktopInput.WasPressedThisFrame);
                        switch (downAction)
                        {
                            case Key.H:
                                {
                                    bool allHighlighted = sites.All(s => s.State.IsHighlighted);
                                    foreach (var site in sites) site.State.IsHighlighted = !allHighlighted;
                                }
                                break;
                            case Key.B:
                                {
                                    bool allBlacklisted = sites.All(s => s.State.IsBlackListed);
                                    foreach (var site in sites) site.State.IsBlackListed = !allBlacklisted;
                                }
                                break;
                            default:
                                {
                                    int index = m_ChangeColorActions.IndexOf(downAction);
                                    if (index == -1) break;

                                    Color color = ColorPickerManager.GetDefaultColor(index);
                                    foreach (var site in sites) site.State.Color = color;
                                }
                                break;
                        }
                    }
                }
            }
        }

        private void PerformActionOnCut()
        {
            CutController selectedCutController = Module3DUI.Scenes.FirstOrDefault(s => s.Key.IsSelected).Value?.CutController;
            if (selectedCutController != null)
            {
                if (DesktopInput.WasPressedThisFrame(Key.A))
                {
                    Module3DMain.SelectedScene.AddCutPlane();
                    selectedCutController.OpenNextController();
                }
                else if (DesktopInput.WasPressedThisFrame(Key.Tab))
                {
                    selectedCutController.OpenNextController();
                }

                Cut selectedCut = selectedCutController.SelectedCut;
                if (selectedCut != null)
                {
                    if (DesktopInput.IsPressed(Key.LeftArrow) || DesktopInput.IsPressed(Key.DownArrow))
                    {
                        selectedCut.Position -= 1.0f / selectedCut.NumberOfCuts;
                    }
                    else if (DesktopInput.IsPressed(Key.RightArrow) || DesktopInput.IsPressed(Key.UpArrow))
                    {
                        selectedCut.Position += 1.0f / selectedCut.NumberOfCuts;
                    }
                    else if (DesktopInput.WasPressedThisFrame(Key.F))
                    {
                        selectedCut.Flip = !selectedCut.Flip;
                    }
                    else if (DesktopInput.WasPressedThisFrame(Key.C))
                    {
                        selectedCut.Orientation = (CutOrientation)(((int)selectedCut.Orientation + 1) % 3);
                    }

                    Module3DMain.SelectedScene.UpdateCutPlane(selectedCut, true);
                }
            }
        }

        #endregion
    }
}
