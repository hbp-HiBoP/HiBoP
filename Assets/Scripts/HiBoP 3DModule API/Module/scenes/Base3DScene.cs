
/**
 * \file    Base3DScene.cs
 * \author  Lance Florian
 * \date    2015
 * \brief   Define Base3DScene and ComputeGeneratorsJob classes
 */

// system
using System;
using System.Collections.Generic;
using System.Linq;

// unity
using UnityEngine;
using UnityEngine.Events;


namespace HBP.VISU3D
{
    /// <summary>
    /// Plot request to be send to the outside UI
    /// </summary>
    public struct SiteRequest
    {
        public bool spScene; /**< is a single patient scene ? */
        public int idSite1; /**< id of the first site */
        public int idSite2;  /**< id of second site */
        public string idPatient; /**< id of the patient corresponding to the first site */
        public string idPatient2;  /**< id of the patient corresponding to the second site*/
        public List<List<bool>> maskColumn; /**< masks of the sites :  dim[0] = columnds data nb, dim[1] = sites nb.  if true : the site is not excluded/blacklisted/column masked and is in a ROI (if there is at least one ROI, if not ROI defined, all the plots are considered inside a ROI) */

        public void display()
        {
            Debug.Log("plotRequest : " + spScene + " " + idSite1 + " " + idSite2 + " " + idPatient + " " + idPatient2 + " " + maskColumn.Count);
            if (maskColumn.Count > 0)
            {
                Debug.Log("size1 : " + maskColumn[0].Count);
                string mask = "";
                for (int ii = 0; ii < maskColumn[0].Count; ++ii)
                    mask += maskColumn[0][ii] + " ";
                Debug.Log("-> mask : " + mask);
            }
        }
    }

    /// <summary>
    /// Site info to be send to the UI
    /// </summary>
    public class SiteInfo
    {
        public SiteInfo(Site site, bool enabled, Vector3 position, bool isFMRI, bool displayLatencies = false, string name = "", string amplitude = "", string height = "", string latency = "")
        {
            this.site = site;
            this.enabled = enabled;
            this.position = position;
            this.isFMRI = isFMRI;
            this.displayLatencies = displayLatencies;
            this.name = name;
            this.amplitude = amplitude;
            this.height = height;
            this.latency = latency;
        }

        public Site site = null;

        public bool isFMRI;
        public bool enabled;
        public bool displayLatencies;

        public Vector3 position;

        public string name;
        public string amplitude;
        public string height;
        public string latency;
    }

    /// <summary>
    /// IRM cal values
    /// </summary>
    public struct MRICalValues
    {
        public float min;
        public float max;
        public float loadedCalMin;
        public float loadedCalMax;
        public float computedCalMin;
        public float computedCalMax;
    }

    /// <summary>
    /// IEEG sites parameters
    /// </summary>
    public struct iEEGSitesParameters
    {
        public int columnId;
        public float gain;
        public float maxDistance;
    }

    /// <summary>
    /// IEEG alpha parameters 
    /// </summary>
    public struct iEEGAlphaParameters
    {
        public int columnId;
        public float alphaMin;
        public float alphaMax;
    }

    /// <summary>
    /// IEEG threhsolds parameters
    /// </summary>
    public struct iEEGThresholdParameters
    {
        public int columnId;
        public float minSpan;
        public float middle;
        public float maxSpan;
    }


    /// <summary>
    /// IEEG data to be send to the UI
    /// </summary>
    public struct iEEGDataParameters
    {
        public int columnId;

        public float maxDistance;
        public float gain;

        public float minAmp;
        public float maxAmp;

        public float alphaMin;
        public float alphaMax;

        public float spanMin;
        public float middle;
        public float spanMax;        
    }

    /// <summary>
    /// IRMF data to be send to the UI
    /// </summary>
    public struct FMriDataParameters
    {
        public bool singlePatient;
        public int columnId;
        public float alpha;
        public float calMin; 
        public float calMax;
        public MRICalValues calValues;
    }

    namespace Events
    {
        /// <summary>
        /// UI event for sending a plot info request to the outside UI (params : plotRequest)
        /// </summary>
        [System.Serializable]
        public class InfoPlotRequest : UnityEvent<SiteRequest> { }

        /// <summary>
        /// Event for sending info in order to display a message in a scene screen (params : message, duration, width, height)
        /// </summary>
        public class DisplaySceneMessage : UnityEvent<string, float, int, int> { }

        /// <summary>
        /// Event for sending info in order to display a loadingbar in a scene screen (params : duration, width, height, value)
        /// </summary>
        public class DisplaySceneProgressBar : UnityEvent<float, int, int, float> { }

        /// <summary>
        /// Event of sending the current selected scene and column (params : spScene, id column)
        /// </summary>
        public class UpdateSceneAndColSelection : UnityEvent<bool, int> { }


        /// <summary>
        /// Event for colormap values associated to a column id (params : minValue, middle, maxValue, id)
        /// </summary>
        public class SendColormapEvent : UnityEvent<float, float, float, int> { }

        /// <summary>
        /// Event for sending IEEG data parameters to UI (params : IEEGDataParameters)
        /// </summary>
        public class SendIEEGParameters : UnityEvent<iEEGDataParameters> { }

        /// <summary>
        /// Event for sending IRMF data parameters to UI (params : IRMFDataParameters)
        /// </summary>
        public class SendFMRIParameters : UnityEvent<FMriDataParameters> { }

        /// <summary>
        /// Event for updating cuts planes 
        /// </summary>
        public class UpdatePlanes : UnityEvent { }

        /// <summary>
        /// Event for updating the planes cuts display in the cameras
        /// </summary>
        public class ModifyPlanesCuts : UnityEvent { }

        /// <summary>
        /// Event for updating the cuts images in the UI (params : textures, columnId, planeNb)
        /// </summary>
        public class UpdateCutsInUI : UnityEvent<List<Texture2D>, int, int> { }

        /// <summary>
        /// Send the new selected id column to the UI (params : idColumn)
        /// </summary>
        public class DefineSelectedColumn : UnityEvent<int> { }

        /// <summary>
        /// Event for updating time in the UI
        /// </summary>
        public class UpdateTimeInUI : UnityEvent { }

        /// <summary>
        /// 
        /// </summary>
        public class UpdateDisplayedSitesInfo : UnityEvent<SiteInfo> { }

        /// <summary>
        /// Ask the UI to udpate the ROI of all the scene columns
        /// </summary>
        public class AskROIUpdate : UnityEvent<int> { }

        /// <summary>
        /// Ask the camera manager to update the target for this scene
        /// </summary>
        public class UpdateCameraTarget : UnityEvent<Vector3> { }

        /// <summary>
        /// Occurs when a plot is clicked in the scene (params : id of the column, if = -1 use the current selected column id)
        /// </summary>
        public class ClickPlot : UnityEvent<int> { }

        /// <summary>
        /// Event for updating the IRM cal values in the UI
        /// </summary>
        public class IRMCalValuesUpdate : UnityEvent<MRICalValues> { }

    }

    /// <summary>
    /// Class containing all the DLL data and the displayable Gameobjects of a 3D scene.
    /// </summary>
    [AddComponentMenu("Scenes/Base 3D Scene")]
    abstract public class Base3DScene : MonoBehaviour
    {
        #region members                 

        // data        

        public bool singlePatient; /**< is a sp scene*/
        private bool m_displaySitesNames = false;
        private Camera m_lastCamera = null;

        private List<Plane> m_planesList = new List<Plane>();         /**< cut planes list */
        public List<Plane> planesList { get { return m_planesList; } }
        public SceneStatesInfo data_ = null; /**<  data of the scene */
        protected ModesManager modes = null; /**< modes of the scene */
        protected DisplayedObjects3DView go_ = null; /**< displayable objects of the scene */
        protected MNIObjects m_MNI = null;
        protected Column3DViewManager m_CM = null; /**< column data manager */
        public Column3DViewManager CM { get { return m_CM; } }
        public UIOverlayManager m_uiOverlayManager; /**< UI overlay manager of the scenes */

        protected TriEraser m_triEraser = new TriEraser();


        // threads / jobs
        protected ComputeGeneratorsJob m_computeGeneratorsJob = null; /**< generator computing job */

        // events
        public Events.UpdatePlanes UpdatePlanes = new Events.UpdatePlanes(); 
        public Events.ModifyPlanesCuts ModifyPlanesCuts = new Events.ModifyPlanesCuts();
        public Events.SendIEEGParameters SendIEEGParameters = new Events.SendIEEGParameters();
        public Events.SendFMRIParameters SendFMRIParameters = new Events.SendFMRIParameters();
        public Events.SendColormapEvent SendColorMapValues = new Events.SendColormapEvent();
        public Events.SendModeSpecifications SendModeSpecifications = new Events.SendModeSpecifications(); 
        public Events.DisplaySceneMessage display_screen_message_event = new Events.DisplaySceneMessage();
        public Events.DisplaySceneProgressBar display_scene_progressbar_event = new Events.DisplaySceneProgressBar();
        public Events.InfoPlotRequest PlotInfoRequest = new Events.InfoPlotRequest();
        public Events.UpdateCutsInUI UpdateCutsInUI = new Events.UpdateCutsInUI();
        public Events.DefineSelectedColumn DefineSelectedColumn = new Events.DefineSelectedColumn();
        public Events.UpdateTimeInUI UpdateTimeInUI = new Events.UpdateTimeInUI();
        public Events.UpdateDisplayedSitesInfo UpdateDisplayedSitesInfo = new Events.UpdateDisplayedSitesInfo();
        public Events.AskROIUpdate AskROIUpdateEvent = new Events.AskROIUpdate();
        public Events.UpdateCameraTarget UpdateCameraTarget = new Events.UpdateCameraTarget();
        public Events.ClickPlot ClickSite = new Events.ClickPlot();
        public Events.IRMCalValuesUpdate IRMCalValuesUpdate = new Events.IRMCalValuesUpdate();
        

        #endregion members

