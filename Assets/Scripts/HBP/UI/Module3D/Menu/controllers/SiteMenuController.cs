
/**
 * \file    SiteMenuController.cs
 * \author  Lance Florian
 * \date    17/05/2016
 * \brief   Define SiteMenuController, SiteMenu classes
 */


// system
using System.Collections.Generic;

// unity
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using HBP.Module3D;

namespace HBP.UI.Module3D
{
    namespace Events
    {
        /// <summary>
        /// Send the new last selected site (params : site)
        /// </summary>
        public class UpdateLastSelectedSite : UnityEvent<Site> { }

        /// <summary>
        /// 
        /// </summary>
        public class SiteInfoRequest : UnityEvent<int> { }

        /// <summary>
        /// Send a signal for closing the window
        /// </summary>
        public class CloseSiteWindow : UnityEvent { }
    }

    /// <summary>
    /// Site UI menu
    /// </summary>
    public class SiteMenu : MonoBehaviour
    {
        #region Properties

        // scenes
        private Base3DScene m_scene = null; /**< SP scene */

        // parameters        
        private bool m_isSelectedSiteMinimized = false;
        private bool m_isMasksMinimized = true;
        private bool m_isCCEPMinimized = true;

        public int m_columnId = -1;
        private int m_idPatientToLoad; /**< patient id of the last selected site */
        private Site m_lastSiteSelected = null; /**< last selected site */
        private List<string> labelCCEP = null;
        private List<GameObject> colLatencyButtons = new List<GameObject>();

        // events
        public Events.UpdateLastSelectedSite UpdateLastSelectedSite = new Events.UpdateLastSelectedSite();
        public Events.SiteInfoRequest SiteInfoRequest = new Events.SiteInfoRequest();
        public Events.CloseSiteWindow CloseSiteWindow = new Events.CloseSiteWindow();
        
        #endregion

        #region Public Methods

        /// <summary>
        /// Init the menu
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="idColumn"></param>
        public void init(Base3DScene scene, int idColumn)
        {
            m_scene = scene;
            m_columnId = idColumn;

            // define name
            string name = "Site left menu ";
            switch(scene.Type)
            {
                case SceneType.SinglePatient:
                    name += "SP";
                    break;
                case SceneType.MultiPatients:
                    name += "MP";
                    break;
            }
            name += "" + idColumn;
            gameObject.name = name;

            // reset size and set position in the hierachy
            gameObject.transform.localScale = new Vector3(1, 1, 1);
            gameObject.transform.SetAsFirstSibling();

            // create listeners
            set_listeners();
        }

