

/**
 * \file    TimelineController.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define TimelineController class
 */

// system
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;

// unity
using UnityEngine;
using UnityEngine.UI;

// hbp
using HBP.Module3D.Cam;

namespace HBP.Module3D
{
    /// <summary>
    /// Controller for a scene timeline
    /// </summary>
    public class TimelineController : IndividualSceneOverlayController
    {
        #region Properties

        private Transform m_timelineControllerOverlay;

        private bool m_showGlobal = true; /**< show the global timeline ? */
        private int m_currentTimelineID = 0; /**< current timeline id to display */

        // ui elements
        private Transform m_globalComputeButton;
        private Transform m_globalTimelinePanel;
        private List<Transform> m_computeButtonList = new List<Transform>();
        private List<Transform> m_timelinePanelList = new List<Transform>();

        // scenes overlay
        private UIOverlay timelineSceneOverlay = new UIOverlay();

        // loaded prefab
        private GameObject m_globalTimelineElements;
        private List<GameObject> m_timelineElementsList = new List<GameObject>();

        private Texture2D m_sliderTexture = null;
        private Texture2D m_sliderGlobalTexture = null;

        // timelines data
        private bool m_timelineIsEnabled = false;
        private bool m_selectedColumnIsIRMF = false; /**< is the current selected column an IRMF ? */
        private bool m_timelinesDefined = false; /**< isthe timeline defined ? */
        private int m_positionMainEvent;    /**< position of the main event */
        private int m_size; /**< size of the timeline */
        private float m_min; /**< min value */
        private float m_max; /**< max value */
        private string m_uniteMin; /**< unite of the min value */
        private string m_uniteMax; /**< unite of the max value */
        private string valueTimeText; /**< text of the time value */

        // global loop
        private bool m_globalIsLooping;
        private Slider m_globalTimeSlider = null;
        private Button m_globalLoopButton = null;
        // individual loop
        private List<bool> m_individualIsLooping = new List<bool>();

        private List<HBP.Data.Visualisation.TimeLine> m_timelinesList; /**< timelines list */
        private List<List<int>> m_secondaryEventsPositions = new List<List<int>>(); /**< positions of the secondary events */
        private List<List<GameObject>> m_secondaryEventsText = new List<List<GameObject>>(); /**< textres of the secondary events */

        #endregion

        #region Private Methods

        /// <summary>
        /// This function is called after all frame updates for the last frame of the object’s existence (the object might be destroyed in response to Object.Destroy or at the closure of a scene).
        /// </summary>
        void OnDestroy()
        {
            Destroy(m_sliderTexture);
            Destroy(m_sliderGlobalTexture);
        }

        #endregion

