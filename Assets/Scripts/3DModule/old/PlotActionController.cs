

///**
// * \file    PlotActionController.cs
// * \author  Lance Florian
// * \date    2015
// * \brief   Define PlotActionController class
// */

//// system
//using System;
//using System.Collections;
//using System.Globalization;
//using System.Collections.Generic;

//// unity
//using UnityEngine;
//using UnityEngine.UI;

//// hbp
//using HBP.VISU3D.Cam;

//namespace HBP.VISU3D
//{
//    /// <summary>
//    /// A class for managing the dialog box when clicking on a plot
//    /// </summary>
//    public class PlotActionController : IndividualSceneOverlayController
//    {

//        #region members

//        private int m_idPatientToLoad = 0; /**< id patient to load in the SP scene */

//        public int widthOffset = 5;
//        public int heightOffset = 5;

//        // scenes overlay
//        private UIOverlay plotActionSceneOverlay = new UIOverlay();

//        private GameObject m_plotActionElements; /**<  main panel */
//        private GameObject m_lastPlotSelected = null;

//        private Transform m_blackListTrasform = null;
//        private Transform m_unblackListTrasform = null;

//        public GameObject baseLatencyButton = null;
//        private List<GameObject> latencyButtons = null;

//        #endregion members

//        #region others

//        /// <summary>
//        /// Init the controller
//        /// </summary>
//        /// <param name="scene"></param>
//        /// <param name="camerasManager"></param>
//        public new void init(Base3DScene scene, CamerasManager camerasManager)
//        {
//            base.init(scene, camerasManager);

//            // associate transform canvas overlays
//            GameObject overlayCanvas = m_canvasOverlayParent.Find("plot action").gameObject;

//            // set scenes overlay transfoms
//            plotActionSceneOverlay.mainUITransform = overlayCanvas.transform;
//            m_plotActionElements = overlayCanvas.transform.Find("main panel").gameObject;


//            m_plotActionElements.transform.Find("Name panel").Find("close plot action button").GetComponent<Button>().onClick.AddListener(
//                delegate
//                {
//                    setStateWindow(false);
//                    scene.updateCurrentColumnPlotWindowState(false);
//                });

//            Button additionnalInfoButton = m_plotActionElements.transform.Find("Name panel").Find("additionnal info button").GetComponent<Button>();
//            additionnalInfoButton.onClick.AddListener(
//                delegate
//                {
//                    if (m_isSPScene)
//                        ((SP3DScene)m_scene).sendAdditionnalPlotInfoRequest();
//                    else
//                        ((MP3DScene)m_scene).sendAdditionnalPlotInfoRequest();

//                });

//            if (m_isSPScene)
//            {
//                Button defineSourceButton = m_plotActionElements.transform.Find("Latency panel").Find("define source button").GetComponent<Button>();
//                Button undefineSourceButton = m_plotActionElements.transform.Find("Latency panel").Find("undefine source button").GetComponent<Button>();
//                Text latencyDataText = m_plotActionElements.transform.Find("Latency panel").Find("latency data text").GetComponent<Text>();

//                defineSourceButton.onClick.AddListener(
//                    delegate
//                    {
//                        ((SP3DScene)m_scene).setCurrentPlotAsSource();
//                        latencyDataText.text = "Plot is the current defined source.";
//                        defineSourceButton.interactable = false;
//                        undefineSourceButton.interactable = true;
//                    });

//                undefineSourceButton.onClick.AddListener(
//                    delegate
//                    {
//                        ((SP3DScene)m_scene).undefineCurrentSource();

//                        if (latencyDataText.text == "Plot is the current defined source.")
//                        {
//                            latencyDataText.text = "Plot is a source.";
//                            defineSourceButton.interactable = true;
//                            undefineSourceButton.interactable = false;
//                        }
//                        else
//                        {
//                            latencyDataText.text = "Plot is not a source.";
//                            defineSourceButton.interactable = false;
//                            undefineSourceButton.interactable = false;
//                        }
//                    });
//            }
//            else
//            {
//                m_plotActionElements.transform.Find("Load panel").Find("load individual patient button").GetComponent<Button>().onClick.AddListener(
//                    delegate
//                    {                        
//                        ((MP3DScene)m_scene).loadPatientInSPScene(m_idPatientToLoad, m_lastPlotSelected.GetComponent<Plot>().idPlotPatient);
//                    });

//                m_blackListTrasform = m_plotActionElements.transform.Find("Blacklist panel").Find("Blacklist button");
//                m_unblackListTrasform = m_plotActionElements.transform.Find("Blacklist panel").Find("Unblacklist button");

//                m_blackListTrasform.GetComponent<Button>().onClick.AddListener(
//                    delegate
//                    {
//                        m_lastPlotSelected.GetComponent<Plot>().blackList = true;
//                        m_scene.setPlotMask(true, m_lastPlotSelected, 2, 0);
//                        m_blackListTrasform.gameObject.SetActive(false);
//                        m_unblackListTrasform.gameObject.SetActive(true);
//                    });