        /// <summary>
        /// Define the listeners of the menu
        /// </summary>
        void set_listeners()
        {
            Transform contentPanelT = transform.Find("panel");

            // close panel
            transform.Find("title panel").Find("close button").GetComponent<Button>().onClick.AddListener(
            delegate
            {
                CloseSiteWindow.Invoke();
            });
            contentPanelT.Find("selected site buttons").Find("unselect button").GetComponent<Button>().onClick.AddListener(() =>
            {
                m_scene.UnselectSite(m_columnId);

                update_menu();
            });

            // update latencies from scene
            switch(m_scene.Type)
            {
                case SceneType.SinglePatient:
                    SinglePatient3DScene SP3DScene = (SinglePatient3DScene)(m_scene);
                    SP3DScene.OnUpdateLatencies.AddListener((labels) =>
                    {
                        labelCCEP = labels;
                        update_CCEP();
                    });
                    break;
                case SceneType.MultiPatients:
                    break;
            }

            // click site  
            m_scene.OnClickSite.AddListener((idColumn) =>
            {
                if (idColumn == -1)
                    idColumn = m_scene.ColumnManager.SelectedColumnID;

                if (idColumn != m_columnId)
                    return;

                if (m_scene.ColumnManager.Columns[idColumn].SelectedSiteID == -1)
                    return;

                SiteInfoRequest.Invoke(m_columnId);
                update_menu();
            });

            // toggle update info
            contentPanelT.Find("selected site buttons").Find("update infos toggle").GetComponent<Toggle>().onValueChanged.AddListener((value) =>
            {
                ((Column3DIEEG)(m_scene.ColumnManager.SelectedColumn)).SendInformation = value;
            });

            // compare site
            contentPanelT.Find("selected site buttons").Find("compare site button").GetComponent<Button>().onClick.AddListener(() =>
            {
                m_scene.CompareSites();
            });

            // apply
            contentPanelT.Find("Apply on parent").Find("apply button").GetComponent<Button>().onClick.AddListener(() =>
            {
                bool exclude = contentPanelT.Find("action parent").Find("exclude toggle").GetComponent<Toggle>().isOn;
                bool include = contentPanelT.Find("action parent").Find("include toggle").GetComponent<Toggle>().isOn;
                bool highlight = contentPanelT.Find("action parent").Find("highlight toggle").GetComponent<Toggle>().isOn;
                bool unhighlight = contentPanelT.Find("action parent").Find("unhighlight toggle").GetComponent<Toggle>().isOn;
                bool marked = contentPanelT.Find("action parent").Find("mark toggle").GetComponent<Toggle>().isOn;

                bool selectedSite = contentPanelT.Find("Apply on parent").Find("selected site toggle").GetComponent<Toggle>().isOn;
                bool electrode = contentPanelT.Find("Apply on parent").Find("electrode toggle").GetComponent<Toggle>().isOn;
                bool patient = contentPanelT.Find("Apply on parent").Find("patient toggle").GetComponent<Toggle>().isOn;
                bool highlighted = contentPanelT.Find("Apply on parent").Find("highlighted toggle").GetComponent<Toggle>().isOn;
                bool unhighlighted = contentPanelT.Find("Apply on parent").Find("unhighlighted toggle").GetComponent<Toggle>().isOn;
                bool allSites = contentPanelT.Find("Apply on parent").Find("all sites toggle").GetComponent<Toggle>().isOn;
                bool inROI = contentPanelT.Find("Apply on parent").Find("in ROI toggle").GetComponent<Toggle>().isOn;
                bool noInROI = contentPanelT.Find("Apply on parent").Find("not in ROI toggle").GetComponent<Toggle>().isOn;
                bool nameSites = contentPanelT.Find("Apply on parent").Find("sites name toggle").GetComponent<Toggle>().isOn;
                bool marsNameSites = contentPanelT.Find("Apply on parent").Find("mars name toggle").GetComponent<Toggle>().isOn;
                string nameFilterStr = nameSites ? contentPanelT.Find("Apply on parent").Find("sites name inputField").GetComponent<InputField>().text :
                                            (marsNameSites ? contentPanelT.Find("Apply on parent").Find("mars name inputField").GetComponent<InputField>().text :
                                            contentPanelT.Find("Apply on parent").Find("broadman name inputField").GetComponent<InputField>().text);

                bool allColumns = contentPanelT.Find("Apply on parent").Find("all columns checkbox").GetComponent<Toggle>().isOn;
                SiteAction action = exclude ? SiteAction.Exclude : (include ? SiteAction.Include : (highlight ? SiteAction.Highlight : (unhighlight ? SiteAction.Unhighlight : (marked ? SiteAction.Mark : SiteAction.Unmark))));
                SiteFilter filter = selectedSite ? SiteFilter.Specific : (electrode ? SiteFilter.Electrode : (patient ? SiteFilter.Patient : (highlighted ? SiteFilter.Highlighted : (unhighlighted ? SiteFilter.Unhighlighted : (allSites ? SiteFilter.All : (inROI ? SiteFilter.InRegionOfInterest : (noInROI ? SiteFilter.OutOfRegionOfInterest : (nameSites ? SiteFilter.Name : (marsNameSites ? SiteFilter.MarsAtlas : SiteFilter.Broadman)))))))));
                m_scene.UpdateSitesMasks(allColumns, m_lastSiteSelected.gameObject, action, filter, nameFilterStr);
            });

            switch(m_scene.Type)
            {
                case SceneType.SinglePatient:
                    Button defineSourceButton = contentPanelT.Find("CCEP parent").Find("site source parent").Find("set site as source button").GetComponent<Button>();
                    Button undefineSourceButton = contentPanelT.Find("CCEP parent").Find("undefine source parent").Find("undefine source button").GetComponent<Button>();
                    Text latencyDataText = contentPanelT.Find("CCEP parent").Find("latency data text").GetComponent<Text>();

                    defineSourceButton.onClick.AddListener(
                        delegate
                        {
                            ((SinglePatient3DScene)m_scene).SetCurrentSiteAsSource();
                            latencyDataText.text = "Current site is the source.";
                            defineSourceButton.interactable = false;
                            undefineSourceButton.interactable = true;
                        });

                    undefineSourceButton.onClick.AddListener(
                        delegate
                        {
                            ((SinglePatient3DScene)m_scene).UndefineCurrentSource();

                            if (latencyDataText.text == "Current site is the source.")
                            {
                                latencyDataText.text = "Site is a source.";
                                defineSourceButton.interactable = true;
                                undefineSourceButton.interactable = false;
                            }
                            else
                            {
                                latencyDataText.text = "Site is not a source.";
                                defineSourceButton.interactable = false;
                                undefineSourceButton.interactable = false;
                            }
                        });
                    break;
                case SceneType.MultiPatients:
                    contentPanelT.Find("MP only options parent").Find("load sp button").GetComponent<Button>().onClick.AddListener(() =>
                    {
                        //((MultiPatients3DScene)m_scene).LoadPatientInSinglePatientScene(m_idPatientToLoad, m_lastSiteSelected.GetComponent<Site>().Information.SitePatientID);
                    });

                    Button blackList = contentPanelT.Find("MP only options parent").Find("blacklist button").GetComponent<Button>();
                    blackList.onClick.AddListener(() =>
                    {
                        if (blackList.transform.Find("Text").GetComponent<Text>().text == "Blacklist site")
                        {
                            m_lastSiteSelected.Information.IsBlackListed = true;
                            m_scene.UpdateSitesMasks(true, m_lastSiteSelected.gameObject, SiteAction.Blacklist, 0);
                            blackList.transform.Find("Text").GetComponent<Text>().text = "Unblacklist site";
                        }
                        else
                        {
                            m_lastSiteSelected.Information.IsBlackListed = false;
                            m_scene.UpdateSitesMasks(true, m_lastSiteSelected.gameObject, SiteAction.Unblacklist, 0);
                            blackList.transform.Find("Text").GetComponent<Text>().text = "Blacklist site";
                        }
                    });
                    break;
            }

            // minimize/expand selected site
            Button selectedSiteButton = contentPanelT.Find("Site parent").Find("Site button").GetComponent<Button>();
            selectedSiteButton.onClick.AddListener(delegate
            {
                m_isSelectedSiteMinimized = !m_isSelectedSiteMinimized;
                contentPanelT.Find("Site parent").Find("expand image").gameObject.SetActive(m_isSelectedSiteMinimized);
                contentPanelT.Find("Site parent").Find("minimize image").gameObject.SetActive(!m_isSelectedSiteMinimized);
                contentPanelT.Find("selected site panel").gameObject.SetActive(!m_isSelectedSiteMinimized);
                contentPanelT.Find("selected site buttons").gameObject.SetActive(!m_isSelectedSiteMinimized);

                if(m_scene.Type == SceneType.MultiPatients)
                {
                    contentPanelT.Find("MP only options parent").gameObject.SetActive(!m_isSelectedSiteMinimized);
                }
            });

            // minimize/expand masks
            Button masksButton = contentPanelT.Find("Masks parent").Find("Masks button").GetComponent<Button>();
            masksButton.onClick.AddListener(delegate
            {
                m_isMasksMinimized = !m_isMasksMinimized;
                contentPanelT.Find("Masks parent").Find("expand image").gameObject.SetActive(m_isMasksMinimized);
                contentPanelT.Find("Masks parent").Find("minimize image").gameObject.SetActive(!m_isMasksMinimized);
                contentPanelT.Find("Action text").gameObject.SetActive(!m_isMasksMinimized);
                contentPanelT.Find("action parent").gameObject.SetActive(!m_isMasksMinimized);
                contentPanelT.Find("space 0").gameObject.SetActive(!m_isMasksMinimized);
                contentPanelT.Find("separation panel 2").gameObject.SetActive(!m_isMasksMinimized);
                contentPanelT.Find("Apply on text").gameObject.SetActive(!m_isMasksMinimized);
                contentPanelT.Find("Apply on parent").gameObject.SetActive(!m_isMasksMinimized);
            });
            
            if(m_scene.Type == SceneType.SinglePatient)
            {
                contentPanelT.Find("CCEP title parent").gameObject.SetActive(true);

                // minimize/expand CCEP
                Button CCEPButton = contentPanelT.Find("CCEP title parent").Find("CCEP button").GetComponent<Button>();
                CCEPButton.onClick.AddListener(delegate
                {
                    m_isCCEPMinimized = !m_isCCEPMinimized;
                    contentPanelT.Find("CCEP title parent").Find("expand image").gameObject.SetActive(m_isCCEPMinimized);
                    contentPanelT.Find("CCEP title parent").Find("minimize image").gameObject.SetActive(!m_isCCEPMinimized);
                    contentPanelT.Find("CCEP parent").gameObject.SetActive(!m_isCCEPMinimized);
                });
            }
        }