        #region Public Methods
        /// <summary>
        /// Init the controller
        /// </summary>
        /// <param name="scene"></param>
        /// <param name="camerasManager"></param>
        public new void init(Base3DScene scene, CamerasManager camerasManager)
        {
            base.Initialize(scene, camerasManager);

            // associate transform canvas overlays
            GameObject overlayCanvas = new GameObject("timeline");
            overlayCanvas.transform.SetParent(transform);
            m_timelineControllerOverlay = overlayCanvas.transform;

            // set scene overlay transform
            timelineSceneOverlay.mainUITransform = m_timelineControllerOverlay;

            // init global 
            //      init timeline
            m_globalTimelineElements = Instantiate(GlobalGOPreloaded.Timeline);
            m_globalTimelineElements.name = "timeline_global";
            m_globalTimelineElements.transform.SetParent(m_timelineControllerOverlay, false);
            m_globalTimelineElements.SetActive(true);
            //      init transforms
            m_globalTimelinePanel = m_globalTimelineElements.transform.Find("timeline_panel");
            m_globalComputeButton = m_globalTimelineElements.transform.Find("compute_button");
            //      set listeners
            m_globalTimeSlider = m_globalTimelinePanel.Find("slider panel").Find("value_slider").gameObject.GetComponent<Slider>();
            m_globalTimeSlider.onValueChanged.AddListener(
                delegate
                {
                    m_Scene.UpdateIEEGTime(0, m_globalTimelinePanel.Find("slider panel").Find("value_slider").gameObject.GetComponent<Slider>().value, true);
                });
            m_globalTimelinePanel.Find("global button").gameObject.GetComponent<Button>().onClick.AddListener(
                delegate
                {
                    toggleGlobal();
                });
            m_globalLoopButton = m_globalTimelinePanel.Find("animate button").gameObject.GetComponent<Button>();
            m_globalLoopButton.onClick.AddListener(
                delegate
                {
                    switchGlobalTimeLoopState();
                });

            m_globalComputeButton.gameObject.GetComponent<Button>().onClick.AddListener(
                delegate
                {
                    m_Scene.UpdateGenerators();
                });
            m_globalTimelinePanel.Find("increment_button").gameObject.GetComponent<Button>().onClick.AddListener(
                delegate
                {
                    m_globalTimelinePanel.Find("slider panel").Find("value_slider").gameObject.GetComponent<Slider>().value += Int32.Parse(m_globalTimelinePanel.Find("offset_input").GetComponent<InputField>().text);
                });
            m_globalTimelinePanel.Find("decrement_button").gameObject.GetComponent<Button>().onClick.AddListener(
                delegate
                {
                    m_globalTimelinePanel.Find("slider panel").Find("value_slider").gameObject.GetComponent<Slider>().value -= Int32.Parse(m_globalTimelinePanel.Find("offset_input").GetComponent<InputField>().text);
                });
            m_globalTimelinePanel.Find("offset_input").gameObject.GetComponent<InputField>().onValueChanged.AddListener(
                delegate
                {
                    string offsetText = m_globalTimelinePanel.Find("offset_input").GetComponent<InputField>().text;

                    Regex regex = new Regex("-");
                    offsetText = regex.Replace(offsetText, "");

                    if (offsetText.Length == 0)
                    {
                        offsetText = "1";
                    }
                    m_globalTimelinePanel.Find("offset_input").GetComponent<InputField>().text = offsetText;

                    int offset = Int32.Parse(offsetText);
                    float t = ((m_max - m_min) / (m_size - 1));   
                    m_globalTimelinePanel.Find("offset panel").Find("offset_text").GetComponent<Text>().text = "t = " + Math.Round((decimal)t, 2) + m_uniteMin + "\noff :" + Math.Round((decimal)(t * offset), 2) + m_uniteMin;
                });
        }


        /// <summary>
        /// Update the UI with the current mode and activity
        /// </summary>
        public override void UpdateUI()
        {
            bool activity = m_CurrentActivity && m_IsVisibleFromScene && m_IsEnoughtRoom;

            if(m_CurrentMode != null)
                if (m_CurrentMode.IDMode == Mode.ModesId.NoPathDefined || m_CurrentMode.IDMode == Mode.ModesId.Error)
                {
                    activity = false;
                }

            // set activity
            timelineSceneOverlay.setActivity(activity);

            // update UI with mode
            Transform timelinePanel, computeButton;
            for (int ii = 0; ii < m_timelineElementsList.Count; ++ii)
            {
                computeButton = m_computeButtonList[ii];
                timelinePanel = m_timelinePanelList[ii];
                setTimelineState(timelinePanel, computeButton, m_CurrentMode);
            }

            computeButton = m_globalComputeButton;
            timelinePanel = m_globalTimelinePanel;
            setTimelineState(timelinePanel, computeButton, m_CurrentMode);
        }

