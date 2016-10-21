
// system
using System.Globalization;
using System.Collections;
using System.Collections.Generic;

// unity
using UnityEngine;
using UnityEngine.UI;

// hbp
using HBP.VISU3D.Cam;

namespace HBP.VISU3D
{
    /// <summary>
    /// A class for managing all the scenes overlay UI panels
    /// </summary>
    public class UIOverlayManager : MonoBehaviour
    {
        #region members

        // public 
        private Canvas m_canvas = null; /**< screen space overlay canvas */
        public Canvas Canvan
        {
            get { return m_canvas; }
        }

        // private
        private ScenesManager m_scenesManager = null; /**< scenes manager */


        // transform
        public Transform m_bothDisplayPlotInfoTransform = null; // TEMP

        // controllers
        private PlanesController m_spPlanesController = null; /**< SP planes controllers */
        private PlanesController m_mpPlanesController = null; /**< MP planes controllers */
        private TimelineController m_spTimelineController = null; /**< SP timeline controllers */
        private TimelineController m_mpTimelineController = null; /**< MP timline controllers */
        private TimeDisplayController m_spTimeDisplayController = null; /**< SP time display controllers */
        private TimeDisplayController m_mpTimeDisplayController = null; /**< MP time display controllers */
        private IconesController m_spIconesController = null; /**< SP icones controllers */
        private IconesController m_mpIconesController = null; /**< MP icones controllers */
        private CutsDisplayController m_bothDisplayImageCutController = null; /**< both display image cut controller */
        private MinimizeController m_bothMinimizeController = null; /**< both minimize controller */
        private ColormapController m_bothColorMapController = null; /**< both colormap controller */
        private ScreenMessageController m_bothScreenMessageController = null; /**< both screen message controller */
        

        public MinimizeController MinimizeController
        {
            get { return m_bothMinimizeController; }
        }

        private bool m_initialized = false;

        #endregion members


        #region mono_behaviour

        void Awake()
        {
            int idScript = TimeExecution.getId();
            TimeExecution.startAwake(idScript, TimeExecution.ScriptsId.UIOverlayManager);
            
            // retrieve contollers 
            //  planes
            m_spPlanesController = transform.Find("SP").Find("planes").gameObject.GetComponent<PlanesController>();
            m_mpPlanesController = transform.Find("MP").Find("planes").gameObject.GetComponent<PlanesController>();
            //  timeline
            m_spTimelineController = transform.Find("SP").Find("timeline").gameObject.GetComponent<TimelineController>();
            m_mpTimelineController = transform.Find("MP").Find("timeline").gameObject.GetComponent<TimelineController>();
            // time
            m_spTimeDisplayController = transform.Find("SP").Find("time").gameObject.GetComponent<TimeDisplayController>();
            m_mpTimeDisplayController = transform.Find("MP").Find("time").gameObject.GetComponent<TimeDisplayController>();
            //  icones
            m_spIconesController = transform.Find("SP").Find("icones").gameObject.GetComponent<IconesController>();
            m_mpIconesController = transform.Find("MP").Find("icones").gameObject.GetComponent<IconesController>();
            //  image cuts
            m_bothDisplayImageCutController = transform.Find("both").Find("image cut display").gameObject.GetComponent<CutsDisplayController>();
            //  minimize
            m_bothMinimizeController = transform.Find("both").Find("minimize cameras").gameObject.GetComponent<MinimizeController>();
            // colormap
            m_bothColorMapController = transform.Find("both").Find("colormap cameras").gameObject.GetComponent<ColormapController>();
            //  screen message
            m_bothScreenMessageController = transform.Find("both").Find("screen message").gameObject.GetComponent<ScreenMessageController>();

            TimeExecution.endAwake(idScript, TimeExecution.ScriptsId.UIOverlayManager, gameObject);
        }

        /// <summary>
        /// Update is called once per frame. It is the main workhorse function for frame updates.
        /// </summary>
        void Update()
        {
            updateUIPosition();
        }

        #endregion mono_behaviour


