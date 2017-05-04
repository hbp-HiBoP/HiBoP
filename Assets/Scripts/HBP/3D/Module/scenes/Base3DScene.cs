
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
using System.Collections.ObjectModel;

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

        public bool IsSelected
        {
            get
            {
                return m_Column3DViewManager.IsFocused;
            }
        }

        private bool m_DisplaySitesNames = false; // DELETEME
        private Camera m_LastCamera = null; // DELETEME

        private List<Plane> m_PlanesList = new List<Plane>();         /**< cut planes list */
        public List<Plane> PlanesList { get { return m_PlanesList; } }

        public SceneStatesInfo SceneInformation { get; set; } /**<  data of the scene */

        protected ModesManager m_ModesManager = null; /**< modes of the scene */
        public ModesManager ModesManager
        {
            get { return m_ModesManager; }
        }

        protected DisplayedObjects3DView m_DisplayedObjects = null; /**< displayable objects of the scene */
        public DisplayedObjects3DView DisplayedObjects
        {
            get
            {
                return m_DisplayedObjects;
            }
        }
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
            m_ModesManager.Initialize(this);
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

            if (m_ModesManager.CurrentModeID == Mode.ModesId.NoPathDefined)
                return;

            UnityEngine.Profiling.Profiler.BeginSample("TEST-Base3DScene-Update");

            // TEMP : useless
            for (int ii = 0; ii < SceneInformation.RemoveFrontPlaneList.Count; ++ii)
                SceneInformation.RemoveFrontPlaneList[ii] = 0;

            // check if we must perform new cuts of the brain            
            if (SceneInformation.CutMeshGeometryNeedsUpdate)
            {
                SceneInformation.IsGeometryUpToDate = false;
                Column3DViewManager.PlanesCutsCopy = new List<Plane>();
                for (int ii = 0; ii < m_PlanesList.Count; ++ii)
                    Column3DViewManager.PlanesCutsCopy.Add(new Plane(m_PlanesList[ii].Point, m_PlanesList[ii].Normal));

                UnityEngine.Profiling.Profiler.BeginSample("TEST-Base3DScene-Update compute_meshes_cuts 1");
                ComputeMeshesCut();
                UnityEngine.Profiling.Profiler.EndSample();
                UnityEngine.Profiling.Profiler.BeginSample("TEST-Base3DScene-Update compute_MRI_textures 1");

                // create textures 
                ComputeMRITextures();

                ComputeFMRITextures(-1, -1);

                SceneInformation.IsGeometryUpToDate = true;
                

                UnityEngine.Profiling.Profiler.EndSample();
            }


            // check job state
            if (m_ComputeGeneratorsJob != null)
            {
                float currState;
                SceneInformation.RWLock.AcquireReaderLock(1000);
                currState = SceneInformation.CurrentComputingState;
                SceneInformation.RWLock.ReleaseReaderLock();

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
            m_DisplaySitesNames = false;
        }
        void OnGUI()
        {
            if (m_DisplaySitesNames)
            {
                for (int ii = 0; ii < m_Column3DViewManager.SelectedColumn.SitesGameObjects.Count; ++ii)
                    for (int jj = 0; jj < m_Column3DViewManager.SelectedColumn.SitesGameObjects[ii].Count; ++jj)
                        for (int kk = 0; kk < m_Column3DViewManager.SelectedColumn.SitesGameObjects[ii][jj].Count; ++kk)
                        {
                            GUI.contentColor =  (kk == 0) ? Color.red : Color.black;
                            Site site = m_Column3DViewManager.SelectedColumn.SitesGameObjects[ii][jj][kk].GetComponent<Site>();
                            if (site || site.Information.IsExcluded || site.Information.IsBlackListed)
                                continue;

                            Vector3 pos = m_LastCamera.WorldToScreenPoint(site.transform.position);
                            Vector2 realPos = new Vector2(pos.x, Screen.height - pos.y);
                            if(m_LastCamera.pixelRect.Contains(realPos))                            
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
            if (SceneInformation.MeshTypeToDisplay == SceneStatesInfo.MeshType.Inflated)
                return;

            bool allColumns = indexColumn == -1;
            bool allCuts = indexCut == -1;

            int[] columnsIndexes = allColumns ? Enumerable.Range(0, m_Column3DViewManager.Columns.Count).ToArray() : new int[1] { indexColumn };
            int[] cutsIndexes = allCuts ? Enumerable.Range(0, m_PlanesList.Count).ToArray() : new int[1] { indexCut };

            UnityEngine.Profiling.Profiler.BeginSample("TEST-Base3DScene compute_MRI_textures 0 create_MRI_texture ");

            for (int ii = 0; ii < columnsIndexes.Length; ++ii)
                for (int jj = 0; jj < cutsIndexes.Length; ++jj)
                    m_Column3DViewManager.CreateMRITexture(cutsIndexes[jj], columnsIndexes[ii]);

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
            int[] cutsIndexes = allCuts ? Enumerable.Range(0, m_PlanesList.Count).ToArray() : new int[1] { indexCut };

            for (int ii = 0; ii < columnsIndexes.Length; ++ii)
            {
                if (Column3DViewManager.Columns[columnsIndexes[ii]].Type == Column3DView.ColumnType.FMRI)
                    return;

                Column3DViewIEEG currCol = (Column3DViewIEEG)Column3DViewManager.Columns[columnsIndexes[ii]];

                // brain surface
                if (surface)
                    if (!m_Column3DViewManager.ComputeSurfaceBrainUVWithIEEG((SceneInformation.MeshTypeToDisplay == SceneStatesInfo.MeshType.Inflated), columnsIndexes[ii]))
                        return;

                // brain cuts
                if (cuts)
                    for (int jj = 0; jj < cutsIndexes.Length; ++jj)
                        m_Column3DViewManager.ColorCutsTexturesWithIEEG(columnsIndexes[ii], cutsIndexes[jj]);

                if (plots)
                {
                    currCol.UpdateSitesSizeAndColorForIEEG();
                    currCol.UpdateSitesRendering(SceneInformation, null);
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
            int[] cutsIndexes = allCuts ? Enumerable.Range(0, m_PlanesList.Count).ToArray() : new int[1] { indexCut };

            for (int ii = 0; ii < columnsIndexes.Length; ++ii)
            {
                //Column3DViewFMRI currCol = Column3DViewManager.FMRI_col(columnsIndexes[ii]);

                // brain cuts
                if (cuts)
                    for (int jj = 0; jj < cutsIndexes.Length; ++jj)
                        m_Column3DViewManager.ColorCutsTexturesWithFMRI(columnsIndexes[ii], cutsIndexes[jj]);
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
            int[] cutsIndexes = allCuts ? Enumerable.Range(0, m_PlanesList.Count).ToArray() : new int[1] { indexCut };

            for (int ii = 0; ii < columnsIndexes.Length; ++ii)
            {
                Column3DView currCol = m_Column3DViewManager.Columns[columnsIndexes[ii]];

                for (int jj = 0; jj < PlanesList.Count; ++jj)
                {
                    switch (currCol.Type)
                    {
                        case Column3DView.ColumnType.FMRI:
                            m_Column3DViewManager.CreateGUIFMRITexture(cutsIndexes[jj], columnsIndexes[ii]);
                            break;
                        case Column3DView.ColumnType.IEEG:
                            if (!SceneInformation.IsGeneratorUpToDate)
                                m_Column3DViewManager.CreateGUIMRITexture(cutsIndexes[jj], columnsIndexes[ii]);
                            else
                                m_Column3DViewManager.CreateGUIIEEGTexture(cutsIndexes[jj], columnsIndexes[ii]);
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
                    texturesToDisplay = ((Column3DViewFMRI)currCol).GUIBrainCutWithFMRITextures;
                    break;
                case Column3DView.ColumnType.IEEG:
                    if (!SceneInformation.IsGeneratorUpToDate)
                        texturesToDisplay = currCol.GUIBrainCutTextures;
                    else
                        texturesToDisplay = ((Column3DViewIEEG)currCol).GUIBrainCutWithIEEGTextures;
                    break;
                default:
                    break;
            }
            UpdateCutsInUI.Invoke(texturesToDisplay, m_Column3DViewManager.SelectedColumnID, m_PlanesList.Count);
        }
        /// <summary>
        /// 
        /// </summary>
        private void FinalizeGeneratorsComputing()
        {
            // computing ended
            m_ComputeGeneratorsJob = null;

            // generators are now up to date
            SceneInformation.IsGeneratorUpToDate = true;
            SceneInformation.IsiEEGOutdated = false;

            // send inf values to overlays
            for (int ii = 0; ii < m_Column3DViewManager.ColumnsIEEG.Count; ++ii)
            {
                float maxValue = Math.Max(Math.Abs(m_Column3DViewManager.ColumnsIEEG[ii].SharedMinInf), Math.Abs(m_Column3DViewManager.ColumnsIEEG[ii].SharedMaxInf));
                float minValue = -maxValue;
                minValue += m_Column3DViewManager.ColumnsIEEG[ii].Middle;
                maxValue += m_Column3DViewManager.ColumnsIEEG[ii].Middle;
                SendColorMapValues.Invoke(minValue, m_Column3DViewManager.ColumnsIEEG[ii].Middle, maxValue, ii);
            }

            // amplitudes are not displayed yet
            for (int ii = 0; ii < m_Column3DViewManager.ColumnsIEEG.Count; ++ii)
                m_Column3DViewManager.ColumnsIEEG[ii].UpdateIEEG = true;

            //####### CHECK ACCESS
            if (!m_ModesManager.FunctionAccess(Mode.FunctionsId.PostUpdateGenerators))
            {
                Debug.LogError("-ERROR : Base3DScene::post_updateGenerators -> no acess for mode : " + m_ModesManager.CurrentModeName);
            }
            //##################

            // update plots visibility
            m_Column3DViewManager.UpdateAllColumnsSitesRendering(SceneInformation);

            DisplayScreenMessage("Computing finished !", 1f, 250, 40);
            DisplayProgressbar(1f, 1f, 250, 40);

            //####### UDPATE MODE
            m_ModesManager.UpdateMode(Mode.FunctionsId.PostUpdateGenerators);
            //##################
        }
        /// <summary>
        /// Init gameobjects of the scene
        /// </summary>
        protected void InitializeSceneGameObjects()
        {
            // init parents 
            m_DisplayedObjects.SitesMeshesParent = transform.Find("base electrodes").gameObject;
            m_DisplayedObjects.BrainSurfaceMeshesParent = transform.Find("meshes").Find("brains").gameObject;
            m_DisplayedObjects.BrainCutMeshesParent = transform.Find("meshes").Find("cuts").gameObject;

            // init lights
            m_DisplayedObjects.SharedDirectionalLight = transform.parent.Find("directionnal light").gameObject;

            // init default planes
            m_PlanesList = new List<Plane>();
            m_Column3DViewManager.PlanesOrientationID = new List<int>();
            m_Column3DViewManager.PlanesOrientationFlip = new List<bool>();
            SceneInformation.RemoveFrontPlaneList = new List<int>();
            SceneInformation.NumberOfCutsPerPlane = new List<int>();
            m_DisplayedObjects.BrainCutMeshes = new List<GameObject>();

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
            m_DisplaySitesNames = true;
            m_LastCamera = camera;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="updateColors"></param>
        public void UpdateColormap(ColorType color, bool updateColors = true)
        {
            Column3DViewManager.UpdateColormap(color);
            if (updateColors)
                Column3DViewManager.ResetColors();

            SharedMaterials.Brain.BrainMaterials[this].SetTexture("_ColorTex", Column3DViewManager.BrainColorMapTexture);

            if (SceneInformation.IsGeometryUpToDate && !SceneInformation.IsiEEGOutdated)
                ComputeIEEGTextures();
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        public void UpdateBrainSurfaceColor(ColorType color)
        {
            Column3DViewManager.BrainColor = color;
            DLL.Texture tex = DLL.Texture.Generate1DColorTexture(Column3DViewManager.BrainColor);
            tex.UpdateTexture2D(Column3DViewManager.BrainColorTexture);

            SharedMaterials.Brain.BrainMaterials[this].SetTexture("_MainTex", Column3DViewManager.BrainColorTexture);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        /// <param name="updateColors"></param>
        public void UpdateBrainCutColor(ColorType color, bool updateColors = true)
        {
            Column3DViewManager.UpdateBrainCutColor(color);
            if (updateColors)
                Column3DViewManager.ResetColors();

            SceneInformation.CutMeshGeometryNeedsUpdate = true;
            SceneInformation.IsiEEGOutdated = true;

            //####### UDPATE MODE
            m_ModesManager.UpdateMode(Mode.FunctionsId.UpdatePlane); // TEMP
            //##################
        }
        /// <summary>
        /// Reset the gameobjects of the scene
        /// </summary>
        public void ResetSceneGameObjects()
        {
            // destroy meshes
            for (int ii = 0; ii < m_DisplayedObjects.BrainSurfaceMeshes.Count; ++ii)
            {
                Destroy(m_DisplayedObjects.BrainSurfaceMeshes[ii]);
            }
            for (int ii = 0; ii < m_DisplayedObjects.BrainCutMeshes.Count; ++ii)
            {
                m_DisplayedObjects.BrainCutMeshes[ii].SetActive(false);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="nbSplits"></param>
        public void ResetSplitsNumber(int nbSplits)
        {
            if (m_Column3DViewManager.MeshSplitNumber == nbSplits)
                return;

            m_Column3DViewManager.MeshSplitNumber = nbSplits;

            if(m_DisplayedObjects.BrainSurfaceMeshes.Count > 0)
                for (int ii = 0; ii < m_DisplayedObjects.BrainSurfaceMeshes.Count; ++ii)
                    Destroy(m_DisplayedObjects.BrainSurfaceMeshes[ii]);

            // reset meshes
            m_DisplayedObjects.BrainSurfaceMeshes = new List<GameObject>(m_Column3DViewManager.MeshSplitNumber);
            for (int ii = 0; ii < m_Column3DViewManager.MeshSplitNumber; ++ii)
            {
                m_DisplayedObjects.BrainSurfaceMeshes.Add(Instantiate(GlobalGOPreloaded.Brain));
                m_DisplayedObjects.BrainSurfaceMeshes[ii].GetComponent<Renderer>().sharedMaterial = SharedMaterials.Brain.BrainMaterials[this];
                m_DisplayedObjects.BrainSurfaceMeshes[ii].name = "brain_" + ii;
                m_DisplayedObjects.BrainSurfaceMeshes[ii].transform.parent = m_DisplayedObjects.BrainSurfaceMeshesParent.transform;
                m_DisplayedObjects.BrainSurfaceMeshes[ii].layer = LayerMask.NameToLayer(SceneInformation.MeshesLayerName);
                m_DisplayedObjects.BrainSurfaceMeshes[ii].AddComponent<MeshCollider>();
                m_DisplayedObjects.BrainSurfaceMeshes[ii].SetActive(true);
            }

            m_Column3DViewManager.ResetSplitsNumber(nbSplits);
        }
        /// <summary>
        /// Set UI screen space/overlays layers mask settings corresponding to the current mode of the scene
        /// </summary>
        public void SetCurrentModeSpecifications(bool force = false)
        {
            m_ModesManager.SetCurrentModeSpecifications(force);
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
            if (!m_ModesManager.FunctionAccess(Mode.FunctionsId.UpdateMaskPlot))
            {
                Debug.LogError("-ERROR : Base3DScene::updateMaskPlot -> no acess for mode : " + m_ModesManager.CurrentModeName);
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
                                if (GlobalGOPreloaded.MarsAtlasIndex.FullName(m_Column3DViewManager.Columns[ii].Sites[jj].Information.MarsAtlasIndex).Contains(nameFilter))
                                    colIdPlots[ii].Add(jj);      
                            }
                        }
                        break;
                    case SiteFilter.Broadman: // broadman filter
                        {
                            for (int jj = 0; jj < m_Column3DViewManager.Columns[ii].Sites.Count; ++jj)
                            {
                                if (GlobalGOPreloaded.MarsAtlasIndex.BroadmanArea(m_Column3DViewManager.Columns[ii].Sites[jj].Information.MarsAtlasIndex).Contains(nameFilter))
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

            m_Column3DViewManager.UpdateAllColumnsSitesRendering(SceneInformation);

            //####### UDPATE MODE
            m_ModesManager.UpdateMode(Mode.FunctionsId.UpdateMaskPlot);
            //##################
        }
        /// <summary>
        /// Set the mesh part to be displayed in the scene
        /// </summary>
        /// <param name="meshPartToDisplay"></param>
        public void UpdateMeshPartToDisplay(SceneStatesInfo.MeshPart meshPartToDisplay)
        {
            //####### CHECK ACESS
            if (!m_ModesManager.FunctionAccess(Mode.FunctionsId.SetDisplayedMesh))
            {
                Debug.LogError("-ERROR : Base3DScene::setDisplayedMesh -> no acess for mode : " + m_ModesManager.CurrentModeName);
                return;
            }
            //##################

            if (!SceneInformation.IsGeometryUpToDate)
                return;

            SceneInformation.MeshPartToDisplay = meshPartToDisplay;
            SceneInformation.CutMeshGeometryNeedsUpdate = true;
            SceneInformation.IsiEEGOutdated = true;
            m_Column3DViewManager.UpdateAllColumnsSitesRendering(SceneInformation);

            //####### UDPATE MODE
            m_ModesManager.UpdateMode(Mode.FunctionsId.SetDisplayedMesh);
            //##################
        }
        /// <summary>
        /// Set the mesh type to be displayed in the scene
        /// </summary>
        /// <param name="meshTypeToDisplay"></param>
        public void UpdateMeshTypeToDisplay(SceneStatesInfo.MeshType meshTypeToDisplay)
        {
            //####### CHECK ACESS
            if (!m_ModesManager.FunctionAccess(Mode.FunctionsId.SetDisplayedMesh))
            {
                Debug.LogError("-ERROR : Base3DScene::setDisplayedMesh -> no acess for mode : " + m_ModesManager.CurrentModeName);
                return;
            }
            //##################

            if (!SceneInformation.IsGeometryUpToDate)
                return;

            switch(meshTypeToDisplay)
            {
                case SceneStatesInfo.MeshType.Hemi:
                    if (!SceneInformation.HemiMeshesAvailables)
                        return;
                    break;
                case SceneStatesInfo.MeshType.White:
                    if (!SceneInformation.WhiteMeshesAvailables)
                        return;
                    break;
                case SceneStatesInfo.MeshType.Inflated:
                    if (!SceneInformation.WhiteInflatedMeshesAvailables)
                        return;
                    break;
            }

            SceneInformation.MeshTypeToDisplay = meshTypeToDisplay;
            SceneInformation.CutMeshGeometryNeedsUpdate = true;
            SceneInformation.IsiEEGOutdated = true;
            m_Column3DViewManager.UpdateAllColumnsSitesRendering(SceneInformation);

            //####### UDPATE MODE
            m_ModesManager.UpdateMode(Mode.FunctionsId.SetDisplayedMesh);
            //##################
        }
        /// <summary>
        /// Add a new cut plane
        /// </summary>
        public void AddCutPlane()
        {
            //####### CHECK ACESS
            if (!m_ModesManager.FunctionAccess(Mode.FunctionsId.AddNewPlane))
            {
                Debug.LogError("-ERROR : Base3DScene::addNewPlane -> no acess for mode : " + m_ModesManager.CurrentModeName);
                return;
            }
            //##################

            m_PlanesList.Add(new Plane(new Vector3(0, 0, 0), new Vector3(1, 0, 0)));
            m_Column3DViewManager.PlanesOrientationID.Add(0);
            m_Column3DViewManager.PlanesOrientationFlip.Add(false);
            SceneInformation.RemoveFrontPlaneList.Add(0);
            SceneInformation.NumberOfCutsPerPlane.Add(SceneInformation.DefaultNumberOfCutsPerPlane);

            GameObject cut = Instantiate(GlobalGOPreloaded.Cut);
            cut.GetComponent<Renderer>().sharedMaterial = SharedMaterials.Brain.CutMaterials[this];
            cut.name = "cut_" + (m_PlanesList.Count - 1);
            cut.transform.parent = m_DisplayedObjects.BrainCutMeshesParent.transform; ;
            cut.AddComponent<MeshCollider>();
            cut.layer = LayerMask.NameToLayer(SceneInformation.MeshesLayerName);
            m_DisplayedObjects.BrainCutMeshes.Add(cut);
            m_DisplayedObjects.BrainCutMeshes[m_DisplayedObjects.BrainCutMeshes.Count - 1].layer = LayerMask.NameToLayer(SceneInformation.MeshesLayerName);

            // update columns manager
            m_Column3DViewManager.UpdateCutNumber(m_DisplayedObjects.BrainCutMeshes.Count);

            // update plots visibility
            m_Column3DViewManager.UpdateAllColumnsSitesRendering(SceneInformation);

            SceneInformation.CutMeshGeometryNeedsUpdate = true;
            SceneInformation.IsiEEGOutdated = true;

            //####### UDPATE MODE
            m_ModesManager.UpdateMode(Mode.FunctionsId.AddNewPlane);
            //##################            
        }
        /// <summary>
        /// Remove the last cut plane
        /// </summary>
        public void RemoveLastCutPlane() // TODO : specify which cut plane
        {
            //####### CHECK ACESS
            if (!m_ModesManager.FunctionAccess(Mode.FunctionsId.RemoveLastPlane))
            {
                Debug.LogError("-ERROR : Base3DScene::removeLastPlane -> no acess for mode : " + m_ModesManager.CurrentModeName);
                return;
            }
            //##################

            m_PlanesList.RemoveAt(m_PlanesList.Count - 1);
            m_Column3DViewManager.PlanesOrientationID.RemoveAt(m_Column3DViewManager.PlanesOrientationID.Count - 1);
            m_Column3DViewManager.PlanesOrientationFlip.RemoveAt(m_Column3DViewManager.PlanesOrientationFlip.Count - 1);
            SceneInformation.RemoveFrontPlaneList.RemoveAt(SceneInformation.RemoveFrontPlaneList.Count - 1);
            SceneInformation.NumberOfCutsPerPlane.RemoveAt(SceneInformation.NumberOfCutsPerPlane.Count - 1);

            Destroy(m_DisplayedObjects.BrainCutMeshes[m_DisplayedObjects.BrainCutMeshes.Count - 1]);
            m_DisplayedObjects.BrainCutMeshes.RemoveAt(m_DisplayedObjects.BrainCutMeshes.Count - 1);

            // update columns manager
            m_Column3DViewManager.UpdateCutNumber(m_DisplayedObjects.BrainCutMeshes.Count);

            // update plots visibility
            m_Column3DViewManager.UpdateAllColumnsSitesRendering(SceneInformation);

            // update cut images display with the new selected column
            //UpdateCutsInUI.Invoke(m_CM.getBrainCutTextureList(m_CM.idSelectedColumn, true, data_.generatorUpToDate, data_.displayLatenciesMode), m_CM.idSelectedColumn, m_CM.planesList.Count);

            SceneInformation.CutMeshGeometryNeedsUpdate = true;
            SceneInformation.IsiEEGOutdated = true;

            //####### UDPATE MODE
            m_ModesManager.UpdateMode(Mode.FunctionsId.RemoveLastPlane);
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
            if (!m_ModesManager.FunctionAccess(Mode.FunctionsId.UpdatePlane))
            {
                Debug.LogError("-ERROR : Base3DScene::updatePlane -> no acess for mode : " + m_ModesManager.CurrentModeName);
                return;
            }
            //##################

            Plane newPlane = new Plane(new Vector3(0, 0, 0), new Vector3(1, 0, 0));
            if (idOrientation == 3 || !SceneInformation.MRILoaded) // custom normal
            {
                if (customNormal.x != 0 || customNormal.y != 0 || customNormal.z != 0)
                    newPlane.Normal = customNormal;
                else
                    newPlane.Normal = new Vector3(1, 0, 0);
            }
            else
            {
                m_Column3DViewManager.DLLVolume.SetPlaneWithOrientation(newPlane, idOrientation, flip);
            }

            m_PlanesList[idPlane].Normal = newPlane.Normal;
            m_Column3DViewManager.PlanesOrientationID[idPlane] = idOrientation;
            m_Column3DViewManager.PlanesOrientationFlip[idPlane] = flip;
            SceneInformation.RemoveFrontPlaneList[idPlane] = removeFrontPlane?1:0;
            SceneInformation.LastPlaneModifiedID = idPlane;

            // ########### cuts base on the mesh
            float offset;
            if (SceneInformation.MeshToDisplay != null)
            {
                offset = SceneInformation.MeshToDisplay.SizeOffsetCutPlane(m_PlanesList[idPlane], SceneInformation.NumberOfCutsPerPlane[idPlane]);
                offset *= 1.05f; // upsize a little bit the bbox for planes
            }
            else
                offset = 0.1f;

            m_PlanesList[idPlane].Point = SceneInformation.MeshCenter + m_PlanesList[idPlane].Normal * (position - 0.5f) * offset * SceneInformation.NumberOfCutsPerPlane[idPlane];

            SceneInformation.CutMeshGeometryNeedsUpdate = true;
            SceneInformation.IsiEEGOutdated = true;

            // update sites visibility
            m_Column3DViewManager.UpdateAllColumnsSitesRendering(SceneInformation);

            // update cameras cuts display
            ModifyPlanesCuts.Invoke();

            //####### UDPATE MODE
            m_ModesManager.UpdateMode(Mode.FunctionsId.UpdatePlane);
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
            if (!m_ModesManager.FunctionAccess(Mode.FunctionsId.ResetNIIBrainVolumeFile))
            {
                Debug.LogError("-ERROR : Base3DScene::resetNIIBrainVolumeFile -> no acess for mode : " + m_ModesManager.CurrentModeName);
                return false;
            }

            SceneInformation.MRILoaded = false;

            // checks parameter
            if (pathNIIBrainVolumeFile.Length == 0)
            {
                Debug.LogError("-ERROR : Base3DScene::resetNIIBrainVolumeFile -> path NII brain volume file is empty. ");
                return (SceneInformation.MeshesLoaded = false);
            }

            // load volume
            bool loadingSuccess = m_Column3DViewManager.DLLNii.LoadNIIFile(pathNIIBrainVolumeFile);
            if (loadingSuccess)
            {
                m_Column3DViewManager.DLLNii.ConvertToVolume(m_Column3DViewManager.DLLVolume);
                SceneInformation.VolumeCenter = m_Column3DViewManager.DLLVolume.Center();
            }
            else
            {
                Debug.LogError("-ERROR : Base3DScene::resetNIIBrainVolumeFile -> load NII file failed. " + pathNIIBrainVolumeFile);
                return SceneInformation.MRILoaded;
            }

            SceneInformation.MRILoaded = loadingSuccess;
            UpdatePlanes.Invoke();

            // send cal values to the UI
            IRMCalValuesUpdate.Invoke(m_Column3DViewManager.DLLVolume.RetrieveExtremeValues());

            //####### UDPATE MODE
            m_ModesManager.UpdateMode(Mode.FunctionsId.ResetNIIBrainVolumeFile);
            //##################

            return SceneInformation.MRILoaded;
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
            m_ModesManager.SetCurrentModeSpecifications(true);

            ComputeGUITextures(-1, m_Column3DViewManager.SelectedColumnID);
            UpdateGUITextures();
        }
        /// <summary>
        /// Update the data render corresponding to the column
        /// </summary>
        /// <param name="indexColumn"></param>
        /// <returns></returns>
        public bool UpdateFocusedColumnRendering()
        {
            if (!SceneInformation.IsGeometryUpToDate)
                return false;
        
            UnityEngine.Profiling.Profiler.BeginSample("TEST-updateColumnRender");

            Column3DView currCol = m_Column3DViewManager.FocusedColumn;
            // TODO : un mesh pour chaque column

            // update cuts textures
            if ((SceneInformation.MeshTypeToDisplay != SceneStatesInfo.MeshType.Inflated))
            {
                for (int ii = 0; ii < PlanesList.Count; ++ii)
                {
                    switch (currCol.Type)
                    {
                        case Column3DView.ColumnType.FMRI:
                            m_DisplayedObjects.BrainCutMeshes[ii].GetComponent<Renderer>().material.mainTexture = ((Column3DViewFMRI)currCol).BrainCutWithFMRITextures[ii];
                            break;
                        case Column3DView.ColumnType.IEEG:
                            if (!SceneInformation.IsGeneratorUpToDate)
                                m_DisplayedObjects.BrainCutMeshes[ii].GetComponent<Renderer>().material.mainTexture = currCol.BrainCutTextures[ii];
                            else
                                m_DisplayedObjects.BrainCutMeshes[ii].GetComponent<Renderer>().material.mainTexture = ((Column3DViewIEEG)currCol).BrainCutWithIEEGTextures[ii];
                            break;
                        default:
                            break;
                    }
                }
            }

            // update meshes splits UV
            for (int ii = 0; ii < m_Column3DViewManager.MeshSplitNumber; ++ii)
            {
                // uv 1 (main)
                //go_.brainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh.uv = m_CM.UVCoordinatesSplits[ii];

                if (currCol.Type == Column3DView.ColumnType.FMRI || !SceneInformation.IsGeneratorUpToDate || SceneInformation.DisplayCCEPMode)
                {
                    // uv 2 (alpha) 
                    m_DisplayedObjects.BrainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh.uv2 = m_Column3DViewManager.UVNull[ii];
                    // uv 3 (color map)
                    m_DisplayedObjects.BrainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh.uv3 = m_Column3DViewManager.UVNull[ii];
                }
                else
                {
                    // uv 2 (alpha) 
                    m_DisplayedObjects.BrainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh.uv2 = ((Column3DViewIEEG)currCol).DLLBrainTextureGenerators[ii].AlphaUV;
                    // uv 3 (color map)
                    m_DisplayedObjects.BrainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh.uv3 = ((Column3DViewIEEG)currCol).DLLBrainTextureGenerators[ii].IEEGUV;
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
            if (!SceneInformation.MeshesLoaded || !SceneInformation.MRILoaded)
                return;

            // update splits colliders
            for(int ii = 0; ii < m_DisplayedObjects.BrainSurfaceMeshes.Count; ++ii)
            {
                m_DisplayedObjects.BrainSurfaceMeshes[ii].GetComponent<MeshCollider>().sharedMesh = null;
                m_DisplayedObjects.BrainSurfaceMeshes[ii].GetComponent<MeshCollider>().sharedMesh = m_DisplayedObjects.BrainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh;
            }

            // update cuts colliders
            for (int ii = 0; ii < m_DisplayedObjects.BrainCutMeshes.Count; ++ii)
            {
                m_DisplayedObjects.BrainCutMeshes[ii].GetComponent<MeshCollider>().sharedMesh = null;
                m_DisplayedObjects.BrainCutMeshes[ii].GetComponent<MeshCollider>().sharedMesh = m_DisplayedObjects.BrainCutMeshes[ii].GetComponent<MeshFilter>().mesh;
            }

            SceneInformation.CollidersUpdated = true;
        }
        /// <summary>
        /// Update the textures generator
        /// </summary>
        public void UpdateGenerators()
        {
            //####### CHECK ACESS
            if (!m_ModesManager.FunctionAccess(Mode.FunctionsId.PreUpdateGenerators))
            {
                Debug.LogError("-ERROR : Base3DScene::pre_updateGenerators -> no acess for mode : " + m_ModesManager.CurrentModeName);
                return;
            }
            //##################

            if (SceneInformation.CutMeshGeometryNeedsUpdate || !SceneInformation.IsGeometryUpToDate) // if update cut plane is pending, cancel action
                return;

            //####### UDPATE MODE
            m_ModesManager.UpdateMode(Mode.FunctionsId.PreUpdateGenerators);
            //##################

            SceneInformation.IsGeneratorUpToDate = false;

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
            m_Column3DViewManager.GlobalTimeline = globalTimeline;
            if (m_Column3DViewManager.GlobalTimeline)
            {
                m_Column3DViewManager.CommonTimelineValue = value;
                for (int ii = 0; ii < m_Column3DViewManager.ColumnsIEEG.Count; ++ii)
                    m_Column3DViewManager.ColumnsIEEG[ii].CurrentTimeLineID = (int)m_Column3DViewManager.CommonTimelineValue;
            }
            else
            {
                Column3DViewIEEG currIEEGCol = (Column3DViewIEEG)m_Column3DViewManager.Columns[id];
                currIEEGCol.ColumnTimeLineID = (int)value;
                currIEEGCol.CurrentTimeLineID = currIEEGCol.ColumnTimeLineID;
            }

            ComputeIEEGTextures();
            m_Column3DViewManager.UpdateAllColumnsSitesRendering(SceneInformation);
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
                m_Column3DViewManager.ColumnsIEEG[ii].CurrentTimeLineID = globalTimeline ? (int)m_Column3DViewManager.CommonTimelineValue : m_Column3DViewManager.ColumnsIEEG[ii].ColumnTimeLineID;
            }

            ComputeIEEGTextures();
            m_Column3DViewManager.UpdateAllColumnsSitesRendering(SceneInformation);
        }
        /// <summary>
        /// 
        /// </summary>
        public void SwitchMarsAtlasColor()
        {
            {                
                SceneInformation.MarsAtlasModeEnabled = !SceneInformation.MarsAtlasModeEnabled;
                m_DisplayedObjects.BrainSurfaceMeshes[0].GetComponent<Renderer>().sharedMaterial.SetInt("_MarsAtlas", SceneInformation.MarsAtlasModeEnabled ? 1 : 0);
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
            if (!SceneInformation.MeshesLoaded || !SceneInformation.MRILoaded)
                return;

            switch (keyCode)
            {
                // enable/disable holes in the cuts
                case KeyCode.H:
                    SceneInformation.CutHolesEnabled = !SceneInformation.CutHolesEnabled;
                    SceneInformation.CutMeshGeometryNeedsUpdate = true;
                    SceneInformation.IsiEEGOutdated = true;
                    m_ModesManager.UpdateMode(Mode.FunctionsId.UpdatePlane); // TEMP
                    break;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool IsTriangleErasingModeEnabled()
        {
            return m_TriEraser.IsEnabled;
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
            m_TriEraser.IsEnabled = enabled;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="updateGO"></param>
        public void ResetTriangleErasing(bool updateGO = true)
        {
            // destroy previous GO
            if (m_DisplayedObjects.InvisibleBrainSurfaceMeshes != null)
                for (int ii = 0; ii < m_DisplayedObjects.InvisibleBrainSurfaceMeshes.Count; ++ii)
                    Destroy(m_DisplayedObjects.InvisibleBrainSurfaceMeshes[ii]);

            // create new GO
            m_DisplayedObjects.InvisibleBrainSurfaceMeshes = new List<GameObject>(m_DisplayedObjects.BrainSurfaceMeshes.Count);
            for (int ii = 0; ii < m_DisplayedObjects.BrainSurfaceMeshes.Count; ++ii)
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
                invisibleBrainPart.SetActive(m_TriEraser.IsEnabled);
                m_DisplayedObjects.InvisibleBrainSurfaceMeshes.Add(invisibleBrainPart);
            }

            m_TriEraser.Reset(m_DisplayedObjects.InvisibleBrainSurfaceMeshes, m_Column3DViewManager.DLLCutsList[0], m_Column3DViewManager.DLLSplittedMeshesList);

            if(updateGO)
                for (int ii = 0; ii < m_Column3DViewManager.DLLSplittedMeshesList.Count; ++ii)
                    m_Column3DViewManager.DLLSplittedMeshesList[ii].UpdateMeshFromDLL(m_DisplayedObjects.BrainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh);
        }
        /// <summary>
        /// 
        /// </summary>
        public void CancelLastTriangleErasingAction()
        {
            m_TriEraser.CancelLastAction();
            for (int ii = 0; ii < m_Column3DViewManager.DLLSplittedMeshesList.Count; ++ii)
                m_Column3DViewManager.DLLSplittedMeshesList[ii].UpdateMeshFromDLL(m_DisplayedObjects.BrainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="mode"></param>
        public void SetTriangleErasingMode(TriEraser.Mode mode)
        {
            TriEraser.Mode previousMode = m_TriEraser.CurrentMode;
            m_TriEraser.CurrentMode = mode;

            if (mode == TriEraser.Mode.Expand || mode == TriEraser.Mode.Invert)
            {
                m_TriEraser.EraseTriangles(new Vector3(), new Vector3());
                for (int ii = 0; ii < m_Column3DViewManager.DLLSplittedMeshesList.Count; ++ii)
                    m_Column3DViewManager.DLLSplittedMeshesList[ii].UpdateMeshFromDLL(m_DisplayedObjects.BrainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh);
                m_TriEraser.CurrentMode = previousMode;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="degrees"></param>
        public void SetTriangleErasingZoneDegrees(float degrees)
        {
            m_TriEraser.Degrees = degrees;
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
            if (IEEGCol.Middle == value)
                return;
            IEEGCol.Middle = value;

            SceneInformation.IsGeneratorUpToDate = false;
            SceneInformation.IsiEEGOutdated = true;
            m_Column3DViewManager.UpdateAllColumnsSitesRendering(SceneInformation);

            //####### UDPATE MODE
            m_ModesManager.UpdateMode(Mode.FunctionsId.UpdateMiddle);
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
            if (IEEGCol.MaxDistanceElec == value)
                return;            
            IEEGCol.MaxDistanceElec = value;

            SceneInformation.IsGeneratorUpToDate = false;
            SceneInformation.IsiEEGOutdated = true;
            m_Column3DViewManager.UpdateAllColumnsSitesRendering(SceneInformation);

            //####### UDPATE MODE
            m_ModesManager.UpdateMode(Mode.FunctionsId.UpdateMiddle);
            //##################
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public void UpdateMRICalMin(float value)
        {
            m_Column3DViewManager.MRICalMinFactor = value;

            if (!SceneInformation.IsGeometryUpToDate)
                return;

            SceneInformation.IsGeneratorUpToDate = false;
            SceneInformation.IsiEEGOutdated = true;

            { //TEST
              // recompute UV
                for (int ii = 0; ii < m_Column3DViewManager.MeshSplitNumber; ++ii)
                    m_Column3DViewManager.DLLCommonBrainTextureGeneratorList[ii].ComputeUVMainWithVolume(m_Column3DViewManager.DLLSplittedMeshesList[ii], m_Column3DViewManager.DLLVolume, m_Column3DViewManager.MRICalMinFactor, m_Column3DViewManager.MRICalMaxFactor);

                // update brain mesh object mesh filter (TODO update only UV)
                for (int ii = 0; ii < m_Column3DViewManager.MeshSplitNumber; ++ii)
                    m_Column3DViewManager.DLLSplittedMeshesList[ii].UpdateMeshFromDLL(m_DisplayedObjects.BrainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh);
            }

            ComputeMRITextures();
            m_Column3DViewManager.UpdateAllColumnsSitesRendering(SceneInformation);

            //####### UDPATE MODE
            m_ModesManager.UpdateMode(Mode.FunctionsId.UpdateMiddle);
            //##################
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="value"></param>
        public void UpdateMRICalMax(float value)
        {
            m_Column3DViewManager.MRICalMaxFactor = value;

            if (!SceneInformation.IsGeometryUpToDate)
                return;

            SceneInformation.IsGeneratorUpToDate = false;
            SceneInformation.IsiEEGOutdated = true;

            { //TEST
              // recompute UV
                for (int ii = 0; ii < m_Column3DViewManager.MeshSplitNumber; ++ii)
                    m_Column3DViewManager.DLLCommonBrainTextureGeneratorList[ii].ComputeUVMainWithVolume(m_Column3DViewManager.DLLSplittedMeshesList[ii], m_Column3DViewManager.DLLVolume, m_Column3DViewManager.MRICalMinFactor, m_Column3DViewManager.MRICalMaxFactor);

                // update brain mesh object mesh filter (TODO update only UV)
                for (int ii = 0; ii < m_Column3DViewManager.MeshSplitNumber; ++ii)
                    m_Column3DViewManager.DLLSplittedMeshesList[ii].UpdateMeshFromDLL(m_DisplayedObjects.BrainSurfaceMeshes[ii].GetComponent<MeshFilter>().mesh);
            }


            ComputeMRITextures();
            m_Column3DViewManager.UpdateAllColumnsSitesRendering(SceneInformation);

            //####### UDPATE MODE
            m_ModesManager.UpdateMode(Mode.FunctionsId.UpdateMiddle);
            //##################
        }
        /// <summary>
        /// Update the cal min value of the input FMRI column
        /// </summary>
        /// <param name="value"></param>
        /// <param name="columnId"> id of the FMRI columns</param>
        public void UpdateFMRICalMin(float value, int columnId)
        {
            m_Column3DViewManager.ColumnsFMRI[columnId].CalMin = value;
            ComputeFMRITextures(-1, -1);
        }
        /// <summary>
        /// Update the cal max value of the input FMRI column
        /// </summary>
        /// <param name="value"></param>
        /// <param name="columnId"> id of the FMRI columns</param>
        public void UpdateFMRICalMax(float value, int columnId)
        {
            m_Column3DViewManager.ColumnsFMRI[columnId].CalMax = value;
            ComputeFMRITextures(-1, -1);
        }
        /// <summary>
        /// Update the alpha value of the input FMRI column
        /// </summary>
        /// <param name="value"></param>
        /// <param name="columnId"> id of the FMRI columns</param>
        public void UpdateFMRIAlpha(float value, int columnId)
        {
            m_Column3DViewManager.ColumnsFMRI[columnId].Alpha = value;
            ComputeFMRITextures(-1, -1);
        }
        /// <summary>
        /// Update the min alpha of a IEEG column
        /// </summary>
        /// <param name="value"></param>
        /// <param name="columnId"></param>
        public void UpdateIEEGMinAlpha(float value, int columnId)
        {
            m_Column3DViewManager.ColumnsIEEG[columnId].AlphaMin = value;

            if (SceneInformation.IsGeometryUpToDate && !SceneInformation.IsiEEGOutdated)
                ComputeIEEGTextures(-1, -1, true, true, false);
        }
        /// <summary>
        /// Update the max alpha of a IEEG  column
        /// </summary>
        /// <param name="value"></param>
        /// <param name="columnId"></param>
        public void UpdateIEEGMaxAlpha(float value, int columnId)
        {
            m_Column3DViewManager.ColumnsIEEG[columnId].AlphaMax = value;

            if (SceneInformation.IsGeometryUpToDate && !SceneInformation.IsiEEGOutdated)
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
            if (IEEGCol.GainBubbles == value)
                return;
            IEEGCol.GainBubbles = value;

            m_Column3DViewManager.UpdateAllColumnsSitesRendering(SceneInformation);
        }
        /// <summary>
        /// Update the span min of a IEEG column
        /// </summary>
        /// <param name="value"></param>
        /// <param name="columnId"></param>
        public void UpdateIEEGSpanMin(float value, int columnId)
        {
            Column3DViewIEEG IEEGCol = m_Column3DViewManager.ColumnsIEEG[columnId];
            if (IEEGCol.SpanMin == value)
                return;
            IEEGCol.SpanMin = value;

            if (!SceneInformation.IsGeometryUpToDate)
                return;

            SceneInformation.IsGeneratorUpToDate = false;
            SceneInformation.IsiEEGOutdated = true;
            UpdateGUITextures();
            m_Column3DViewManager.UpdateAllColumnsSitesRendering(SceneInformation);            

            //####### UDPATE MODE
            m_ModesManager.UpdateMode(Mode.FunctionsId.UpdateMiddle);
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
            if (IEEGCol.SpanMax == value)
                return;
            IEEGCol.SpanMax = value;

            if (!SceneInformation.IsGeometryUpToDate)
                return;

            SceneInformation.IsGeneratorUpToDate = false;
            SceneInformation.IsiEEGOutdated = true;
            UpdateGUITextures();
            m_Column3DViewManager.UpdateAllColumnsSitesRendering(SceneInformation);

            //####### UDPATE MODE
            m_ModesManager.UpdateMode(Mode.FunctionsId.UpdateMiddle);
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
            if (m_Column3DViewManager.DLLNii.LoadNIIFile(FMRIPath))
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
            if (!m_ModesManager.FunctionAccess(Mode.FunctionsId.AddFMRIColumn))
            {
                Debug.LogError("-ERROR : Base3DScene::add_FMRI_column -> no acess for mode : " + m_ModesManager.CurrentModeName);
                return false;
            }
            //##################

            // update columns number
            int newFMRIColNb = m_Column3DViewManager.ColumnsFMRI.Count + 1;
            m_Column3DViewManager.UpdateColumnsNumber(m_Column3DViewManager.ColumnsIEEG.Count, newFMRIColNb, m_PlanesList.Count);

            int idCol = newFMRIColNb - 1;
            m_Column3DViewManager.ColumnsFMRI[idCol].Label = IMRFLabel;

            // update plots visibility
            m_Column3DViewManager.UpdateAllColumnsSitesRendering(SceneInformation);

            // convert to volume            
            m_Column3DViewManager.DLLNii.ConvertToVolume(m_Column3DViewManager.DLLVolumeFMriList[idCol]);

            if (Type == SceneType.SinglePatient)
                AskROIUpdateEvent.Invoke(m_Column3DViewManager.ColumnsIEEG.Count + idCol);

            // send parameters to UI
            //IRMCalValues calValues = m_CM.DLLVolumeIRMFList[idCol].retrieveExtremeValues();

            FMriDataParameters FMRIParams = new FMriDataParameters();
            FMRIParams.calValues = m_Column3DViewManager.DLLVolumeFMriList[idCol].RetrieveExtremeValues();
            FMRIParams.columnId = idCol;
            FMRIParams.alpha  = m_Column3DViewManager.ColumnsFMRI[idCol].Alpha;
            FMRIParams.calMin = m_Column3DViewManager.ColumnsFMRI[idCol].CalMin;
            FMRIParams.calMax = m_Column3DViewManager.ColumnsFMRI[idCol].CalMax;
            FMRIParams.singlePatient = Type == SceneType.SinglePatient;

            m_Column3DViewManager.ColumnsFMRI[idCol].CalMin = FMRIParams.calValues.computedCalMin;
            m_Column3DViewManager.ColumnsFMRI[idCol].CalMax = FMRIParams.calValues.computedCalMax;            

            // update camera
            UpdateCameraTarget.Invoke(Type == SceneType.SinglePatient ?  m_Column3DViewManager.BothHemi.BoundingBox().Center() : m_MNIObjects.BothHemi.BoundingBox().Center());

            ComputeMRITextures(-1, -1);

            SendFMRIParameters.Invoke(FMRIParams);
            ComputeFMRITextures(-1, -1);


            //####### UDPATE MODE
            m_ModesManager.UpdateMode(Mode.FunctionsId.AddFMRIColumn);
            //##################

            return true;
        }
        /// <summary>
        /// Remove the last IRMF column
        /// </summary>
        public void RemoveLastFMRIColumn()
        {
            //####### CHECK ACESS
            if (!m_ModesManager.FunctionAccess(Mode.FunctionsId.RemoveLastFMRIColumn))
            {
                Debug.LogError("-ERROR : Base3DScene::remove_last_FMRI_column -> no acess for mode : " + m_ModesManager.CurrentModeName);
                return;
            }
            //##################
            
            // update columns number
            m_Column3DViewManager.UpdateColumnsNumber(m_Column3DViewManager.ColumnsIEEG.Count, m_Column3DViewManager.ColumnsFMRI.Count - 1, m_PlanesList.Count);

            // update plots visibility
            m_Column3DViewManager.UpdateAllColumnsSitesRendering(SceneInformation);

            ComputeMRITextures(-1, -1);
            ComputeFMRITextures(-1, -1);
            UpdateGUITextures();

            //####### UDPATE MODE
            m_ModesManager.UpdateMode(Mode.FunctionsId.RemoveLastFMRIColumn);
            //##################
        }
        /// <summary>
        /// Is the latency mode enabled ?
        /// </summary>
        /// <returns></returns>
        public bool IsLatencyModeEnabled()
        {
            return SceneInformation.DisplayCCEPMode;
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
                iEEGDataParams.minAmp       = m_Column3DViewManager.ColumnsIEEG[ii].MinAmp;
                iEEGDataParams.maxAmp       = m_Column3DViewManager.ColumnsIEEG[ii].MaxAmp;

                iEEGDataParams.spanMin      = m_Column3DViewManager.ColumnsIEEG[ii].SpanMin;
                iEEGDataParams.middle       = m_Column3DViewManager.ColumnsIEEG[ii].Middle;
                iEEGDataParams.spanMax      = m_Column3DViewManager.ColumnsIEEG[ii].SpanMax;

                iEEGDataParams.gain         = m_Column3DViewManager.ColumnsIEEG[ii].GainBubbles;
                iEEGDataParams.maxDistance  = m_Column3DViewManager.ColumnsIEEG[ii].MaxDistanceElec;
                iEEGDataParams.columnId     = ii;

                iEEGDataParams.alphaMin     = m_Column3DViewManager.ColumnsIEEG[ii].AlphaMin;
                iEEGDataParams.alphaMax     = m_Column3DViewManager.ColumnsIEEG[ii].AlphaMax; // useless

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
                FMRIDataParams.alpha = m_Column3DViewManager.ColumnsFMRI[ii].Alpha;
                FMRIDataParams.calMin = m_Column3DViewManager.ColumnsFMRI[ii].CalMin;
                FMRIDataParams.calMax = m_Column3DViewManager.ColumnsFMRI[ii].CalMax;
                FMRIDataParams.columnId = ii;

                FMRIDataParams.calValues = m_Column3DViewManager.DLLVolumeFMriList[ii].RetrieveExtremeValues(); 
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
            SceneInformation.IsComparingSites = true;
        }
        /// <summary>
        /// Unselect the site of the corresponding column
        /// </summary>
        /// <param name="columnId"></param>
        public void UnselectSite(int columnId)
        {
            m_Column3DViewManager.Columns[columnId].SelectedSiteID = -1; // unselect current site
            m_Column3DViewManager.UpdateAllColumnsSitesRendering(SceneInformation);
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

            data_.RWLock.AcquireWriterLock(1000);
            data_.CurrentComputingState = 0f;
            data_.RWLock.ReleaseWriterLock();

            // copy from main generators
            for (int ii = 0; ii < cm_.ColumnsIEEG.Count; ++ii)
            {
                for (int jj = 0; jj < cm_.MeshSplitNumber; ++jj)
                {
                    cm_.ColumnsIEEG[ii].DLLBrainTextureGenerators[jj] = (DLL.MRIBrainGenerator)cm_.DLLCommonBrainTextureGeneratorList[jj].Clone();
                }
            }

            float offsetState = 1f / (2 * cm_.ColumnsIEEG.Count);

            // Do your threaded task. DON'T use the Unity API here
            if (data_.MeshTypeToDisplay != SceneStatesInfo.MeshType.Inflated)
            {
                for (int ii = 0; ii < cm_.ColumnsIEEG.Count; ++ii)
                {
                    data_.RWLock.AcquireWriterLock(1000);
                    data_.CurrentComputingState += offsetState;
                    data_.RWLock.ReleaseWriterLock();

                    float currentMaxDensity, currentMinInfluence, currentMaxInfluence;
                    float maxDensity = 1;

                    cm_.ColumnsIEEG[ii].SharedMinInf = float.MaxValue;
                    cm_.ColumnsIEEG[ii].SharedMaxInf = float.MinValue;

                    // update raw electrodes
                    cm_.ColumnsIEEG[ii].UpdateDLLSitesMask();

                    // splits
                    for (int jj = 0; jj < cm_.MeshSplitNumber; ++jj)
                    {
                        cm_.ColumnsIEEG[ii].DLLBrainTextureGenerators[jj].InitializeOctree(cm_.ColumnsIEEG[ii].RawElectrodes);


                        if (!cm_.ColumnsIEEG[ii].DLLBrainTextureGenerators[jj].ComputeDistances(cm_.ColumnsIEEG[ii].MaxDistanceElec, true))
                        {
                            Debug.LogError("Abort computing"); // useless
                            return;
                        }

                        if (!cm_.ColumnsIEEG[ii].DLLBrainTextureGenerators[jj].ComputeInfluences(cm_.ColumnsIEEG[ii], useMultiCPU, addValues, ratioDistances))
                        {
                            Debug.LogError("Abort computing"); // useless
                            return;
                        }
                        currentMaxDensity = cm_.ColumnsIEEG[ii].DLLBrainTextureGenerators[jj].GetMaximumDensity();
                        currentMinInfluence = cm_.ColumnsIEEG[ii].DLLBrainTextureGenerators[jj].GetMinimumInfluence();
                        currentMaxInfluence = cm_.ColumnsIEEG[ii].DLLBrainTextureGenerators[jj].GetMaximumInfluence();

                        if (currentMaxDensity > maxDensity)
                            maxDensity = currentMaxDensity;

                        if (currentMinInfluence < cm_.ColumnsIEEG[ii].SharedMinInf)
                            cm_.ColumnsIEEG[ii].SharedMinInf = currentMinInfluence;

                        if (currentMaxInfluence > cm_.ColumnsIEEG[ii].SharedMaxInf)
                            cm_.ColumnsIEEG[ii].SharedMaxInf = currentMaxInfluence;

                    }

                    data_.RWLock.AcquireWriterLock(1000);
                    data_.CurrentComputingState += offsetState;
                    data_.RWLock.ReleaseWriterLock();


                    // cuts
                    for (int jj = 0; jj < cm_.PlanesCutsCopy.Count; ++jj)
                    {
                        cm_.ColumnsIEEG[ii].DLLMRITextureCutGenerators[jj].InitializeOctree(cm_.ColumnsIEEG[ii].RawElectrodes);
                        cm_.ColumnsIEEG[ii].DLLMRITextureCutGenerators[jj].ComputeDistances(cm_.ColumnsIEEG[ii].MaxDistanceElec, true);

                        if (!cm_.ColumnsIEEG[ii].DLLMRITextureCutGenerators[jj].ComputeInfluences(cm_.ColumnsIEEG[ii], useMultiCPU, addValues, ratioDistances))
                        {
                            Debug.LogError("Abort computing");
                            return;
                        }

                        currentMaxDensity = cm_.ColumnsIEEG[ii].DLLMRITextureCutGenerators[jj].MaximumDensity();
                        currentMinInfluence = cm_.ColumnsIEEG[ii].DLLMRITextureCutGenerators[jj].MinimumInfluence();
                        currentMaxInfluence = cm_.ColumnsIEEG[ii].DLLMRITextureCutGenerators[jj].MaximumInfluence();

                        if (currentMaxDensity > maxDensity)
                            maxDensity = currentMaxDensity;

                        if (currentMinInfluence < cm_.ColumnsIEEG[ii].SharedMinInf)
                            cm_.ColumnsIEEG[ii].SharedMinInf = currentMinInfluence;

                        if (currentMaxInfluence > cm_.ColumnsIEEG[ii].SharedMaxInf)
                            cm_.ColumnsIEEG[ii].SharedMaxInf = currentMaxInfluence;
                    }

                    // synchronize max density
                    for (int jj = 0; jj < cm_.MeshSplitNumber; ++jj)
                        cm_.ColumnsIEEG[ii].DLLBrainTextureGenerators[jj].SynchronizeWithOthersGenerators(maxDensity, cm_.ColumnsIEEG[ii].SharedMinInf, cm_.ColumnsIEEG[ii].SharedMaxInf);
                    for (int jj = 0; jj < cm_.PlanesCutsCopy.Count; ++jj)
                        cm_.ColumnsIEEG[ii].DLLMRITextureCutGenerators[jj].SynchronizeWithOthersGenerators(maxDensity, cm_.ColumnsIEEG[ii].SharedMinInf, cm_.ColumnsIEEG[ii].SharedMaxInf);

                    for (int jj = 0; jj < cm_.MeshSplitNumber; ++jj)
                        cm_.ColumnsIEEG[ii].DLLBrainTextureGenerators[jj].AdjustInfluencesToColormap();
                    for (int jj = 0; jj < cm_.PlanesCutsCopy.Count; ++jj)
                        cm_.ColumnsIEEG[ii].DLLMRITextureCutGenerators[jj].AdjustInfluencesToColormap();
                }
            }
            else // if inflated white mesh is displayed, we compute only on the complete white mesh
            {
                for (int ii = 0; ii < cm_.ColumnsIEEG.Count; ++ii)
                {
                    data_.RWLock.AcquireWriterLock(1000);
                    data_.CurrentComputingState += offsetState;
                    data_.RWLock.ReleaseWriterLock();

                    float currentMaxDensity, currentMinInfluence, currentMaxInfluence;
                    float maxDensity = 1;

                    cm_.ColumnsIEEG[ii].SharedMinInf = float.MaxValue;
                    cm_.ColumnsIEEG[ii].SharedMaxInf = float.MinValue;

                    // update raw electrodes
                    cm_.ColumnsIEEG[ii].UpdateDLLSitesMask();

                    // splits
                    for (int jj = 0; jj < cm_.MeshSplitNumber; ++jj)
                    {
                        cm_.ColumnsIEEG[ii].DLLBrainTextureGenerators[jj].Reset(cm_.DLLSplittedWhiteMeshesList[jj], cm_.DLLVolume); // TODO : ?
                        cm_.ColumnsIEEG[ii].DLLBrainTextureGenerators[jj].InitializeOctree(cm_.ColumnsIEEG[ii].RawElectrodes);

                        if (!cm_.ColumnsIEEG[ii].DLLBrainTextureGenerators[jj].ComputeDistances(cm_.ColumnsIEEG[ii].MaxDistanceElec, true))
                        {
                            Debug.LogError("Abort computing");
                            return;
                        }

                        if (!cm_.ColumnsIEEG[ii].DLLBrainTextureGenerators[jj].ComputeInfluences(cm_.ColumnsIEEG[ii], useMultiCPU, addValues, ratioDistances))
                        {
                            Debug.LogError("Abort computing");
                            return;
                        }

                        currentMaxDensity = cm_.ColumnsIEEG[ii].DLLBrainTextureGenerators[jj].GetMaximumDensity();
                        currentMinInfluence = cm_.ColumnsIEEG[ii].DLLBrainTextureGenerators[jj].GetMinimumInfluence();
                        currentMaxInfluence = cm_.ColumnsIEEG[ii].DLLBrainTextureGenerators[jj].GetMaximumInfluence();

                        if (currentMaxDensity > maxDensity)
                            maxDensity = currentMaxDensity;

                        if (currentMinInfluence < cm_.ColumnsIEEG[ii].SharedMinInf)
                            cm_.ColumnsIEEG[ii].SharedMinInf = currentMinInfluence;

                        if (currentMaxInfluence > cm_.ColumnsIEEG[ii].SharedMaxInf)
                            cm_.ColumnsIEEG[ii].SharedMaxInf = currentMaxInfluence;
                    }

                    data_.RWLock.AcquireWriterLock(1000);
                    data_.CurrentComputingState += offsetState;
                    data_.RWLock.ReleaseWriterLock();


                    // synchronize max density
                    for (int jj = 0; jj < cm_.MeshSplitNumber; ++jj)
                        cm_.ColumnsIEEG[ii].DLLBrainTextureGenerators[jj].SynchronizeWithOthersGenerators(maxDensity, cm_.ColumnsIEEG[ii].SharedMinInf, cm_.ColumnsIEEG[ii].SharedMaxInf);

                    for (int jj = 0; jj < cm_.MeshSplitNumber; ++jj)
                        cm_.ColumnsIEEG[ii].DLLBrainTextureGenerators[jj].AdjustInfluencesToColormap();
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