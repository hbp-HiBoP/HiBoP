
/**
 * \file    PlotMenuController.cs
 * \author  Lance Florian
 * \date    17/05/2016
 * \brief   Define PlotMenuController, PlotMenu classes
 */


// system
using System.Collections.Generic;

// unity
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

namespace HBP.VISU3D
{
    namespace Events
    {
        /// <summary>
        /// Send the new last selected plot (params : plot)
        /// </summary>
        public class UpdateLastSelectedPlot : UnityEvent<Plot> { }

        /// <summary>
        /// 
        /// </summary>
        public class PlotInfoRequest : UnityEvent<int> { }

        /// <summary>
        /// Send a signal for closing the window
        /// </summary>
        public class ClosePlotWindow : UnityEvent { }
    }

    /// <summary>
    /// Plot UI menu
    /// </summary>
    public class PlotMenu : MonoBehaviour
    {
        #region members

        // scenes
        private Base3DScene m_scene = null; /**< SP scene */

        // parameters        
        private bool m_isSelectedPlotMinimized = false;
        private bool m_isMasksMinimized = true;
        private bool m_isCCEPMinimized = true;

        public int m_columnId = -1;
        private int m_idPatientToLoad; /**< patient id of the last selected plot */
        private Plot m_lastPlotSelected = null; /**< last selected plot */
        private List<string> labelCCEP = null;
        private List<GameObject> colLatencyButtons = new List<GameObject>();

        // events
        public Events.UpdateLastSelectedPlot UpdateLastSelectedPlot = new Events.UpdateLastSelectedPlot();
        public Events.PlotInfoRequest PlotInfoRequest = new Events.PlotInfoRequest();
        public Events.ClosePlotWindow ClosePlotWindow = new Events.ClosePlotWindow();
        
        #endregion members

        #region functions

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
            string name = "Plot left menu ";
            if (scene.singlePatient)
                name += "SP";
            else
                name += "MP";
            name += "" + idColumn;
            gameObject.name = name;

            // reset size and set position in the hierachy
            gameObject.transform.localScale = new Vector3(1, 1, 1);
            gameObject.transform.SetAsFirstSibling();

            // create listeners
            setListeners();
        }