        /// <summary>
        /// Update the position of the UI in the scene
        /// </summary>
        public override void UpdatePosition()
        {
            GameObject timeline;
            Transform timelinePanel;
            if (m_showGlobal)
            {
                timeline = m_globalTimelineElements;
                timelinePanel = m_globalTimelinePanel;
            }
            else
            {
                if (m_currentTimelineID >= m_timelineElementsList.Count)
                    return;

                timeline = m_timelineElementsList[m_currentTimelineID];
                timelinePanel = m_timelinePanelList[m_currentTimelineID];
            }

            // update rect transform
            RectTransform rectTransform;
            Rect rectCamera = m_CamerasManager.GetSceneRectTransform(m_Scene.Type).rect;
            rectTransform = timeline.gameObject.GetComponent<RectTransform>();
            rectTransform.position = rectCamera.position + new Vector2(rectCamera.width / 2, 17);
            rectTransform.sizeDelta = new Vector2(rectCamera.width, 35); ;

            // update slider
            Slider s = timelinePanel.Find("slider panel").Find("value_slider").gameObject.GetComponent<Slider>();
            timelinePanel.Find("value_text").position = s.gameObject.GetComponent<Transform>().position;
            timelinePanel.Find("value_text").gameObject.GetComponent<Text>().text = "" + Math.Round((decimal)timelinePanel.Find("slider panel").Find("value_slider").gameObject.GetComponent<Slider>().value, 2); ;

            if (!m_timelinesDefined)
                return;

            // update min/max textes/positions
            if (!m_showGlobal)
            {
                // min position
                if (m_timelinesList[m_currentTimelineID].Start.Position != 0 && m_timelinePanelList.Count > 1)
                {
                    float posMinText = (1f * m_timelinesList[m_currentTimelineID].Start.Position / (m_size - 1));
                    timelinePanel.Find("min_text").position = s.GetComponent<Transform>().position - new Vector3((0.5f - posMinText) * s.GetComponent<RectTransform>().rect.width, -10, 0);
                }
                else
                {
                    timelinePanel.Find("min_text").GetComponent<Text>().text = "";
                }

                // max position
                if (m_timelinesList[m_currentTimelineID].End.Position != m_size && m_timelinePanelList.Count > 1)
                {
                    float posMaxText = (1f * m_timelinesList[m_currentTimelineID].End.Position / (m_size - 1));
                    timelinePanel.Find("max_text").position = s.GetComponent<Transform>().position - new Vector3((0.5f - posMaxText) * s.GetComponent<RectTransform>().rect.width, -10, 0);
                }
                else
                {
                    timelinePanel.Find("max_text").GetComponent<Text>().text = "";
                }

                // secondary events position
                for (int ii = 0; ii < m_secondaryEventsText[m_currentTimelineID].Count; ++ii)
                {
                    float posSecondaryEvent = (1f * m_secondaryEventsPositions[m_currentTimelineID][ii] / (m_size - 1));
                    m_secondaryEventsText[m_currentTimelineID][ii].transform.position = s.GetComponent<Transform>().position - new Vector3((0.5f - posSecondaryEvent) * s.GetComponent<RectTransform>().rect.width, 9, 0);
                }
            }
            else
            {
                timelinePanel.Find("min_text").GetComponent<Text>().text = "";
                timelinePanel.Find("max_text").GetComponent<Text>().text = "";
            }

            // update value text
            valueTimeText = "" + s.value + " : " + Math.Round((decimal)(m_min + ((m_max - m_min) / (m_size - 1)) * (s.value)), 2) + m_uniteMin;
            timelinePanel.Find("value_text").GetComponent<Text>().text = valueTimeText;

            float posMainEvent = (1f * m_positionMainEvent / (m_size - 1));
            timelinePanel.Find("mainEvent_text").position = s.GetComponent<Transform>().position - new Vector3((0.5f - posMainEvent) * s.GetComponent<RectTransform>().rect.width, 9, 0);

            // check if enought plance to display timeline elements
            if (m_timelineIsEnabled)
            {
                bool elementsVisible = rectCamera.width > 500;
                timelinePanel.Find("slider panel").gameObject.SetActive(elementsVisible);
                timelinePanel.Find("mainEvent_text").gameObject.SetActive(elementsVisible);
                timelinePanel.Find("value_text").gameObject.SetActive(elementsVisible);
                for (int ii = 0; ii < m_secondaryEventsText[m_currentTimelineID].Count; ++ii)
                {
                    m_secondaryEventsText[m_currentTimelineID][ii].gameObject.SetActive(elementsVisible);
                }
            }

            // check if enought room
            bool previous = m_IsEnoughtRoom;
            m_IsEnoughtRoom = (rectCamera.width > 250);
            if (previous != m_IsEnoughtRoom)
            {
                UpdateUI();
            }
        }

        /// <summary>
        /// Return the current time
        /// </summary>
        /// <returns></returns>
        public string getTime() { return valueTimeText; }