        private void update_menu()
        {
            m_lastSiteSelected = m_scene.ColumnManager.SelectedColumn.SelectedSite;
            bool siteNotNull = m_lastSiteSelected != null;

            // update selected patient
            bool patientComplete = true;
            if (m_scene.ColumnManager.SelectedPatientID != -1)
            {
                m_idPatientToLoad = m_scene.ColumnManager.SelectedPatientID;

                if(m_scene.Type == SceneType.MultiPatients)
                {
                    Data.Patient patient = (m_scene as MultiPatients3DScene).Patients[m_idPatientToLoad];
                    patientComplete = (patient.Brain.PreoperativeMRI.Length > 0) && (patient.Brain.RightCerebralHemisphereMesh.Length > 0) && (patient.Brain.LeftCerebralHemisphereMesh.Length > 0);
                }
            }

            UpdateLastSelectedSite.Invoke(m_lastSiteSelected);

            Transform panel = transform.Find("panel");
            // parents
            panel.Find("MP only options parent").gameObject.SetActive(m_scene.Type == SceneType.MultiPatients);
            panel.Find("CCEP parent").gameObject.SetActive(m_scene.Type == SceneType.SinglePatient && m_scene.ColumnManager.SelectedColumn.Type == Column3D.ColumnType.IEEG && !m_isCCEPMinimized);
            // text
            panel.Find("selected site panel").Find("Text").GetComponent<Text>().text = siteNotNull ? m_lastSiteSelected.Information.FullName : "...";
            panel.Find("selected site panel").Find("mars atlas text").GetComponent<Text>().text = siteNotNull ? (m_lastSiteSelected.Information.MarsAtlasIndex == -1 ? "Mars atlas: not found" : GlobalGOPreloaded.MarsAtlasIndex.FullName(m_lastSiteSelected.Information.MarsAtlasIndex)) : "...";

            // active buttons            
            panel.Find("MP only options parent").Find("blacklist button").GetComponent<Button>().interactable = m_scene.Type == SceneType.MultiPatients && siteNotNull;
            panel.Find("selected site buttons").Find("unselect button").GetComponent<Button>().interactable = siteNotNull;
            panel.Find("selected site buttons").Find("update infos toggle").GetComponent<Toggle>().interactable = siteNotNull;
            panel.Find("selected site buttons").Find("compare site button").GetComponent<Button>().interactable = siteNotNull;
            panel.Find("Apply on parent").Find("apply button").GetComponent<Button>().interactable = siteNotNull;
            panel.Find("Apply on parent").Find("in ROI toggle").GetComponent<Toggle>().interactable = m_scene.Type == SceneType.MultiPatients;
            panel.Find("Apply on parent").Find("not in ROI toggle").GetComponent<Toggle>().interactable = m_scene.Type == SceneType.MultiPatients;
            panel.Find("MP only options parent").Find("load sp button").GetComponent<Button>().interactable = m_scene.Type == SceneType.MultiPatients && siteNotNull && patientComplete;

            switch(m_scene.Type)
            {
                case SceneType.SinglePatient:
                    bool activeCCEP = m_scene.ColumnManager.LatencyFileAvailable && m_scene.SceneInformation.DisplayCCEPMode;
                    Button setSiteAsSourceButton = panel.Find("CCEP parent").Find("site source parent").Find("set site as source button").GetComponent<Button>();
                    Button undefineSourceButton = panel.Find("CCEP parent").Find("undefine source parent").Find("undefine source button").GetComponent<Button>();
                    Text dataLatencyText = panel.Find("CCEP parent").Find("latency data text").GetComponent<Text>();
                    setSiteAsSourceButton.interactable = activeCCEP && siteNotNull;
                    undefineSourceButton.interactable = activeCCEP && siteNotNull;

                    switch (m_scene.ColumnManager.SelectedColumn.Type)
                    {
                        case Column3D.ColumnType.FMRI:
                            return;
                        case Column3D.ColumnType.IEEG:
                            Column3DIEEG IEEGCol = (Column3DIEEG)m_scene.ColumnManager.SelectedColumn;

                            // color buttons
                            for (int ii = 0; ii < colLatencyButtons.Count; ++ii)
                            {
                                if (colLatencyButtons[ii] == null)
                                    break;

                                ColorBlock cb = colLatencyButtons[ii].GetComponent<Button>().colors;
                                if (ii == IEEGCol.CurrentLatencyFile)
                                {
                                    cb.normalColor = Color.green;
                                    cb.highlightedColor = Color.green;
                                }
                                else
                                {
                                    cb.normalColor = Color.white;
                                    cb.highlightedColor = Color.white;
                                }
                                colLatencyButtons[ii].GetComponent<Button>().colors = cb;
                            }

                            if (IEEGCol.SourceDefined)
                            {
                                if (IEEGCol.IsSiteASource)
                                {
                                    dataLatencyText.text = "Site is the current \ndefined source.";
                                    setSiteAsSourceButton.interactable = false;
                                    undefineSourceButton.interactable &= true;
                                }
                                else
                                {
                                    if (IEEGCol.SiteLatencyData)
                                    {
                                        dataLatencyText.text = "Latency data available\n for this site.";
                                        setSiteAsSourceButton.interactable &= true;
                                        undefineSourceButton.interactable &= true;
                                    }
                                }
                            }
                            else
                            {
                                if (IEEGCol.IsSiteASource)
                                {
                                    dataLatencyText.text = "Site is a source.";
                                    setSiteAsSourceButton.interactable &= true;
                                    undefineSourceButton.interactable = false;
                                }
                                else
                                {
                                    dataLatencyText.text = "Site is not a source.";
                                    setSiteAsSourceButton.interactable = false;
                                    undefineSourceButton.interactable = false;
                                }
                            }
                            break;
                        default:
                            break;
                    }
                    break;
                case SceneType.MultiPatients:
                    if (m_scene.ColumnManager.SelectedColumn.SelectedSite != null)
                        panel.Find("MP only options parent").Find("blacklist button").Find("Text").GetComponent<Text>().text
                            = m_scene.ColumnManager.SelectedColumn.SelectedSite.Information.IsBlackListed ? "Unblacklist site" : "Blacklist site";
                    break;
            }
        }