        /// <summary>
        /// Init the overlay manager.
        /// </summary>
        /// <param name="cameraManager"></param>
        /// <param name="singlePatientScene"></param>
        /// <param name="multiPatientsScene"></param>
        public void init(ScenesManager scenesManager, Canvas overlay)
        {
            // retrieve canvas
            m_canvas = overlay;

            // find elements UI
            m_bothDisplayPlotInfoTransform = m_canvas.transform.Find("others").Find("display plot info panel"); // temp

            // init controllers
            //  planes 
            m_spPlanesController.init(scenesManager.SPScene, scenesManager.CamerasManager);
            m_mpPlanesController.init(scenesManager.MPScene, scenesManager.CamerasManager);
            //  timeline
            m_spTimelineController.init(scenesManager.SPScene, scenesManager.CamerasManager);
            m_mpTimelineController.init(scenesManager.MPScene, scenesManager.CamerasManager);
            // time
            m_spTimeDisplayController.init(scenesManager.SPScene, scenesManager.CamerasManager);
            m_mpTimeDisplayController.init(scenesManager.MPScene, scenesManager.CamerasManager);
            //  icones
            m_spIconesController.init(scenesManager.SPScene, scenesManager.CamerasManager);
            m_mpIconesController.init(scenesManager.MPScene, scenesManager.CamerasManager);
            //  display image cut            
            m_bothDisplayImageCutController.init(scenesManager);
            //  minimize
            m_bothMinimizeController.init(scenesManager);
            // colormap
            m_bothColorMapController.init(scenesManager);
            //  screen message
            m_bothScreenMessageController.init(scenesManager);

            // init condition columns nb
            //  sp
            setIEEGColumnsNb(true, null);
            //  mp
            setIEEGColumnsNb(false, null);

            // set listeners
            m_bothMinimizeController.m_minimizeStateSwitchEvent.AddListener((spScene) =>
            {
                // controllers to update when a change in the minimize states occurs
                m_bothColorMapController.updateUI();
            });

            // listeners
            scenesManager.SPScene.UpdateTimeInUI.AddListener(() =>
            {
                updateTime(true);
            });
            scenesManager.MPScene.UpdateTimeInUI.AddListener(() =>
            {
                updateTime(false);
            });


            // TEMP
            scenesManager.SPScene.UpdateDisplayedPlotsInfo.AddListener((plotInfo) =>
            {
                udpateDisplayPlotInfo(true, plotInfo);
            });
            scenesManager.MPScene.UpdateDisplayedPlotsInfo.AddListener((plotInfo) =>
            {
                udpateDisplayPlotInfo(false, plotInfo);
            });

            m_scenesManager = scenesManager;
            m_initialized = true;
        }

        /// <summary>
        /// Set the scene visibility for all the overlays
        /// </summary>
        /// <param name="active"></param>
        /// <param name="spScene"></param>
        public void setOverlaySceneVisibility(bool active, bool spScene)
        {
            m_bothMinimizeController.setUIVisibleFromScene(spScene, active);
            m_bothColorMapController.setUIVisibleFromScene(spScene, active);
            m_bothDisplayImageCutController.setUIVisibleFromScene(spScene, active);
            m_bothScreenMessageController.setUIVisibleFromScene(spScene, active);

            if (spScene)
            {
                m_spPlanesController.setUIVisibleFromScene(active);
                m_spTimelineController.setUIVisibleFromScene(active);
                m_spIconesController.setUIVisibleFromScene(active);
                m_spTimeDisplayController.setUIVisibleFromScene(active);
            }
            else
            {
                m_mpPlanesController.setUIVisibleFromScene(active);
                m_mpTimelineController.setUIVisibleFromScene(active);
                m_mpIconesController.setUIVisibleFromScene(active);
                m_mpTimeDisplayController.setUIVisibleFromScene(active);
            }
        }


        /// <summary>
        /// Set a specific activity for an overlay
        /// </summary>
        /// <param name="active"></param>
        /// <param name="spScene"></param>
        /// <param name="idOverlay"></param>
        /// <param name="mode"></param>
        public void setSpecificOverlayActive(bool active, int idOverlay, Mode mode)
        {
            switch (idOverlay)
            {
                // TODO : m_bothScreenMessageController

                case 0:
                    if (mode.m_sceneSp)
                        m_spPlanesController.setUIActivity(active, mode);
                    else
                        m_mpPlanesController.setUIActivity(active, mode);
                    break;
                case 1:
                    if (mode.m_sceneSp)
                        m_spTimelineController.setUIActivity(active, mode);
                    else
                        m_mpTimelineController.setUIActivity(active, mode);
                    break;
                case 2:
                    if (mode.m_sceneSp)
                        m_spIconesController.setUIActivity(active, mode);
                    else
                        m_mpIconesController.setUIActivity(active, mode);
                    break;
                case 3:
                    m_bothDisplayImageCutController.setUIActivity(active, mode);
                    break;
                case 4:
                    m_bothColorMapController.setUIActivity(active, mode);
                    break;
                case 5:
                    m_bothMinimizeController.setUIActivity(active, mode);
                    break;
                case 6:
                    if (mode.m_sceneSp)
                        m_spTimeDisplayController.setUIActivity(active, mode);
                    else
                        m_mpTimeDisplayController.setUIActivity(active, mode);
                    break;
                default:
                    break;
            }
        }