        /// <summary>
        /// Update the controller with new timelines
        /// </summary>
        /// <param name="timelinesList"></param>
        public void updateTimelinesUI(List<HBP.Data.Visualisation.TimeLine> timelinesList)
        {
            if (timelinesList.Count != m_timelineElementsList.Count)
            {
                Debug.LogError("-ERROR : TimelineController::updateTimelinesUI : columns nb and specific timelines nb don't match.");
                return;
            }

            if(timelinesList.Count == 0)
            {
                Debug.LogError("-ERROR : TimelineController::updateTimelinesUI : timelist size is null.");
                return;
            }

            // clean
            m_secondaryEventsPositions.Clear();
            for (int ii = 0; ii < m_secondaryEventsText.Count; ++ii)
            {
                for (int jj = 0; jj < m_secondaryEventsText[ii].Count; ++jj)
                    Destroy(m_secondaryEventsText[ii][jj]);
            }
            m_secondaryEventsText.Clear();

            Transform timelinePanel;
            int offset, posMin = Int32.MaxValue, posMax = Int32.MinValue;
            float t;
            m_size = 0;
            m_min = Single.MaxValue;
            m_max = Single.MinValue;
            m_uniteMin = "";
            m_uniteMax = "";

            // retrieve min/max
            for (int ii = 0; ii < m_timelineElementsList.Count; ++ii)
            {
                timelinePanel = m_timelinePanelList[ii];
                HBP.Data.Visualisation.TimeLine timeLine = timelinesList[ii];

                Slider slider = timelinePanel.Find("slider panel").Find("value_slider").GetComponent<Slider>();
                slider.wholeNumbers = true;
                slider.minValue = 0;
                slider.maxValue = timeLine.Lenght - 1;

                m_uniteMin = timeLine.Start.Unite;
                m_uniteMax = timeLine.End.Unite;

                if (m_min > timeLine.Start.Value)
                    m_min = timeLine.Start.Value;

                if (m_max < timeLine.End.Value)
                    m_max = timeLine.End.Value;

                if (posMin > timeLine.Start.Position)
                    posMin = timeLine.Start.Position;

                if (posMax < timeLine.End.Position)
                    posMax = timeLine.End.Position;

                m_size = timeLine.Lenght; // sizes indentical for all columns     

                m_positionMainEvent = timeLine.MainEvent.Position;

                m_secondaryEventsText.Add(new List<GameObject>());
                m_secondaryEventsPositions.Add(new List<int>());
                for (int jj = 0; jj < timeLine.SecondaryEvents.Length; ++jj)
                {
                    GameObject textSecondaryEvent = Instantiate<GameObject>(timelinePanel.Find("mainEvent_text").gameObject);
                    textSecondaryEvent.transform.SetParent(timelinePanel.transform);
                    textSecondaryEvent.transform.name = "secondary_text_" + jj;
                    textSecondaryEvent.GetComponent<Text>().text = "SE" + jj;
                    m_secondaryEventsText[m_secondaryEventsText.Count - 1].Add(textSecondaryEvent);
                    m_secondaryEventsPositions[m_secondaryEventsPositions.Count - 1].Add(timeLine.SecondaryEvents[jj].Position);
                }
            }

            for (int ii = 0; ii < m_timelineElementsList.Count; ++ii)
            {
                timelinePanel = m_timelinePanelList[ii];
                HBP.Data.Visualisation.TimeLine timeLine = timelinesList[ii];

                // define texts
                string startS = "", endS = "";
                startS += m_min + timeLine.Start.Unite;
                endS += m_max + timeLine.End.Unite;
                timelinePanel.Find("slider panel").Find("t0_text").GetComponent<Text>().text = startS;
                timelinePanel.Find("slider panel").Find("tEnd_text").GetComponent<Text>().text = endS;

                offset = Int32.Parse(timelinePanel.Find("offset_input").GetComponent<InputField>().text);
                t = ((m_max - m_min) / (m_size - 1));
                timelinePanel.Find("offset panel").Find("offset_text").GetComponent<Text>().text = "t = " + Math.Round((decimal)t, 2) + m_uniteMin + "\noff :" + Math.Round((decimal)(t * offset), 2) + m_uniteMin;
                timelinePanel.Find("min_text").GetComponent<Text>().text = "" + timeLine.Start.Value + m_uniteMin;
                timelinePanel.Find("max_text").GetComponent<Text>().text = "" + timeLine.End.Value + m_uniteMax;

                // define texture
                m_sliderTexture = UITextureGenerator.generate_slider_background_texture(posMin, posMax-1, timeLine.Start.Position, timeLine.End.Position, timeLine.Lenght, m_positionMainEvent, m_secondaryEventsPositions[ii]);
                timelinePanel.Find("slider panel").Find("value_slider").Find("Background").GetComponent<Image>().sprite = Sprite.Create(m_sliderTexture, new Rect(0, 0, m_sliderTexture.width, m_sliderTexture.height), new Vector2(0, 0));
            }

            // global timeline
            timelinePanel = m_globalTimelinePanel;
            timelinePanel.Find("slider panel").Find("t0_text").GetComponent<Text>().text = "" + m_min + m_uniteMin;
            timelinePanel.Find("slider panel").Find("tEnd_text").GetComponent<Text>().text = "" + m_max + m_uniteMax;
            timelinePanel.Find("slider panel").Find("value_slider").GetComponent<Slider>().minValue = 0;
            timelinePanel.Find("slider panel").Find("value_slider").GetComponent<Slider>().maxValue = m_size - 1;

            //########################################################################################################### TODO : NaN ERROR
            offset = Int32.Parse(timelinePanel.Find("offset_input").GetComponent<InputField>().text);
            t = ((m_max - m_min) / (m_size - 1));
            timelinePanel.Find("offset panel").Find("offset_text").GetComponent<Text>().text = "t = " + Math.Round((decimal)t, 2) + m_uniteMin + "\noff :" + Math.Round((decimal)(t * offset), 2) + m_uniteMin; // TODO : error

            //if (m_sliderGlobalTexture != null)
            //    Destroy(m_sliderGlobalTexture);

            m_sliderGlobalTexture = UITextureGenerator.generate_slider_background_texture(0, m_size - 1, 0, m_size - 1, m_size, m_positionMainEvent, new List<int>());
            timelinePanel.Find("slider panel").Find("value_slider").Find("Background").GetComponent<Image>().sprite = Sprite.Create(m_sliderGlobalTexture, new Rect(0, 0, m_sliderGlobalTexture.width, m_sliderGlobalTexture.height), new Vector2(0, 0));

            m_timelinesList = timelinesList;

            m_timelinesDefined = true;
        }