        /// <summary>
        /// Update CCEP UI
        /// </summary>
        private void update_CCEP()
        {
            if (colLatencyButtons != null)
                for (int ii = 0; ii < colLatencyButtons.Count; ++ii)
                    Destroy(colLatencyButtons[ii]);

            colLatencyButtons = new List<GameObject>(labelCCEP.Count);

            Transform panel = transform.Find("panel");
            Transform parentButtons = panel.Find("CCEP parent").Find("latency files scrollable list panel").Find("scrollable list").Find("grid elements");
            GameObject defaultButton = parentButtons.Find("default button").gameObject;
            for (int jj = 0; jj < labelCCEP.Count; ++jj)
            {
                GameObject latencyButton = Instantiate(defaultButton);
                latencyButton.transform.SetParent(parentButtons);

                latencyButton.transform.localScale = new Vector3(1, 1, 1);
                Vector3 pos = latencyButton.transform.localPosition;
                pos.z = 0;
                latencyButton.transform.localPosition = pos;

                latencyButton.name = labelCCEP[jj] + " button";
                latencyButton.GetComponent<LatencyButton>().init(jj);
                latencyButton.GetComponent<LatencyButton>().ChooseLatencyFile.AddListener((id) =>
                {
                    ((SinglePatient3DScene)m_scene).UpdateCurrentLatencyFile(id);
                    update_menu();
                });
                latencyButton.SetActive(true);
                latencyButton.transform.Find("Text").GetComponent<Text>().text = labelCCEP[jj];

                if (jj == 0)
                {
                    ColorBlock cb = latencyButton.GetComponent<Button>().colors;
                    cb.normalColor = Color.green;
                    cb.highlightedColor = Color.green;
                    latencyButton.GetComponent<Button>().colors = cb;
                }

                colLatencyButtons.Add(latencyButton);
            }
        }