        /// <summary>
        /// Update the UI poistion of all the overlays
        /// </summary>
        public void updateUIPosition()
        {
            if (!m_initialized)
                return;

            m_bothDisplayImageCutController.updateUIPosition();
            m_bothMinimizeController.updateUIPosition();
            m_bothColorMapController.updateUIPosition();
            m_spPlanesController.updateUIPosition();
            m_mpPlanesController.updateUIPosition();
            m_spTimelineController.updateUIPosition();
            m_mpTimelineController.updateUIPosition();
            m_spTimeDisplayController.updateUIPosition();
            m_mpTimeDisplayController.updateUIPosition();
            m_spIconesController.updateUIPosition();
            m_mpIconesController.updateUIPosition();
            m_bothScreenMessageController.updateUIPosition();
        }

        /// <summary>
        /// Check if a click occurs on the overlay ui
        /// </summary>
        /// <param name="spScene"></param>
        /// <returns></returns>
        public bool checkIfClickOnOverlay(bool spScene)
        {
            bool onTimelineOverlayClick = checkIfClickOnTimeline(spScene);
            bool onChoosePlaneOverlayClick = checkIfClickOnChoosePlane(spScene);
            bool onMinimizeButtonOverlayClick =checkIfClickOnMinimizeButtons(spScene);
            bool onColormapOverlayClick = checkIfCLickOnColormap(spScene);
            bool onTimeOverlayClick = false;// checkIfClickOnTime(spScene); // TODO
            bool onIconeOverlayClick = false;
            return (onTimelineOverlayClick || onChoosePlaneOverlayClick || onMinimizeButtonOverlayClick || onColormapOverlayClick || onTimeOverlayClick || onIconeOverlayClick);
        }

        /// <summary>
        /// Check if a click occurs on the input rect transform
        /// </summary>
        /// <param name="mousePosition"></param>
        /// <param name="rectT"></param>
        /// <returns></returns>
        private bool checkIfClickOnRect(Vector3 mousePosition, RectTransform rectT)
        {
            Vector2 pt = new Vector2();
            RectTransformUtility.ScreenPointToLocalPointInRectangle(rectT, mousePosition, null, out pt);
            return rectT.rect.Contains(pt);
        }

        /// <summary>
        /// Check if a click occurs on the colormapcontroller ui
        /// </summary>
        /// <param name="spScene"></param>
        /// <returns></returns>
        private bool checkIfCLickOnColormap(bool spScene)
        {
            Vector3 mousePosition = Input.mousePosition;
            for (int ii = 0; ii < m_bothColorMapController.getColumnsNb(spScene); ++ii)
                if (checkIfClickOnRect(mousePosition, m_bothColorMapController.getColormapRectT(spScene, ii)))
                    return true;

            return false;
        }

        /// <summary>
        /// Check if a click occurs on the minimize buttons controller ui
        /// </summary>
        /// <param name="spScene"></param>
        /// <returns></returns>
        private bool checkIfClickOnMinimizeButtons(bool spScene)
        {
            Vector3 mousePosition = Input.mousePosition;
            for (int ii = 0; ii < m_bothMinimizeController.getColumnsNb(spScene); ++ii)
                if (checkIfClickOnRect(mousePosition, m_bothMinimizeController.getMinimizeButtonRectT(spScene, ii)) )
                    return true;

            return false;
        }

        //private bool checkIfClickOnTime(bool spScene)
        //{
        //    Vector3 mousePosition = Input.mousePosition;
        //    if(spScene)
        //        for (int ii = 0; ii <  ++ii)
        //            if (checkIfClickOnRect(mousePosition, m_bothMinimizeController.getMinimizeButtonRectT(spScene, ii)))
        //                return true;

