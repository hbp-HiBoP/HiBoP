using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using HBP.Module3D;

namespace HBP.UI.Module3D
{
    /// <summary>
    /// A class for managing all the scenes overlay UI panels
    /// </summary>
    public class UIOverlayManager : MonoBehaviour
    {
        #region Properties
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

        #endregion

        #region Private Methods

        void Awake()
        {
            int idScript = TimeExecution.get_ID();
            TimeExecution.start_awake(idScript, TimeExecution.ScriptsId.UIOverlayManager);
            
            // retrieve contollers 
            //  planes
            m_spPlanesController = transform.Find("SP").GetComponent<PlanesController>();
            m_mpPlanesController = transform.Find("MP").GetComponent<PlanesController>();
            //  timeline
            m_spTimelineController = transform.Find("SP").GetComponent<TimelineController>();
            m_mpTimelineController = transform.Find("MP").GetComponent<TimelineController>();
            // time
            m_spTimeDisplayController = transform.Find("SP").GetComponent<TimeDisplayController>();
            m_mpTimeDisplayController = transform.Find("MP").GetComponent<TimeDisplayController>();
            //  icones
            m_spIconesController = transform.Find("SP").GetComponent<IconesController>();
            m_mpIconesController = transform.Find("MP").GetComponent<IconesController>();
            //  image cuts
            m_bothDisplayImageCutController = transform.Find("both").GetComponent<CutsDisplayController>();
            //  minimize
            m_bothMinimizeController = transform.Find("both").GetComponent<MinimizeController>();
            // colormap
            m_bothColorMapController = transform.Find("both").GetComponent<ColormapController>();
            //  screen message
            m_bothScreenMessageController = transform.Find("both").GetComponent<ScreenMessageController>();

            TimeExecution.end_awake(idScript, TimeExecution.ScriptsId.UIOverlayManager, gameObject);
        }

        void Update()
        {
            update_UI_position();
        }

        #endregion

        #region Public Methods
        /// <summary>
        /// Init the overlay manager.
        /// </summary>
        /// <param name="cameraManager"></param>
        /// <param name="singlePatientScene"></param>
        /// <param name="multiPatientsScene"></param>
        public void Initialize(ScenesManager scenesManager)
        {
            // find elements UI
            m_bothDisplaySiteInfoTransform = transform.FindChild("others").FindChild("display site info panel");

            // init controllers
            /*
            //  planes 
            m_spPlanesController.Initialize(scenesManager.SinglePatientScene, scenesManager.CamerasManager);
            m_mpPlanesController.Initialize(scenesManager.MultiPatientsScene, scenesManager.CamerasManager);
            //  timeline
            m_spTimelineController.init(scenesManager.SinglePatientScene, scenesManager.CamerasManager);
            m_mpTimelineController.init(scenesManager.MultiPatientsScene, scenesManager.CamerasManager);
            // time
            m_spTimeDisplayController.init(scenesManager.SinglePatientScene, scenesManager.CamerasManager);
            m_mpTimeDisplayController.init(scenesManager.MultiPatientsScene, scenesManager.CamerasManager);
            //  icones
            m_spIconesController.init(scenesManager.SinglePatientScene, scenesManager.CamerasManager);
            m_mpIconesController.init(scenesManager.MultiPatientsScene, scenesManager.CamerasManager);
            */
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
            SetiEEGColumnsNb(SceneType.SinglePatient, null);
            //  mp
            SetiEEGColumnsNb(SceneType.MultiPatients, null);

            // set listeners
            m_bothMinimizeController.m_minimizeStateSwitchEvent.AddListener((spScene) =>
            {
                // controllers to update when a change in the minimize states occurs
                m_bothColorMapController.UpdateUI();
            });

            /*
            // listeners
            scenesManager.SinglePatientScene.UpdateTimeInUI.AddListener(() =>
            {
                update_time(true);
            });
            scenesManager.MultiPatientsScene.UpdateTimeInUI.AddListener(() =>
            {
                update_time(false);
            });


            scenesManager.SinglePatientScene.UpdateDisplayedSitesInfo.AddListener((siteInfo) =>
            {
                udpate_display_site_infos(true, siteInfo);
            });
            scenesManager.MultiPatientsScene.UpdateDisplayedSitesInfo.AddListener((siteInfo) =>
            {
                udpate_display_site_infos(false, siteInfo);
            });
            */
            m_scenesManager = scenesManager;
            m_initialized = true;
        }