        #endregion
    }

    /// <summary>
    /// A class for managing the site settings menues for each scene and column
    /// </summary>
    public class SiteMenuController : MonoBehaviour, UICameraOverlay
    {
        #region Properties

        // scenes
        private Base3DScene m_scene = null; /**< scene */

        // canvas
        public Transform m_middlePanelT = null; /**< middle scene panel transform */
        List<GameObject> m_siteMenuList = new List<GameObject>(); /**< iEEG menu list */

        // parameters
        private bool m_displayMenu = false; /**< menu displayed ? */
        private int m_ID = 0; /**< ID of the column menu to display */

        // last update
        private int m_lastUpdatedColumn = -1;
        private Site m_lastSiteSelected = null; /**< last selected site */

        #endregion

        #region Public Methods

        /// <summary>
        /// Init the controller
        /// </summary>
        /// <param name="scenesManager"></param>
        public void init(Base3DScene scene)
        {
            m_scene = scene;

            // default initialization with one menu
            m_siteMenuList.Add(generate_menu(0));
        }

        /// <summary>
        /// Generate a new Site menu
        /// </summary>
        /// <param name="columndId"></param>
        /// <returns></returns>
        private GameObject generate_menu(int columndId)
        {
            GameObject siteLeftMenuGO = Instantiate(GlobalGOPreloaded.SiteLeftMenu);
            siteLeftMenuGO.AddComponent<SiteMenu>();
            siteLeftMenuGO.transform.SetParent(m_middlePanelT);

            SiteMenu menu = siteLeftMenuGO.GetComponent<SiteMenu>();
            menu.init(m_scene, columndId);

            // set listeners 
            menu.CloseSiteWindow.AddListener(() =>
            {
                switch_UI_visibility();
            });

            menu.UpdateLastSelectedSite.AddListener((site) =>
            {
                m_lastSiteSelected = site;
                m_lastUpdatedColumn = columndId;
            });

            menu.SiteInfoRequest.AddListener((idColumn) =>
            {
                if (m_scene.ColumnManager.SelectedColumn.SelectedSite != null)
                {
                    if (m_scene.SceneInformation.IsComparingSites)
                    {
                        m_scene.SceneInformation.IsComparingSites = false;
                        m_scene.DisplayScreenMessage("Compare : " + m_lastSiteSelected.name + " from col " + m_lastUpdatedColumn + "\n with " + m_scene.ColumnManager.SelectedColumn.SelectedSite.name + " from col " + idColumn, 5f, 250, 80);
                        m_scene.SendAdditionalSiteInfoRequest(m_lastSiteSelected);
                    }
                    else
                    {
                        Column3D col = m_scene.ColumnManager.SelectedColumn;
                        switch (col.Type)
                        {
                            case Column3D.ColumnType.FMRI:
                                break;
                            case Column3D.ColumnType.IEEG:
                                if (((Column3DIEEG)col).SendInformation)
                                    m_scene.SendAdditionalSiteInfoRequest();
                                break;
                            default:
                                break;
                        }
                    }
                }

                define_current_column(columndId);
                set_UI_visibility(true);
            });

            return siteLeftMenuGO;
        }

