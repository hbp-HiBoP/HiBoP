
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
using HBP.UI.Module3D;


namespace HBP.Module3D
{
    #region Struct
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
    #endregion

    namespace Events
    {
        /// <summary>
        /// UI event for sending a plot info request to the outside UI (params : plotRequest)
        /// </summary>
        [System.Serializable]
        public class SiteInfoRequest : UnityEvent<SiteRequest> { }
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
    public abstract class Base3DScene : MonoBehaviour
    {
        #region Properties
        public string Name
        {
            get
            {
                return Visualisation.Name;
            }
        }
        public abstract SceneType Type { get; }
        protected Data.Visualisation.Visualisation m_Visualisation;
        public virtual Data.Visualisation.Visualisation Visualisation
        {
            get
            {
                return m_Visualisation;
            }
            set
            {
                m_Visualisation = value;
            }
        }

        private bool m_displaySitesNames = false; // DELETEME
        private Camera m_lastCamera = null; // DELETEME

        private List<Plane> m_planesList = new List<Plane>();         /**< cut planes list */
        public List<Plane> PlanesList { get { return m_planesList; } }

        public SceneStatesInfo SceneInformation { get; set; } /**<  data of the scene */

        protected ModesManager m_ModesManager = null; /**< modes of the scene */
        public ModesManager ModesManager
        {
            get { return m_ModesManager; }
        }

        protected DisplayedObjects3DView m_DisplayedObjects = null; /**< displayable objects of the scene */
        protected MNIObjects m_MNIObjects = null;

        protected Column3DViewManager m_Column3DViewManager = null; /**< column data manager */
        public Column3DViewManager Column3DViewManager { get { return m_Column3DViewManager; } }

        public UIOverlayManager m_uiOverlayManager; /**< UI overlay manager of the scenes */ // DELETEME

        protected TriEraser m_TriEraser = new TriEraser();
        // threads / jobs
        protected ComputeGeneratorsJob m_ComputeGeneratorsJob = null; /**< generator computing job */

        // events
        public Events.UpdatePlanes UpdatePlanes = new Events.UpdatePlanes(); 
        public Events.ModifyPlanesCuts ModifyPlanesCuts = new Events.ModifyPlanesCuts();
        public Events.SendIEEGParameters SendIEEGParameters = new Events.SendIEEGParameters();
        public Events.SendFMRIParameters SendFMRIParameters = new Events.SendFMRIParameters();
        public Events.SendColormapEvent SendColorMapValues = new Events.SendColormapEvent();
        public Events.SendModeSpecifications SendModeSpecifications = new Events.SendModeSpecifications(); 
        public Events.DisplaySceneMessage display_screen_message_event = new Events.DisplaySceneMessage();
        public Events.DisplaySceneProgressBar display_scene_progressbar_event = new Events.DisplaySceneProgressBar();
        public Events.SiteInfoRequest SiteInfoRequest = new Events.SiteInfoRequest();
        public Events.UpdateCutsInUI UpdateCutsInUI = new Events.UpdateCutsInUI();
        public Events.DefineSelectedColumn DefineSelectedColumn = new Events.DefineSelectedColumn();
        public Events.UpdateTimeInUI UpdateTimeInUI = new Events.UpdateTimeInUI();
        public Events.UpdateDisplayedSitesInfo UpdateDisplayedSitesInfo = new Events.UpdateDisplayedSitesInfo();
        public Events.AskROIUpdate AskROIUpdateEvent = new Events.AskROIUpdate();
        public Events.UpdateCameraTarget UpdateCameraTarget = new Events.UpdateCameraTarget();
        public Events.ClickPlot ClickSite = new Events.ClickPlot();
        public Events.IRMCalValuesUpdate IRMCalValuesUpdate = new Events.IRMCalValuesUpdate();
        #endregion