        #region mono_behaviour

        /// <summary>
        /// This function is always called before any Start functions and also just after a prefab is instantiated. (If a GameObject is inactive during start up Awake is not called until it is made active.)
        /// </summary>
        protected void Awake()
        {         
            int idScript = TimeExecution.get_ID();
            TimeExecution.start_awake(idScript, TimeExecution.ScriptsId.Base3DScene);

            go_ = new DisplayedObjects3DView();
            data_ = new SceneStatesInfo();
            m_CM = GetComponent<Column3DViewManager>();
            singlePatient = (name == "SP");


            // set meshes layer
            data_.MeshesLayerName = singlePatient ? "Meshes_SP" : "Meshes_MP";

            // init modes            
            modes = transform.Find("modes").gameObject.GetComponent<ModesManager>();
            modes.init(this);
            modes.SendModeSpecifications.AddListener((specs) =>
            {
                SendModeSpecifications.Invoke(specs);

                // update scene visibility (useless)
                //UnityEngine.Profiling.Profiler.BeginSample("TEST-Base3DScene-SendModeSpecifications update_scene_items_visibility");
                //    update_scene_items_visibility(specs.itemMaskDisplay[0], specs.itemMaskDisplay[1], specs.itemMaskDisplay[2]);
                //UnityEngine.Profiling.Profiler.EndSample();
            });

            // init GO
            init_scene_GO();


            TimeExecution.end_awake(idScript, TimeExecution.ScriptsId.Base3DScene, gameObject);
        }


        /// <summary>
        /// Update is called once per frame. It is the main workhorse function for frame updates.
        /// </summary>
        protected void Update()
        {
            UnityEngine.Profiling.Profiler.BeginSample("TEST-Base3DScene-Update: set_current_mode_specifications");
            set_current_mode_specifications();
            UnityEngine.Profiling.Profiler.EndSample();

            if (modes.currentIdMode() == Mode.ModesId.NoPathDefined)
                return;

            UnityEngine.Profiling.Profiler.BeginSample("TEST-Base3DScene-Update");

            // TEMP : useless
            for (int ii = 0; ii < data_.removeFrontPlaneList.Count; ++ii)
                data_.removeFrontPlaneList[ii] = 0;

            // check if we must perform new cuts of the brain            
            if (data_.updateCutMeshGeometry)
            {
                data_.geometryUpToDate = false;
                CM.planesCutsCopy = new List<Plane>();
                for (int ii = 0; ii < m_planesList.Count; ++ii)
                    CM.planesCutsCopy.Add(new Plane(m_planesList[ii].point, m_planesList[ii].normal));

                UnityEngine.Profiling.Profiler.BeginSample("TEST-Base3DScene-Update compute_meshes_cuts 1");
                compute_meshes_cuts();
                UnityEngine.Profiling.Profiler.EndSample();
                UnityEngine.Profiling.Profiler.BeginSample("TEST-Base3DScene-Update compute_MRI_textures 1");

                // create textures 
                compute_MRI_textures();

                compute_FMRI_textures(-1, -1);

                data_.geometryUpToDate = true;
                

                UnityEngine.Profiling.Profiler.EndSample();
            }


            // check job state
            if (m_computeGeneratorsJob != null)
            {
                float currState;
                data_.rwl.AcquireReaderLock(1000);
                currState = data_.currentComputingState;
                data_.rwl.ReleaseReaderLock();

                display_sceen_message("Computing...", 50f, 250, 40);
                display_progressbar(currState, 50f, 250, 40);

                if (m_computeGeneratorsJob.Update())
                {                    
                    finalize_generators_computing();
                    compute_IEEG_textures();                   
                }
            }

            UnityEngine.Profiling.Profiler.EndSample();
        }

        protected void LateUpdate()
        {
            m_displaySitesNames = false;
        }

        void OnGUI()
        {
            if (m_displaySitesNames)
            {
                for (int ii = 0; ii < m_CM.current_column().plotsGO.Count; ++ii)
                    for (int jj = 0; jj < m_CM.current_column().plotsGO[ii].Count; ++jj)
                        for (int kk = 0; kk < m_CM.current_column().plotsGO[ii][jj].Count; ++kk)
                        {
                            GUI.contentColor =  (kk == 0) ? Color.red : Color.black;
                            Site site = m_CM.current_column().plotsGO[ii][jj][kk].GetComponent<Site>();
                            if (site.columnROI || site.exclude || site.blackList)
                                continue;

                            Vector3 pos = m_lastCamera.WorldToScreenPoint(site.transform.position);
                            Vector2 realPos = new Vector2(pos.x, Screen.height - pos.y);
                            if(m_lastCamera.pixelRect.Contains(realPos))                            
                                GUI.Label(new Rect(realPos.x -10, realPos.y, 100, 20), site.name);
                        }
            }
        }

        #endregion mono_behaviour   

        #region functions

        public abstract void compute_meshes_cuts();
        public abstract void send_additionnal_site_info_request(Site previousPlot = null);

        public void move_mouse_on_scene(Ray ray, Vector3 mousePosition, int idColumn)
        {
            // ...
        }

        public void display_sites_names(Camera camera)
        {

            m_displaySitesNames = true;
            m_lastCamera = camera;
        }

        private void finalize_generators_computing()
        {            
            // computing ended
            m_computeGeneratorsJob = null;

            // generators are now up to date
            data_.generatorUpToDate = true;
            data_.iEEGOutdated = false;

            // send inf values to overlays
            for (int ii = 0; ii < m_CM.IEEG_columns_nb(); ++ii)
            {
                float maxValue = Math.Max(Math.Abs(m_CM.IEEG_col(ii).sharedMinInf), Math.Abs(m_CM.IEEG_col(ii).sharedMaxInf));
                float minValue = -maxValue;
                minValue += m_CM.IEEG_col(ii).middle;
                maxValue += m_CM.IEEG_col(ii).middle;
                SendColorMapValues.Invoke(minValue, m_CM.IEEG_col(ii).middle, maxValue, ii);
            }

            // amplitudes are not displayed yet
            for (int ii = 0; ii < m_CM.IEEG_columns_nb(); ++ii)
                m_CM.IEEG_col(ii).updateIEEG = true;

            //####### CHECK ACCESS
            if (!modes.functionAccess(Mode.FunctionsId.post_updateGenerators))
            {
                Debug.LogError("-ERROR : Base3DScene::post_updateGenerators -> no acess for mode : " + modes.currentModeName());
            }
            //##################

            // update plots visibility
            m_CM.update_all_columns_sites_rendering(data_);

            display_sceen_message("Computing finished !", 1f, 250, 40);
            display_progressbar(1f, 1f, 250, 40);

            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.post_updateGenerators);
            //##################
        }

        /// <summary>
        /// Init gameobjects of the scene
        /// </summary>
        protected void init_scene_GO()
        {
            // init parents 
            go_.sitesMeshesParent = transform.Find("base electrodes").gameObject;
            go_.brainSurfaceMeshesParent = transform.Find("meshes").Find("brains").gameObject;
            go_.brainCutMeshesParent = transform.Find("meshes").Find("cuts").gameObject;

            // init lights
            go_.sharedDirLight = transform.parent.Find("directionnal light").gameObject;

            // init default planes
            m_planesList = new List<Plane>();
            m_CM.idPlanesOrientationList = new List<int>();
            m_CM.planesOrientationFlipList = new List<bool>();
            data_.removeFrontPlaneList = new List<int>();
            data_.numberOfCutsPerPlane = new List<int>();
            go_.brainCutMeshes = new List<GameObject>();

            update_brain_surface_color(15);
            update_colormap(13, false);
            update_brain_cut_color(14, true);
        }

        public void update_colormap(int id, bool updateColors = true)
        {
            CM.update_colormap(id);
            if (updateColors)
                CM.reset_colors();

            if (singlePatient)
                SharedMaterials.Brain.SpBrain.SetTexture("_ColorTex", CM.brainColorMapTexture);
            else
                SharedMaterials.Brain.MpBrain.SetTexture("_ColorTex", CM.brainColorMapTexture);

            if (data_.geometryUpToDate && !data_.iEEGOutdated)
                compute_IEEG_textures();
        }

        public void update_brain_surface_color(int id)
        {
            CM.m_idBrainColor = id;
            DLL.Texture tex = DLL.Texture.generate_1D_color_texture(CM.m_idBrainColor);
            tex.update_texture_2D(CM.brainColorTexture);

            if (singlePatient)
                SharedMaterials.Brain.SpBrain.SetTexture("_MainTex", CM.brainColorTexture);
            else
                SharedMaterials.Brain.MpBrain.SetTexture("_MainTex", CM.brainColorTexture);           
        }