        /// <summary>
        /// Set the scene visibility for all the overlays
        /// </summary>
        /// <param name="visible"></param>
        /// <param name="spScene"></param>
        public void set_overlay_scene_visibility(bool visible, SceneType type)
        {
            switch (type)
            {
                case SceneType.SinglePatient:
                    m_bothMinimizeController.IsVisibleFromSinglePatientScene = visible;
                    m_bothDisplayImageCutController.update_UI_visibility(true, visible);
                    m_bothScreenMessageController.IsVisibleFromSinglePatientScene = visible;
                    m_spPlanesController.IsVisibleFromScene = visible;
                    m_spTimelineController.IsVisibleFromScene = visible;
                    m_spIconesController.IsVisibleFromScene = visible;
                    m_spTimeDisplayController.IsVisibleFromScene = visible;
                    break;
                case SceneType.MultiPatients:
                    m_bothMinimizeController.IsVisibleFromMultiPatientsScene = visible;
                    m_bothDisplayImageCutController.update_UI_visibility(false, visible);
                    m_bothScreenMessageController.IsVisibleFromMultiPatientsScene = visible;
                    m_mpPlanesController.IsVisibleFromScene = visible;
                    m_mpTimelineController.IsVisibleFromScene = visible;
                    m_mpIconesController.IsVisibleFromScene = visible;
                    m_mpTimeDisplayController.IsVisibleFromScene = visible;
                    break;
                default:
                    break;
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
                    switch (mode.Type)
                    {
                        case SceneType.SinglePatient:
                            m_spPlanesController.CurrentActivity = active;
                            m_spPlanesController.CurrentMode = mode;
                            break;
                        case SceneType.MultiPatients:
                            m_mpPlanesController.CurrentActivity = active;
                            m_mpPlanesController.CurrentMode = mode;
                            break;
                        default:
                            break;
                    }
                    break;
                case 1:
                    switch (mode.Type)
                    {
                        case SceneType.SinglePatient:
                            m_spTimelineController.CurrentActivity = active;
                            m_spTimelineController.CurrentMode = mode;
                            break;
                        case SceneType.MultiPatients:
                            m_mpTimelineController.CurrentActivity = active;
                            m_mpTimelineController.CurrentMode = mode;
                            break;
                        default:
                            break;
                    }
                    break;
                case 2:
                    switch (mode.Type)
                    {
                        case SceneType.SinglePatient:
                            m_spIconesController.CurrentActivity = active;
                            m_spIconesController.CurrentMode = mode;
                            break;
                        case SceneType.MultiPatients:
                            m_mpIconesController.CurrentActivity = active;
                            m_mpIconesController.CurrentMode = mode;
                            break;
                        default:
                            break;
                    }
                    break;
                case 3:
                    //m_bothDisplayImageCutController.setUIActivity(active, mode);
                    break;
                case 4:
                    m_bothColorMapController.SetActivity(active, mode);
                    break;
                case 5:
                    m_bothMinimizeController.SetActivity(active, mode);
                    break;
                case 6:
                    switch (mode.Type)
                    {
                        case SceneType.SinglePatient:
                            m_spTimeDisplayController.CurrentActivity = active;
                            m_spTimeDisplayController.CurrentMode = mode;
                            break;
                        case SceneType.MultiPatients:
                            m_mpTimeDisplayController.CurrentActivity = active;
                            m_mpTimeDisplayController.CurrentMode = mode;
                            break;
                        default:
                            break;
                    }
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
            m_bothMinimizeController.UpdatePosition();
            m_bothColorMapController.UpdatePosition();
            m_spPlanesController.UpdatePosition();
            m_mpPlanesController.UpdatePosition();
            m_spTimelineController.UpdatePosition();
            m_mpTimelineController.UpdatePosition();
            m_spTimeDisplayController.UpdatePosition();
            m_mpTimeDisplayController.UpdatePosition();
            m_spIconesController.UpdatePosition();
            m_mpIconesController.UpdatePosition();
            m_bothScreenMessageController.UpdatePosition();
        }

        /// <summary>
        /// Check if a click occurs on the overlay ui
        /// </summary>
        /// <param name="spScene"></param>
        /// <returns></returns>
        public bool check_if_click_on_overlay(SceneType type)
        {
            bool onTimelineOverlayClick = check_if_click_on_timeline(type);
            bool onChoosePlaneOverlayClick = check_if_click_on_chosse_plane(type);
            bool onMinimizeButtonOverlayClick =check_if_click_on_minimize_button(type);
            bool onColormapOverlayClick = check_if_click_on_colormap(type);
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
        private bool check_if_click_on_colormap(SceneType type)
        {
            Vector3 mousePosition = Input.mousePosition;
            for (int ii = 0; ii < m_bothColorMapController.columns_number(type); ++ii)
                if (check_if_click_on_rect(mousePosition, m_bothColorMapController.colormap_rect_transform(type, ii)))
                    return true;

            return false;
        }

        /// <summary>
        /// Check if a click occurs on the minimize buttons controller ui
        /// </summary>
        /// <param name="spScene"></param>
        /// <returns></returns>
        private bool check_if_click_on_minimize_button(SceneType type)
        {
            Vector3 mousePosition = Input.mousePosition;
            for (int ii = 0; ii < m_bothMinimizeController.columns_nb(type); ++ii)
                if (check_if_click_on_rect(mousePosition, m_bothMinimizeController.minimized_button_rectT(type, ii)) )
                    return true;

            return false;
        }

        /// <summary>
        /// Check if a click occurs on the timeline controller ui
        /// </summary>
        /// <param name="spScene"></param>
        /// <returns></returns>
        private bool check_if_click_on_timeline(SceneType type)
        {
            RectTransform rectTransform;
            switch (type)
            {
                case SceneType.SinglePatient:
                    rectTransform = m_spTimelineController.getTimelineRectT();
                    break;
                case SceneType.MultiPatients:
                    rectTransform = m_mpTimelineController.getTimelineRectT();
                    break;
                default:
                    rectTransform = null;
                    break;
            }
            return check_if_click_on_rect(Input.mousePosition, rectTransform);
        }

        /// <summary>
        /// Check if a click occurs on the choos plane controller ui
        /// </summary>
        /// <param name="spScene"></param>
        /// <returns></returns>
        private bool check_if_click_on_chosse_plane(SceneType type)
        {
            RectTransform rectT = null;
            Vector3 mousePosition = Input.mousePosition;

            switch (type)
            {
                case SceneType.SinglePatient:
                    rectT = m_spPlanesController.getPlanesChooseRectT();
                    break;
                case SceneType.MultiPatients:
                    rectT = m_mpPlanesController.getPlanesChooseRectT();
                    break;
                default:
                    break;
            }

            if (check_if_click_on_rect(mousePosition, rectT))
                return true;

            switch (type)
            {
                case SceneType.SinglePatient:
                    rectT = m_spPlanesController.getSetPlanesRectT();
                    break;
                case SceneType.MultiPatients:
                    rectT = m_mpPlanesController.getSetPlanesRectT();
                    break;
                default:
                    break;
            }

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
        public void update_columns_name(SceneType type, int columnId, string name)
        { 
            if(m_bothMinimizeController != null)
                m_bothMinimizeController.set_column_name(type, columnId, name);            
        }

        /// <summary>
        /// Update the focused selected column of a scene for all concerned ui
        /// </summary>
        /// <param name="spScene"> is a single patient scene </param>
        /// <param name="columnId"> id column </param>
        public void UpdateFocusedSceneAndColumn(bool spScene, int columnId)
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
        public void SetiEEGColumnsNb(SceneType type, List<HBP.Data.Visualization.Column> columnsData)
        {
            int nbColumns = (columnsData != null  ? columnsData.Count : 1);
            switch (type)
            {
                case SceneType.SinglePatient:
                    m_bothMinimizeController.update_SP_minimize_button_nb(nbColumns);
                    m_bothColorMapController.update_SP_column_number(nbColumns);
                    m_spIconesController.adapt_icons_nb(nbColumns);
                    m_spTimelineController.updateTimelinesNumber(nbColumns);
                    m_spTimeDisplayController.adaptNumber(nbColumns);
                    m_spIeegColNb = nbColumns;
                    break;
                case SceneType.MultiPatients:
                    m_bothMinimizeController.update_MP_minimize_button_nb(nbColumns);
                    m_bothColorMapController.update_MP_column_number(nbColumns);
                    m_mpIconesController.adapt_icons_nb(nbColumns);
                    m_mpTimelineController.updateTimelinesNumber(nbColumns);
                    m_mpTimeDisplayController.adaptNumber(nbColumns);
                    m_mpIeegColNb = nbColumns;
                    break;
                default:
                    break;
            }        

            // boths
            if (columnsData != null)
            {
                update_iEEG_columns_data_UI(type, columnsData);

                // update names
                for (int ii = 0; ii < nbColumns; ++ii)
                {
                    update_columns_name(type, ii, columnsData[ii].DataLabel);
                }
            }
        }

        /// <summary>
        /// Add an IMRF column overlay ui to the input scene
        /// </summary>
        /// <param name="spScene"></param>
        public void AddfMRIColumn(SceneType type, string label)
        {
            int idCol;
            switch (type)
            {
                case SceneType.SinglePatient:
                    m_bothMinimizeController.add_SP_minimize_button();
                    m_spFmriColNb++;
                    idCol = m_spIeegColNb + m_spFmriColNb - 1;
                    break;
                case SceneType.MultiPatients:
                    m_bothMinimizeController.add_MP_minimize_button();
                    m_mpFmriColNb++;
                    idCol = m_mpIeegColNb + m_mpFmriColNb - 1;
                    break;
                default:
                    idCol = -1;
                    break;
            }
            update_columns_name(type, idCol, label);
        }

        /// <summary>
        /// Remove an IRMF column overlay ui from the input scene
        /// </summary>
        /// <param name="spScene"></param>
        public void RemoveLastfMRIColumn(SceneType type)
        {
            switch (type)
            {
                case SceneType.SinglePatient:
                    m_bothMinimizeController.remove_SP_minimize_button();
                    m_spFmriColNb--;
                    break;
                case SceneType.MultiPatients:
                    m_bothMinimizeController.remove_MP_minimize_button();
                    m_mpFmriColNb--;
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Update the controllers with column data
        /// </summary>
        /// <param name="spScene"></param>
        /// <param name="iEEGDataCols"></param>
        private void update_iEEG_columns_data_UI(SceneType type, List<Data.Visualization.Column> iEEGDataCols)
        {
            // timeline controller
            List<Data.Visualization.Timeline> timelines = new List<Data.Visualization.Timeline>(iEEGDataCols.Count);
            for (int ii = 0; ii < iEEGDataCols.Count; ++ii)
            {
                timelines.Add(iEEGDataCols[ii].TimeLine);
            }

            // icon controller
            List<HBP.Data.Visualization.IconicScenario> iconicScenarioList = new List<Data.Visualization.IconicScenario>();
            for (int ii = 0; ii < iEEGDataCols.Count; ++ii)
                iconicScenarioList.Add(iEEGDataCols[ii].IconicScenario);

            switch (type)
            {
                case SceneType.SinglePatient:
                    m_spTimelineController.updateTimelinesUI(timelines);
                    m_spIconesController.define_iconic_scenario(iconicScenarioList);
                    m_spTimeDisplayController.updateTimelinesUI(timelines);
                    break;
                case SceneType.MultiPatients:
                    m_mpTimelineController.updateTimelinesUI(timelines);
                    m_mpIconesController.define_iconic_scenario(iconicScenarioList);
                    m_mpTimeDisplayController.updateTimelinesUI(timelines);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Update the timeline current time for the controllers
        /// </summary>
        /// <param name="spScene"></param>
        public void update_time(Base3DScene scene)
        {
            Column3DViewManager cm = scene.Column3DViewManager;
            int time = cm.GlobalTimeline ? (int)cm.CommonTimelineValue : ((Column3DViewIEEG)cm.SelectedColumn).ColumnTimeLineID;

            switch (scene.Type)
            {
                case SceneType.SinglePatient:
                    m_spIconesController.set_time(cm.GlobalTimeline, cm.SelectedColumnID, time);
                    m_spTimeDisplayController.updateTime(cm.SelectedColumnID, time, cm.GlobalTimeline);
                    break;
                case SceneType.MultiPatients:
                    m_mpIconesController.set_time(cm.GlobalTimeline, cm.SelectedColumnID, time);
                    m_mpTimeDisplayController.updateTime(cm.SelectedColumnID, time, cm.GlobalTimeline);
                    break;
                default:
                    break;
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
                    m_bothDisplaySiteInfoTransform.Find("mars atlas name text").GetComponent<Text>().text = "Mars atlas : " + (siteInfo.site.Information.MarsAtlasIndex == -1 ? "not found" : GlobalGOPreloaded.MarsAtlasIndex.FullName(siteInfo.site.Information.MarsAtlasIndex));
                    m_bothDisplaySiteInfoTransform.Find("broadman name text").GetComponent<Text>().text = "Broadman : " + (siteInfo.site.Information.MarsAtlasIndex == -1 ? "not found" :  GlobalGOPreloaded.MarsAtlasIndex.BroadmanArea(siteInfo.site.Information.MarsAtlasIndex));
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
        #endregion
    }
}