        #region Private Methods
        protected void Awake()
        {         
            int idScript = TimeExecution.get_ID();
            TimeExecution.start_awake(idScript, TimeExecution.ScriptsId.Base3DScene);

            m_DisplayedObjects = new DisplayedObjects3DView();
            SceneInformation = new SceneStatesInfo();
            m_Column3DViewManager = GetComponent<Column3DViewManager>();

            // set meshes layer
            switch(Type)
            {
                case SceneType.SinglePatient:
                    SceneInformation.MeshesLayerName = "Meshes_SP";
                    break;
                case SceneType.MultiPatients:
                    SceneInformation.MeshesLayerName = "Meshes_MP";
                    break;
            }

            // init modes            
            m_ModesManager = transform.Find("modes").gameObject.GetComponent<ModesManager>();
            m_ModesManager.init(this);
            m_ModesManager.SendModeSpecifications.AddListener((specs) =>
            {
                SendModeSpecifications.Invoke(specs);

                // update scene visibility (useless)
                //UnityEngine.Profiling.Profiler.BeginSample("TEST-Base3DScene-SendModeSpecifications update_scene_items_visibility");
                //    update_scene_items_visibility(specs.itemMaskDisplay[0], specs.itemMaskDisplay[1], specs.itemMaskDisplay[2]);
                //UnityEngine.Profiling.Profiler.EndSample();
            });

            // init GO
            InitializeSceneGameObjects();


            TimeExecution.end_awake(idScript, TimeExecution.ScriptsId.Base3DScene, gameObject);
        }
        protected void Update()
        {
            UnityEngine.Profiling.Profiler.BeginSample("TEST-Base3DScene-Update: set_current_mode_specifications");
            SetCurrentModeSpecifications();
            UnityEngine.Profiling.Profiler.EndSample();

            if (m_ModesManager.currentIdMode() == Mode.ModesId.NoPathDefined)
                return;

            UnityEngine.Profiling.Profiler.BeginSample("TEST-Base3DScene-Update");

            // TEMP : useless
            for (int ii = 0; ii < SceneInformation.removeFrontPlaneList.Count; ++ii)
                SceneInformation.removeFrontPlaneList[ii] = 0;

            // check if we must perform new cuts of the brain            
            if (SceneInformation.updateCutMeshGeometry)
            {
                SceneInformation.geometryUpToDate = false;
                Column3DViewManager.planesCutsCopy = new List<Plane>();
                for (int ii = 0; ii < m_planesList.Count; ++ii)
                    Column3DViewManager.planesCutsCopy.Add(new Plane(m_planesList[ii].point, m_planesList[ii].normal));

                UnityEngine.Profiling.Profiler.BeginSample("TEST-Base3DScene-Update compute_meshes_cuts 1");
                ComputeMeshesCut();
                UnityEngine.Profiling.Profiler.EndSample();
                UnityEngine.Profiling.Profiler.BeginSample("TEST-Base3DScene-Update compute_MRI_textures 1");

                // create textures 
                ComputeMRITextures();

                ComputeFMRITextures(-1, -1);

                SceneInformation.geometryUpToDate = true;
                

                UnityEngine.Profiling.Profiler.EndSample();
            }


            // check job state
            if (m_ComputeGeneratorsJob != null)
            {
                float currState;
                SceneInformation.rwl.AcquireReaderLock(1000);
                currState = SceneInformation.currentComputingState;
                SceneInformation.rwl.ReleaseReaderLock();

                DisplayScreenMessage("Computing...", 50f, 250, 40);
                DisplayProgressbar(currState, 50f, 250, 40);

                if (m_ComputeGeneratorsJob.Update())
                {                    
                    FinalizeGeneratorsComputing();
                    ComputeIEEGTextures();                   
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
                for (int ii = 0; ii < m_Column3DViewManager.SelectedColumn.plotsGO.Count; ++ii)
                    for (int jj = 0; jj < m_Column3DViewManager.SelectedColumn.plotsGO[ii].Count; ++jj)
                        for (int kk = 0; kk < m_Column3DViewManager.SelectedColumn.plotsGO[ii][jj].Count; ++kk)
                        {
                            GUI.contentColor =  (kk == 0) ? Color.red : Color.black;
                            Site site = m_Column3DViewManager.SelectedColumn.plotsGO[ii][jj][kk].GetComponent<Site>();
                            if (site || site.Information.IsExcluded || site.Information.IsBlackListed)
                                continue;

                            Vector3 pos = m_lastCamera.WorldToScreenPoint(site.transform.position);
                            Vector2 realPos = new Vector2(pos.x, Screen.height - pos.y);
                            if(m_lastCamera.pixelRect.Contains(realPos))                            
                                GUI.Label(new Rect(realPos.x -10, realPos.y, 100, 20), site.name);
                        }
            }
        }
        /// <summary>
        /// 
        /// When to call ?  changes in DLLCutColorScheme, MRICalMinFactor, MRICalMaxFactor
        /// </summary>
        /// <param name="indexCut"></param>
        /// <param name="indexColumn"></param>
        private void ComputeMRITextures(int indexCut = -1, int indexColumn = -1)
        {
            if (SceneInformation.meshTypeToDisplay == SceneStatesInfo.MeshType.Inflated)
                return;

            bool allColumns = indexColumn == -1;
            bool allCuts = indexCut == -1;

            int[] columnsIndexes = allColumns ? Enumerable.Range(0, m_Column3DViewManager.Columns.Count).ToArray() : new int[1] { indexColumn };
            int[] cutsIndexes = allCuts ? Enumerable.Range(0, m_planesList.Count).ToArray() : new int[1] { indexCut };

            UnityEngine.Profiling.Profiler.BeginSample("TEST-Base3DScene compute_MRI_textures 0 create_MRI_texture ");

            for (int ii = 0; ii < columnsIndexes.Length; ++ii)
                for (int jj = 0; jj < cutsIndexes.Length; ++jj)
                    m_Column3DViewManager.create_MRI_texture(cutsIndexes[jj], columnsIndexes[ii]);

            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("TEST-Base3DScene compute_MRI_textures 1 compute_GUI_textures");
            ComputeGUITextures(indexCut, m_Column3DViewManager.SelectedColumnID);
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("TEST-Base3DScene compute_MRI_textures 2 update_GUI_textures");
            UpdateGUITextures();
            UnityEngine.Profiling.Profiler.EndSample();
        }
        /// <summary>
        /// 
        /// When to call ? changes in IEEGColumn.currentTimeLineID, IEEGColumn.alphaMin, IEEGColumn.alphaMax / DLLCutColorScheme
        /// </summary>
        /// <param name="indexCut"></param>
        /// <param name="indexColumn"></param>
        private void ComputeIEEGTextures(int indexCut = -1, int indexColumn = -1, bool surface = true, bool cuts = true, bool plots = true)
        {
            UnityEngine.Profiling.Profiler.BeginSample("TEST-compute_IEEG_textures");

            UnityEngine.Profiling.Profiler.BeginSample("TEST-compute_IEEG_textures 0");
            bool allColumns = indexColumn == -1;
            bool allCuts = indexCut == -1;

            int[] columnsIndexes = allColumns ? Enumerable.Range(0, m_Column3DViewManager.Columns.Count).ToArray() : new int[1] { indexColumn };
            int[] cutsIndexes = allCuts ? Enumerable.Range(0, m_planesList.Count).ToArray() : new int[1] { indexCut };

            for (int ii = 0; ii < columnsIndexes.Length; ++ii)
            {
                if (Column3DViewManager.Columns[columnsIndexes[ii]].Type == Column3DView.ColumnType.FMRI)
                    return;

                Column3DViewIEEG currCol = (Column3DViewIEEG)Column3DViewManager.Columns[columnsIndexes[ii]];

                // brain surface
                if (surface)
                    if (!m_Column3DViewManager.compute_surface_brain_UV_with_IEEG((SceneInformation.meshTypeToDisplay == SceneStatesInfo.MeshType.Inflated), columnsIndexes[ii]))
                        return;

                // brain cuts
                if (cuts)
                    for (int jj = 0; jj < cutsIndexes.Length; ++jj)
                        m_Column3DViewManager.color_cuts_textures_with_IEEG(columnsIndexes[ii], cutsIndexes[jj]);

                if (plots)
                {
                    currCol.update_sites_size_and_color_arrays_for_IEEG();
                    currCol.update_sites_rendering(SceneInformation, null);
                }
            }
            UnityEngine.Profiling.Profiler.EndSample();
            UnityEngine.Profiling.Profiler.BeginSample("TEST-compute_IEEG_textures 1 compute_GUI_textures");
            ComputeGUITextures(indexCut, m_Column3DViewManager.SelectedColumnID);
            UnityEngine.Profiling.Profiler.EndSample();

            UnityEngine.Profiling.Profiler.BeginSample("TEST-compute_IEEG_textures 2 update_GUI_textures");
            UpdateGUITextures();
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
        private void ComputeFMRITextures(int indexCut = -1, int indexColumn = -1, bool surface = true, bool cuts = true)
        {
            bool allColumns = indexColumn == -1;
            bool allCuts = indexCut == -1;

            int[] columnsIndexes = allColumns ? Enumerable.Range(0, m_Column3DViewManager.ColumnsFMRI.Count).ToArray() : new int[1] { indexColumn };
            int[] cutsIndexes = allCuts ? Enumerable.Range(0, m_planesList.Count).ToArray() : new int[1] { indexCut };

            for (int ii = 0; ii < columnsIndexes.Length; ++ii)
            {
                //Column3DViewFMRI currCol = Column3DViewManager.FMRI_col(columnsIndexes[ii]);

                // brain cuts
                if (cuts)
                    for (int jj = 0; jj < cutsIndexes.Length; ++jj)
                        m_Column3DViewManager.color_cuts_textures_with_FMRI(columnsIndexes[ii], cutsIndexes[jj]);
            }

            ComputeGUITextures(indexCut, indexColumn);

            UpdateGUITextures();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="indexCut"></param>
        /// <param name="indexColumn"></param>
        private void ComputeGUITextures(int indexCut = -1, int indexColumn = -1)
        {
            bool allColumns = indexColumn == -1;
            bool allCuts = indexCut == -1;

            int[] columnsIndexes = allColumns ? Enumerable.Range(0, m_Column3DViewManager.Columns.Count).ToArray() : new int[1] { indexColumn };
            int[] cutsIndexes = allCuts ? Enumerable.Range(0, m_planesList.Count).ToArray() : new int[1] { indexCut };

            for (int ii = 0; ii < columnsIndexes.Length; ++ii)
            {
                Column3DView currCol = m_Column3DViewManager.Columns[columnsIndexes[ii]];

                for (int jj = 0; jj < PlanesList.Count; ++jj)
                {
                    switch (currCol.Type)
                    {
                        case Column3DView.ColumnType.FMRI:
                            m_Column3DViewManager.create_GUI_FMRI_texture(cutsIndexes[jj], columnsIndexes[ii]);
                            break;
                        case Column3DView.ColumnType.IEEG:
                            if (!SceneInformation.generatorUpToDate)
                                m_Column3DViewManager.create_GUI_MRI_texture(cutsIndexes[jj], columnsIndexes[ii]);
                            else
                                m_Column3DViewManager.create_GUI_IEEG_texture(cutsIndexes[jj], columnsIndexes[ii]);
                            break;
                        default:
                            break;
                    }
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="indexCut"></param>
        private void UpdateGUITextures()
        {
            Column3DView currCol = m_Column3DViewManager.Columns[m_Column3DViewManager.SelectedColumnID];
            List<Texture2D> texturesToDisplay = null;
            switch (currCol.Type)
            {
                case Column3DView.ColumnType.FMRI:
                    texturesToDisplay = ((Column3DViewFMRI)currCol).guiBrainCutWithFMRITextures;
                    break;
                case Column3DView.ColumnType.IEEG:
                    if (!SceneInformation.generatorUpToDate)
                        texturesToDisplay = currCol.guiBrainCutTextures;
                    else
                        texturesToDisplay = ((Column3DViewIEEG)currCol).guiBrainCutWithIEEGTextures;
                    break;
                default:
                    break;
            }
            UpdateCutsInUI.Invoke(texturesToDisplay, m_Column3DViewManager.SelectedColumnID, m_planesList.Count);
        }
        /// <summary>
        /// 
        /// </summary>
        private void FinalizeGeneratorsComputing()
        {
            // computing ended
            m_ComputeGeneratorsJob = null;

            // generators are now up to date
            SceneInformation.generatorUpToDate = true;
            SceneInformation.iEEGOutdated = false;

            // send inf values to overlays
            for (int ii = 0; ii < m_Column3DViewManager.ColumnsIEEG.Count; ++ii)
            {
                float maxValue = Math.Max(Math.Abs(m_Column3DViewManager.ColumnsIEEG[ii].sharedMinInf), Math.Abs(m_Column3DViewManager.ColumnsIEEG[ii].sharedMaxInf));
                float minValue = -maxValue;
                minValue += m_Column3DViewManager.ColumnsIEEG[ii].middle;
                maxValue += m_Column3DViewManager.ColumnsIEEG[ii].middle;
                SendColorMapValues.Invoke(minValue, m_Column3DViewManager.ColumnsIEEG[ii].middle, maxValue, ii);
            }

            // amplitudes are not displayed yet
            for (int ii = 0; ii < m_Column3DViewManager.ColumnsIEEG.Count; ++ii)
                m_Column3DViewManager.ColumnsIEEG[ii].updateIEEG = true;

            //####### CHECK ACCESS
            if (!m_ModesManager.functionAccess(Mode.FunctionsId.post_updateGenerators))
            {
                Debug.LogError("-ERROR : Base3DScene::post_updateGenerators -> no acess for mode : " + m_ModesManager.currentModeName());
            }
            //##################

            // update plots visibility
            m_Column3DViewManager.update_all_columns_sites_rendering(SceneInformation);

            DisplayScreenMessage("Computing finished !", 1f, 250, 40);
            DisplayProgressbar(1f, 1f, 250, 40);

            //####### UDPATE MODE
            m_ModesManager.updateMode(Mode.FunctionsId.post_updateGenerators);
            //##################
        }
        /// <summary>
        /// Init gameobjects of the scene
        /// </summary>
        protected void InitializeSceneGameObjects()
        {
            // init parents 
            m_DisplayedObjects.sitesMeshesParent = transform.Find("base electrodes").gameObject;
            m_DisplayedObjects.brainSurfaceMeshesParent = transform.Find("meshes").Find("brains").gameObject;
            m_DisplayedObjects.brainCutMeshesParent = transform.Find("meshes").Find("cuts").gameObject;

            // init lights
            m_DisplayedObjects.sharedDirLight = transform.parent.Find("directionnal light").gameObject;

            // init default planes
            m_planesList = new List<Plane>();
            m_Column3DViewManager.idPlanesOrientationList = new List<int>();
            m_Column3DViewManager.planesOrientationFlipList = new List<bool>();
            SceneInformation.removeFrontPlaneList = new List<int>();
            SceneInformation.numberOfCutsPerPlane = new List<int>();
            m_DisplayedObjects.brainCutMeshes = new List<GameObject>();

            UpdateBrainSurfaceColor(ColorType.BrainColor);
            UpdateColormap(ColorType.MatLab, false);
            UpdateBrainCutColor(ColorType.Default, true);
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Display the site names when pressing SPACE
        /// </summary>
        /// <param name="camera"></param>
        public void DisplaySitesName(Camera camera) // TODO : do this in overlay manager and not here
        {
            m_displaySitesNames = true;
            m_lastCamera = camera;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="updateColors"></param>
        public void UpdateColormap(ColorType color, bool updateColors = true)
        {
            Column3DViewManager.update_colormap(color);
            if (updateColors)
                Column3DViewManager.reset_colors();

            SharedMaterials.Brain.BrainMaterials[this].SetTexture("_ColorTex", Column3DViewManager.brainColorMapTexture);

            if (SceneInformation.geometryUpToDate && !SceneInformation.iEEGOutdated)
                ComputeIEEGTextures();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        public void UpdateBrainSurfaceColor(ColorType color)
        {
            Column3DViewManager.m_BrainColor = color;
            DLL.Texture tex = DLL.Texture.generate_1D_color_texture(Column3DViewManager.m_BrainColor);
            tex.update_texture_2D(Column3DViewManager.brainColorTexture);

            SharedMaterials.Brain.BrainMaterials[this].SetTexture("_MainTex", Column3DViewManager.brainColorTexture);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="updateColors"></param>
        public void UpdateBrainCutColor(ColorType color, bool updateColors = true)
        {
            Column3DViewManager.update_brain_cut_color(color);
            if (updateColors)
                Column3DViewManager.reset_colors();

            SceneInformation.updateCutMeshGeometry = true;
            SceneInformation.iEEGOutdated = true;

            //####### UDPATE MODE
            m_ModesManager.updateMode(Mode.FunctionsId.updatePlane); // TEMP
            //##################
        }
        /// <summary>
        /// Reset the gameobjects of the scene
        /// </summary>
        public void ResetSceneGameObjects()
        {
            // destroy meshes
            for (int ii = 0; ii < m_DisplayedObjects.brainSurfaceMeshes.Count; ++ii)
            {
                Destroy(m_DisplayedObjects.brainSurfaceMeshes[ii]);
            }
            for (int ii = 0; ii < m_DisplayedObjects.brainCutMeshes.Count; ++ii)
            {
                m_DisplayedObjects.brainCutMeshes[ii].SetActive(false);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="nbSplits"></param>
        public void ResetSplitsNumber(int nbSplits)
        {
            if (m_Column3DViewManager.meshSplitNb == nbSplits)
                return;

            m_Column3DViewManager.meshSplitNb = nbSplits;

            if(m_DisplayedObjects.brainSurfaceMeshes.Count > 0)
                for (int ii = 0; ii < m_DisplayedObjects.brainSurfaceMeshes.Count; ++ii)
                    Destroy(m_DisplayedObjects.brainSurfaceMeshes[ii]);

            // reset meshes
            m_DisplayedObjects.brainSurfaceMeshes = new List<GameObject>(m_Column3DViewManager.meshSplitNb);
            for (int ii = 0; ii < m_Column3DViewManager.meshSplitNb; ++ii)
            {
                m_DisplayedObjects.brainSurfaceMeshes.Add(Instantiate(GlobalGOPreloaded.Brain));
                m_DisplayedObjects.brainSurfaceMeshes[ii].GetComponent<Renderer>().sharedMaterial = SharedMaterials.Brain.BrainMaterials[this];
                m_DisplayedObjects.brainSurfaceMeshes[ii].name = "brain_" + ii;
                m_DisplayedObjects.brainSurfaceMeshes[ii].transform.parent = m_DisplayedObjects.brainSurfaceMeshesParent.transform;
                m_DisplayedObjects.brainSurfaceMeshes[ii].layer = LayerMask.NameToLayer(SceneInformation.MeshesLayerName);
                m_DisplayedObjects.brainSurfaceMeshes[ii].AddComponent<MeshCollider>();
                m_DisplayedObjects.brainSurfaceMeshes[ii].SetActive(true);
            }

            m_Column3DViewManager.reset_splits_nb(nbSplits);
        }
        /// <summary>
        /// Set UI screen space/overlays layers mask settings corresponding to the current mode of the scene
        /// </summary>
        public void SetCurrentModeSpecifications(bool force = false)
        {
            m_ModesManager.set_current_mode_specifications(force);
        }
        /// <summary>
        /// Update the sites masks
        /// </summary>
        /// <param name="allColumn"></param>
        /// <param name="siteGO"></param>
        /// <param name="action"> 0 : excluded / 1 : included / 2 : blacklisted / 3 : unblacklist / 4 : highlight / 5 : unhighlight / 6 : marked / 7 : unmarked </param>
        /// <param name="range"> 0 : a plot / 1 : all plots from an electrode / 2 : all plots from a patient / 3 : all highlighted / 4 : all unhighlighted 
        /// / 5 : all plots / 6 : in ROI / 7 : not in ROI / 8 : names filter / 9 : mars filter / 10 : broadman filter </param>
        public void UpdateSitesMasks(bool allColumn, GameObject siteGO, SiteAction action = SiteAction.Exclude, SiteFilter filter = SiteFilter.Specific, string nameFilter = "")
        {
            //####### CHECK ACESS
            if (!m_ModesManager.functionAccess(Mode.FunctionsId.updateMaskPlot))
            {
                Debug.LogError("-ERROR : Base3DScene::updateMaskPlot -> no acess for mode : " + m_ModesManager.currentModeName());
                return;
            }
            //##################

            //Column3DView col = m_CM.currentColumn();

            List<List<int>> colIdPlots = new List<List<int>>(m_Column3DViewManager.Columns.Count);
            for(int ii = 0; ii < m_Column3DViewManager.Columns.Count; ++ii)
            {
                colIdPlots.Add(new List<int>(m_Column3DViewManager.SitesList.Count));

                if(!allColumn)
                {
                    if (ii != m_Column3DViewManager.SelectedColumnID)
                        continue;
                }

                switch(filter)
                {
                    case SiteFilter.Specific:
                        {// one specific plot
                            colIdPlots[ii].Add(siteGO.GetComponent<Site>().Information.GlobalID);
                        }
                    break;
                    case SiteFilter.Electrode:
                        { // all plots from an electrode
                            Transform parentElectrode = siteGO.transform.parent;
                            for (int jj = 0; jj < parentElectrode.childCount; ++jj)
                                colIdPlots[ii].Add(parentElectrode.GetChild(jj).gameObject.GetComponent<Site>().Information.GlobalID);
                        }
                    break;
                    case SiteFilter.Patient:
                        { // all plots from a patient
                            Transform parentPatient = siteGO.transform.parent.parent;
                            for (int jj = 0; jj < parentPatient.childCount; ++jj)
                            {
                                Transform parentElectrode = parentPatient.GetChild(jj);
                                for (int kk = 0; kk < parentElectrode.childCount; kk++)
                                {
                                    colIdPlots[ii].Add(parentElectrode.GetChild(kk).gameObject.GetComponent<Site>().Information.GlobalID);
                                }
                            }
                        }
                    break;
                    case SiteFilter.Highlighted: // all highlighted plots
                        {
                            for (int jj = 0; jj < m_Column3DViewManager.Columns[ii].Sites.Count; ++jj)
                            {                                
                                if (m_Column3DViewManager.Columns[ii].Sites[jj].Information.IsHighlighted)
                                    colIdPlots[ii].Add(jj);
                            }
                        }
                        break;
                    case SiteFilter.Unhighlighted: // all unhighlighted plots
                        {                            
                            for (int jj = 0; jj < m_Column3DViewManager.Columns[ii].Sites.Count; ++jj)
                            {
                                if (!m_Column3DViewManager.Columns[ii].Sites[jj].Information.IsHighlighted)
                                    colIdPlots[ii].Add(jj);
                            }
                        }
                        break;
                    case SiteFilter.All: // all plots
                        {
                            for (int jj = 0; jj < m_Column3DViewManager.Columns[ii].Sites.Count; ++jj)
                            {                                
                                colIdPlots[ii].Add(jj);
                            }
                        }
                        break;
                    case SiteFilter.InRegionOfInterest: // in ROI
                        {
                            for (int jj = 0; jj < m_Column3DViewManager.Columns[ii].Sites.Count; ++jj)
                            {
                                if (!m_Column3DViewManager.Columns[ii].Sites[jj].Information.IsInROI)
                                    colIdPlots[ii].Add(jj);
                            }
                        }
                        break;
                    case SiteFilter.OutOfRegionOfInterest: // no in ROI
                        {
                            for (int jj = 0; jj < m_Column3DViewManager.Columns[ii].Sites.Count; ++jj)
                            {
                                if (m_Column3DViewManager.Columns[ii].Sites[jj].Information.IsInROI)
                                    colIdPlots[ii].Add(jj);
                            }
                        }
                        break;
                    case SiteFilter.Name: // names filter
                        {
                            for (int jj = 0; jj < m_Column3DViewManager.Columns[ii].Sites.Count; ++jj)
                            {
                                if (m_Column3DViewManager.Columns[ii].Sites[jj].Information.FullName.Contains(nameFilter))
                                    colIdPlots[ii].Add(jj);
                            }
                        }
                        break;
                    case SiteFilter.MarsAtlas: // mars filter
                        {                            
                            for (int jj = 0; jj < m_Column3DViewManager.Columns[ii].Sites.Count; ++jj)
                            {
                                if (GlobalGOPreloaded.MarsAtlasIndex.full_name(m_Column3DViewManager.Columns[ii].Sites[jj].Information.MarsAtlasIndex).Contains(nameFilter))
                                    colIdPlots[ii].Add(jj);      
                            }
                        }
                        break;
                    case SiteFilter.Broadman: // broadman filter
                        {
                            for (int jj = 0; jj < m_Column3DViewManager.Columns[ii].Sites.Count; ++jj)
                            {
                                if (GlobalGOPreloaded.MarsAtlasIndex.broadman_area(m_Column3DViewManager.Columns[ii].Sites[jj].Information.MarsAtlasIndex).Contains(nameFilter))
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
                    switch (action)
                    {
                        case SiteAction.Include:
                            m_Column3DViewManager.Columns[ii].Sites[colIdPlots[ii][jj]].Information.IsExcluded = false;
                            break;
                        case SiteAction.Exclude:
                            m_Column3DViewManager.Columns[ii].Sites[colIdPlots[ii][jj]].Information.IsExcluded = true;
                            break;
                        case SiteAction.Blacklist:
                            m_Column3DViewManager.Columns[ii].Sites[colIdPlots[ii][jj]].Information.IsBlackListed = true;
                            break;
                        case SiteAction.Unblacklist:
                            m_Column3DViewManager.Columns[ii].Sites[colIdPlots[ii][jj]].Information.IsBlackListed = false;
                            break;
                        case SiteAction.Highlight:
                            m_Column3DViewManager.Columns[ii].Sites[colIdPlots[ii][jj]].Information.IsHighlighted = true;
                            break;
                        case SiteAction.Unhighlight:
                            m_Column3DViewManager.Columns[ii].Sites[colIdPlots[ii][jj]].Information.IsHighlighted = false;
                            break;
                        case SiteAction.Mark:
                            m_Column3DViewManager.Columns[ii].Sites[colIdPlots[ii][jj]].Information.IsMarked = true;
                            break;
                        case SiteAction.Unmark:
                            m_Column3DViewManager.Columns[ii].Sites[colIdPlots[ii][jj]].Information.IsMarked = false;
                            break;
                        default:
                            break;
                    }
                }
            }

            m_Column3DViewManager.update_all_columns_sites_rendering(SceneInformation);

            //####### UDPATE MODE
            m_ModesManager.updateMode(Mode.FunctionsId.updateMaskPlot);
            //##################
        }
        /// <summary>
        /// Set the mesh part to be displayed in the scene
        /// </summary>
        /// <param name="meshPartToDisplay"></param>
        public void UpdateMeshPartToDisplay(SceneStatesInfo.MeshPart meshPartToDisplay)
        {
            //####### CHECK ACESS
            if (!m_ModesManager.functionAccess(Mode.FunctionsId.setDisplayedMesh))
            {
                Debug.LogError("-ERROR : Base3DScene::setDisplayedMesh -> no acess for mode : " + m_ModesManager.currentModeName());
                return;
            }
            //##################

            if (!SceneInformation.geometryUpToDate)
                return;

            SceneInformation.meshPartToDisplay = meshPartToDisplay;
            SceneInformation.updateCutMeshGeometry = true;
            SceneInformation.iEEGOutdated = true;
            m_Column3DViewManager.update_all_columns_sites_rendering(SceneInformation);

            //####### UDPATE MODE
            m_ModesManager.updateMode(Mode.FunctionsId.setDisplayedMesh);
            //##################
        }
        /// <summary>
        /// Set the mesh type to be displayed in the scene
        /// </summary>
        /// <param name="meshTypeToDisplay"></param>
        public void UpdateMeshTypeToDisplay(SceneStatesInfo.MeshType meshTypeToDisplay)
        {
            //####### CHECK ACESS
            if (!m_ModesManager.functionAccess(Mode.FunctionsId.setDisplayedMesh))
            {
                Debug.LogError("-ERROR : Base3DScene::setDisplayedMesh -> no acess for mode : " + m_ModesManager.currentModeName());
                return;
            }
            //##################

            if (!SceneInformation.geometryUpToDate)
                return;

            switch(meshTypeToDisplay)
            {
                case SceneStatesInfo.MeshType.Hemi:
                    if (!SceneInformation.hemiMeshesAvailables)
                        return;
                    break;
                case SceneStatesInfo.MeshType.White:
                    if (!SceneInformation.whiteMeshesAvailables)
                        return;
                    break;
                case SceneStatesInfo.MeshType.Inflated:
                    if (!SceneInformation.whiteInflatedMeshesAvailables)
                        return;
                    break;
            }

            SceneInformation.meshTypeToDisplay = meshTypeToDisplay;
            SceneInformation.updateCutMeshGeometry = true;
            SceneInformation.iEEGOutdated = true;
            m_Column3DViewManager.update_all_columns_sites_rendering(SceneInformation);

            //####### UDPATE MODE
            m_ModesManager.updateMode(Mode.FunctionsId.setDisplayedMesh);
            //##################
        }
        /// <summary>
        /// Add a new cut plane
        /// </summary>
        public void AddCutPlane()
        {
            //####### CHECK ACESS
            if (!m_ModesManager.functionAccess(Mode.FunctionsId.addNewPlane))
            {
                Debug.LogError("-ERROR : Base3DScene::addNewPlane -> no acess for mode : " + m_ModesManager.currentModeName());
                return;
            }
            //##################

            m_planesList.Add(new Plane(new Vector3(0, 0, 0), new Vector3(1, 0, 0)));
            m_Column3DViewManager.idPlanesOrientationList.Add(0);
            m_Column3DViewManager.planesOrientationFlipList.Add(false);
            SceneInformation.removeFrontPlaneList.Add(0);
            SceneInformation.numberOfCutsPerPlane.Add(SceneInformation.defaultNbOfCutsPerPlane);

            GameObject cut = Instantiate(GlobalGOPreloaded.Cut);
            cut.GetComponent<Renderer>().sharedMaterial = SharedMaterials.Brain.CutMaterials[this];
            cut.name = "cut_" + (m_planesList.Count - 1);
            cut.transform.parent = m_DisplayedObjects.brainCutMeshesParent.transform; ;
            cut.AddComponent<MeshCollider>();
            cut.layer = LayerMask.NameToLayer(SceneInformation.MeshesLayerName);
            m_DisplayedObjects.brainCutMeshes.Add(cut);
            m_DisplayedObjects.brainCutMeshes[m_DisplayedObjects.brainCutMeshes.Count - 1].layer = LayerMask.NameToLayer(SceneInformation.MeshesLayerName);

            // update columns manager
            m_Column3DViewManager.update_cuts_nb(m_DisplayedObjects.brainCutMeshes.Count);

            // update plots visibility
            m_Column3DViewManager.update_all_columns_sites_rendering(SceneInformation);

            SceneInformation.updateCutMeshGeometry = true;
            SceneInformation.iEEGOutdated = true;

            //####### UDPATE MODE
            m_ModesManager.updateMode(Mode.FunctionsId.addNewPlane);
            //##################            
        }
        /// <summary>
        /// Remove the last cut plane
        /// </summary>
        public void RemoveLastCutPlane() // TODO : specify which cut plane
        {
            //####### CHECK ACESS
            if (!m_ModesManager.functionAccess(Mode.FunctionsId.removeLastPlane))
            {
                Debug.LogError("-ERROR : Base3DScene::removeLastPlane -> no acess for mode : " + m_ModesManager.currentModeName());
                return;
            }
            //##################

            m_planesList.RemoveAt(m_planesList.Count - 1);
            m_Column3DViewManager.idPlanesOrientationList.RemoveAt(m_Column3DViewManager.idPlanesOrientationList.Count - 1);
            m_Column3DViewManager.planesOrientationFlipList.RemoveAt(m_Column3DViewManager.planesOrientationFlipList.Count - 1);
            SceneInformation.removeFrontPlaneList.RemoveAt(SceneInformation.removeFrontPlaneList.Count - 1);
            SceneInformation.numberOfCutsPerPlane.RemoveAt(SceneInformation.numberOfCutsPerPlane.Count - 1);

            Destroy(m_DisplayedObjects.brainCutMeshes[m_DisplayedObjects.brainCutMeshes.Count - 1]);
            m_DisplayedObjects.brainCutMeshes.RemoveAt(m_DisplayedObjects.brainCutMeshes.Count - 1);

            // update columns manager
            m_Column3DViewManager.update_cuts_nb(m_DisplayedObjects.brainCutMeshes.Count);

            // update plots visibility
            m_Column3DViewManager.update_all_columns_sites_rendering(SceneInformation);

            // update cut images display with the new selected column
            //UpdateCutsInUI.Invoke(m_CM.getBrainCutTextureList(m_CM.idSelectedColumn, true, data_.generatorUpToDate, data_.displayLatenciesMode), m_CM.idSelectedColumn, m_CM.planesList.Count);

            SceneInformation.updateCutMeshGeometry = true;
            SceneInformation.iEEGOutdated = true;

            //####### UDPATE MODE
            m_ModesManager.updateMode(Mode.FunctionsId.removeLastPlane);
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
        public void UpdateCutPlane(int idOrientation, bool flip, bool removeFrontPlane, Vector3 customNormal, int idPlane, float position)
        {
            //####### CHECK ACESS
            if (!m_ModesManager.functionAccess(Mode.FunctionsId.updatePlane))
            {
                Debug.LogError("-ERROR : Base3DScene::updatePlane -> no acess for mode : " + m_ModesManager.currentModeName());
                return;
            }
            //##################

            Plane newPlane = new Plane(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
            if (idOrientation == 3 || !SceneInformation.mriLoaded) // custom normal
            {
                if (customNormal.x != 0 || customNormal.y != 0 || customNormal.z != 0)
                    newPlane.normal = customNormal;
                else
                    newPlane.normal = new Vector3(1, 0, 0);
            }
            else
            {
                m_Column3DViewManager.DLLVolume.set_plane_with_orientation(newPlane, idOrientation, flip);
            }

            m_planesList[idPlane].normal = newPlane.normal;
            m_Column3DViewManager.idPlanesOrientationList[idPlane] = idOrientation;
            m_Column3DViewManager.planesOrientationFlipList[idPlane] = flip;
            SceneInformation.removeFrontPlaneList[idPlane] = removeFrontPlane?1:0;
            SceneInformation.lastIdPlaneModified = idPlane;

            // ########### cuts base on the mesh
            float offset;
            if (SceneInformation.meshToDisplay != null)
            {
                offset = SceneInformation.meshToDisplay.size_offset_cut_plane(m_planesList[idPlane], SceneInformation.numberOfCutsPerPlane[idPlane]);
                offset *= 1.05f; // upsize a little bit the bbox for planes
            }
            else
                offset = 0.1f;

            m_planesList[idPlane].point = SceneInformation.meshCenter + m_planesList[idPlane].normal * (position - 0.5f) * offset * SceneInformation.numberOfCutsPerPlane[idPlane];

            SceneInformation.updateCutMeshGeometry = true;
            SceneInformation.iEEGOutdated = true;

            // update sites visibility
            m_Column3DViewManager.update_all_columns_sites_rendering(SceneInformation);

            // update cameras cuts display
            ModifyPlanesCuts.Invoke();

            //####### UDPATE MODE
            m_ModesManager.updateMode(Mode.FunctionsId.updatePlane);
            //##################
        }
        /// <summary>
        /// Reset the volume of the scene
        /// </summary>
        /// <param name="pathNIIBrainVolumeFile"></param>
        /// <returns></returns>
        public bool ResetNiftiBrainVolume(string pathNIIBrainVolumeFile)
        {
            //####### CHECK ACESS
            if (!m_ModesManager.functionAccess(Mode.FunctionsId.resetNIIBrainVolumeFile))
            {
                Debug.LogError("-ERROR : Base3DScene::resetNIIBrainVolumeFile -> no acess for mode : " + m_ModesManager.currentModeName());
                return false;
            }

            SceneInformation.mriLoaded = false;

            // checks parameter
            if (pathNIIBrainVolumeFile.Length == 0)
            {
                Debug.LogError("-ERROR : Base3DScene::resetNIIBrainVolumeFile -> path NII brain volume file is empty. ");
                return (SceneInformation.meshesLoaded = false);
            }

            // load volume
            bool loadingSuccess = m_Column3DViewManager.DLLNii.load_nii_file(pathNIIBrainVolumeFile);
            if (loadingSuccess)
            {
                m_Column3DViewManager.DLLNii.convert_to_volume(m_Column3DViewManager.DLLVolume);
                SceneInformation.volumeCenter = m_Column3DViewManager.DLLVolume.center();
            }
            else
            {
                Debug.LogError("-ERROR : Base3DScene::resetNIIBrainVolumeFile -> load NII file failed. " + pathNIIBrainVolumeFile);
                return SceneInformation.mriLoaded;
            }

            SceneInformation.mriLoaded = loadingSuccess;
            UpdatePlanes.Invoke();

            // send cal values to the UI
            IRMCalValuesUpdate.Invoke(m_Column3DViewManager.DLLVolume.retrieve_extreme_values());

            //####### UDPATE MODE
            m_ModesManager.updateMode(Mode.FunctionsId.resetNIIBrainVolumeFile);
            //##################

            return SceneInformation.mriLoaded;
        }
        /// <summary>
        /// Update the selected column of the scene
        /// </summary>
        /// <param name="idColumn"></param>
        public void UpdateSelectedColumn(int idColumn)
        {
            if (idColumn >= m_Column3DViewManager.Columns.Count)
                return;

            m_Column3DViewManager.SelectedColumnID = idColumn;

            // force mode to update UI
            m_ModesManager.set_current_mode_specifications(true);

            ComputeGUITextures(-1, m_Column3DViewManager.SelectedColumnID);
            UpdateGUITextures();
        }
        /// <summary>
        /// Update the data render corresponding to the column
        /// </summary>
        /// <param name="indexColumn"></param>
        /// <returns></returns>
        public bool UpdateColumnRendering(int indexColumn)
        {
            if (!SceneInformation.geometryUpToDate)
                return false;
        
            UnityEngine.Profiling.Profiler.BeginSample("TEST-updateColumnRender");

            Column3DView currCol = m_Column3DViewManager.Columns[indexColumn];
            // TODO : un mesh pour chaque column

            // update cuts textures
            if ((SceneInformation.meshTypeToDisplay != SceneStatesInfo.MeshType.Inflated))
            {
                for (int ii = 0; ii < PlanesList.Count; ++ii)
                {
                    switch (currCol.Type)
                    {
                        case Column3DView.ColumnType.FMRI:
                            m_DisplayedObjects.brainCutMeshes[ii].GetComponent<Renderer>().material.mainTexture = ((Column3DViewFMRI)currCol).brainCutWithFMRITextures[ii];
                            break;
                        case Column3DView.ColumnType.IEEG:
                            if (!SceneInformation.generatorUpToDate)
                                m_DisplayedObjects.brainCutMeshes[ii].GetComponent<Renderer>().material.mainTexture = currCol.brainCutTextures[ii];
                            else
                                m_DisplayedObjects.brainCutMeshes[ii].GetComponent<Renderer>().material.mainTexture = ((Column3DViewIEEG)currCol).brainCutWithIEEGTextures[ii];
                            break;
                        default:
                            break;
                    }
                }
            }

            // update meshes splits UV
            for (int ii = 0; ii < m_Column3DViewManager.meshSplitNb; ++ii)
            {
                // uv 1 (main)
                //go_.brainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh.uv = m_CM.UVCoordinatesSplits[ii];

                if (currCol.Type == Column3DView.ColumnType.FMRI || !SceneInformation.generatorUpToDate || SceneInformation.displayCcepMode)
                {
                    // uv 2 (alpha) 
                    m_DisplayedObjects.brainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh.uv2 = m_Column3DViewManager.uvNull[ii];
                    // uv 3 (color map)
                    m_DisplayedObjects.brainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh.uv3 = m_Column3DViewManager.uvNull[ii];
                }
                else
                {
                    // uv 2 (alpha) 
                    m_DisplayedObjects.brainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh.uv2 = ((Column3DViewIEEG)currCol).DLLBrainTextureGeneratorList[ii].get_alpha_UV();
                    // uv 3 (color map)
                    m_DisplayedObjects.brainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh.uv3 = ((Column3DViewIEEG)currCol).DLLBrainTextureGeneratorList[ii].get_iEEG_UV();
                }
            }


            UnityEngine.Profiling.Profiler.EndSample();

            return true;
        }
        /// <summary>
        /// Update the brain and the cuts meshes colliders
        /// </summary>
        public void UpdateMeshesColliders()
        {
            if (!SceneInformation.meshesLoaded || !SceneInformation.mriLoaded)
                return;

            // update splits colliders
            for(int ii = 0; ii < m_DisplayedObjects.brainSurfaceMeshes.Count; ++ii)
            {
                m_DisplayedObjects.brainSurfaceMeshes[ii].GetComponent<MeshCollider>().sharedMesh = null;
                m_DisplayedObjects.brainSurfaceMeshes[ii].GetComponent<MeshCollider>().sharedMesh = m_DisplayedObjects.brainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh;
            }

            // update cuts colliders
            for (int ii = 0; ii < m_DisplayedObjects.brainCutMeshes.Count; ++ii)
            {
                m_DisplayedObjects.brainCutMeshes[ii].GetComponent<MeshCollider>().sharedMesh = null;
                m_DisplayedObjects.brainCutMeshes[ii].GetComponent<MeshCollider>().sharedMesh = m_DisplayedObjects.brainCutMeshes[ii].GetComponent<MeshFilter>().mesh;
            }

            SceneInformation.collidersUpdated = true;
        }
        /// <summary>
        /// Update the textures generator
        /// </summary>
        public void UpdateGenerators()
        {
            //####### CHECK ACESS
            if (!m_ModesManager.functionAccess(Mode.FunctionsId.pre_updateGenerators))
            {
                Debug.LogError("-ERROR : Base3DScene::pre_updateGenerators -> no acess for mode : " + m_ModesManager.currentModeName());
                return;
            }
            //##################

            if (SceneInformation.updateCutMeshGeometry || !SceneInformation.geometryUpToDate) // if update cut plane is pending, cancel action
                return;

            //####### UDPATE MODE
            m_ModesManager.updateMode(Mode.FunctionsId.pre_updateGenerators);
            //##################

            SceneInformation.generatorUpToDate = false;

            m_ComputeGeneratorsJob = new ComputeGeneratorsJob();
            m_ComputeGeneratorsJob.data_ = SceneInformation;
            m_ComputeGeneratorsJob.cm_ = m_Column3DViewManager;
            m_ComputeGeneratorsJob.Start();
        }
        /// <summary>
        /// Update the displayed amplitudes on the brain and the cuts with the slider position.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="slider"></param>
        /// <param name="globalTimeline"> if globaltime is true, update all columns with the same slider, else udapte only current selected column </param>
        public void UpdateIEEGTime(int id, float value, bool globalTimeline)
        {
            m_Column3DViewManager.globalTimeline = globalTimeline;
            if (m_Column3DViewManager.globalTimeline)
            {
                m_Column3DViewManager.commonTimelineValue = value;
                for (int ii = 0; ii < m_Column3DViewManager.ColumnsIEEG.Count; ++ii)
                    m_Column3DViewManager.ColumnsIEEG[ii].currentTimeLineID = (int)m_Column3DViewManager.commonTimelineValue;
            }
            else
            {
                Column3DViewIEEG currIEEGCol = (Column3DViewIEEG)m_Column3DViewManager.Columns[id];
                currIEEGCol.columnTimeLineID = (int)value;
                currIEEGCol.currentTimeLineID = currIEEGCol.columnTimeLineID;
            }

            ComputeIEEGTextures();
            m_Column3DViewManager.update_all_columns_sites_rendering(SceneInformation);
            UpdateTimeInUI.Invoke();
        }
        /// <summary>
        /// Update displayed amplitudes with the timeline id corresponding to global timeline mode or individual timeline mode
        /// </summary>
        /// <param name="globalTimeline"></param>
        public void UpdateAllIEEGTime(bool globalTimeline)
        {
            for (int ii = 0; ii < m_Column3DViewManager.ColumnsIEEG.Count; ++ii)
            {
                m_Column3DViewManager.ColumnsIEEG[ii].currentTimeLineID = globalTimeline ? (int)m_Column3DViewManager.commonTimelineValue : m_Column3DViewManager.ColumnsIEEG[ii].columnTimeLineID;
            }

            ComputeIEEGTextures();
            m_Column3DViewManager.update_all_columns_sites_rendering(SceneInformation);
        }
        /// <summary>
        /// 
        /// </summary>
        public void SwitchMarsAtlasColor()
        {
            {                
                SceneInformation.MarsAtlasModeEnabled = !SceneInformation.MarsAtlasModeEnabled;
                m_DisplayedObjects.brainSurfaceMeshes[0].GetComponent<Renderer>().sharedMaterial.SetInt("_MarsAtlas", SceneInformation.MarsAtlasModeEnabled ? 1 : 0);
            }
        }
        /// <summary>
        /// Mouse scroll events managements
        /// </summary>
        /// <param name="scrollDelta"></param>
        public void MouseScrollAction(Vector2 scrollDelta)
        {
            // nothing for now 
            // (not not delete children classes use it)
        }
        /// <summary>
        /// Keyboard events management
        /// </summary>
        /// <param name="keyCode"></param>
        public void KeyboardAction(KeyCode keyCode)
        {
            if (!SceneInformation.meshesLoaded || !SceneInformation.mriLoaded)
                return;

            switch (keyCode)
            {
                // enable/disable holes in the cuts
                case KeyCode.H:
                    SceneInformation.holesEnabled = !SceneInformation.holesEnabled;
                    SceneInformation.updateCutMeshGeometry = true;
                    SceneInformation.iEEGOutdated = true;
                    m_ModesManager.updateMode(Mode.FunctionsId.updatePlane); // TEMP
                    break;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool IsTriangleErasingModeEnabled()
        {
            return m_TriEraser.is_enabled();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public TriEraser.Mode CurrentTriangleErasingMode()
        {
            return m_TriEraser.CurrentMode;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="enabled"></param>
        public void SetTriangleErasing(bool enabled)
        {
            m_TriEraser.set_enabled(enabled);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="updateGO"></param>
        public void ResetTriangleErasing(bool updateGO = true)
        {
            // destroy previous GO
            if (m_DisplayedObjects.invisibleBrainSurfaceMeshes != null)
                for (int ii = 0; ii < m_DisplayedObjects.invisibleBrainSurfaceMeshes.Count; ++ii)
                    Destroy(m_DisplayedObjects.invisibleBrainSurfaceMeshes[ii]);

            // create new GO
            m_DisplayedObjects.invisibleBrainSurfaceMeshes = new List<GameObject>(m_DisplayedObjects.brainSurfaceMeshes.Count);
            for (int ii = 0; ii < m_DisplayedObjects.brainSurfaceMeshes.Count; ++ii)
            {
                GameObject invisibleBrainPart = Instantiate(GlobalGOPreloaded.InvisibleBrain);
                invisibleBrainPart.name = "erased brain part " + ii;
                invisibleBrainPart.transform.SetParent(transform.Find("meshes").Find("erased brains"));
                switch(Type)
                {
                    case SceneType.SinglePatient:
                        invisibleBrainPart.layer = LayerMask.NameToLayer("Meshes_SP");
                        break;
                    case SceneType.MultiPatients:
                        invisibleBrainPart.layer = LayerMask.NameToLayer("Meshes_MP");
                        break;
                }
                invisibleBrainPart.AddComponent<MeshFilter>();
                invisibleBrainPart.transform.localScale = new Vector3(-1, 1, 1);
                invisibleBrainPart.transform.localPosition = new Vector3(0, 0, 0);
                invisibleBrainPart.SetActive(m_TriEraser.is_enabled());
                m_DisplayedObjects.invisibleBrainSurfaceMeshes.Add(invisibleBrainPart);
            }

            m_TriEraser.reset(m_DisplayedObjects.invisibleBrainSurfaceMeshes, m_Column3DViewManager.DLLCutsList[0], m_Column3DViewManager.DLLSplittedMeshesList);

            if(updateGO)
                for (int ii = 0; ii < m_Column3DViewManager.DLLSplittedMeshesList.Count; ++ii)
                    m_Column3DViewManager.DLLSplittedMeshesList[ii].update_mesh_from_dll(m_DisplayedObjects.brainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh);
        }
        /// <summary>
        /// 
        /// </summary>
        public void CancelLastTriangleErasingAction()
        {
            m_TriEraser.cancel_last_action();
            for (int ii = 0; ii < m_Column3DViewManager.DLLSplittedMeshesList.Count; ++ii)
                m_Column3DViewManager.DLLSplittedMeshesList[ii].update_mesh_from_dll(m_DisplayedObjects.brainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mode"></param>
        public void SetTriangleErasingMode(TriEraser.Mode mode)
        {
            TriEraser.Mode previousMode = m_TriEraser.CurrentMode;
            m_TriEraser.set_tri_erasing_mode(mode);

            if (mode == TriEraser.Mode.Expand || mode == TriEraser.Mode.Invert)
            {
                m_TriEraser.erase_triangles(new Vector3(), new Vector3());
                for (int ii = 0; ii < m_Column3DViewManager.DLLSplittedMeshesList.Count; ++ii)
                    m_Column3DViewManager.DLLSplittedMeshesList[ii].update_mesh_from_dll(m_DisplayedObjects.brainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh);
                m_TriEraser.set_tri_erasing_mode(previousMode);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="degrees"></param>
        public void SetTriangleErasingZoneDegrees(float degrees)
        {
            m_TriEraser.set_zone_degrees(degrees);
        }
        /// <summary>
        /// Return the id of the current select column in the scene
        /// </summary>
        /// <returns></returns>
        public int RetrieveCurrentSelectedColumnID()
        {
            return m_Column3DViewManager.SelectedColumnID;
        }
        /// <summary>
        /// Update the middle of a IEEG column
        /// </summary>
        /// <param name="value"></param>
        /// <param name="columnId"></param>
        public void UpdateIEEGMiddle(float value, int columnId)
        {
            // update value
            Column3DViewIEEG IEEGCol = (Column3DViewIEEG)m_Column3DViewManager.Columns[columnId];
            if (IEEGCol.middle == value)
                return;
            IEEGCol.middle = value;

            SceneInformation.generatorUpToDate = false;
            SceneInformation.iEEGOutdated = true;
            m_Column3DViewManager.update_all_columns_sites_rendering(SceneInformation);

            //####### UDPATE MODE
            m_ModesManager.updateMode(Mode.FunctionsId.updateMiddle);
            //##################
        }
        /// <summary>
        /// Update the max distance of a IEEG column
        /// </summary>
        /// <param name="value"></param>
        /// <param name="columnId"></param>
        public void UpdateSiteMaximumInfluence(float value, int columnId)
        {
            Column3DViewIEEG IEEGCol = (Column3DViewIEEG)m_Column3DViewManager.Columns[columnId];
            if (IEEGCol.maxDistanceElec == value)
                return;            
            IEEGCol.maxDistanceElec = value;

            SceneInformation.generatorUpToDate = false;
            SceneInformation.iEEGOutdated = true;
            m_Column3DViewManager.update_all_columns_sites_rendering(SceneInformation);

            //####### UDPATE MODE
            m_ModesManager.updateMode(Mode.FunctionsId.updateMiddle);
            //##################
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public void UpdateMRICalMin(float value)
        {
            m_Column3DViewManager.MRICalMinFactor = value;

            if (!SceneInformation.geometryUpToDate)
                return;

            SceneInformation.generatorUpToDate = false;
            SceneInformation.iEEGOutdated = true;

            { //TEST
              // recompute UV
                for (int ii = 0; ii < m_Column3DViewManager.meshSplitNb; ++ii)
                    m_Column3DViewManager.DLLCommonBrainTextureGeneratorList[ii].compute_UVMain_with_volume(m_Column3DViewManager.DLLSplittedMeshesList[ii], m_Column3DViewManager.DLLVolume, m_Column3DViewManager.MRICalMinFactor, m_Column3DViewManager.MRICalMaxFactor);

                // update brain mesh object mesh filter (TODO update only UV)
                for (int ii = 0; ii < m_Column3DViewManager.meshSplitNb; ++ii)
                    m_Column3DViewManager.DLLSplittedMeshesList[ii].update_mesh_from_dll(m_DisplayedObjects.brainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh);
            }

            ComputeMRITextures();
            m_Column3DViewManager.update_all_columns_sites_rendering(SceneInformation);

            //####### UDPATE MODE
            m_ModesManager.updateMode(Mode.FunctionsId.updateMiddle);
            //##################
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public void UpdateMRICalMax(float value)
        {
            m_Column3DViewManager.MRICalMaxFactor = value;

            if (!SceneInformation.geometryUpToDate)
                return;

            SceneInformation.generatorUpToDate = false;
            SceneInformation.iEEGOutdated = true;

            { //TEST
              // recompute UV
                for (int ii = 0; ii < m_Column3DViewManager.meshSplitNb; ++ii)
                    m_Column3DViewManager.DLLCommonBrainTextureGeneratorList[ii].compute_UVMain_with_volume(m_Column3DViewManager.DLLSplittedMeshesList[ii], m_Column3DViewManager.DLLVolume, m_Column3DViewManager.MRICalMinFactor, m_Column3DViewManager.MRICalMaxFactor);

                // update brain mesh object mesh filter (TODO update only UV)
                for (int ii = 0; ii < m_Column3DViewManager.meshSplitNb; ++ii)
                    m_Column3DViewManager.DLLSplittedMeshesList[ii].update_mesh_from_dll(m_DisplayedObjects.brainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh);
            }


            ComputeMRITextures();
            m_Column3DViewManager.update_all_columns_sites_rendering(SceneInformation);

            //####### UDPATE MODE
            m_ModesManager.updateMode(Mode.FunctionsId.updateMiddle);
            //##################
        }
        /// <summary>
        /// Update the cal min value of the input FMRI column
        /// </summary>
        /// <param name="value"></param>
        /// <param name="columnId"> id of the FMRI columns</param>
        public void UpdateFMRICalMin(float value, int columnId)
        {
            m_Column3DViewManager.ColumnsFMRI[columnId].calMin = value;
            ComputeFMRITextures(-1, -1);
        }
        /// <summary>
        /// Update the cal max value of the input FMRI column
        /// </summary>
        /// <param name="value"></param>
        /// <param name="columnId"> id of the FMRI columns</param>
        public void UpdateFMRICalMax(float value, int columnId)
        {
            m_Column3DViewManager.ColumnsFMRI[columnId].calMax = value;
            ComputeFMRITextures(-1, -1);
        }
        /// <summary>
        /// Update the alpha value of the input FMRI column
        /// </summary>
        /// <param name="value"></param>
        /// <param name="columnId"> id of the FMRI columns</param>
        public void UpdateFMRIAlpha(float value, int columnId)
        {
            m_Column3DViewManager.ColumnsFMRI[columnId].alpha = value;
            ComputeFMRITextures(-1, -1);
        }
        /// <summary>
        /// Update the min alpha of a IEEG column
        /// </summary>
        /// <param name="value"></param>
        /// <param name="columnId"></param>
        public void UpdateIEEGMinAlpha(float value, int columnId)
        {
            m_Column3DViewManager.ColumnsIEEG[columnId].alphaMin = value;

            if (SceneInformation.geometryUpToDate && !SceneInformation.iEEGOutdated)
                ComputeIEEGTextures(-1, -1, true, true, false);
        }
        /// <summary>
        /// Update the max alpha of a IEEG  column
        /// </summary>
        /// <param name="value"></param>
        /// <param name="columnId"></param>
        public void UpdateIEEGMaxAlpha(float value, int columnId)
        {
            m_Column3DViewManager.ColumnsIEEG[columnId].alphaMax = value;

            if (SceneInformation.geometryUpToDate && !SceneInformation.iEEGOutdated)
                ComputeIEEGTextures(-1, -1, true, true, false);
        }
        /// <summary>
        /// Update the bubbles gain of a IEEG column
        /// </summary>
        /// <param name="value"></param>
        /// <param name="columnId"></param>
        public void UpdateBubblesGain(float value, int columnId)
        {
            Column3DViewIEEG IEEGCol = m_Column3DViewManager.ColumnsIEEG[columnId];
            if (IEEGCol.gainBubbles == value)
                return;
            IEEGCol.gainBubbles = value;

            m_Column3DViewManager.update_all_columns_sites_rendering(SceneInformation);
        }
        /// <summary>
        /// Update the span min of a IEEG column
        /// </summary>
        /// <param name="value"></param>
        /// <param name="columnId"></param>
        public void UpdateIEEGSpanMin(float value, int columnId)
        {
            Column3DViewIEEG IEEGCol = m_Column3DViewManager.ColumnsIEEG[columnId];
            if (IEEGCol.spanMin == value)
                return;
            IEEGCol.spanMin = value;

            if (!SceneInformation.geometryUpToDate)
                return;

            SceneInformation.generatorUpToDate = false;
            SceneInformation.iEEGOutdated = true;
            UpdateGUITextures();
            m_Column3DViewManager.update_all_columns_sites_rendering(SceneInformation);            

            //####### UDPATE MODE
            m_ModesManager.updateMode(Mode.FunctionsId.updateMiddle);
            //##################
        }
        /// <summary>
        /// Update the span max of a IEEG column
        /// </summary>
        /// <param name="value"></param>
        /// <param name="columnId"></param>
        public void UpdateIEEGSpanMax(float value, int columnId)
        {
            Column3DViewIEEG IEEGCol = m_Column3DViewManager.ColumnsIEEG[columnId];
            if (IEEGCol.spanMax == value)
                return;
            IEEGCol.spanMax = value;

            if (!SceneInformation.geometryUpToDate)
                return;

            SceneInformation.generatorUpToDate = false;
            SceneInformation.iEEGOutdated = true;
            UpdateGUITextures();
            m_Column3DViewManager.update_all_columns_sites_rendering(SceneInformation);

            //####### UDPATE MODE
            m_ModesManager.updateMode(Mode.FunctionsId.updateMiddle);
            //##################
        }
        /// <summary>
        /// Return the number of FMRI colums
        /// </summary>
        /// <returns></returns>
        public int GetNumberOffMRIColumns()
        {
            return m_Column3DViewManager.ColumnsFMRI.Count;
        }
        /// <summary>
        /// Load an FMRI column
        /// </summary>
        /// <param name="FMRIPath"></param>
        /// <returns></returns>
        public bool LoadFMRIFile(string FMRIPath)
        {
            if (m_Column3DViewManager.DLLNii.load_nii_file(FMRIPath))
                return true;

            Debug.LogError("-ERROR : Base3DScene::load_FMRI_file -> load NII file failed. " + FMRIPath);
            return false;
        }
        /// <summary>
        /// Add a FMRI column
        /// </summary>
        /// <param name="IRMFPath"></param>
        /// <returns></returns>
        public bool AddfMRIColumn(string IMRFLabel)
        {
            //####### CHECK ACESS
            if (!m_ModesManager.functionAccess(Mode.FunctionsId.add_FMRI_column))
            {
                Debug.LogError("-ERROR : Base3DScene::add_FMRI_column -> no acess for mode : " + m_ModesManager.currentModeName());
                return false;
            }
            //##################

            // update columns number
            int newFMRIColNb = m_Column3DViewManager.ColumnsFMRI.Count + 1;
            m_Column3DViewManager.update_columns_nb(m_Column3DViewManager.ColumnsIEEG.Count, newFMRIColNb, m_planesList.Count);

            int idCol = newFMRIColNb - 1;
            m_Column3DViewManager.ColumnsFMRI[idCol].Label = IMRFLabel;

            // update plots visibility
            m_Column3DViewManager.update_all_columns_sites_rendering(SceneInformation);

            // convert to volume            
            m_Column3DViewManager.DLLNii.convert_to_volume(m_Column3DViewManager.DLLVolumeFMriList[idCol]);

            if (Type == SceneType.SinglePatient)
                AskROIUpdateEvent.Invoke(m_Column3DViewManager.ColumnsIEEG.Count + idCol);

            // send parameters to UI
            //IRMCalValues calValues = m_CM.DLLVolumeIRMFList[idCol].retrieveExtremeValues();

            FMriDataParameters FMRIParams = new FMriDataParameters();
            FMRIParams.calValues = m_Column3DViewManager.DLLVolumeFMriList[idCol].retrieve_extreme_values();
            FMRIParams.columnId = idCol;
            FMRIParams.alpha  = m_Column3DViewManager.ColumnsFMRI[idCol].alpha;
            FMRIParams.calMin = m_Column3DViewManager.ColumnsFMRI[idCol].calMin;
            FMRIParams.calMax = m_Column3DViewManager.ColumnsFMRI[idCol].calMax;
            FMRIParams.singlePatient = Type == SceneType.SinglePatient;

            m_Column3DViewManager.ColumnsFMRI[idCol].calMin = FMRIParams.calValues.computedCalMin;
            m_Column3DViewManager.ColumnsFMRI[idCol].calMax = FMRIParams.calValues.computedCalMax;            

            // update camera
            UpdateCameraTarget.Invoke(Type == SceneType.SinglePatient ?  m_Column3DViewManager.BothHemi.bounding_box().center() : m_MNIObjects.BothHemi.bounding_box().center());

            ComputeMRITextures(-1, -1);

            SendFMRIParameters.Invoke(FMRIParams);
            ComputeFMRITextures(-1, -1);


            //####### UDPATE MODE
            m_ModesManager.updateMode(Mode.FunctionsId.add_FMRI_column);
            //##################

            return true;
        }
        /// <summary>
        /// Remove the last IRMF column
        /// </summary>
        public void RemoveLastFMRIColumn()
        {
            //####### CHECK ACESS
            if (!m_ModesManager.functionAccess(Mode.FunctionsId.removeLastIRMFColumn))
            {
                Debug.LogError("-ERROR : Base3DScene::remove_last_FMRI_column -> no acess for mode : " + m_ModesManager.currentModeName());
                return;
            }
            //##################
            
            // update columns number
            m_Column3DViewManager.update_columns_nb(m_Column3DViewManager.ColumnsIEEG.Count, m_Column3DViewManager.ColumnsFMRI.Count - 1, m_planesList.Count);

            // update plots visibility
            m_Column3DViewManager.update_all_columns_sites_rendering(SceneInformation);

            ComputeMRITextures(-1, -1);
            ComputeFMRITextures(-1, -1);
            UpdateGUITextures();

            //####### UDPATE MODE
            m_ModesManager.updateMode(Mode.FunctionsId.removeLastIRMFColumn);
            //##################
        }
        /// <summary>
        /// Is the latency mode enabled ?
        /// </summary>
        /// <returns></returns>
        public bool IsLatencyModeEnabled()
        {
            return SceneInformation.displayCcepMode;
        }
        /// <summary>
        /// Updat visibility of the columns 3D items
        /// </summary>
        /// <param name="displayMeshes"></param>
        /// <param name="displaySites"></param>
        /// <param name="displayROI"></param>
        public void UpdateSceneItemsVisibility(bool displayMeshes, bool displaySites, bool displayROI)
        {
            //if (!singlePatient)
            //    m_CM.update_ROI_visibility(displayROI && data_.ROICreationMode);

            //m_CM.update_sites_visibiliy(displaySites);
        }
        /// <summary>
        /// Send IEEG read min/max/middle to the IEEG menu
        /// </summary>
        public void SendIEEGParametersToMenu()
        {            
            for (int ii = 0; ii < m_Column3DViewManager.ColumnsIEEG.Count; ++ii)
            {
                iEEGDataParameters iEEGDataParams;
                iEEGDataParams.minAmp       = m_Column3DViewManager.ColumnsIEEG[ii].minAmp;
                iEEGDataParams.maxAmp       = m_Column3DViewManager.ColumnsIEEG[ii].maxAmp;

                iEEGDataParams.spanMin      = m_Column3DViewManager.ColumnsIEEG[ii].spanMin;
                iEEGDataParams.middle       = m_Column3DViewManager.ColumnsIEEG[ii].middle;
                iEEGDataParams.spanMax      = m_Column3DViewManager.ColumnsIEEG[ii].spanMax;

                iEEGDataParams.gain         = m_Column3DViewManager.ColumnsIEEG[ii].gainBubbles;
                iEEGDataParams.maxDistance  = m_Column3DViewManager.ColumnsIEEG[ii].maxDistanceElec;
                iEEGDataParams.columnId     = ii;

                iEEGDataParams.alphaMin     = m_Column3DViewManager.ColumnsIEEG[ii].alphaMin;
                iEEGDataParams.alphaMax     = m_Column3DViewManager.ColumnsIEEG[ii].alphaMax; // useless

                SendIEEGParameters.Invoke(iEEGDataParams);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public void SendFMRIParametersToMenu() // TODO
        {
            for (int ii = 0; ii < m_Column3DViewManager.ColumnsIEEG.Count; ++ii)
            {
                FMriDataParameters FMRIDataParams;
                FMRIDataParams.alpha = m_Column3DViewManager.ColumnsFMRI[ii].alpha;
                FMRIDataParams.calMin = m_Column3DViewManager.ColumnsFMRI[ii].calMin;
                FMRIDataParams.calMax = m_Column3DViewManager.ColumnsFMRI[ii].calMax;
                FMRIDataParams.columnId = ii;

                FMRIDataParams.calValues = m_Column3DViewManager.DLLVolumeFMriList[ii].retrieve_extreme_values(); 
                FMRIDataParams.singlePatient = Type == SceneType.SinglePatient;
                
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
        public void DisplayScreenMessage(string message, float duration, int width, int height)
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
        public void DisplayProgressbar(float value, float duration, int width, int height)
        {
            display_scene_progressbar_event.Invoke(duration, width, height, value);
        }
        /// <summary>
        /// 
        /// </summary>
        public void CompareSites()
        {
            DisplayScreenMessage("Select site to compare ", 3f, 200, 40);
            SceneInformation.compareSite = true;
        }
        /// <summary>
        /// Unselect the site of the corresponding column
        /// </summary>
        /// <param name="columnId"></param>
        public void UnselectSite(int columnId)
        {
            m_Column3DViewManager.Columns[columnId].SelectedSiteID = -1; // unselect current site
            m_Column3DViewManager.update_all_columns_sites_rendering(SceneInformation);
            ClickSite.Invoke(-1); // update menu
        }
        #endregion

        #region Abstract Methods
        /// <summary>
        /// 
        /// </summary>
        public abstract void ComputeMeshesCut();
        /// <summary>
        /// 
        /// </summary>
        /// <param name="previousPlot"></param>
        public abstract void SendAdditionalSiteInfoRequest(Site previousPlot = null);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ray"></param>
        public abstract void ClickOnScene(Ray ray);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="idColumn"></param>
        public abstract void DisableSiteDisplayWindow(int idColumn);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ray"></param>
        /// <param name="mousePosition"></param>
        /// <param name="idColumn"></param>
        public abstract void MoveMouseOnScene(Ray ray, Vector3 mousePosition, int idColumn);
        /// <summary>
        /// Reset the rendering settings for this scene, called by each camera before rendering
        /// </summary>
        /// <param name="cameraRotation"></param>
        public abstract void ResetRenderingSettings(Vector3 cameraRotation);
        #endregion
    }

    /// <summary>
    /// The job class for doing the textures generators computing stuff
    /// </summary>
    public class ComputeGeneratorsJob : ThreadedJob
    {
        #region Properties
        public SceneStatesInfo data_ = null;
        public Column3DViewManager cm_ = null;
        #endregion

        #region Private Methods
        /// <summary>
        /// 
        /// </summary>
        protected override void ThreadFunction()
        {
            bool useMultiCPU = true;
            bool addValues = false;
            bool ratioDistances = true;

            data_.rwl.AcquireWriterLock(1000);
            data_.currentComputingState = 0f;
            data_.rwl.ReleaseWriterLock();

            // copy from main generators
            for (int ii = 0; ii < cm_.ColumnsIEEG.Count; ++ii)
            {
                for (int jj = 0; jj < cm_.meshSplitNb; ++jj)
                {
                    cm_.ColumnsIEEG[ii].DLLBrainTextureGeneratorList[jj] = (DLL.MRIBrainGenerator)cm_.DLLCommonBrainTextureGeneratorList[jj].Clone();
                }
            }

            float offsetState = 1f / (2 * cm_.ColumnsIEEG.Count);

            // Do your threaded task. DON'T use the Unity API here
            if (data_.meshTypeToDisplay != SceneStatesInfo.MeshType.Inflated)
            {
                for (int ii = 0; ii < cm_.ColumnsIEEG.Count; ++ii)
                {
                    data_.rwl.AcquireWriterLock(1000);
                    data_.currentComputingState += offsetState;
                    data_.rwl.ReleaseWriterLock();

                    float currentMaxDensity, currentMinInfluence, currentMaxInfluence;
                    float maxDensity = 1;

                    cm_.ColumnsIEEG[ii].sharedMinInf = float.MaxValue;
                    cm_.ColumnsIEEG[ii].sharedMaxInf = float.MinValue;

                    // update raw electrodes
                    cm_.ColumnsIEEG[ii].update_DLL_sites_mask();

                    // splits
                    for (int jj = 0; jj < cm_.meshSplitNb; ++jj)
                    {
                        cm_.ColumnsIEEG[ii].DLLBrainTextureGeneratorList[jj].init_octree(cm_.ColumnsIEEG[ii].RawElectrodes);


                        if (!cm_.ColumnsIEEG[ii].DLLBrainTextureGeneratorList[jj].compute_distances(cm_.ColumnsIEEG[ii].maxDistanceElec, true))
                        {
                            Debug.LogError("Abort computing"); // useless
                            return;
                        }

                        if (!cm_.ColumnsIEEG[ii].DLLBrainTextureGeneratorList[jj].compute_influences(cm_.ColumnsIEEG[ii], useMultiCPU, addValues, ratioDistances))
                        {
                            Debug.LogError("Abort computing"); // useless
                            return;
                        }
                        currentMaxDensity = cm_.ColumnsIEEG[ii].DLLBrainTextureGeneratorList[jj].getMaximumDensity();
                        currentMinInfluence = cm_.ColumnsIEEG[ii].DLLBrainTextureGeneratorList[jj].getMinimumInfluence();
                        currentMaxInfluence = cm_.ColumnsIEEG[ii].DLLBrainTextureGeneratorList[jj].getMaximumInfluence();

                        if (currentMaxDensity > maxDensity)
                            maxDensity = currentMaxDensity;

                        if (currentMinInfluence < cm_.ColumnsIEEG[ii].sharedMinInf)
                            cm_.ColumnsIEEG[ii].sharedMinInf = currentMinInfluence;

                        if (currentMaxInfluence > cm_.ColumnsIEEG[ii].sharedMaxInf)
                            cm_.ColumnsIEEG[ii].sharedMaxInf = currentMaxInfluence;

                    }

                    data_.rwl.AcquireWriterLock(1000);
                    data_.currentComputingState += offsetState;
                    data_.rwl.ReleaseWriterLock();


                    // cuts
                    for (int jj = 0; jj < cm_.planesCutsCopy.Count; ++jj)
                    {
                        cm_.ColumnsIEEG[ii].DLLMRITextureCutGeneratorList[jj].init_octree(cm_.ColumnsIEEG[ii].RawElectrodes);
                        cm_.ColumnsIEEG[ii].DLLMRITextureCutGeneratorList[jj].compute_distances(cm_.ColumnsIEEG[ii].maxDistanceElec, true);

                        if (!cm_.ColumnsIEEG[ii].DLLMRITextureCutGeneratorList[jj].compute_influences(cm_.ColumnsIEEG[ii], useMultiCPU, addValues, ratioDistances))
                        {
                            Debug.LogError("Abort computing");
                            return;
                        }

                        currentMaxDensity = cm_.ColumnsIEEG[ii].DLLMRITextureCutGeneratorList[jj].maximum_density();
                        currentMinInfluence = cm_.ColumnsIEEG[ii].DLLMRITextureCutGeneratorList[jj].minimum_influence();
                        currentMaxInfluence = cm_.ColumnsIEEG[ii].DLLMRITextureCutGeneratorList[jj].maximum_influence();

                        if (currentMaxDensity > maxDensity)
                            maxDensity = currentMaxDensity;

                        if (currentMinInfluence < cm_.ColumnsIEEG[ii].sharedMinInf)
                            cm_.ColumnsIEEG[ii].sharedMinInf = currentMinInfluence;

                        if (currentMaxInfluence > cm_.ColumnsIEEG[ii].sharedMaxInf)
                            cm_.ColumnsIEEG[ii].sharedMaxInf = currentMaxInfluence;
                    }

                    // synchronize max density
                    for (int jj = 0; jj < cm_.meshSplitNb; ++jj)
                        cm_.ColumnsIEEG[ii].DLLBrainTextureGeneratorList[jj].synchronizeWithOthersGenerators(maxDensity, cm_.ColumnsIEEG[ii].sharedMinInf, cm_.ColumnsIEEG[ii].sharedMaxInf);
                    for (int jj = 0; jj < cm_.planesCutsCopy.Count; ++jj)
                        cm_.ColumnsIEEG[ii].DLLMRITextureCutGeneratorList[jj].synchronize_with_others_generators(maxDensity, cm_.ColumnsIEEG[ii].sharedMinInf, cm_.ColumnsIEEG[ii].sharedMaxInf);

                    for (int jj = 0; jj < cm_.meshSplitNb; ++jj)
                        cm_.ColumnsIEEG[ii].DLLBrainTextureGeneratorList[jj].ajustInfluencesToColormap();
                    for (int jj = 0; jj < cm_.planesCutsCopy.Count; ++jj)
                        cm_.ColumnsIEEG[ii].DLLMRITextureCutGeneratorList[jj].ajust_influences_to_colormap();
                }
            }
            else // if inflated white mesh is displayed, we compute only on the complete white mesh
            {
                for (int ii = 0; ii < cm_.ColumnsIEEG.Count; ++ii)
                {
                    data_.rwl.AcquireWriterLock(1000);
                    data_.currentComputingState += offsetState;
                    data_.rwl.ReleaseWriterLock();

                    float currentMaxDensity, currentMinInfluence, currentMaxInfluence;
                    float maxDensity = 1;

                    cm_.ColumnsIEEG[ii].sharedMinInf = float.MaxValue;
                    cm_.ColumnsIEEG[ii].sharedMaxInf = float.MinValue;

                    // update raw electrodes
                    cm_.ColumnsIEEG[ii].update_DLL_sites_mask();

                    // splits
                    for (int jj = 0; jj < cm_.meshSplitNb; ++jj)
                    {
                        cm_.ColumnsIEEG[ii].DLLBrainTextureGeneratorList[jj].reset(cm_.DLLSplittedWhiteMeshesList[jj], cm_.DLLVolume); // TODO : ?
                        cm_.ColumnsIEEG[ii].DLLBrainTextureGeneratorList[jj].init_octree(cm_.ColumnsIEEG[ii].RawElectrodes);

                        if (!cm_.ColumnsIEEG[ii].DLLBrainTextureGeneratorList[jj].compute_distances(cm_.ColumnsIEEG[ii].maxDistanceElec, true))
                        {
                            Debug.LogError("Abort computing");
                            return;
                        }

                        if (!cm_.ColumnsIEEG[ii].DLLBrainTextureGeneratorList[jj].compute_influences(cm_.ColumnsIEEG[ii], useMultiCPU, addValues, ratioDistances))
                        {
                            Debug.LogError("Abort computing");
                            return;
                        }

                        currentMaxDensity = cm_.ColumnsIEEG[ii].DLLBrainTextureGeneratorList[jj].getMaximumDensity();
                        currentMinInfluence = cm_.ColumnsIEEG[ii].DLLBrainTextureGeneratorList[jj].getMinimumInfluence();
                        currentMaxInfluence = cm_.ColumnsIEEG[ii].DLLBrainTextureGeneratorList[jj].getMaximumInfluence();

                        if (currentMaxDensity > maxDensity)
                            maxDensity = currentMaxDensity;

                        if (currentMinInfluence < cm_.ColumnsIEEG[ii].sharedMinInf)
                            cm_.ColumnsIEEG[ii].sharedMinInf = currentMinInfluence;

                        if (currentMaxInfluence > cm_.ColumnsIEEG[ii].sharedMaxInf)
                            cm_.ColumnsIEEG[ii].sharedMaxInf = currentMaxInfluence;
                    }

                    data_.rwl.AcquireWriterLock(1000);
                    data_.currentComputingState += offsetState;
                    data_.rwl.ReleaseWriterLock();


                    // synchronize max density
                    for (int jj = 0; jj < cm_.meshSplitNb; ++jj)
                        cm_.ColumnsIEEG[ii].DLLBrainTextureGeneratorList[jj].synchronizeWithOthersGenerators(maxDensity, cm_.ColumnsIEEG[ii].sharedMinInf, cm_.ColumnsIEEG[ii].sharedMaxInf);

                    for (int jj = 0; jj < cm_.meshSplitNb; ++jj)
                        cm_.ColumnsIEEG[ii].DLLBrainTextureGeneratorList[jj].ajustInfluencesToColormap();
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        protected override void OnFinished()
        { }
        #endregion

        #region Public Methods
        /// <summary>
        /// 
        /// </summary>
        public ComputeGeneratorsJob()
        { }
        #endregion


    }
}