        /// <summary>
        /// Define the listeners of the menu
        /// </summary>
        void setListeners()
        {
            Transform contentPanelT = transform.Find("panel");

            // close panel
            transform.Find("title panel").Find("close button").GetComponent<Button>().onClick.AddListener(
            delegate
            {
                ClosePlotWindow.Invoke();
            });
            contentPanelT.Find("selected plot buttons").Find("unselect button").GetComponent<Button>().onClick.AddListener(() =>
            {
                m_scene.unselectPlot(m_columnId);
                //ClosePlotWindow.Invoke();
                updateMenu();
            });

            // update latencies from scene
            if (m_scene.singlePatient)
            {
                SP3DScene SP3DScene = (SP3DScene)(m_scene);
                SP3DScene.UpdateLatencies.AddListener((labels) =>
                {
                    labelCCEP = labels;
                    updateCCEP();
                });
            }

            // click plot  
            m_scene.ClickPlot.AddListener((idColumn) =>
            {
                if (idColumn == -1)
                    idColumn = m_scene.CM.idSelectedColumn;

                if (idColumn != m_columnId)
                    return;

                if (m_scene.CM.col(idColumn).idSelectedPlot == -1)
                    return;

                PlotInfoRequest.Invoke(m_columnId);
                updateMenu();
            });

            // toggle update info
            contentPanelT.Find("selected plot buttons").Find("update infos toggle").GetComponent<Toggle>().onValueChanged.AddListener((value) =>
            {
                ((Column3DViewIEEG)(m_scene.CM.currentColumn())).sendInfos = value;
            });

            // compare plot
            contentPanelT.Find("selected plot buttons").Find("compare plot button").GetComponent<Button>().onClick.AddListener(() =>
            {
                m_scene.comparePlot();
            });

            // apply
            contentPanelT.Find("Apply on parent").Find("apply button").GetComponent<Button>().onClick.AddListener(() =>
            {
                bool exclude = contentPanelT.Find("action parent").Find("exclude toggle").GetComponent<Toggle>().isOn;
                bool include = contentPanelT.Find("action parent").Find("include toggle").GetComponent<Toggle>().isOn;
                bool highlight = contentPanelT.Find("action parent").Find("highlight toggle").GetComponent<Toggle>().isOn;

                bool selectedPlot = contentPanelT.Find("Apply on parent").Find("selected plot toggle").GetComponent<Toggle>().isOn;
                bool electrode = contentPanelT.Find("Apply on parent").Find("electrode toggle").GetComponent<Toggle>().isOn;
                bool patient = contentPanelT.Find("Apply on parent").Find("patient toggle").GetComponent<Toggle>().isOn;
                bool highlighted = contentPanelT.Find("Apply on parent").Find("highlighted toggle").GetComponent<Toggle>().isOn;
                bool unhighlighted = contentPanelT.Find("Apply on parent").Find("unhighlighted toggle").GetComponent<Toggle>().isOn;
                bool allPlots = contentPanelT.Find("Apply on parent").Find("all plots toggle").GetComponent<Toggle>().isOn;
                bool inROI = contentPanelT.Find("Apply on parent").Find("in ROI toggle").GetComponent<Toggle>().isOn;
                bool noInROI = contentPanelT.Find("Apply on parent").Find("not in ROI toggle").GetComponent<Toggle>().isOn;
                //bool nameFilter = contentPanelT.Find("Apply on parent").Find("sites name toggle").GetComponent<Toggle>().isOn;
                string nameFilterStr = contentPanelT.Find("Apply on parent").Find("sites name inputField").GetComponent<InputField>().text;

                bool allColumns = contentPanelT.Find("Apply on parent").Find("all columns checkbox").GetComponent<Toggle>().isOn;

                int action = exclude ? 0 : (include ? 1 : (highlight ? 4 : 5));
                int range = selectedPlot ? 0 : (electrode ? 1 : (patient ? 2 : (highlighted ? 3 : (unhighlighted ? 4 : (allPlots ? 5 : (inROI ? 6 : (noInROI ? 7 : 8)))))));
                m_scene.setPlotMask(allColumns, m_lastPlotSelected.gameObject, action, range, nameFilterStr);                
            });

            if (m_scene.singlePatient)
            {
                Button defineSourceButton = contentPanelT.Find("CCEP parent").Find("plot source parent").Find("set plot as source button").GetComponent<Button>();
                Button undefineSourceButton = contentPanelT.Find("CCEP parent").Find("undefine source parent").Find("undefine source button").GetComponent<Button>();
                Text latencyDataText = contentPanelT.Find("CCEP parent").Find("latency data text").GetComponent<Text>();

                defineSourceButton.onClick.AddListener(
                    delegate
                    {
                        ((SP3DScene)m_scene).setCurrentPlotAsSource();
                        latencyDataText.text = "Current plot is the source.";
                        defineSourceButton.interactable = false;
                        undefineSourceButton.interactable = true;
                    });

                undefineSourceButton.onClick.AddListener(
                    delegate
                    {
                        ((SP3DScene)m_scene).undefineCurrentSource();

                        if (latencyDataText.text == "Current plot is the source.")
                        {
                            latencyDataText.text = "Plot is a source.";
                            defineSourceButton.interactable = true;
                            undefineSourceButton.interactable = false;
                        }
                        else
                        {
                            latencyDataText.text = "Plot is not a source.";
                            defineSourceButton.interactable = false;
                            undefineSourceButton.interactable = false;
                        }
                    });
            }
            else
            {
                contentPanelT.Find("MP only options parent").Find("load sp button").GetComponent<Button>().onClick.AddListener(() =>
                {
                    ((MP3DScene)m_scene).loadPatientInSPScene(m_idPatientToLoad, m_lastPlotSelected.GetComponent<Plot>().idPlotPatient);
                });

                Button blackList = contentPanelT.Find("MP only options parent").Find("blacklist button").GetComponent<Button>();
                blackList.onClick.AddListener(() =>
                {
                    if (blackList.transform.Find("Text").GetComponent<Text>().text == "Blacklist plot")
                    {
                        m_lastPlotSelected.blackList = true;
                        m_scene.setPlotMask(true, m_lastPlotSelected.gameObject, 2, 0);
                        blackList.transform.Find("Text").GetComponent<Text>().text = "Unblacklist plot";
                    }
                    else
                    {
                        m_lastPlotSelected.blackList = false;
                        m_scene.setPlotMask(true, m_lastPlotSelected.gameObject, 3, 0);
                        blackList.transform.Find("Text").GetComponent<Text>().text = "Blacklist plot";
                    }
                });
            }


            // minimize/expand selected plot
            Button selectedPlotButton = contentPanelT.Find("Plot parent").Find("Plot button").GetComponent<Button>();
            selectedPlotButton.onClick.AddListener(delegate
            {
                m_isSelectedPlotMinimized = !m_isSelectedPlotMinimized;
                contentPanelT.Find("Plot parent").Find("expand image").gameObject.SetActive(m_isSelectedPlotMinimized);
                contentPanelT.Find("Plot parent").Find("minimize image").gameObject.SetActive(!m_isSelectedPlotMinimized);
                contentPanelT.Find("selected plot panel").gameObject.SetActive(!m_isSelectedPlotMinimized);
                contentPanelT.Find("selected plot buttons").gameObject.SetActive(!m_isSelectedPlotMinimized);

                if(!m_scene.singlePatient)
                    contentPanelT.Find("MP only options parent").gameObject.SetActive(!m_isSelectedPlotMinimized);
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
            
            if (m_scene.singlePatient)
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

        private void updateMenu()
        {
            m_lastPlotSelected = m_scene.CM.currPlotOfCurrCol();
            bool plotNotNull = m_lastPlotSelected != null;

            // update selected patient
            bool patientComplete = true;
            if (m_scene.CM.idSelectedPatient != -1)
            {
                m_idPatientToLoad = m_scene.CM.idSelectedPatient;

                if (!m_scene.singlePatient)
                {
                    Data.Patient.Patient patient = m_scene.CM.mpPatients[m_idPatientToLoad];
                    patientComplete = (patient.Brain.PreIRM.Length > 0) && (patient.Brain.RightMesh.Length > 0) && (patient.Brain.LeftMesh.Length > 0);
                }
            }

            UpdateLastSelectedPlot.Invoke(m_lastPlotSelected);

            Transform panel = transform.Find("panel");
            // parents
            panel.Find("MP only options parent").gameObject.SetActive(!m_scene.singlePatient);
            panel.Find("CCEP parent").gameObject.SetActive(m_scene.singlePatient && !m_scene.CM.isIRMFCurrentColumn() && !m_isCCEPMinimized);
            // text
            panel.Find("selected plot panel").Find("Text").GetComponent<Text>().text = plotNotNull ? m_lastPlotSelected.fullName : "...";
            // active buttons            
            panel.Find("MP only options parent").Find("blacklist button").GetComponent<Button>().interactable = !m_scene.singlePatient && plotNotNull;
            panel.Find("selected plot buttons").Find("unselect button").GetComponent<Button>().interactable = plotNotNull;
            panel.Find("selected plot buttons").Find("update infos toggle").GetComponent<Toggle>().interactable = plotNotNull;
            panel.Find("selected plot buttons").Find("compare plot button").GetComponent<Button>().interactable = plotNotNull;
            panel.Find("Apply on parent").Find("apply button").GetComponent<Button>().interactable = plotNotNull;
            panel.Find("Apply on parent").Find("in ROI toggle").GetComponent<Toggle>().interactable = !m_scene.singlePatient;
            panel.Find("Apply on parent").Find("not in ROI toggle").GetComponent<Toggle>().interactable = !m_scene.singlePatient;
            panel.Find("MP only options parent").Find("load sp button").GetComponent<Button>().interactable = !m_scene.singlePatient && plotNotNull && patientComplete;

            if (m_scene.singlePatient)
            {
                bool activeCCEP = m_scene.CM.latencyFileAvailable && m_scene.data_.displayLatenciesMode;
                Button setPlotAsSourceButton = panel.Find("CCEP parent").Find("plot source parent").Find("set plot as source button").GetComponent<Button>();
                Button undefineSourceButton = panel.Find("CCEP parent").Find("undefine source parent").Find("undefine source button").GetComponent<Button>();
                Text dataLatencyText = panel.Find("CCEP parent").Find("latency data text").GetComponent<Text>();
                setPlotAsSourceButton.interactable = activeCCEP && plotNotNull;
                undefineSourceButton.interactable = activeCCEP && plotNotNull;

                if (m_scene.CM.isIRMFCurrentColumn())
                    return;

                Column3DViewIEEG iEGGCol = (Column3DViewIEEG)m_scene.CM.currentColumn();

                // color buttons
                for (int ii = 0; ii < colLatencyButtons.Count; ++ii)
                {
                    if (colLatencyButtons[ii] == null)
                        break;

                    ColorBlock cb = colLatencyButtons[ii].GetComponent<Button>().colors;
                    if (ii == iEGGCol.currentLatencyFile)
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

                if (iEGGCol.sourceDefined)
                {
                    if (iEGGCol.plotIsASource)
                    {
                        dataLatencyText.text = "Plot is the current \ndefined source.";
                        setPlotAsSourceButton.interactable = false;
                        undefineSourceButton.interactable &= true;
                    }
                    else
                    {
                        if (iEGGCol.plotLatencyData)
                        {
                            dataLatencyText.text = "Latency data available\n for this plot.";
                            setPlotAsSourceButton.interactable &= true;
                            undefineSourceButton.interactable &= true;
                        }
                    }
                }
                else
                {
                    if (iEGGCol.plotIsASource)
                    {
                        dataLatencyText.text = "Plot is a source.";
                        setPlotAsSourceButton.interactable &= true;
                        undefineSourceButton.interactable = false;
                    }
                    else
                    {
                        dataLatencyText.text = "Plot is not a source.";
                        setPlotAsSourceButton.interactable = false;
                        undefineSourceButton.interactable = false;
                    }
                }
            }
            else
            {
                if (m_scene.CM.currPlotOfCurrCol() != null)
                    panel.Find("MP only options parent").Find("blacklist button").Find("Text").GetComponent<Text>().text
                        = m_scene.CM.currPlotOfCurrCol().blackList ? "Unblacklist plot" : "Blacklist plot";

                // TODO : dectect if single patient can be loaded
            }
        }

        /// <summary>
        /// Update CCEP UI
        /// </summary>
        private void updateCCEP()
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
                    ((SP3DScene)m_scene).updateCurrentLatencyFile(id);
                    updateMenu();
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


        #endregion functions
    }

    /// <summary>
    /// A class for managing the plot settings menues for each scene and column
    /// </summary>
    public class PlotMenuController : MonoBehaviour, UICameraOverlay
    {
        #region members

        // scenes
        private Base3DScene m_spScene = null; /**< SP scene */
        private Base3DScene m_mpScene = null; /**< MP scene */

        // canvas
        public Transform m_middlePanelT = null; /**< middle scene panel transform */
        List<GameObject> m_spPlotMenuList = null; /**< SP iEEG menu list */
        List<GameObject> m_mpPlotMenuList = null; /**< MP iEEG menu list */

        // parameters
        private bool m_displayMenu = false; /**< menu displayed ? */
        private bool m_spSettings = true; /**< is menu displayed from the sp scene ? */
        private int m_spID = 0; /**< ID of the sp column menu to display */
        private int m_mpID = 0; /**< ID of the mp column menu to display */

        // last update
        private int m_lastUpdatedColumn = -1;
        private Plot m_lastPlotSelected = null; /**< last selected plot */

        #endregion members

        #region functions

        /// <summary>
        /// Init the controller
        /// </summary>
        /// <param name="scenesManager"></param>
        public void init(ScenesManager scenesManager)
        {
            m_spScene = scenesManager.SPScene;
            m_mpScene = scenesManager.MPScene;

            // init lists of UI
            m_spPlotMenuList = new List<GameObject>();
            m_mpPlotMenuList = new List<GameObject>();

            // default initialization with one menu per scene
            m_spPlotMenuList.Add(generateMenu(m_spScene, 0));
            m_mpPlotMenuList.Add(generateMenu(m_mpScene, 0));
        }

        /// <summary>
        /// Generate a new Plot menu
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="columndId"></param>
        /// <returns></returns>
        private GameObject generateMenu(Base3DScene scene, int columndId)
        {
            GameObject plotMenuGO = Instantiate(BaseGameObjects.PlotLeftMenu);
            plotMenuGO.AddComponent<PlotMenu>();
            plotMenuGO.transform.SetParent(m_middlePanelT.Find("Plot left menu list"));

            PlotMenu menu = plotMenuGO.GetComponent<PlotMenu>();
            menu.init(scene, columndId);

            // set listeners 
            menu.ClosePlotWindow.AddListener(() =>
            {
                switchUIVisibility();
            });

            menu.UpdateLastSelectedPlot.AddListener((plot) =>
            {
                m_lastPlotSelected = plot;
                m_lastUpdatedColumn = columndId;
            });

            menu.PlotInfoRequest.AddListener((idColumn) =>
            {
                if (scene.CM.currPlotOfCurrCol() != null)
                {
                    if (scene.data_.comparePlot)
                    {
                        scene.data_.comparePlot = false;
                        scene.displayScreenMessage("Compare : " + m_lastPlotSelected.name + " from col " + m_lastUpdatedColumn + "\n with " + scene.CM.currPlotOfCurrCol().name + " from col " + idColumn, 5f, 250, 80);
                        scene.sendAdditionnalPlotInfoRequest(m_lastPlotSelected);
                    }
                    else
                    {
                        Column3DView col = scene.CM.currentColumn();
                        if (!col.isIRMF)
                        {
                            if (((Column3DViewIEEG)col).sendInfos)
                                scene.sendAdditionnalPlotInfoRequest();
                        }
                    }
                }

                defineCurrentMenu(scene.singlePatient, columndId);
                setUIVisibility(true);
            });

            return plotMenuGO;
        }

        /// <summary>
        /// Update the UI visibility with the current mode
        /// </summary>
        /// <param name="mode"></param>
        public void setUIActivity(Mode mode)
        {
            bool menuDisplayed = m_displayMenu;

            // define mode ui specifities
            switch (mode.m_idMode)
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
            setUIVisibility(menuDisplayed);
        }

        /// <summary>
        /// Swith the UI visibility of the UI
        /// </summary>
        public void switchUIVisibility()
        {
            m_displayMenu = !m_displayMenu;
            updateUI();
        }

        /// <summary>
        /// Set the visibility of the UI
        /// </summary>
        /// <param name="visible"></param>
        private void setUIVisibility(bool visible)
        {
            m_displayMenu = visible;
            updateUI();
        }

        /// <summary>
        /// Define the current scene and column selected
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="columnId"></param>
        public void defineCurrentMenu(bool spScene, int columnId = -1)
        {
            m_spSettings = spScene;

            if (columnId != -1)
            {
                if (m_spSettings)
                {
                    m_spID = columnId;
                }
                else
                {
                    m_mpID = columnId;
                }
            }

            updateUI();
        }

        /// <summary>
        /// Update the menu UI activity with the current parameters
        /// </summary>
        private void updateUI()
        {
            // hide all the menus
            for (int ii = 0; ii < m_spPlotMenuList.Count; ++ii)
                m_spPlotMenuList[ii].SetActive(false);

            for (int ii = 0; ii < m_mpPlotMenuList.Count; ++ii)
                m_mpPlotMenuList[ii].SetActive(false);

            // display the menu corresponding to the current scene and column
            if (m_displayMenu)
            {
                if (m_spSettings)
                    m_spPlotMenuList[m_spID].SetActive(true);
                else
                    m_mpPlotMenuList[m_mpID].SetActive(true);
            }
        }


        /// <summary>
        /// Adapt the number of menues
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="iEEGColumnsNb"></param>
        public void defineColumnsNb(bool spScene, int iEEGColumnsNb)
        {
            List<GameObject> menuList = spScene ? m_spPlotMenuList : m_mpPlotMenuList;
            Base3DScene scene = spScene ? m_spScene : m_mpScene;

            int diff = menuList.Count - iEEGColumnsNb;
            if (diff < 0) // add menus
                for (int ii = 0; ii < -diff; ++ii)
                    menuList.Add(generateMenu(scene, menuList.Count));
            else if (diff > 0) // remove menus
            {
                for (int ii = 0; ii < diff; ++ii)
                {
                    int id = menuList.Count - 1;
                    Destroy(menuList[id]);
                    menuList.RemoveAt(id);
                }
            }

            // define the current scene menu
            defineCurrentMenu(spScene, 0);
        }


        /// <summary>
        /// Add a new column menu
        /// </summary>
        public void addMenu(bool spScene)
        {
            List<GameObject> menuList = spScene ? m_spPlotMenuList : m_mpPlotMenuList;
            Base3DScene scene = spScene ? m_spScene : m_mpScene;
            menuList.Add(generateMenu(scene, menuList.Count));
        }

        /// <summary>
        /// Remove the last column menu
        /// </summary>
        /// <param name="spScene"></param>
        public void removeLastMenu(bool spScene)
        {
            List<GameObject> menuList = spScene ? m_spPlotMenuList : m_mpPlotMenuList;
            int id = menuList.Count - 1;
            Destroy(menuList[id]);
            menuList.RemoveAt(id);

            if (spScene)
            {
                if (m_spID > menuList.Count - 1)
                    m_spID = -1;
            }
            else
            {
                if (m_mpID > menuList.Count - 1)
                    m_mpID = -1;
            }
        }

        #endregion functions
    }
}