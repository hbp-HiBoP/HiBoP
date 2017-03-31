
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


        private int m_spIeegColNb = 0;
        private int m_mpIeegColNb = 0;

        private int m_spFmriColNb = 0;
        private int m_mpFmriColNb = 0;
        


        // private
        private ScenesManager m_scenesManager = null; /**< scenes manager */


        // transform
        public Transform m_bothDisplaySiteInfoTransform = null; // TEMP

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
            int idScript = TimeExecution.get_ID();
            TimeExecution.start_awake(idScript, TimeExecution.ScriptsId.UIOverlayManager);
            
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

            TimeExecution.end_awake(idScript, TimeExecution.ScriptsId.UIOverlayManager, gameObject);
        }

        void Update()
        {
            update_UI_position();
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
            m_bothDisplaySiteInfoTransform = m_canvas.transform.Find("others").Find("display site info panel");

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
            set_iEEG_columns_nb(true, null);
            //  mp
            set_iEEG_columns_nb(false, null);

            // set listeners
            m_bothMinimizeController.m_minimizeStateSwitchEvent.AddListener((spScene) =>
            {
                // controllers to update when a change in the minimize states occurs
                m_bothColorMapController.update_UI();
            });

            // listeners
            scenesManager.SPScene.UpdateTimeInUI.AddListener(() =>
            {
                update_time(true);
            });
            scenesManager.MPScene.UpdateTimeInUI.AddListener(() =>
            {
                update_time(false);
            });


            scenesManager.SPScene.UpdateDisplayedSitesInfo.AddListener((siteInfo) =>
            {
                udpate_display_site_infos(true, siteInfo);
            });
            scenesManager.MPScene.UpdateDisplayedSitesInfo.AddListener((siteInfo) =>
            {
                udpate_display_site_infos(false, siteInfo);
            });

            m_scenesManager = scenesManager;
            m_initialized = true;
        }

        /// <summary>
        /// Set the scene visibility for all the overlays
        /// </summary>
        /// <param name="active"></param>
        /// <param name="spScene"></param>
        public void set_overlay_scene_visibility(bool active, bool spScene)
        {
            m_bothMinimizeController.set_UI_visibility_from_scene(spScene, active);
            m_bothColorMapController.set_UI_visibility_from_scene(spScene, active);

            m_bothDisplayImageCutController.update_UI_visibility(spScene, active);

            m_bothScreenMessageController.set_UI_visibility_from_scene(spScene, active);

            if (spScene)
            {
                m_spPlanesController.set_UI_visibility_from_scene(active);
                m_spTimelineController.set_UI_visibility_from_scene(active);
                m_spIconesController.set_UI_visibility_from_scene(active);
                m_spTimeDisplayController.set_UI_visibility_from_scene(active);
            }
            else
            {
                m_mpPlanesController.set_UI_visibility_from_scene(active);
                m_mpTimelineController.set_UI_visibility_from_scene(active);
                m_mpIconesController.set_UI_visibility_from_scene(active);
                m_mpTimeDisplayController.set_UI_visibility_from_scene(active);
            }
        }

        /// <summary>
        /// Set a specific activity for an overlay
        /// </summary>
        /// <param name="active"></param>
        /// <param name="spScene"></param>
        /// <param name="idOverlay"></param>
        /// <param name="mode"></param>
        public void set_specific_overlay_active(bool active, int idOverlay, Mode mode)
        {
            switch (idOverlay)
            {
                // TODO : m_bothScreenMessageController
                case 0:
                    if (mode.m_sceneSp)
                        m_spPlanesController.set_UI_activity(active, mode);
                    else
                        m_mpPlanesController.set_UI_activity(active, mode);
                    break;
                case 1:
                    if (mode.m_sceneSp)
                        m_spTimelineController.set_UI_activity(active, mode);
                    else
                        m_mpTimelineController.set_UI_activity(active, mode);
                    break;
                case 2:
                    if (mode.m_sceneSp)
                        m_spIconesController.set_UI_activity(active, mode);
                    else
                        m_mpIconesController.set_UI_activity(active, mode);
                    break;
                case 3:
                    //m_bothDisplayImageCutController.setUIActivity(active, mode);
                    break;
                case 4:
                    m_bothColorMapController.set_UI_activity(active, mode);
                    break;
                case 5:
                    m_bothMinimizeController.set_UI_activity(active, mode);
                    break;
                case 6:
                    if (mode.m_sceneSp)
                        m_spTimeDisplayController.set_UI_activity(active, mode);
                    else
                        m_mpTimeDisplayController.set_UI_activity(active, mode);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Update the UI poistion of all the overlays
        /// </summary>
        public void update_UI_position()
        {
            if (!m_initialized)
                return;

            m_bothDisplayImageCutController.update_UI_position();
            m_bothMinimizeController.update_UI_position();
            m_bothColorMapController.update_UI_position();
            m_spPlanesController.update_UI_position();
            m_mpPlanesController.update_UI_position();
            m_spTimelineController.update_UI_position();
            m_mpTimelineController.update_UI_position();
            m_spTimeDisplayController.update_UI_position();
            m_mpTimeDisplayController.update_UI_position();
            m_spIconesController.update_UI_position();
            m_mpIconesController.update_UI_position();
            m_bothScreenMessageController.update_UI_position();
        }

        /// <summary>
        /// Check if a click occurs on the overlay ui
        /// </summary>
        /// <param name="spScene"></param>
        /// <returns></returns>
        public bool check_if_click_on_overlay(bool spScene)
        {
            bool onTimelineOverlayClick = check_if_click_on_timeline(spScene);
            bool onChoosePlaneOverlayClick = check_if_click_on_chosse_plane(spScene);
            bool onMinimizeButtonOverlayClick =check_if_click_on_minimize_button(spScene);
            bool onColormapOverlayClick = check_if_click_on_colormap(spScene);
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
        private bool check_if_click_on_rect(Vector3 mousePosition, RectTransform rectT)
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
        private bool check_if_click_on_colormap(bool spScene)
        {
            Vector3 mousePosition = Input.mousePosition;
            for (int ii = 0; ii < m_bothColorMapController.columns_number(spScene); ++ii)
                if (check_if_click_on_rect(mousePosition, m_bothColorMapController.colormap_rect_transform(spScene, ii)))
                    return true;

            return false;
        }

        /// <summary>
        /// Check if a click occurs on the minimize buttons controller ui
        /// </summary>
        /// <param name="spScene"></param>
        /// <returns></returns>
        private bool check_if_click_on_minimize_button(bool spScene)
        {
            Vector3 mousePosition = Input.mousePosition;
            for (int ii = 0; ii < m_bothMinimizeController.columns_nb(spScene); ++ii)
                if (check_if_click_on_rect(mousePosition, m_bothMinimizeController.minimized_button_rectT(spScene, ii)) )
                    return true;

            return false;
        }

        /// <summary>
        /// Check if a click occurs on the timeline controller ui
        /// </summary>
        /// <param name="spScene"></param>
        /// <returns></returns>
        private bool check_if_click_on_timeline(bool spScene)
        {
            return check_if_click_on_rect(Input.mousePosition, spScene ? m_spTimelineController.getTimelineRectT() : m_mpTimelineController.getTimelineRectT());
        }

        /// <summary>
        /// Check if a click occurs on the choos plane controller ui
        /// </summary>
        /// <param name="spScene"></param>
        /// <returns></returns>
        private bool check_if_click_on_chosse_plane(bool spScene)
        {
            RectTransform rectT = null;
            Vector3 mousePosition = Input.mousePosition;

            if (spScene)
                rectT = m_spPlanesController.getPlanesChooseRectT();
            else
                rectT = m_mpPlanesController.getPlanesChooseRectT();

            if (check_if_click_on_rect(mousePosition, rectT))
                return true;

            if (spScene)
                rectT = m_spPlanesController.getSetPlanesRectT();
            else
                rectT = m_mpPlanesController.getSetPlanesRectT();

            if (rectT == null)
                return false;

            return check_if_click_on_rect(mousePosition, rectT);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="columnId"></param>
        /// <param name="name"></param>
        public void update_columns_name(bool spScene, int columnId, string name)
        { 
            if(m_bothMinimizeController != null)
                m_bothMinimizeController.set_column_name(spScene, columnId, name);            
        }

        /// <summary>
        /// Update the focused selected column of a scene for all concerned ui
        /// </summary>
        /// <param name="spScene"> is a single patient scene </param>
        /// <param name="columnId"> id column </param>
        public void update_focused_scene_and_column(bool spScene, int columnId)
        {
            if (spScene)
            {
                m_spTimelineController.setCurrentTimeline(columnId);
            }
            else
            {
                m_mpTimelineController.setCurrentTimeline(columnId);
            }

            m_bothDisplayImageCutController.set_scene_to_display(spScene);
        }


        /// <summary>
        /// Update the current IEEG columns number for all concerned ui overlays
        /// </summary>
        /// <param name="spScene"> is a single patient scene </param>
        /// <param name="nbColumns"> columns nb</param>
        public void set_iEEG_columns_nb(bool spScene, List<HBP.Data.Visualisation.ColumnData> columnsData)
        {
            int nbColumns = (columnsData != null  ? columnsData.Count : 1);
            if (spScene) // SP
            {
                m_bothMinimizeController.update_SP_minimize_button_nb(nbColumns);
                m_bothColorMapController.update_SP_column_number(nbColumns);
                m_spIconesController.adapt_icons_nb(nbColumns);
                m_spTimelineController.updateTimelinesNumber(nbColumns);
                m_spTimeDisplayController.adaptNumber(nbColumns);
                m_spIeegColNb = nbColumns;
            }
            else // MP
            {
                m_bothMinimizeController.update_MP_minimize_button_nb(nbColumns);
                m_bothColorMapController.update_MP_column_number(nbColumns);
                m_mpIconesController.adapt_icons_nb(nbColumns);
                m_mpTimelineController.updateTimelinesNumber(nbColumns);
                m_mpTimeDisplayController.adaptNumber(nbColumns);
                m_mpIeegColNb = nbColumns;
            }            

            // boths
            if (columnsData != null)
            {
                update_iEEG_columns_data_UI(spScene, columnsData);

                // update names
                for (int ii = 0; ii < nbColumns; ++ii)
                {
                    update_columns_name(spScene, ii, columnsData[ii].Label);
                }
            }
        }

        /// <summary>
        /// Add an IMRF column overlay ui to the input scene
        /// </summary>
        /// <param name="spScene"></param>
        public void add_fMRI_column(bool spScene, string label)
        {
            int idCol;
            if (spScene)
            {
                m_bothMinimizeController.add_SP_minimize_button();
                m_spFmriColNb++;
                idCol = m_spIeegColNb + m_spFmriColNb - 1;
            }
            else
            {
                m_bothMinimizeController.add_MP_minimize_button();
                m_mpFmriColNb++;
                idCol = m_mpIeegColNb + m_mpFmriColNb - 1;
            }

            update_columns_name(spScene, idCol, label);
        }

        /// <summary>
        /// Remove an IRMF column overlay ui from the input scene
        /// </summary>
        /// <param name="spScene"></param>
        public void remove_last_fMRI_column(bool spScene)
        {
            if (spScene)
            {
                m_bothMinimizeController.remove_SP_minimize_button();
                m_spFmriColNb--;
            }
            else
            {
                m_bothMinimizeController.remove_MP_minimize_button();
                m_mpFmriColNb--;
            }
        }

        /// <summary>
        /// Update the controllers with column data
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="iEEGDataCols"></param>
        private void update_iEEG_columns_data_UI(bool spScene, List<HBP.Data.Visualisation.ColumnData> iEEGDataCols)
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
                iconicScenarioList.Add(iEEGDataCols[ii].IconicScenario);

            if (spScene)
            {
                m_spTimelineController.updateTimelinesUI(timelines);
                m_spIconesController.define_iconic_scenario(iconicScenarioList);
                m_spTimeDisplayController.updateTimelinesUI(timelines);
            }
            else
            {
                m_mpTimelineController.updateTimelinesUI(timelines);
                m_mpIconesController.define_iconic_scenario(iconicScenarioList);
                m_mpTimeDisplayController.updateTimelinesUI(timelines);
            }
        }

        /// <summary>
        /// Update the timeline current time for the controllers
        /// </summary>
        /// <param name="spScene"></param>
        public void update_time(bool spScene)
        {
            Column3DViewManager cm = spScene ? m_scenesManager.SPScene.CM : m_scenesManager.MPScene.CM;
            int time = cm.globalTimeline ? (int)cm.commonTimelineValue : ((Column3DViewIEEG)cm.current_column()).columnTimeLineID;

            if (spScene)
            {                
                m_spIconesController.set_time(cm.globalTimeline, cm.idSelectedColumn, time);
                m_spTimeDisplayController.updateTime(cm.idSelectedColumn, time, cm.globalTimeline);
            }
            else
            {
                m_mpIconesController.set_time(cm.globalTimeline, cm.idSelectedColumn, time);
                m_mpTimeDisplayController.updateTime(cm.idSelectedColumn, time, cm.globalTimeline);
            }
        }

        /// <summary>
        /// Update the info of the current displayed site window (TEMP)
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="siteInfo"></param>
        public void udpate_display_site_infos(bool spScene, SiteInfo siteInfo)
        {
            string currentTime = get_time_of_timeline(spScene);

            m_bothDisplaySiteInfoTransform.gameObject.SetActive(siteInfo.enabled);

            if (siteInfo.enabled)
            {
                m_bothDisplaySiteInfoTransform.position = siteInfo.position;
                m_bothDisplaySiteInfoTransform.Find("name text").GetComponent<Text>().text = siteInfo.name;

                if (siteInfo.isFMRI)
                {
                    m_bothDisplaySiteInfoTransform.Find("value text").GetComponent<Text>().text = "";
                    m_bothDisplaySiteInfoTransform.Find("latency text").GetComponent<Text>().text = "";
                    return;
                }

                if (siteInfo.displayLatencies)
                {
                    m_bothDisplaySiteInfoTransform.Find("value text").GetComponent<Text>().text = "Height : " + siteInfo.height;
                    m_bothDisplaySiteInfoTransform.Find("latency text").GetComponent<Text>().text = "Latency : " + siteInfo.latency;
                    return;
                }

                m_bothDisplaySiteInfoTransform.Find("value text").GetComponent<Text>().text = "iEEG : " + siteInfo.amplitude;
                m_bothDisplaySiteInfoTransform.Find("latency text").GetComponent<Text>().text = "Time : " + currentTime;

                if (siteInfo.site != null)
                {
                    m_bothDisplaySiteInfoTransform.Find("mars atlas name text").GetComponent<Text>().text = "Mars atlas : " + (siteInfo.site.labelMarsAtlas == -1 ? "not found" : GlobalGOPreloaded.MarsAtlasIndex.full_name(siteInfo.site.labelMarsAtlas));
                    m_bothDisplaySiteInfoTransform.Find("broadman name text").GetComponent<Text>().text = "Broadman : " + (siteInfo.site.labelMarsAtlas == -1 ? "not found" :  GlobalGOPreloaded.MarsAtlasIndex.broadman_area(siteInfo.site.labelMarsAtlas));
                }
            }
        }

        /// <summary>
        /// Return timeline time
        /// </summary>
        /// <param name="spScene"></param>
        /// <returns></returns>
        public string get_time_of_timeline(bool spScene)
        {
            if (spScene)
            {
                return m_spTimelineController.getTime();
            }

            return m_mpTimelineController.getTime(); 
        }
    }
}