        /// <summary>
        /// Set the current timeline to be displayed
        /// </summary>
        /// <param name="id"></param>
        public void setCurrentTimeline(int id)
        {
            if (!m_showGlobal)
            {
                // previous timeline became inactive
                m_timelineElementsList[m_currentTimelineID].SetActive(false);
            }

            if(id >= m_timelineElementsList.Count) // IRMF column
            {
                m_selectedColumnIsIRMF = true;
                m_timelineElementsList[m_currentTimelineID].SetActive(false);
                return;
            }


            // update id
            m_selectedColumnIsIRMF = false;
            m_currentTimelineID = id;

            // current timeline became active
            if (!m_showGlobal)
            {
                m_timelineElementsList[m_currentTimelineID].SetActive(true);
            }
        }

        /// <summary>
        /// Update the input timeline UI elements with a mode
        /// </summary>
        /// <param name="timelinePanel"></param>
        /// <param name="computeButton"></param>
        /// <param name="mode"></param>
        private void setTimelineState(Transform timelinePanel, Transform computeButton, Mode mode)
        {
            if(m_selectedColumnIsIRMF || m_Scene.IsLatencyModeEnabled() || mode == null)
            {
                m_timelineIsEnabled = false;
                computeButton.gameObject.SetActive(false);
                timelinePanel.gameObject.SetActive(false);
                return;
            }

            string textComputeButton = "";
            Color colorComputeButton = Color.white;
            computeButton.gameObject.GetComponent<Button>().interactable = m_Scene.SceneInformation.iEEGOutdated;
            textComputeButton = "Update iEEG";
            computeButton.gameObject.SetActive(true);

            if (mode.IDMode == Mode.ModesId.AmplitudesComputed) 
            {
                m_timelineIsEnabled = true;
                timelinePanel.gameObject.SetActive(true);                
            }
            else if (mode.IDMode == Mode.ModesId.ComputingAmplitudes)
            {
                m_timelineIsEnabled = false;
                computeButton.gameObject.GetComponent<Button>().interactable = false;
                textComputeButton = "Computing...";
                timelinePanel.gameObject.SetActive(false);
            }
            else if (mode.IDMode == Mode.ModesId.AmpNeedUpdate)
            {
                m_timelineIsEnabled = true;                                
                timelinePanel.gameObject.SetActive(true);
                colorComputeButton = Color.red;
            }
            else // amp not computed
            {
                m_timelineIsEnabled = false;
                timelinePanel.gameObject.SetActive(false);
            }

            computeButton.gameObject.transform.Find("Text").GetComponent<Text>().text = textComputeButton;
            computeButton.gameObject.transform.Find("Text").GetComponent<Text>().color = colorComputeButton;
        }