        /// <summary>
        /// Update the UI visibility with the current mode
        /// </summary>
        /// <param name="mode"></param>
        public void UpdateByMode(Mode mode)
        {
            bool menuDisplayed = m_displayMenu;

            // define mode ui specifities
            switch (mode.ID)
            {
                case Mode.ModesId.NoPathDefined:
                    menuDisplayed = false;
                    break;
                case Mode.ModesId.MinPathDefined:
                    menuDisplayed = false;
                    break;
                case Mode.ModesId.TriErasing:
                    menuDisplayed = false;
                    break;
                case Mode.ModesId.Error:
                    menuDisplayed = false;
                    break;
            }

            // set the state of the menu
            set_UI_visibility(menuDisplayed);
        }

        /// <summary>
        /// Swith the UI visibility of the UI
        /// </summary>
        public void switch_UI_visibility()
        {
            m_displayMenu = !m_displayMenu;
            update_UI();
        }

        /// <summary>
        /// Set the visibility of the UI
        /// </summary>
        /// <param name="visible"></param>
        private void set_UI_visibility(bool visible)
        {
            m_displayMenu = visible;
            update_UI();
        }

        /// <summary>
        /// Define the current column selected
        /// </summary>
        /// <param name="columnId"></param>
        public void define_current_column(int columnId = -1)
        {           
            if (columnId != -1)
                m_ID = columnId;

            update_UI();
        }