//                m_unblackListTrasform.GetComponent<Button>().onClick.AddListener(
//                    delegate
//                    {
//                        m_lastPlotSelected.GetComponent<Plot>().blackList = false;
//                        m_scene.setPlotMask(true, m_lastPlotSelected, 3, 0);
//                        m_blackListTrasform.gameObject.SetActive(true);
//                        m_unblackListTrasform.gameObject.SetActive(false);
//                    });
//            }

//            Transform maskPanel = m_plotActionElements.transform.Find("Mask panel");
//            maskPanel.Find("Exclude options parent").Find("Plot button").GetComponent<Button>().onClick.AddListener(
//                delegate
//                {
//                    bool exclude = maskPanel.Find("Mask option parent").Find("Exclude toggle").GetComponent<Toggle>().isOn;
//                    bool currentColumn = maskPanel.Find("Column option parent").Find("Current column toggle").GetComponent<Toggle>().isOn;
//                    int action = exclude ? 0 : 1;
//                    m_scene.setPlotMask(!currentColumn, m_lastPlotSelected, action, 0);
//                });

//            maskPanel.Find("Exclude options parent").Find("Electrode button").GetComponent<Button>().onClick.AddListener(
//                delegate
//                {
//                    bool exclude = maskPanel.Find("Mask option parent").Find("Exclude toggle").GetComponent<Toggle>().isOn;
//                    bool currentColumn = maskPanel.Find("Column option parent").Find("Current column toggle").GetComponent<Toggle>().isOn;
//                    int action = exclude ? 0 : 1;
//                    m_scene.setPlotMask(!currentColumn, m_lastPlotSelected, action, 1);
//                });

//            maskPanel.Find("Exclude options parent").Find("Patient button").GetComponent<Button>().onClick.AddListener(
//                delegate
//                {
//                    bool exclude = maskPanel.Find("Mask option parent").Find("Exclude toggle").GetComponent<Toggle>().isOn;
//                    bool currentColumn = maskPanel.Find("Column option parent").Find("Current column toggle").GetComponent<Toggle>().isOn;
//                    int action = exclude ? 0 : 1;
//                    m_scene.setPlotMask(!currentColumn, m_lastPlotSelected, action, 2);
//                });


//            m_plotActionElements.SetActive(false);
//        }

//        /// <summary>
//        /// Update the UI with the current mode and activity
//        /// </summary>
//        public override void updateUI()
//        {
//            // set activity
//            plotActionSceneOverlay.setActivity(isVisibleFromScene);
//        }

//        /// <summary>
//        /// Update the position of the UI in the scene
//        /// </summary>
//        public override void updateUIPosition()
//        {
//            // update rect transform
//            RectTransform rectTransform;
//            Rect rectCamera = CamerasManager.GetScreenRect(m_camerasManager.getSceneRectT(m_scene.singlePatient), m_backGroundCamera);
//            //Rect screenRect = CamerasManager.GetScreenRect(m_scenePanel.gameObject.GetComponent<RectTransform>(), m_backGroundCamera);
//            rectTransform = m_plotActionElements.gameObject.GetComponent<RectTransform>();
//            rectTransform.position = rectCamera.position + new Vector2(widthOffset, rectCamera.height - heightOffset);
//        }

//        /// <summary>
//        /// Open the window and update the UI with the input parameters
//        /// </summary>
//        /// <param name="cm"></param>
//        /// <param name="latencyDataMode"></param>
//        public void setWindowsParameters(Column3DViewManager cm, bool latencyDataMode)
//        {
//            // open the window
//            setStateWindow(true);

//            // retrieve plot
//            Plot selectedPlot = cm.currentSelectedPlot();
//            m_lastPlotSelected = selectedPlot.gameObject;
//            m_plotActionElements.transform.Find("Name panel").Find("plot selected text").GetComponent<Text>().text = "Plot selected : " + selectedPlot.name;

//            // update selected patient
//            if (cm.idSelectedPatient != -1)
//            {                
//                m_idPatientToLoad = cm.idSelectedPatient;
//            }

//            if (!m_isSPScene) // multi patients scene
//            {
//                if (selectedPlot.blackList)
//                {
//                    m_blackListTrasform.gameObject.SetActive(false);
//                    m_unblackListTrasform.gameObject.SetActive(true);
//                }
//                else
//                {
//                    m_blackListTrasform.gameObject.SetActive(true);
//                    m_unblackListTrasform.gameObject.SetActive(false);
//                }
//            }
//            else // latency panel
//            {
//                Button defineSourceButton = m_plotActionElements.transform.Find("Latency panel").Find("define source button").GetComponent<Button>();
//                Button undefineSourceButton = m_plotActionElements.transform.Find("Latency panel").Find("undefine source button").GetComponent<Button>();
//                Text latencyDataText = m_plotActionElements.transform.Find("Latency panel").Find("latency data text").GetComponent<Text>();

//                bool updateLatency = latencyDataMode && !cm.isIRMFCurrentColumn();
//                m_plotActionElements.transform.Find("Latency panel").gameObject.SetActive(updateLatency);