        /// <summary>
        /// Update the number of timelines
        /// </summary>
        /// <param name="nbColumns"></param>
        public void updateTimelinesNumber(int nbColumns)
        {
            int diff = m_timelineElementsList.Count - nbColumns;

            if (diff < 0) // add timelines
            {
                for (int ii = 0; ii < -diff; ++ii)
                {
                    // add timeline
                    GameObject timelineElements = Instantiate(GlobalGOPreloaded.Timeline);
                    timelineElements.name = "timeline_" + m_timelineElementsList.Count;
                    timelineElements.transform.SetParent(m_timelineControllerOverlay, false);
                    timelineElements.SetActive(false);
                    m_timelineElementsList.Add(timelineElements);

                    int id = m_timelineElementsList.Count - 1;

                    // init transforms
                    m_timelinePanelList.Add(m_timelineElementsList[id].transform.Find("timeline_panel"));
                    m_computeButtonList.Add(m_timelineElementsList[id].transform.Find("compute_button"));
                    m_individualIsLooping.Add(false);

                    // set listeners
                    m_timelinePanelList[id].Find("slider panel").Find("value_slider").gameObject.GetComponent<Slider>().onValueChanged.AddListener(delegate
                    {
                            m_Scene.UpdateIEEGTime(id, m_timelinePanelList[id].Find("slider panel").Find("value_slider").gameObject.GetComponent<Slider>().value, false);
                    });
                    m_computeButtonList[id].gameObject.GetComponent<Button>().onClick.AddListener(delegate
                    {
                        m_Scene.UpdateGenerators();
                    });
                    m_timelinePanelList[id].Find("global button").gameObject.GetComponent<Button>().onClick.AddListener(delegate
                    {
                        toggleGlobal();
                    });

                    Button loopButton = m_timelinePanelList[id].Find("animate button").gameObject.GetComponent<Button>();
                    loopButton.onClick.AddListener(
                        delegate
                        {
                            switchIndividualLoopState(loopButton, m_timelinePanelList[id].Find("slider panel").Find("value_slider").gameObject.GetComponent<Slider>(), id);
                        });                    

                    m_timelinePanelList[id].Find("increment_button").gameObject.GetComponent<Button>().onClick.AddListener(
                        delegate
                        {
                            m_timelinePanelList[id].Find("slider panel").Find("value_slider").gameObject.GetComponent<Slider>().value += Int32.Parse(m_timelinePanelList[id].Find("offset_input").GetComponent<InputField>().text);
                        });
                    m_timelinePanelList[id].Find("decrement_button").gameObject.GetComponent<Button>().onClick.AddListener(
                        delegate
                        {
                            m_timelinePanelList[id].Find("slider panel").Find("value_slider").gameObject.GetComponent<Slider>().value -= Int32.Parse(m_timelinePanelList[id].Find("offset_input").GetComponent<InputField>().text);
                        });
                    m_timelinePanelList[id].Find("offset_input").gameObject.GetComponent<InputField>().onValueChanged.AddListener(
                        delegate
                        {
                            string offsetText = m_timelinePanelList[id].Find("offset_input").GetComponent<InputField>().text;

                            Regex regex = new Regex("-");
                            offsetText = regex.Replace(offsetText, "");

                            if (offsetText.Length == 0)
                            {
                                offsetText = "1";
                            }
                            
                            m_timelinePanelList[id].Find("offset_input").GetComponent<InputField>().text = offsetText;

                            int offset = Int32.Parse(offsetText);
                            float t = ((m_max - m_min) / (m_size - 1));
                            m_timelinePanelList[id].Find("offset panel").Find("offset_text").GetComponent<Text>().text = "t = " + Math.Round((decimal)t, 2) + m_uniteMin + "\noff: " + Math.Round((decimal)(t * offset), 2) + m_uniteMin;
                        });

                }
            }
            else if (diff > 0) // remove timelines
            {
                for (int ii = 0; ii < diff; ++ii)
                {
                    int id = m_timelineElementsList.Count - 1;
                    m_timelinePanelList.RemoveAt(id);
                    m_computeButtonList.RemoveAt(id);

                    Destroy(m_timelineElementsList[id]);
                    m_timelineElementsList.RemoveAt(id);

                    m_individualIsLooping.RemoveAt(id);
                }
            }
        }