        public void update_brain_cut_color(int id, bool updateColors = true)
        {
            CM.update_brain_cut_color(id);
            if (updateColors)
                CM.reset_colors();

            data_.updateCutMeshGeometry = true;
            data_.iEEGOutdated = true;

            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.updatePlane); // TEMP
            //##################
        }


        /// <summary>
        /// Reset the gameobjects of the scene
        /// </summary>
        public void reset_scene_GO()
        {
            // destroy meshes
            for (int ii = 0; ii < go_.brainSurfaceMeshes.Count; ++ii)
            {
                Destroy(go_.brainSurfaceMeshes[ii]);
            }
            for (int ii = 0; ii < go_.brainCutMeshes.Count; ++ii)
            {
                go_.brainCutMeshes[ii].SetActive(false);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="nbSplits"></param>
        public void reset_splits_nb(int nbSplits)
        {
            if (m_CM.meshSplitNb == nbSplits)
                return;

            m_CM.meshSplitNb = nbSplits;

            if(go_.brainSurfaceMeshes.Count > 0)
                for (int ii = 0; ii < go_.brainSurfaceMeshes.Count; ++ii)
                    Destroy(go_.brainSurfaceMeshes[ii]);

            // reset meshes
            go_.brainSurfaceMeshes = new List<GameObject>(m_CM.meshSplitNb);
            for (int ii = 0; ii < m_CM.meshSplitNb; ++ii)
            {
                go_.brainSurfaceMeshes.Add(Instantiate(GlobalGOPreloaded.Brain));
                go_.brainSurfaceMeshes[ii].GetComponent<Renderer>().sharedMaterial = singlePatient ? SharedMaterials.Brain.SpBrain : SharedMaterials.Brain.MpBrain;
                go_.brainSurfaceMeshes[ii].name = "brain_" + ii;
                go_.brainSurfaceMeshes[ii].transform.parent = go_.brainSurfaceMeshesParent.transform;
                go_.brainSurfaceMeshes[ii].layer = LayerMask.NameToLayer(data_.MeshesLayerName);
                go_.brainSurfaceMeshes[ii].AddComponent<MeshCollider>();
                go_.brainSurfaceMeshes[ii].SetActive(true);
            }

            m_CM.reset_splits_nb(nbSplits);
        }

        /// <summary>
        /// Set UI screen space/overlays layers mask settings corresponding to the current mode of the scene
        /// </summary>
        public void set_current_mode_specifications(bool force = false)
        {
            modes.set_current_mode_specifications(force);
        }

        /// <summary>
        /// Update the sites masks
        /// </summary>
        /// <param name="allColumn"></param>
        /// <param name="plotGO"></param>
        /// <param name="action"> 0 : excluded / 1 : included / 2 : blacklisted / 3 : unblacklist / 4 : highlight / 5 : unhighlight / 6 : marked / 7 : unmarked </param>
        /// <param name="range"> 0 : a plot / 1 : all plots from an electrode / 2 : all plots from a patient / 3 : all highlighted / 4 : all unhighlighted 
        /// / 5 : all plots / 6 : in ROI / 7 : not in ROI / 8 : names filter / 9 : mars filter / 10 : broadman filter </param>
        public void update_sites_masks(bool allColumn, GameObject plotGO, int action = 0, int range = 0, string nameFilter = "")
        {
            //####### CHECK ACESS
            if (!modes.functionAccess(Mode.FunctionsId.updateMaskPlot))
            {
                Debug.LogError("-ERROR : Base3DScene::updateMaskPlot -> no acess for mode : " + modes.currentModeName());
                return;
            }
            //##################

            //Column3DView col = m_CM.currentColumn();

            List<List<int>> colIdPlots = new List<List<int>>(m_CM.columns.Count);
            for(int ii = 0; ii < m_CM.columns.Count; ++ii)
            {
                colIdPlots.Add(new List<int>(m_CM.SitesList.Count));

                if(!allColumn)
                {
                    if (ii != m_CM.idSelectedColumn)
                        continue;
                }

                switch(range)
                {
                    case 0:
                        {// one specific plot
                            colIdPlots[ii].Add(plotGO.GetComponent<Site>().idGlobal);
                        }
                    break;
                    case 1:
                        { // all plots from an electrode
                            Transform parentElectrode = plotGO.transform.parent;
                            for (int jj = 0; jj < parentElectrode.childCount; ++jj)
                                colIdPlots[ii].Add(parentElectrode.GetChild(jj).gameObject.GetComponent<Site>().idGlobal);
                        }
                    break;
                    case 2:
                        { // all plots from a patient
                            Transform parentPatient = plotGO.transform.parent.parent;
                            for (int jj = 0; jj < parentPatient.childCount; ++jj)
                            {
                                Transform parentElectrode = parentPatient.GetChild(jj);
                                for (int kk = 0; kk < parentElectrode.childCount; kk++)
                                {
                                    colIdPlots[ii].Add(parentElectrode.GetChild(kk).gameObject.GetComponent<Site>().idGlobal);
                                }
                            }
                        }
                    break;
                    case 3: // all highlighted plots
                        {
                            for (int jj = 0; jj < m_CM.col(ii).Sites.Count; ++jj)
                            {                                
                                if (m_CM.col(ii).Sites[jj].highlight)
                                    colIdPlots[ii].Add(jj);
                            }
                        }
                        break;
                    case 4: // all unhighlighted plots
                        {                            
                            for (int jj = 0; jj < m_CM.col(ii).Sites.Count; ++jj)
                            {
                                if (!m_CM.col(ii).Sites[jj].highlight)
                                    colIdPlots[ii].Add(jj);
                            }
                        }
                        break;
                    case 5: // all plots
                        {
                            for (int jj = 0; jj < m_CM.col(ii).Sites.Count; ++jj)
                            {                                
                                colIdPlots[ii].Add(jj);
                            }
                        }
                        break;
                    case 6: // in ROI
                        {
                            for (int jj = 0; jj < m_CM.col(ii).Sites.Count; ++jj)
                            {
                                if (!m_CM.col(ii).Sites[jj].columnROI)
                                    colIdPlots[ii].Add(jj);
                            }
                        }
                        break;
                    case 7: // no in ROI
                        {
                            for (int jj = 0; jj < m_CM.col(ii).Sites.Count; ++jj)
                            {
                                if (m_CM.col(ii).Sites[jj].columnROI)
                                    colIdPlots[ii].Add(jj);
                            }
                        }
                        break;
                    case 8: // names filter
                        {
                            for (int jj = 0; jj < m_CM.col(ii).Sites.Count; ++jj)
                            {
                                if (m_CM.col(ii).Sites[jj].fullName.ToLower().Contains(nameFilter.ToLower()))
                                    colIdPlots[ii].Add(jj);
                            }
                        }
                        break;
                    case 9: // mars filter
                        {                            
                            for (int jj = 0; jj < m_CM.col(ii).Sites.Count; ++jj)
                            {
                                if (GlobalGOPreloaded.MarsAtlasIndex.full_name(m_CM.col(ii).Sites[jj].labelMarsAtlas).ToLower().Contains(nameFilter.ToLower()))
                                    colIdPlots[ii].Add(jj);      
                            }
                        }
                        break;
                    case 10: // broadman filter
                        {
                            for (int jj = 0; jj < m_CM.col(ii).Sites.Count; ++jj)
                            {
                                if (GlobalGOPreloaded.MarsAtlasIndex.broadman_area(m_CM.col(ii).Sites[jj].labelMarsAtlas).ToLower().Contains(nameFilter.ToLower()))
                                    colIdPlots[ii].Add(jj);
                            }
                        }
                        break;
                }
            }

            // apply action
            for (int ii = 0; ii < colIdPlots.Count; ++ii)
            {
                for (int jj = 0; jj < colIdPlots[ii].Count; jj++)
                {
                    if (action == 0 || action == 1) // excluded | included
                    {
                        m_CM.col(ii).Sites[colIdPlots[ii][jj]].exclude = (action == 0);
                    }
                    else if (action == 2 || action == 3)// blacklisted | unblacklisted
                    {
                        m_CM.col(ii).Sites[colIdPlots[ii][jj]].blackList = (action == 2);
                    }
                    else if(action == 4 || action == 5) // highlight | unhighlight
                    {
                        m_CM.col(ii).Sites[colIdPlots[ii][jj]].highlight = (action == 4);
                    }
                    else // marked | unmarked
                    {
                        m_CM.col(ii).Sites[colIdPlots[ii][jj]].marked = (action == 6);
                    }
                }
            }

            m_CM.update_all_columns_sites_rendering(data_);

            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.updateMaskPlot);
            //##################
        }

        /// <summary>
        /// Set the mesh part to be displayed in the scene
        /// </summary>
        /// <param name="meshPartToDisplay"></param>
        public void update_mesh_part_to_display(SceneStatesInfo.MeshPart meshPartToDisplay)
        {
            //####### CHECK ACESS
            if (!modes.functionAccess(Mode.FunctionsId.setDisplayedMesh))
            {
                Debug.LogError("-ERROR : Base3DScene::setDisplayedMesh -> no acess for mode : " + modes.currentModeName());
                return;
            }
            //##################

            if (!data_.geometryUpToDate)
                return;

            data_.meshPartToDisplay = meshPartToDisplay;
            data_.updateCutMeshGeometry = true;
            data_.iEEGOutdated = true;
            m_CM.update_all_columns_sites_rendering(data_);

            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.setDisplayedMesh);
            //##################
        }

        /// <summary>
        /// Set the mesh type to be displayed in the scene
        /// </summary>
        /// <param name="meshTypeToDisplay"></param>
        public void update_mesh_type_to_display(SceneStatesInfo.MeshType meshTypeToDisplay)
        {
            //####### CHECK ACESS
            if (!modes.functionAccess(Mode.FunctionsId.setDisplayedMesh))
            {
                Debug.LogError("-ERROR : Base3DScene::setDisplayedMesh -> no acess for mode : " + modes.currentModeName());
                return;
            }
            //##################

            if (!data_.geometryUpToDate)
                return;

            switch(meshTypeToDisplay)
            {
                case SceneStatesInfo.MeshType.Hemi:
                    if (!data_.hemiMeshesAvailables)
                        return;
                    break;
                case SceneStatesInfo.MeshType.White:
                    if (!data_.whiteMeshesAvailables)
                        return;
                    break;
                case SceneStatesInfo.MeshType.Inflated:
                    if (!data_.whiteInflatedMeshesAvailables)
                        return;
                    break;
            }

            data_.meshTypeToDisplay = meshTypeToDisplay;
            data_.updateCutMeshGeometry = true;
            data_.iEEGOutdated = true;
            m_CM.update_all_columns_sites_rendering(data_);

            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.setDisplayedMesh);
            //##################
        }

        /// <summary>
        /// Add a new cut plane
        /// </summary>
        public void add_new_cut_plane()
        {
            //####### CHECK ACESS
            if (!modes.functionAccess(Mode.FunctionsId.addNewPlane))
            {
                Debug.LogError("-ERROR : Base3DScene::addNewPlane -> no acess for mode : " + modes.currentModeName());
                return;
            }
            //##################

            m_planesList.Add(new Plane(new Vector3(0, 0, 0), new Vector3(1, 0, 0)));
            m_CM.idPlanesOrientationList.Add(0);
            m_CM.planesOrientationFlipList.Add(false);
            data_.removeFrontPlaneList.Add(0);
            data_.numberOfCutsPerPlane.Add(data_.defaultNbOfCutsPerPlane);

            GameObject cut = Instantiate(GlobalGOPreloaded.Cut);
            cut.GetComponent<Renderer>().sharedMaterial = singlePatient ? SharedMaterials.Brain.SpCut : SharedMaterials.Brain.MpCut;
            cut.name = "cut_" + (m_planesList.Count - 1);
            cut.transform.parent = go_.brainCutMeshesParent.transform; ;
            cut.AddComponent<MeshCollider>();
            cut.layer = LayerMask.NameToLayer(data_.MeshesLayerName);
            go_.brainCutMeshes.Add(cut);
            go_.brainCutMeshes[go_.brainCutMeshes.Count - 1].layer = LayerMask.NameToLayer(data_.MeshesLayerName);

            // update columns manager
            m_CM.update_cuts_nb(go_.brainCutMeshes.Count);

            // update plots visibility
            m_CM.update_all_columns_sites_rendering(data_);

            data_.updateCutMeshGeometry = true;
            data_.iEEGOutdated = true;

            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.addNewPlane);
            //##################            
        }

        /// <summary>
        /// Remove the last cut plane
        /// </summary>
        public void remove_last_cut_plane()
        {
            //####### CHECK ACESS
            if (!modes.functionAccess(Mode.FunctionsId.removeLastPlane))
            {
                Debug.LogError("-ERROR : Base3DScene::removeLastPlane -> no acess for mode : " + modes.currentModeName());
                return;
            }
            //##################

            m_planesList.RemoveAt(m_planesList.Count - 1);
            m_CM.idPlanesOrientationList.RemoveAt(m_CM.idPlanesOrientationList.Count - 1);
            m_CM.planesOrientationFlipList.RemoveAt(m_CM.planesOrientationFlipList.Count - 1);
            data_.removeFrontPlaneList.RemoveAt(data_.removeFrontPlaneList.Count - 1);
            data_.numberOfCutsPerPlane.RemoveAt(data_.numberOfCutsPerPlane.Count - 1);

            Destroy(go_.brainCutMeshes[go_.brainCutMeshes.Count - 1]);
            go_.brainCutMeshes.RemoveAt(go_.brainCutMeshes.Count - 1);

            // update columns manager
            m_CM.update_cuts_nb(go_.brainCutMeshes.Count);

            // update plots visibility
            m_CM.update_all_columns_sites_rendering(data_);

            // update cut images display with the new selected column
            //UpdateCutsInUI.Invoke(m_CM.getBrainCutTextureList(m_CM.idSelectedColumn, true, data_.generatorUpToDate, data_.displayLatenciesMode), m_CM.idSelectedColumn, m_CM.planesList.Count);

            data_.updateCutMeshGeometry = true;
            data_.iEEGOutdated = true;

            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.removeLastPlane);
            //##################            
        }

        /// <summary>
        /// Update a cut plane
        /// </summary>
        /// <param name="idOrientation"></param>
        /// <param name="flip"></param>
        /// <param name="removeFrontPlane"></param>
        /// <param name="customNormal"></param>
        /// <param name="idPlane"></param>
        /// <param name="position"></param>
        public void update_cut_plane(int idOrientation, bool flip, bool removeFrontPlane, Vector3 customNormal, int idPlane, float position)
        {
            //####### CHECK ACESS
            if (!modes.functionAccess(Mode.FunctionsId.updatePlane))
            {
                Debug.LogError("-ERROR : Base3DScene::updatePlane -> no acess for mode : " + modes.currentModeName());
                return;
            }
            //##################

            Plane newPlane = new Plane(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
            if (idOrientation == 3 || !data_.mriLoaded) // custom normal
            {
                if (customNormal.x != 0 || customNormal.y != 0 || customNormal.z != 0)
                    newPlane.normal = customNormal;
                else
                    newPlane.normal = new Vector3(1, 0, 0);
            }
            else
            {
                m_CM.DLLVolume.set_plane_with_orientation(newPlane, idOrientation, flip);
            }

            m_planesList[idPlane].normal = newPlane.normal;
            m_CM.idPlanesOrientationList[idPlane] = idOrientation;
            m_CM.planesOrientationFlipList[idPlane] = flip;
            data_.removeFrontPlaneList[idPlane] = removeFrontPlane?1:0;
            data_.lastIdPlaneModified = idPlane;

            // ########### cuts base on the mesh
            float offset;
            if (data_.meshToDisplay != null)
            {
                offset = data_.meshToDisplay.size_offset_cut_plane(m_planesList[idPlane], data_.numberOfCutsPerPlane[idPlane]);
                offset *= 1.05f; // upsize a little bit the bbox for planes
            }
            else
                offset = 0.1f;

            m_planesList[idPlane].point = data_.meshCenter + m_planesList[idPlane].normal * (position - 0.5f) * offset * data_.numberOfCutsPerPlane[idPlane];

            data_.updateCutMeshGeometry = true;
            data_.iEEGOutdated = true;

            // update sites visibility
            m_CM.update_all_columns_sites_rendering(data_);

            // update cameras cuts display
            ModifyPlanesCuts.Invoke();

            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.updatePlane);
            //##################
        }

        /// <summary>
        /// Reset the volume of the scene
        /// </summary>
        /// <param name="pathNIIBrainVolumeFile"></param>
        /// <returns></returns>
        public bool reset_NII_brain_volume(string pathNIIBrainVolumeFile)
        {
            //####### CHECK ACESS
            if (!modes.functionAccess(Mode.FunctionsId.resetNIIBrainVolumeFile))
            {
                Debug.LogError("-ERROR : Base3DScene::resetNIIBrainVolumeFile -> no acess for mode : " + modes.currentModeName());
                return false;
            }

            data_.mriLoaded = false;

            // checks parameter
            if (pathNIIBrainVolumeFile.Length == 0)
            {
                Debug.LogError("-ERROR : Base3DScene::resetNIIBrainVolumeFile -> path NII brain volume file is empty. ");
                return (data_.meshesLoaded = false);
            }

            // load volume
            bool loadingSuccess = m_CM.DLLNii.load_nii_file(pathNIIBrainVolumeFile);
            if (loadingSuccess)
            {
                m_CM.DLLNii.convert_to_volume(m_CM.DLLVolume);
                data_.volumeCenter = m_CM.DLLVolume.center();
            }
            else
            {
                Debug.LogError("-ERROR : Base3DScene::resetNIIBrainVolumeFile -> load NII file failed. " + pathNIIBrainVolumeFile);
                return data_.mriLoaded;
            }

            data_.mriLoaded = loadingSuccess;
            UpdatePlanes.Invoke();

            // send cal values to the UI
            IRMCalValuesUpdate.Invoke(m_CM.DLLVolume.retrieve_extreme_values());

            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.resetNIIBrainVolumeFile);
            //##################

            return data_.mriLoaded;
        }

        /// <summary>
        /// Reset the rendering settings for this scene, called by each camera before rendering
        /// </summary>
        /// <param name="cameraRotation"></param>
        abstract public void reset_rendering_settings(Vector3 cameraRotation);

        /// <summary>
        /// Update the selected column of the scene
        /// </summary>
        /// <param name="idColumn"></param>
        public void update_selected_column(int idColumn)
        {
            if (idColumn >= m_CM.columns.Count)
                return;

            m_CM.idSelectedColumn = idColumn;

            // force mode to update UI
            modes.set_current_mode_specifications(true);

            compute_GUI_textures(-1, m_CM.idSelectedColumn);
            update_GUI_textures();
        }

        /// <summary>
        /// 
        /// When to call ?  changes in DLLCutColorScheme, MRICalMinFactor, MRICalMaxFactor
        /// </summary>
        /// <param name="indexCut"></param>
        /// <param name="indexColumn"></param>
        private void compute_MRI_textures(int indexCut = -1, int indexColumn = -1)
        {
            if (data_.meshTypeToDisplay == SceneStatesInfo.MeshType.Inflated)
                return;

            bool allColumns = indexColumn == -1;
            bool allCuts = indexCut == -1;

            int[] columnsIndexes = allColumns ? Enumerable.Range(0, m_CM.columns.Count).ToArray() : new int[1] { indexColumn };
            int[] cutsIndexes    = allCuts ?    Enumerable.Range(0, m_planesList.Count).ToArray() : new int[1] { indexCut };

            UnityEngine.Profiling.Profiler.BeginSample("TEST-Base3DScene compute_MRI_textures 0 create_MRI_texture ");

            for (int ii = 0; ii < columnsIndexes.Length; ++ii)
                for (int jj = 0; jj < cutsIndexes.Length; ++jj)
                    m_CM.create_MRI_texture(cutsIndexes[jj], columnsIndexes[ii]);

            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("TEST-Base3DScene compute_MRI_textures 1 compute_GUI_textures");
            compute_GUI_textures(indexCut, m_CM.idSelectedColumn);
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("TEST-Base3DScene compute_MRI_textures 2 update_GUI_textures");
            update_GUI_textures();
            UnityEngine.Profiling.Profiler.EndSample();
        }

        /// <summary>
        /// 
        /// When to call ? changes in IEEGColumn.currentTimeLineID, IEEGColumn.alphaMin, IEEGColumn.alphaMax / DLLCutColorScheme
        /// </summary>
        /// <param name="indexCut"></param>
        /// <param name="indexColumn"></param>
        private void compute_IEEG_textures(int indexCut = -1, int indexColumn = -1, bool surface = true, bool cuts = true, bool plots = true)
        {
            UnityEngine.Profiling.Profiler.BeginSample("TEST-compute_IEEG_textures");

            UnityEngine.Profiling.Profiler.BeginSample("TEST-compute_IEEG_textures 0");
            bool allColumns = indexColumn == -1;
            bool allCuts = indexCut == -1;

            int[] columnsIndexes = allColumns ? Enumerable.Range(0, m_CM.columns.Count).ToArray() : new int[1] { indexColumn };
            int[] cutsIndexes    = allCuts ?    Enumerable.Range(0, m_planesList.Count).ToArray() : new int[1] { indexCut };

            for (int ii = 0; ii < columnsIndexes.Length; ++ii)
            {
                if (CM.col(columnsIndexes[ii]).isFMRI)
                    return;

                Column3DViewIEEG currCol = (Column3DViewIEEG)CM.col(columnsIndexes[ii]);

                // brain surface
                if(surface)
                    if (!m_CM.compute_surface_brain_UV_with_IEEG((data_.meshTypeToDisplay == SceneStatesInfo.MeshType.Inflated), columnsIndexes[ii]))
                        return;

                // brain cuts
                if (cuts)
                    for (int jj = 0; jj < cutsIndexes.Length; ++jj)
                        m_CM.color_cuts_textures_with_IEEG(columnsIndexes[ii], cutsIndexes[jj]);

                if (plots)
                {
                    currCol.update_sites_size_and_color_arrays_for_IEEG();
                    currCol.update_sites_rendering(data_, null);
                }
            }
            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("TEST-compute_IEEG_textures 1 compute_GUI_textures");
            compute_GUI_textures(indexCut, m_CM.idSelectedColumn);
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("TEST-compute_IEEG_textures 2 update_GUI_textures");
            update_GUI_textures();
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.EndSample();
        }

        /// <summary>
        /// When to call ? changes in FMRIColumn.alpha / calMin/ calMax/ DLLCutColorScheme
        /// </summary>
        /// <param name="indexCut"></param>
        /// <param name="indexColumn"></param>
        /// <param name="surface"></param>
        /// <param name="cuts"></param>
        private void compute_FMRI_textures(int indexCut = -1, int indexColumn = -1, bool surface = true, bool cuts = true)
        {
            bool allColumns = indexColumn == -1;
            bool allCuts = indexCut == -1;

            int[] columnsIndexes = allColumns ? Enumerable.Range(0, m_CM.FMRI_columns_nb()).ToArray() : new int[1] { indexColumn };
            int[] cutsIndexes = allCuts ? Enumerable.Range(0, m_planesList.Count).ToArray() : new int[1] { indexCut };
           
            for (int ii = 0; ii < columnsIndexes.Length; ++ii)
            {
                //Column3DViewFMRI currCol = CM.FMRI_col(columnsIndexes[ii]);

                // brain cuts
                if (cuts)
                    for (int jj = 0; jj < cutsIndexes.Length; ++jj)
                        m_CM.color_cuts_textures_with_FMRI(columnsIndexes[ii], cutsIndexes[jj]);
            }

            compute_GUI_textures(indexCut, indexColumn);

            update_GUI_textures();
        }



        /// <summary>
        /// 
        /// </summary>
        /// <param name="indexCut"></param>
        /// <param name="indexColumn"></param>
        private void compute_GUI_textures(int indexCut = -1, int indexColumn = -1)
        {
            bool allColumns = indexColumn == -1;
            bool allCuts    = indexCut == -1;

            int[] columnsIndexes = allColumns ? Enumerable.Range(0, m_CM.columns.Count).ToArray() : new int[1] { indexColumn };
            int[] cutsIndexes = allCuts ? Enumerable.Range(0, m_planesList.Count).ToArray() : new int[1] { indexCut };

            for (int ii = 0; ii < columnsIndexes.Length; ++ii)
            {
                Column3DView currCol = m_CM.col(columnsIndexes[ii]);

                for (int jj = 0; jj < planesList.Count; ++jj)
                {
                    if (!currCol.isFMRI)
                    {
                        if (!data_.generatorUpToDate)
                            m_CM.create_GUI_MRI_texture(cutsIndexes[jj], columnsIndexes[ii]);
                        else
                            m_CM.create_GUI_IEEG_texture(cutsIndexes[jj], columnsIndexes[ii]);
                    }
                    else
                    {
                        m_CM.create_GUI_FMRI_texture(cutsIndexes[jj], columnsIndexes[ii]);
                    }
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="indexCut"></param>
        private void update_GUI_textures()
        {
            Column3DView currCol = m_CM.col(m_CM.idSelectedColumn);
            List<Texture2D> texturesToDisplay = null;
            if (!currCol.isFMRI)
            {
                if (!data_.generatorUpToDate)
                    texturesToDisplay = currCol.guiBrainCutTextures;
                else
                    texturesToDisplay = ((Column3DViewIEEG)currCol).guiBrainCutWithIEEGTextures;
            }
            else
            {
                texturesToDisplay = ((Column3DViewFMRI)currCol).guiBrainCutWithFMRITextures;
            }

            UpdateCutsInUI.Invoke(texturesToDisplay, m_CM.idSelectedColumn, m_planesList.Count);
        }

        /// <summary>
        /// Update the data render corresponding to the column
        /// </summary>
        /// <param name="indexColumn"></param>
        /// <returns></returns>
        public bool update_column_rendering(int indexColumn)
        {
            if (!data_.geometryUpToDate)
                return false;
        
            UnityEngine.Profiling.Profiler.BeginSample("TEST-updateColumnRender");

            Column3DView currCol = m_CM.col(indexColumn);
            bool isFMRI = currCol.isFMRI;


            // TODO : un mesh pour chaque column

            // update cuts textures
            if ((data_.meshTypeToDisplay != SceneStatesInfo.MeshType.Inflated))
            {
                for (int ii = 0; ii < planesList.Count; ++ii)
                {
                    if (!currCol.isFMRI)
                    {
                        if (!data_.generatorUpToDate)
                            go_.brainCutMeshes[ii].GetComponent<Renderer>().material.mainTexture = currCol.brainCutTextures[ii];
                        else
                            go_.brainCutMeshes[ii].GetComponent<Renderer>().material.mainTexture = ((Column3DViewIEEG)currCol).brainCutWithIEEGTextures[ii];
                    }
                    else
                        go_.brainCutMeshes[ii].GetComponent<Renderer>().material.mainTexture = ((Column3DViewFMRI)currCol).brainCutWithFMRITextures[ii];
                }
            }

            // update meshes splits UV
            for (int ii = 0; ii < m_CM.meshSplitNb; ++ii)
            {
                // uv 1 (main)
                //go_.brainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh.uv = m_CM.UVCoordinatesSplits[ii];

                if (isFMRI || !data_.generatorUpToDate || data_.displayCcepMode)
                {
                    // uv 2 (alpha) 
                    go_.brainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh.uv2 = m_CM.uvNull[ii];
                    // uv 3 (color map)
                    go_.brainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh.uv3 = m_CM.uvNull[ii];
                }
                else
                {
                    // uv 2 (alpha) 
                    go_.brainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh.uv2 = ((Column3DViewIEEG)currCol).DLLBrainTextureGeneratorList[ii].get_alpha_UV();
                    // uv 3 (color map)
                    go_.brainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh.uv3 = ((Column3DViewIEEG)currCol).DLLBrainTextureGeneratorList[ii].get_iEEG_UV();
                }
            }


            UnityEngine.Profiling.Profiler.EndSample();

            return true;
        }



        /// <summary>
        /// Update the brain and the cuts meshes colliders
        /// </summary>
        public void update_meshes_colliders()
        {
            if (!data_.meshesLoaded || !data_.mriLoaded)
                return;

            // update splits colliders
            for(int ii = 0; ii < go_.brainSurfaceMeshes.Count; ++ii)
            {
                go_.brainSurfaceMeshes[ii].GetComponent<MeshCollider>().sharedMesh = null;
                go_.brainSurfaceMeshes[ii].GetComponent<MeshCollider>().sharedMesh = go_.brainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh;
            }

            // update cuts colliders
            for (int ii = 0; ii < go_.brainCutMeshes.Count; ++ii)
            {
                go_.brainCutMeshes[ii].GetComponent<MeshCollider>().sharedMesh = null;
                go_.brainCutMeshes[ii].GetComponent<MeshCollider>().sharedMesh = go_.brainCutMeshes[ii].GetComponent<MeshFilter>().mesh;
            }

            data_.collidersUpdated = true;
        }

        /// <summary>
        /// Update the textures generator
        /// </summary>
        public void update_generators()
        {
            //####### CHECK ACESS
            if (!modes.functionAccess(Mode.FunctionsId.pre_updateGenerators))
            {
                Debug.LogError("-ERROR : Base3DScene::pre_updateGenerators -> no acess for mode : " + modes.currentModeName());
                return;
            }
            //##################

            if (data_.updateCutMeshGeometry || !data_.geometryUpToDate) // if update cut plane is pending, cancel action
                return;

            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.pre_updateGenerators);
            //##################

            data_.generatorUpToDate = false;

            m_computeGeneratorsJob = new ComputeGeneratorsJob();
            m_computeGeneratorsJob.data_ = data_;
            m_computeGeneratorsJob.cm_ = m_CM;
            m_computeGeneratorsJob.Start();
        }

        /// <summary>
        /// Update the displayed amplitudes on the brain and the cuts with the slider position.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="slider"></param>
        /// <param name="globalTimeline"> if globaltime is true, update all columns with the same slider, else udapte only current selected column </param>
        public void update_IEEG_time(int id, float value, bool globalTimeline)
        {
            m_CM.globalTimeline = globalTimeline;
            if (m_CM.globalTimeline)
            {
                m_CM.commonTimelineValue = value;
                for (int ii = 0; ii < m_CM.IEEG_columns_nb(); ++ii)
                    m_CM.IEEG_col(ii).currentTimeLineID = (int)m_CM.commonTimelineValue;
            }
            else
            {
                Column3DViewIEEG currIEEGCol = (Column3DViewIEEG)m_CM.columns[id];
                currIEEGCol.columnTimeLineID = (int)value;
                currIEEGCol.currentTimeLineID = currIEEGCol.columnTimeLineID;
            }

            compute_IEEG_textures();
            m_CM.update_all_columns_sites_rendering(data_);
            UpdateTimeInUI.Invoke();
        }

        /// <summary>
        /// Update displayed amplitudes with the timeline id corresponding to global timeline mode or individual timeline mode
        /// </summary>
        /// <param name="globalTimeline"></param>
        public void update_IEEG_all_times(bool globalTimeline)
        {
            for (int ii = 0; ii < m_CM.IEEG_columns_nb(); ++ii)
            {
                m_CM.IEEG_col(ii).currentTimeLineID = globalTimeline ? (int)m_CM.commonTimelineValue : m_CM.IEEG_col(ii).columnTimeLineID;
            }

            compute_IEEG_textures();
            m_CM.update_all_columns_sites_rendering(data_);
        }


        /// <summary>
        /// 
        /// </summary>
        public void switch_mars_atlas_color()
        {
            {                
                data_.marsAtlasMode = !data_.marsAtlasMode;
                go_.brainSurfaceMeshes[0].GetComponent<Renderer>().sharedMaterial.SetInt("_MarsAtlas", data_.marsAtlasMode ? 1 : 0);
            }
        }

        /// <summary>
        /// Mouse scroll events managements
        /// </summary>
        /// <param name="scrollDelta"></param>
        public void mouse_scroll_action(Vector2 scrollDelta)
        {
            // nothing for now 
            // (not not delete children classes use it)
        }

        /// <summary>
        /// Keyboard events management
        /// </summary>
        /// <param name="keyCode"></param>
        public void keyboard_action(KeyCode keyCode)
        {
            if (!data_.meshesLoaded || !data_.mriLoaded)
                return;

            switch (keyCode)
            {
                // enable/disable holes in the cuts
                case KeyCode.H:
                    data_.holesEnabled = !data_.holesEnabled;
                    data_.updateCutMeshGeometry = true;
                    data_.iEEGOutdated = true;
                    modes.updateMode(Mode.FunctionsId.updatePlane); // TEMP
                    break;
            }
        }


        public bool is_tri_erasing_enabled()
        {
            return m_triEraser.is_enabled();
        }

        public TriEraser.Mode current_tri_erasing_mode()
        {
            return m_triEraser.mode();
        }

        public void set_tri_erasing(bool enabled)
        {
            m_triEraser.set_enabled(enabled);
        }

        public void reset_tri_erasing(bool updateGO = true)
        {
            // destroy previous GO
            if (go_.invisibleBrainSurfaceMeshes != null)
                for (int ii = 0; ii < go_.invisibleBrainSurfaceMeshes.Count; ++ii)
                    Destroy(go_.invisibleBrainSurfaceMeshes[ii]);

            // create new GO
            go_.invisibleBrainSurfaceMeshes = new List<GameObject>(go_.brainSurfaceMeshes.Count);
            for (int ii = 0; ii < go_.brainSurfaceMeshes.Count; ++ii)
            {
                GameObject invisibleBrainPart = Instantiate(GlobalGOPreloaded.InvisibleBrain);
                invisibleBrainPart.name = "erased brain part " + ii;
                invisibleBrainPart.transform.SetParent(transform.Find("meshes").Find("erased brains"));
                invisibleBrainPart.layer = LayerMask.NameToLayer(singlePatient ? "Meshes_SP" : "Meshes_MP");
                invisibleBrainPart.AddComponent<MeshFilter>();
                invisibleBrainPart.transform.localScale = new Vector3(-1, 1, 1);
                invisibleBrainPart.transform.localPosition = new Vector3(0, 0, 0);
                invisibleBrainPart.SetActive(m_triEraser.is_enabled());
                go_.invisibleBrainSurfaceMeshes.Add(invisibleBrainPart);
            }

            m_triEraser.reset(go_.invisibleBrainSurfaceMeshes, m_CM.DLLCutsList[0], m_CM.DLLSplittedMeshesList);

            if(updateGO)
                for (int ii = 0; ii < m_CM.DLLSplittedMeshesList.Count; ++ii)
                    m_CM.DLLSplittedMeshesList[ii].update_mesh_from_dll(go_.brainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh);
        }

        /// <summary>
        /// 
        /// </summary>
        public void cancel_last_tri_erasing()
        {
            m_triEraser.cancel_last_action();
            for (int ii = 0; ii < m_CM.DLLSplittedMeshesList.Count; ++ii)
                m_CM.DLLSplittedMeshesList[ii].update_mesh_from_dll(go_.brainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="mode"></param>
        public void set_tri_erasing_mode(TriEraser.Mode mode)
        {
            TriEraser.Mode previousMode = m_triEraser.mode();
            m_triEraser.set_tri_erasing_mode(mode);

            if (mode == TriEraser.Mode.Expand || mode == TriEraser.Mode.Invert)
            {
                m_triEraser.erase_triangles(new Vector3(), new Vector3());
                for (int ii = 0; ii < m_CM.DLLSplittedMeshesList.Count; ++ii)
                    m_CM.DLLSplittedMeshesList[ii].update_mesh_from_dll(go_.brainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh);
                m_triEraser.set_tri_erasing_mode(previousMode);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="degrees"></param>
        public void set_tri_erasing_zone_degrees(float degrees)
        {
            m_triEraser.set_zone_degrees(degrees);
        }

        /// <summary>
        /// Return the id of the current select column in the scene
        /// </summary>
        /// <returns></returns>
        public int retrieve_current_selected_column_id()
        {
            return m_CM.idSelectedColumn;
        }

        /// <summary>
        /// Update the middle of a IEEG column
        /// </summary>
        /// <param name="value"></param>
        /// <param name="columnId"></param>
        public void update_IEEG_middle(float value, int columnId)
        {
            // update value
            Column3DViewIEEG IEEGCol = (Column3DViewIEEG)m_CM.col(columnId);
            if (IEEGCol.middle == value)
                return;
            IEEGCol.middle = value;

            data_.generatorUpToDate = false;
            data_.iEEGOutdated = true;
            m_CM.update_all_columns_sites_rendering(data_);

            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.updateMiddle);
            //##################
        }

        /// <summary>
        /// Update the max distance of a IEEG column
        /// </summary>
        /// <param name="value"></param>
        /// <param name="columnId"></param>
        public void update_site_maximum_influence(float value, int columnId)
        {
            Column3DViewIEEG IEEGCol = (Column3DViewIEEG)m_CM.col(columnId);
            if (IEEGCol.maxDistanceElec == value)
                return;            
            IEEGCol.maxDistanceElec = value;

            data_.generatorUpToDate = false;
            data_.iEEGOutdated = true;
            m_CM.update_all_columns_sites_rendering(data_);

            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.updateMiddle);
            //##################
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public void update_MRI_cal_min(float value)
        {
            m_CM.MRICalMinFactor = value;

            if (!data_.geometryUpToDate)
                return;

            data_.generatorUpToDate = false;
            data_.iEEGOutdated = true;

            { //TEST
              // recompute UV
                for (int ii = 0; ii < m_CM.meshSplitNb; ++ii)
                    m_CM.DLLCommonBrainTextureGeneratorList[ii].compute_UVMain_with_volume(m_CM.DLLSplittedMeshesList[ii], m_CM.DLLVolume, m_CM.MRICalMinFactor, m_CM.MRICalMaxFactor);

                // update brain mesh object mesh filter (TODO update only UV)
                for (int ii = 0; ii < m_CM.meshSplitNb; ++ii)
                    m_CM.DLLSplittedMeshesList[ii].update_mesh_from_dll(go_.brainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh);
            }

            compute_MRI_textures();
            m_CM.update_all_columns_sites_rendering(data_);

            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.updateMiddle);
            //##################
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public void update_MRI_cal_max(float value)
        {
            m_CM.MRICalMaxFactor = value;

            if (!data_.geometryUpToDate)
                return;

            data_.generatorUpToDate = false;
            data_.iEEGOutdated = true;

            { //TEST
              // recompute UV
                for (int ii = 0; ii < m_CM.meshSplitNb; ++ii)
                    m_CM.DLLCommonBrainTextureGeneratorList[ii].compute_UVMain_with_volume(m_CM.DLLSplittedMeshesList[ii], m_CM.DLLVolume, m_CM.MRICalMinFactor, m_CM.MRICalMaxFactor);

                // update brain mesh object mesh filter (TODO update only UV)
                for (int ii = 0; ii < m_CM.meshSplitNb; ++ii)
                    m_CM.DLLSplittedMeshesList[ii].update_mesh_from_dll(go_.brainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh);
            }


            compute_MRI_textures();
            m_CM.update_all_columns_sites_rendering(data_);

            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.updateMiddle);
            //##################
        }

        /// <summary>
        /// Update the cal min value of the input FMRI column
        /// </summary>
        /// <param name="value"></param>
        /// <param name="columnId"> id of the FMRI columns</param>
        public void update_FMRI_cal_min(float value, int columnId)
        {
            m_CM.FMRI_col(columnId).calMin = value;
            compute_FMRI_textures(-1, -1);
        }

        /// <summary>
        /// Update the cal max value of the input FMRI column
        /// </summary>
        /// <param name="value"></param>
        /// <param name="columnId"> id of the FMRI columns</param>
        public void update_FMRI_cal_max(float value, int columnId)
        {
            m_CM.FMRI_col(columnId).calMax = value;
            compute_FMRI_textures(-1, -1);
        }

        /// <summary>
        /// Update the alpha value of the input FMRI column
        /// </summary>
        /// <param name="value"></param>
        /// <param name="columnId"> id of the FMRI columns</param>
        public void update_FMRI_alpha(float value, int columnId)
        {
            m_CM.FMRI_col(columnId).alpha = value;
            compute_FMRI_textures(-1, -1);
        }


        /// <summary>
        /// Update the min alpha of a IEEG column
        /// </summary>
        /// <param name="value"></param>
        /// <param name="columnId"></param>
        public void update_IEEG_min_alpha(float value, int columnId)
        {
            m_CM.IEEG_col(columnId).alphaMin = value;

            if (data_.geometryUpToDate && !data_.iEEGOutdated)
                compute_IEEG_textures(-1, -1, true, true, false);
        }

        /// <summary>
        /// Update the max alpha of a IEEG  column
        /// </summary>
        /// <param name="value"></param>
        /// <param name="columnId"></param>
        public void update_IEEG_max_alpha(float value, int columnId)
        {
            m_CM.IEEG_col(columnId).alphaMax = value;

            if (data_.geometryUpToDate && !data_.iEEGOutdated)
                compute_IEEG_textures(-1, -1, true, true, false);
        }

        /// <summary>
        /// Update the bubbles gain of a IEEG column
        /// </summary>
        /// <param name="value"></param>
        /// <param name="columnId"></param>
        public void update_gain_bubbles(float value, int columnId)
        {
            Column3DViewIEEG IEEGCol = m_CM.IEEG_col(columnId);
            if (IEEGCol.gainBubbles == value)
                return;
            IEEGCol.gainBubbles = value;

            m_CM.update_all_columns_sites_rendering(data_);
        }


        /// <summary>
        /// Update the span min of a IEEG column
        /// </summary>
        /// <param name="value"></param>
        /// <param name="columnId"></param>
        public void update_IEEG_span_min(float value, int columnId)
        {
            Column3DViewIEEG IEEGCol = m_CM.IEEG_col(columnId);
            if (IEEGCol.spanMin == value)
                return;
            IEEGCol.spanMin = value;

            if (!data_.geometryUpToDate)
                return;

            data_.generatorUpToDate = false;
            data_.iEEGOutdated = true;
            update_GUI_textures();
            m_CM.update_all_columns_sites_rendering(data_);            

            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.updateMiddle);
            //##################
        }

        /// <summary>
        /// Update the span max of a IEEG column
        /// </summary>
        /// <param name="value"></param>
        /// <param name="columnId"></param>
        public void update_IEEG_span_max(float value, int columnId)
        {
            Column3DViewIEEG IEEGCol = m_CM.IEEG_col(columnId);
            if (IEEGCol.spanMax == value)
                return;
            IEEGCol.spanMax = value;

            if (!data_.geometryUpToDate)
                return;

            data_.generatorUpToDate = false;
            data_.iEEGOutdated = true;
            update_GUI_textures();
            m_CM.update_all_columns_sites_rendering(data_);

            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.updateMiddle);
            //##################
        }

        /// <summary>
        /// Return the number of FMRI colums
        /// </summary>
        /// <returns></returns>
        public int FMRI_columns_nb()
        {
            return m_CM.FMRI_columns_nb();
        }

        /// <summary>
        /// Load an FMRI column
        /// </summary>
        /// <param name="FMRIPath"></param>
        /// <returns></returns>
        public bool load_FMRI_file(string FMRIPath)
        {
            if (m_CM.DLLNii.load_nii_file(FMRIPath))
                return true;

            Debug.LogError("-ERROR : Base3DScene::load_FMRI_file -> load NII file failed. " + FMRIPath);
            return false;
        }

        /// <summary>
        /// Add a FMRI column
        /// </summary>
        /// <param name="IRMFPath"></param>
        /// <returns></returns>
        public bool add_FMRI_column(string IMRFLabel)
        {
            //####### CHECK ACESS
            if (!modes.functionAccess(Mode.FunctionsId.add_FMRI_column))
            {
                Debug.LogError("-ERROR : Base3DScene::add_FMRI_column -> no acess for mode : " + modes.currentModeName());
                return false;
            }
            //##################

            // update columns number
            int newFMRIColNb = m_CM.FMRI_columns_nb() + 1;
            m_CM.update_columns_nb(m_CM.IEEG_columns_nb(), newFMRIColNb, m_planesList.Count);

            int idCol = newFMRIColNb - 1;
            m_CM.FMRI_col(idCol).Label = IMRFLabel;

            // update plots visibility
            m_CM.update_all_columns_sites_rendering(data_);

            // convert to volume            
            m_CM.DLLNii.convert_to_volume(m_CM.DLLVolumeFMriList[idCol]);

            if (!singlePatient)
                AskROIUpdateEvent.Invoke(m_CM.IEEG_columns_nb() + idCol);

            // send parameters to UI
            //IRMCalValues calValues = m_CM.DLLVolumeIRMFList[idCol].retrieveExtremeValues();

            FMriDataParameters FMRIParams = new FMriDataParameters();
            FMRIParams.calValues = m_CM.DLLVolumeFMriList[idCol].retrieve_extreme_values();
            FMRIParams.columnId = idCol;
            FMRIParams.alpha  = m_CM.FMRI_col(idCol).alpha;
            FMRIParams.calMin = m_CM.FMRI_col(idCol).calMin;
            FMRIParams.calMax = m_CM.FMRI_col(idCol).calMax;
            FMRIParams.singlePatient = singlePatient;

            m_CM.FMRI_col(idCol).calMin = FMRIParams.calValues.computedCalMin;
            m_CM.FMRI_col(idCol).calMax = FMRIParams.calValues.computedCalMax;            

            // update camera
            UpdateCameraTarget.Invoke(singlePatient ?  m_CM.BothHemi.bounding_box().center() : m_MNI.BothHemi.bounding_box().center());

            compute_MRI_textures(-1, -1);

            SendFMRIParameters.Invoke(FMRIParams);
            compute_FMRI_textures(-1, -1);


            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.add_FMRI_column);
            //##################

            return true;
        }

        /// <summary>
        /// Remove the last IRMF column
        /// </summary>
        public void remove_last_FMRI_column()
        {
            //####### CHECK ACESS
            if (!modes.functionAccess(Mode.FunctionsId.removeLastIRMFColumn))
            {
                Debug.LogError("-ERROR : Base3DScene::remove_last_FMRI_column -> no acess for mode : " + modes.currentModeName());
                return;
            }
            //##################
            
            // update columns number
            m_CM.update_columns_nb(m_CM.IEEG_columns_nb(), m_CM.FMRI_columns_nb() - 1, m_planesList.Count);

            // update plots visibility
            m_CM.update_all_columns_sites_rendering(data_);

            compute_MRI_textures(-1, -1);
            compute_FMRI_textures(-1, -1);
            update_GUI_textures();

            //####### UDPATE MODE
            modes.updateMode(Mode.FunctionsId.removeLastIRMFColumn);
            //##################
        }

        /// <summary>
        /// Is the latency mode enabled ?
        /// </summary>
        /// <returns></returns>
        public bool is_latency_mode_enabled()
        {
            return data_.displayCcepMode;
        }

        /// <summary>
        /// Updat visibility of the columns 3D items
        /// </summary>
        /// <param name="displayMeshes"></param>
        /// <param name="displaySites"></param>
        /// <param name="displayROI"></param>
        public void update_scene_items_visibility(bool displayMeshes, bool displaySites, bool displayROI)
        {
            //if (!singlePatient)
            //    m_CM.update_ROI_visibility(displayROI && data_.ROICreationMode);

            //m_CM.update_sites_visibiliy(displaySites);
        }

        /// <summary>
        /// Send IEEG read min/max/middle to the IEEG menu
        /// </summary>
        public void send_IEEG_parameters_to_menu()
        {            
            for (int ii = 0; ii < m_CM.IEEG_columns_nb(); ++ii)
            {
                iEEGDataParameters iEEGDataParams;
                iEEGDataParams.minAmp       = m_CM.IEEG_col(ii).minAmp;
                iEEGDataParams.maxAmp       = m_CM.IEEG_col(ii).maxAmp;

                iEEGDataParams.spanMin      = m_CM.IEEG_col(ii).spanMin;
                iEEGDataParams.middle       = m_CM.IEEG_col(ii).middle;
                iEEGDataParams.spanMax      = m_CM.IEEG_col(ii).spanMax;

                iEEGDataParams.gain         = m_CM.IEEG_col(ii).gainBubbles;
                iEEGDataParams.maxDistance  = m_CM.IEEG_col(ii).maxDistanceElec;
                iEEGDataParams.columnId     = ii;

                iEEGDataParams.alphaMin     = m_CM.IEEG_col(ii).alphaMin;
                iEEGDataParams.alphaMax     = m_CM.IEEG_col(ii).alphaMax; // useless

                SendIEEGParameters.Invoke(iEEGDataParams);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public void send_FMRI_parameters_to_menu() // TODO
        {
            for (int ii = 0; ii < m_CM.IEEG_columns_nb(); ++ii)
            {
                FMriDataParameters FMRIDataParams;
                FMRIDataParams.alpha = m_CM.FMRI_col(ii).alpha;
                FMRIDataParams.calMin = m_CM.FMRI_col(ii).calMin;
                FMRIDataParams.calMax = m_CM.FMRI_col(ii).calMax;
                FMRIDataParams.columnId = ii;

                FMRIDataParams.calValues = m_CM.DLLVolumeFMriList[ii].retrieve_extreme_values(); 
                FMRIDataParams.singlePatient = singlePatient;
                
                SendFMRIParameters.Invoke(FMRIDataParams);
            }
        }

        /// <summary>
        /// Send an event for displaying a message on the scene screen
        /// </summary>
        /// <param name="message"></param>
        /// <param name="duration"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void display_sceen_message(string message, float duration, int width, int height)
        {            
            display_screen_message_event.Invoke(message, duration, width , height);
        }

        /// <summary>
        /// Send an event for displaying a progressbar on the scene screen
        /// </summary>
        /// <param name="value"></param>
        /// <param name="duration"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void display_progressbar(float value, float duration, int width, int height)
        {
            display_scene_progressbar_event.Invoke(duration, width, height, value);
        }

        /// <summary>
        /// 
        /// </summary>
        public void compate_sites()
        {
            display_sceen_message("Select site to compare ", 3f, 200, 40);
            data_.compareSite = true;
        }

        /// <summary>
        /// Unselect the site of the corresponding column
        /// </summary>
        /// <param name="columnId"></param>
        public void unselect_site(int columnId)
        {
            m_CM.col(columnId).id_selected_site = -1; // unselect current site
            m_CM.update_all_columns_sites_rendering(data_);
            ClickSite.Invoke(-1); // update menu
        }

        
        #endregion functions
    }

    /// <summary>
    /// The job class for doing the textures generators computing stuff
    /// </summary>
    public class ComputeGeneratorsJob : ThreadedJob
    {
        public ComputeGeneratorsJob()
        { }

        public SceneStatesInfo data_ = null;
        public Column3DViewManager cm_ = null;

        protected override void ThreadFunction()
        {
            bool useMultiCPU = true;
            bool addValues = false;
            bool ratioDistances = true;
            
            data_.rwl.AcquireWriterLock(1000);
                data_.currentComputingState = 0f;
            data_.rwl.ReleaseWriterLock();

            // copy from main generators
            for (int ii = 0; ii < cm_.IEEG_columns_nb(); ++ii)
            {                
                for(int jj = 0; jj < cm_.meshSplitNb; ++jj)
                {
                    cm_.IEEG_col(ii).DLLBrainTextureGeneratorList[jj] = (DLL.MRIBrainGenerator)cm_.DLLCommonBrainTextureGeneratorList[jj].Clone();
                }
            }

            float offsetState = 1f / (2 * cm_.IEEG_columns_nb());

            // Do your threaded task. DON'T use the Unity API here
            if (data_.meshTypeToDisplay != SceneStatesInfo.MeshType.Inflated)
            {
                for (int ii = 0; ii < cm_.IEEG_columns_nb(); ++ii)
                {
                    data_.rwl.AcquireWriterLock(1000);
                        data_.currentComputingState += offsetState;
                    data_.rwl.ReleaseWriterLock();

                    float currentMaxDensity, currentMinInfluence, currentMaxInfluence;
                    float maxDensity = 1;

                    cm_.IEEG_col(ii).sharedMinInf = float.MaxValue;
                    cm_.IEEG_col(ii).sharedMaxInf = float.MinValue;

                    // update raw electrodes
                    cm_.IEEG_col(ii).update_DLL_sites_mask();

                    // splits
                    for(int jj = 0; jj < cm_.meshSplitNb; ++jj)
                    {
                        cm_.IEEG_col(ii).DLLBrainTextureGeneratorList[jj].init_octree(cm_.IEEG_col(ii).RawElectrodes);

                        
                        if(!cm_.IEEG_col(ii).DLLBrainTextureGeneratorList[jj].compute_distances(cm_.IEEG_col(ii).maxDistanceElec, true))
                        {
                            Debug.LogError("Abort computing"); // useless
                            return;
                        }

                        if(!cm_.IEEG_col(ii).DLLBrainTextureGeneratorList[jj].compute_influences(cm_.IEEG_col(ii), useMultiCPU, addValues, ratioDistances))
                        {
                            Debug.LogError("Abort computing"); // useless
                            return;
                        }
                        currentMaxDensity = cm_.IEEG_col(ii).DLLBrainTextureGeneratorList[jj].getMaximumDensity();
                        currentMinInfluence = cm_.IEEG_col(ii).DLLBrainTextureGeneratorList[jj].getMinimumInfluence();
                        currentMaxInfluence = cm_.IEEG_col(ii).DLLBrainTextureGeneratorList[jj].getMaximumInfluence();

                        if (currentMaxDensity > maxDensity)
                            maxDensity = currentMaxDensity;

                        if (currentMinInfluence < cm_.IEEG_col(ii).sharedMinInf)
                            cm_.IEEG_col(ii).sharedMinInf = currentMinInfluence;

                        if (currentMaxInfluence > cm_.IEEG_col(ii).sharedMaxInf)
                            cm_.IEEG_col(ii).sharedMaxInf = currentMaxInfluence;

                    }

                    data_.rwl.AcquireWriterLock(1000);
                        data_.currentComputingState += offsetState;
                    data_.rwl.ReleaseWriterLock();


                    // cuts
                    for (int jj = 0; jj < cm_.planesCutsCopy.Count; ++jj)
                    {
                        cm_.IEEG_col(ii).DLLMRITextureCutGeneratorList[jj].init_octree(cm_.IEEG_col(ii).RawElectrodes);
                        cm_.IEEG_col(ii).DLLMRITextureCutGeneratorList[jj].compute_distances(cm_.IEEG_col(ii).maxDistanceElec, true);

                        if (!cm_.IEEG_col(ii).DLLMRITextureCutGeneratorList[jj].compute_influences(cm_.IEEG_col(ii), useMultiCPU, addValues, ratioDistances))
                        {
                            Debug.LogError("Abort computing");
                            return;
                        }
     
                        currentMaxDensity = cm_.IEEG_col(ii).DLLMRITextureCutGeneratorList[jj].maximum_density();
                        currentMinInfluence = cm_.IEEG_col(ii).DLLMRITextureCutGeneratorList[jj].minimum_influence();
                        currentMaxInfluence = cm_.IEEG_col(ii).DLLMRITextureCutGeneratorList[jj].maximum_influence();

                        if (currentMaxDensity > maxDensity)
                            maxDensity = currentMaxDensity;

                        if (currentMinInfluence < cm_.IEEG_col(ii).sharedMinInf)
                            cm_.IEEG_col(ii).sharedMinInf = currentMinInfluence;

                        if (currentMaxInfluence > cm_.IEEG_col(ii).sharedMaxInf)
                            cm_.IEEG_col(ii).sharedMaxInf = currentMaxInfluence;
                    }
  
                    // synchronize max density
                    for (int jj = 0; jj < cm_.meshSplitNb; ++jj)
                        cm_.IEEG_col(ii).DLLBrainTextureGeneratorList[jj].synchronizeWithOthersGenerators(maxDensity, cm_.IEEG_col(ii).sharedMinInf, cm_.IEEG_col(ii).sharedMaxInf);
                    for (int jj = 0; jj < cm_.planesCutsCopy.Count; ++jj)
                        cm_.IEEG_col(ii).DLLMRITextureCutGeneratorList[jj].synchronize_with_others_generators(maxDensity, cm_.IEEG_col(ii).sharedMinInf, cm_.IEEG_col(ii).sharedMaxInf);

                    for (int jj = 0; jj < cm_.meshSplitNb; ++jj)
                        cm_.IEEG_col(ii).DLLBrainTextureGeneratorList[jj].ajustInfluencesToColormap();
                    for (int jj = 0; jj < cm_.planesCutsCopy.Count; ++jj)
                        cm_.IEEG_col(ii).DLLMRITextureCutGeneratorList[jj].ajust_influences_to_colormap();
                }                
            }
            else // if inflated white mesh is displayed, we compute only on the complete white mesh
            {
                for (int ii = 0; ii < cm_.IEEG_columns_nb(); ++ii)
                {
                    data_.rwl.AcquireWriterLock(1000);
                        data_.currentComputingState += offsetState;
                    data_.rwl.ReleaseWriterLock();

                    float currentMaxDensity, currentMinInfluence, currentMaxInfluence;
                    float maxDensity = 1;

                    cm_.IEEG_col(ii).sharedMinInf = float.MaxValue;
                    cm_.IEEG_col(ii).sharedMaxInf = float.MinValue;

                    // update raw electrodes
                    cm_.IEEG_col(ii).update_DLL_sites_mask();

                    // splits
                    for (int jj = 0; jj < cm_.meshSplitNb; ++jj)
                    {
                        cm_.IEEG_col(ii).DLLBrainTextureGeneratorList[jj].reset(cm_.DLLSplittedWhiteMeshesList[jj], cm_.DLLVolume); // TODO : ?
                        cm_.IEEG_col(ii).DLLBrainTextureGeneratorList[jj].init_octree(cm_.IEEG_col(ii).RawElectrodes);

                        if(!cm_.IEEG_col(ii).DLLBrainTextureGeneratorList[jj].compute_distances(cm_.IEEG_col(ii).maxDistanceElec, true))
                        {
                            Debug.LogError("Abort computing");
                            return;
                        }

                        if(!cm_.IEEG_col(ii).DLLBrainTextureGeneratorList[jj].compute_influences(cm_.IEEG_col(ii), useMultiCPU, addValues, ratioDistances))
                        {
                            Debug.LogError("Abort computing");
                            return;
                        }

                        currentMaxDensity = cm_.IEEG_col(ii).DLLBrainTextureGeneratorList[jj].getMaximumDensity();
                        currentMinInfluence = cm_.IEEG_col(ii).DLLBrainTextureGeneratorList[jj].getMinimumInfluence();
                        currentMaxInfluence = cm_.IEEG_col(ii).DLLBrainTextureGeneratorList[jj].getMaximumInfluence();

                        if (currentMaxDensity > maxDensity)
                            maxDensity = currentMaxDensity;

                        if (currentMinInfluence < cm_.IEEG_col(ii).sharedMinInf)
                            cm_.IEEG_col(ii).sharedMinInf = currentMinInfluence;

                        if (currentMaxInfluence > cm_.IEEG_col(ii).sharedMaxInf)
                            cm_.IEEG_col(ii).sharedMaxInf = currentMaxInfluence;
                    }

                    data_.rwl.AcquireWriterLock(1000);
                        data_.currentComputingState += offsetState;
                    data_.rwl.ReleaseWriterLock();


                    // synchronize max density
                    for (int jj = 0; jj < cm_.meshSplitNb; ++jj)
                        cm_.IEEG_col(ii).DLLBrainTextureGeneratorList[jj].synchronizeWithOthersGenerators(maxDensity, cm_.IEEG_col(ii).sharedMinInf, cm_.IEEG_col(ii).sharedMaxInf);

                    for (int jj = 0; jj < cm_.meshSplitNb; ++jj)
                        cm_.IEEG_col(ii).DLLBrainTextureGeneratorList[jj].ajustInfluencesToColormap();
                }
            }
        }

        protected override void OnFinished()
        { }     
    }
}