//                RectTransform panelRect = m_plotActionElements.GetComponent<RectTransform>();
//                if (!updateLatency) // if not in the latency mode or IRMF
//                {
//                    panelRect.sizeDelta = new Vector2(panelRect.sizeDelta.x, 130f);
//                    return;
//                }
//                panelRect.sizeDelta = new Vector2(panelRect.sizeDelta.x, 260f);

//                // latency mode
//                if (!cm.latencyFileAvailable) 
//                {
//                    latencyDataText.text = "No latency file available";
//                    defineSourceButton.interactable = false;
//                    undefineSourceButton.interactable = false;
//                }
//                else
//                {
//                    // color buttons
//                    for(int ii = 0; ii < latencyButtons.Count; ++ii)
//                    {
//                        ColorBlock cb = latencyButtons[ii].GetComponent<Button>().colors;
//                        if (ii == cm.currentIEEGColumn().currentLatencyFile)
//                        {
//                            cb.normalColor = Color.green;
//                            cb.highlightedColor = Color.green;
//                        }
//                        else
//                        {
//                            cb.normalColor = Color.white;
//                            cb.highlightedColor = Color.white;
//                        }
//                        latencyButtons[ii].GetComponent<Button>().colors = cb;
//                    }

//                    if (cm.currentIEEGColumn().sourceDefined)
//                    {
//                        if (cm.currentIEEGColumn().plotIsASource)
//                        {
//                            latencyDataText.text = "Plot is the current defined source.";
//                            defineSourceButton.interactable = false;
//                            undefineSourceButton.interactable = true;
//                        }
//                        else
//                        {
//                            if (cm.currentIEEGColumn().plotLatencyData) // if (isPlotLatencyData)
//                            {
//                                latencyDataText.text = "Latency data available for this plot.";
//                                defineSourceButton.interactable = true;
//                                undefineSourceButton.interactable = true;
//                            }
//                            else
//                            {
//                                latencyDataText.text = "No latency data available for this plot.";
//                                defineSourceButton.interactable = false;
//                                undefineSourceButton.interactable = true;
//                            }
//                        }
//                    }
//                    else
//                    {
//                        if (cm.currentIEEGColumn().plotIsASource) // if (isPlotASource)
//                        {
//                            latencyDataText.text = "Plot is a source.";
//                            defineSourceButton.interactable = true;
//                            undefineSourceButton.interactable = false;
//                        }
//                        else
//                        {
//                            latencyDataText.text = "Plot is not a source.";
//                            defineSourceButton.interactable = false;
//                            undefineSourceButton.interactable = false;
//                        }
//                    }
//                }
//            }

//        }

//        /// <summary>
//        /// Return the rect transform of the window
//        /// </summary>
//        /// <returns></returns>
//        public RectTransform getPlotActionRectT()
//        {
//            return m_plotActionElements.GetComponent<RectTransform>();
//        }

//        /// <summary>
//        /// Return the visibility of the window
//        /// </summary>
//        /// <returns></returns>
//        public bool isWindowVisible()
//        {
//            return m_plotActionElements.activeSelf;
//        }

//        /// <summary>
//        /// Set the open state of the window
//        /// </summary>
//        /// <param name="open"></param>
//        public void setStateWindow(bool open)
//        {
//            m_plotActionElements.SetActive(open);
//        }

//        /// <summary>
//        /// Close the window
//        /// </summary>
//        public void closeWindow()
//        {
//            setStateWindow(false);
//        }

//        /// <summary>
//        /// Update the current latency file with a new id
//        /// </summary>
//        /// <param name="idFile"></param>
//        public void updateCurrentLatencyFile(int idFile)
//        {
//            ((SP3DScene)m_scene).updateCurrentLatencyFile(idFile);
//        }

//        /// <summary>
//        /// Update the latency files names
//        /// </summary>
//        /// <param name="labels"></param>
//        public void updateLatencyFiles(List<string> labels)
//        {
//            if(latencyButtons != null)
//            {
//                for (int ii = 0; ii < latencyButtons.Count; ++ii)
//                    Destroy(latencyButtons[ii]);
//            }

//            latencyButtons = new List<GameObject>(labels.Count);
//            for(int ii = 0; ii < labels.Count; ++ii)
//            {
//                GameObject latencyButton = Instantiate(baseLatencyButton);
//                latencyButton.transform.SetParent(baseLatencyButton.transform.parent);
//                latencyButton.name = labels[ii] + " button";
//                latencyButton.GetComponent<LatencyButton>().init(ii);
//                latencyButton.SetActive(true);
//                latencyButton.transform.Find("Text").GetComponent<Text>().text = labels[ii];

//                if(ii == 0)
//                {
//                    ColorBlock cb = latencyButton.GetComponent<Button>().colors;
//                    cb.normalColor = Color.green;
//                    cb.highlightedColor = Color.green;
//                    latencyButton.GetComponent<Button>().colors = cb;
//                }

//                latencyButtons.Add(latencyButton); 
//            }
//        }

//        #endregion others
//    }
//}