        /// <summary>
        /// Update the menu UI activity with the current parameters
        /// </summary>
        private void update_UI()
        {
            // hide all the menus
            for (int ii = 0; ii < m_siteMenuList.Count; ++ii)
                m_siteMenuList[ii].SetActive(false);

            if (m_siteMenuList.Count == 0 || m_ID == -1)
                return;
            
            if (m_ID >= m_siteMenuList.Count)
                m_ID = m_siteMenuList.Count - 1;

            // display the menu corresponding to the current scene and
            m_siteMenuList[m_ID].SetActive(m_displayMenu);
        }


        /// <summary>
        /// Adapt the number of menues
        /// </summary>
        /// <param name="iEEGColumnsNb"></param>
        public void define_columns_nb(int iEEGColumnsNb)
        {
            int diff = m_siteMenuList.Count - iEEGColumnsNb;
            if (diff < 0) // add menus
                for (int ii = 0; ii < -diff; ++ii)
                    m_siteMenuList.Add(generate_menu(m_siteMenuList.Count));
            else if (diff > 0) // remove menus
            {
                for (int ii = 0; ii < diff; ++ii)
                {
                    int id = m_siteMenuList.Count - 1;
                    Destroy(m_siteMenuList[id]);
                    m_siteMenuList.RemoveAt(id);
                }
            }

            define_current_column(0);
        }


        /// <summary>
        /// Add a new column menu
        /// </summary>
        public void add_menu()
        {
            m_siteMenuList.Add(generate_menu(m_siteMenuList.Count));
            update_UI();
        }

        /// <summary>
        /// Remove the last column menu
        /// </summary>
        public void remove_last_menu()
        {
            int id = m_siteMenuList.Count - 1;
            Destroy(m_siteMenuList[id]);
            m_siteMenuList.RemoveAt(id);

            if (m_ID > m_siteMenuList.Count - 1)
                m_ID = -1;
            update_UI();
        }

        #endregion
    }
}