        //    return false;
        //}

        /// <summary>
        /// Check if a click occurs on the timeline controller ui
        /// </summary>
        /// <param name="spScene"></param>
        /// <returns></returns>
        private bool checkIfClickOnTimeline(bool spScene)
        {
            return checkIfClickOnRect(Input.mousePosition, spScene ? m_spTimelineController.getTimelineRectT() : m_mpTimelineController.getTimelineRectT());
        }

        /// <summary>
        /// Check if a click occurs on the choos plane controller ui
        /// </summary>
        /// <param name="spScene"></param>
        /// <returns></returns>
        private bool checkIfClickOnChoosePlane(bool spScene)
        {
            RectTransform rectT = null;
            Vector3 mousePosition = Input.mousePosition;

            if (spScene)
                rectT = m_spPlanesController.getPlanesChooseRectT();
            else
                rectT = m_mpPlanesController.getPlanesChooseRectT();

            if (checkIfClickOnRect(mousePosition, rectT))
                return true;

            if (spScene)
                rectT = m_spPlanesController.getSetPlanesRectT();
            else
                rectT = m_mpPlanesController.getSetPlanesRectT();

            if (rectT == null)
                return false;

            return checkIfClickOnRect(mousePosition, rectT);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="columnId"></param>
        /// <param name="name"></param>
        public void updateColumnName(bool spScene, int columnId, string name)
        { 
            if(m_bothMinimizeController != null)
                m_bothMinimizeController.setColumnName(spScene, columnId, name);
        }


        /// <summary>
        /// Update the focused selected column of a scene for all concerned ui
        /// </summary>
        /// <param name="spScene"> is a single patient scene </param>
        /// <param name="columnId"> id column </param>
        public void updatFocusedSceneAndColumn(bool spScene, int columnId)
        {
            if (spScene)
            {
                m_spTimelineController.setCurrentTimeline(columnId);
            }
            else
            {
                m_mpTimelineController.setCurrentTimeline(columnId);
            }

            m_bothDisplayImageCutController.setSceneToDisplay(spScene);
        }


        /// <summary>
        /// Update the current IEEG columns number for all concerned ui overlays
        /// </summary>
        /// <param name="spScene"> is a single patient scene </param>
        /// <param name="nbColumns"> columns nb</param>
        public void setIEEGColumnsNb(bool spScene,List<HBP.Data.Visualisation.ColumnData> columnsData)
        {
            int nbColumns = (columnsData != null  ? columnsData.Count : 1);
            if (spScene) // SP
            {
                m_bothMinimizeController.updateSPMinimizeButtonNb(nbColumns);
                m_bothColorMapController.updateSPColorMapNb(nbColumns);
                m_spIconesController.adaptIconesNb(nbColumns);
                m_spTimelineController.updateTimelinesNumber(nbColumns);
                m_spTimeDisplayController.adaptNumber(nbColumns);        
            }
            else // MP
            {
                m_bothMinimizeController.updateMPMinimizeButtonNb(nbColumns);
                m_bothColorMapController.updateMPColorMapNb(nbColumns);
                m_mpIconesController.adaptIconesNb(nbColumns);
                m_mpTimelineController.updateTimelinesNumber(nbColumns);
                m_mpTimeDisplayController.adaptNumber(nbColumns);
            }            

            // boths
            if (columnsData != null)
            {
                updateIEEGColumnsDataForUI(spScene, columnsData);

                // update names
                for (int ii = 0; ii < nbColumns; ++ii)
                {
                    updateColumnName(spScene, ii, columnsData[ii].Label);
                }
            }
        }

        /// <summary>
        /// Add an IMRF column overlay ui to the input scene
        /// </summary>
        /// <param name="spScene"></param>
        public void addIRMFColumn(bool spScene)
        {
            if (spScene)
            {
                m_bothMinimizeController.addSPMinimizeButton();
            }
            else
            {
                m_bothMinimizeController.addMPMinimizeButton();
            }
        }

        /// <summary>
        /// Remove an IRMF column overlay ui from the input scene
        /// </summary>
        /// <param name="spScene"></param>
        public void removeLastIRMFColumn(bool spScene)
        {
            if (spScene)
            {
                m_bothMinimizeController.removeSPMinimizeButton();
            }
            else
            {
                m_bothMinimizeController.removeMPMinimizeButton();
            }
        }

        /// <summary>
        /// Update the controllers with column data
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="iEEGDataCols"></param>
        private void updateIEEGColumnsDataForUI(bool spScene, List<HBP.Data.Visualisation.ColumnData> iEEGDataCols)
        {
            // timeline controller
            List<HBP.Data.Visualisation.TimeLine> timelines = new List<HBP.Data.Visualisation.TimeLine>(iEEGDataCols.Count);
            for (int ii = 0; ii < iEEGDataCols.Count; ++ii)
            {
                timelines.Add(iEEGDataCols[ii].TimeLine);
            }

            // icon controller
            List<HBP.Data.Visualisation.IconicScenario> iconicScenarioList = new List<HBP.Data.Visualisation.IconicScenario>();
            for (int ii = 0; ii < iEEGDataCols.Count; ++ii)
            {
                iconicScenarioList.Add(iEEGDataCols[ii].IconicScenario);
            }

            if (spScene)
            {
                m_spTimelineController.updateTimelinesUI(timelines);
                m_spIconesController.defineIconicScenario(iconicScenarioList);
                m_spTimeDisplayController.updateTimelinesUI(timelines);
            }
            else
            {
                m_mpTimelineController.updateTimelinesUI(timelines);
                m_mpIconesController.defineIconicScenario(iconicScenarioList);
                m_mpTimeDisplayController.updateTimelinesUI(timelines);
            }
        }

        /// <summary>
        /// Update the timeline current time for the controllers
        /// </summary>
        /// <param name="spScene"></param>
        public void updateTime(bool spScene)
        {
            Column3DViewManager cm = spScene ? m_scenesManager.SPScene.CM : m_scenesManager.MPScene.CM;
            int time = cm.globalTimeline ? (int)cm.commonTimelineValue : ((Column3DViewIEEG)cm.currentColumn()).columnTimeLineID;

            if (spScene)
            {                
                m_spIconesController.setTime(cm.globalTimeline, cm.idSelectedColumn, time);
                m_spTimeDisplayController.updateTime(cm.idSelectedColumn, time, cm.globalTimeline);
            }
            else
            {
                m_mpIconesController.setTime(cm.globalTimeline, cm.idSelectedColumn, time);
                m_mpTimeDisplayController.updateTime(cm.idSelectedColumn, time, cm.globalTimeline);
            }
        }

        /// <summary>
        /// Update the info of the current displayed plot window (TEMP)
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="plotInfo"></param>
        public void udpateDisplayPlotInfo(bool spScene, plotInfo plotInfo)
        {
            string currentTime = getTimeOfTimeline(spScene);

            m_bothDisplayPlotInfoTransform.gameObject.SetActive(plotInfo.enabled);

            if (plotInfo.enabled)
            {
                m_bothDisplayPlotInfoTransform.position = plotInfo.position;
                m_bothDisplayPlotInfoTransform.Find("name text").gameObject.GetComponent<Text>().text = plotInfo.name;

                if(plotInfo.isIRMF)
                {
                    m_bothDisplayPlotInfoTransform.Find("value text").gameObject.GetComponent<Text>().text = "IRMF";
                    m_bothDisplayPlotInfoTransform.Find("latency text").gameObject.GetComponent<Text>().text = "...";
                    return;
                }

                if (plotInfo.displayLatencies)
                {
                    m_bothDisplayPlotInfoTransform.Find("value text").gameObject.GetComponent<Text>().text = "Height : " + plotInfo.height;
                    m_bothDisplayPlotInfoTransform.Find("latency text").gameObject.GetComponent<Text>().text = "Latency : " + plotInfo.latency;
                    return;
                }

                m_bothDisplayPlotInfoTransform.Find("value text").gameObject.GetComponent<Text>().text = "Amp : " + plotInfo.amplitude;
                m_bothDisplayPlotInfoTransform.Find("latency text").gameObject.GetComponent<Text>().text = "Time : " + currentTime;                         
            }
        }

        /// <summary>
        /// Return timeline time
        /// </summary>
        /// <param name="spScene"></param>
        /// <returns></returns>
        public string getTimeOfTimeline(bool spScene)
        {
            if (spScene)
            {
                return m_spTimelineController.getTime();
            }

            return m_mpTimelineController.getTime(); 
        }
    }
}