        /// <summary>
        /// Switch between global timeline and individual ones
        /// </summary>
        private void toggleGlobal()
        {
            m_globalIsLooping = false;
            for (int jj = 0; jj < m_individualIsLooping.Count; ++jj)
                m_individualIsLooping[jj] = false;

            // retrieve toggle state
            bool stateToggle;
            if (m_showGlobal)
                stateToggle = m_globalTimelinePanel.Find("global button").Find("Text").GetComponent<Text>().text == "Global";
            else
                stateToggle = m_timelinePanelList[m_currentTimelineID].Find("global button").Find("Text").GetComponent<Text>().text == "Global";

            // apply state on all other toggles
            string textToApply = stateToggle ? "Individual" : "Global";
            m_globalTimelinePanel.Find("global button").Find("Text").GetComponent<Text>().text = textToApply;
            for (int ii = 0; ii < m_timelinePanelList.Count; ++ii)
            {
                m_timelinePanelList[m_currentTimelineID].Find("global button").Find("Text").GetComponent<Text>().text = textToApply;
            }

            m_showGlobal = !stateToggle;

            if (m_showGlobal)
            {
                m_globalTimelineElements.SetActive(true);
                m_timelineElementsList[m_currentTimelineID].SetActive(false);
            }
            else
            {
                m_globalTimelineElements.SetActive(false);
                m_timelineElementsList[m_currentTimelineID].SetActive(true);
            }

            m_Scene.UpdateAllIEEGTime(m_showGlobal);
        }

        /// <summary>
        /// Return the rect transform of the current timeline
        /// </summary>
        /// <returns></returns>
        public RectTransform getTimelineRectT()
        {
            if (m_showGlobal)
                return m_globalTimelineElements.gameObject.GetComponent<RectTransform>();
            else
                return m_timelineElementsList[m_currentTimelineID].gameObject.GetComponent<RectTransform>();
        }

        private void switchGlobalTimeLoopState()
        {
            m_globalIsLooping = !m_globalIsLooping;
            if(m_globalIsLooping)
                StartCoroutine("globalLoopTimeline");
        }

        private void switchIndividualLoopState(Button loopButton, Slider timelineSlider, int id)
        {
            m_individualIsLooping[id] = !m_individualIsLooping[id];
            if (m_individualIsLooping[id])
                StartCoroutine(loopIndividualTimeline(loopButton, timelineSlider, id));                
        }

        private IEnumerator globalLoopTimeline()
        {
            m_globalLoopButton.transform.Find("Text").GetComponent<Text>().text = "Stop";

            //System.Random rnd = new System.Random();
            float startValue = m_globalTimeSlider.value;// m_globalTimeSlider.minValue;
            while (m_globalIsLooping)
            {
                if(!m_Scene.SceneInformation.generatorUpToDate)
                {
                    m_globalTimeSlider.value = m_globalTimeSlider.minValue;
                    m_globalIsLooping = false;
                    break;
                }

                if (startValue > m_globalTimeSlider.maxValue)
                    startValue = m_globalTimeSlider.minValue;
                else
                    startValue += 1;

                m_globalTimeSlider.value = startValue;

                yield return StartCoroutine(CoroutineUtil.WaitForRealSeconds(0.05f));
            }
            m_globalLoopButton.transform.Find("Text").GetComponent<Text>().text = "Loop";
        }

        private IEnumerator loopIndividualTimeline(Button loopButton, Slider timelineSlider, int id)
        {
            loopButton.transform.Find("Text").GetComponent<Text>().text = "Stop";

            float startValue = timelineSlider.value;// timelineSlider.minValue;
            while (m_individualIsLooping[id])
            {
                if (!m_Scene.SceneInformation.generatorUpToDate)
                {
                    timelineSlider.value = timelineSlider.minValue;
                    m_individualIsLooping[id] = false;
                    break;
                }

                if (startValue > timelineSlider.maxValue)
                    startValue = timelineSlider.minValue;
                else
                    startValue += 1;

                timelineSlider.value = startValue;

                yield return StartCoroutine(CoroutineUtil.WaitForRealSeconds(0.05f));
            }
            loopButton.transform.Find("Text").GetComponent<Text>().text = "Loop";
        }
        #endregion
    }
}